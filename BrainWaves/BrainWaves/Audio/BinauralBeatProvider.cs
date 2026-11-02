using System;
using NAudio.Wave;

namespace BrainWaves.Audio
{
    /// <summary>
    /// 왼쪽과 오른쪽 채널에 서로 다른 주파수의 사인파를 만든다.
    /// 주파수, 채널 음량, 소리를 낼지 여부는 목표값으로만 받고, 소리에는 SmoothedValue로 샘플마다 조금씩 옮겨 반영한다.
    /// 만들자마자 소리를 키우기 시작하고, FadeOut으로 끝까지 줄이면 스트림을 끝낸다. 출력 장치는 남은 버퍼를 들려준 뒤 스스로 멈춘다.
    /// </summary>
    // INTENT: 목표값과 실제로 소리에 쓰는 값을 나눠 둔다. UI 스레드는 목표값만 잠금 안에서 바꾸고,
    // 소리를 만드는 스레드는 버퍼를 채우기 시작할 때 목표를 한 번 읽어 간다. 버퍼 전체를 잠근 채 채우면 슬라이더를 끄는 동안
    // UI 스레드가 그만큼 기다린다. 소리에 닿는 값은 모두 SmoothedValue를 거치므로, 값을 새로 더해도 같은 방식으로 끊기지 않는다.
    internal sealed class BinauralBeatProvider : ISampleProvider
    {
        public const int SampleRate = 44100;

        /// <summary>재생을 시작하고 멈출 때 소리를 키우고 줄이는 시간.</summary>
        public static readonly TimeSpan FadeTime = TimeSpan.FromMilliseconds(250);

        // 음량과 주파수를 새 값으로 옮기는 시간. 슬라이더를 따라가는 지연이 느껴지지 않을 만큼 짧고, 계단이 들리지 않을 만큼 길게 둔다.
        private static readonly TimeSpan LevelRamp = TimeSpan.FromMilliseconds(50);
        private static readonly TimeSpan FrequencyRamp = TimeSpan.FromMilliseconds(50);

        // 사인파의 최대 진폭. 출력 장치가 표현하는 최대치(1.0)에 닿지 않게 여유를 둔다.
        private const double Headroom = 0.8;

        private readonly object _lock = new();
        private double _leftFrequencyTarget;
        private double _rightFrequencyTarget;
        private double _leftLevelTarget;
        private double _rightLevelTarget;
        private bool _audible = true;
        private bool _finished;

        // 아래는 소리를 만드는 스레드만 쓴다.
        private readonly SmoothedValue _leftFrequency;
        private readonly SmoothedValue _rightFrequency;
        private readonly SmoothedValue _leftLevel;
        private readonly SmoothedValue _rightLevel;
        private readonly SmoothedValue _fade;
        private double _leftPhase;
        private double _rightPhase;

        /// <param name="leftLevel">왼쪽 채널 음량. 채널 음량과 전체 음량을 곱한 0에서 1 사이 값이다.</param>
        /// <param name="rightLevel">오른쪽 채널 음량.</param>
        public BinauralBeatProvider(double leftFrequency, double rightFrequency, double leftLevel, double rightLevel)
        {
            _leftFrequencyTarget = leftFrequency;
            _rightFrequencyTarget = rightFrequency;
            _leftLevelTarget = leftLevel;
            _rightLevelTarget = rightLevel;

            // 주파수와 음량은 그 값에서 바로 시작하고, 들리는 크기만 0에서 키운다.
            _leftFrequency = new SmoothedValue(leftFrequency, FrequencyRamp, SampleRate);
            _rightFrequency = new SmoothedValue(rightFrequency, FrequencyRamp, SampleRate);
            _leftLevel = new SmoothedValue(leftLevel, LevelRamp, SampleRate);
            _rightLevel = new SmoothedValue(rightLevel, LevelRamp, SampleRate);
            _fade = new SmoothedValue(0, FadeTime, SampleRate);
        }

        public WaveFormat WaveFormat { get; } = WaveFormat.CreateIeeeFloatWaveFormat(SampleRate, 2);

        public void SetFrequencies(double left, double right)
        {
            lock (_lock)
            {
                _leftFrequencyTarget = left;
                _rightFrequencyTarget = right;
            }
        }

        public void SetLevels(double left, double right)
        {
            lock (_lock)
            {
                _leftLevelTarget = left;
                _rightLevelTarget = right;
            }
        }

        /// <summary>
        /// 줄이던 소리를 다시 키운다. 이미 스트림을 끝냈으면 false를 돌려주며, 그때는 출력 장치를 새로 열어야 한다.
        /// </summary>
        public bool FadeIn()
        {
            lock (_lock)
            {
                if (_finished) return false;
                _audible = true;
                return true;
            }
        }

        /// <summary>
        /// 소리를 줄인다. 끝까지 줄면 스트림을 끝낸다.
        /// </summary>
        public void FadeOut()
        {
            lock (_lock)
            {
                _audible = false;
            }
        }

        public int Read(float[] buffer, int offset, int count)
        {
            bool audible;
            lock (_lock)
            {
                // 0을 돌려주면 출력 장치는 스트림이 끝난 것으로 보고, 이미 채운 버퍼를 다 들려준 뒤 멈춘다.
                if (_finished) return 0;

                _leftFrequency.SetTarget(_leftFrequencyTarget);
                _rightFrequency.SetTarget(_rightFrequencyTarget);
                _leftLevel.SetTarget(_leftLevelTarget);
                _rightLevel.SetTarget(_rightLevelTarget);
                audible = _audible;
            }
            _fade.SetTarget(audible ? 1 : 0);

            for (var i = 0; i + 1 < count; i += 2)
            {
                var fade = EaseInOut(_fade.Next());
                buffer[offset + i] = (float)(Headroom * fade * _leftLevel.Next() * Math.Sin(_leftPhase));
                buffer[offset + i + 1] = (float)(Headroom * fade * _rightLevel.Next() * Math.Sin(_rightPhase));

                _leftPhase = Advance(_leftPhase, _leftFrequency.Next());
                _rightPhase = Advance(_rightPhase, _rightFrequency.Next());
            }

            if (!audible && _fade.IsSettled)
            {
                lock (_lock)
                {
                    // 이 버퍼를 채우는 사이 다시 키우라는 요청이 왔으면 끝내지 않고 다음 버퍼에서 키운다.
                    if (!_audible) _finished = true;
                }
            }

            return count;
        }

        // 페이드의 시작과 끝에서 기울기가 0이 되도록 직선 진행을 코사인 곡선으로 바꾼다.
        // 직선 그대로 쓰면 페이드가 시작하고 끝나는 순간 음량의 변화율이 갑자기 바뀌어 작게 들린다.
        private static double EaseInOut(double progress) => 0.5 - 0.5 * Math.Cos(Math.PI * progress);

        // 위상을 이어서 더하므로 주파수가 바뀌어도 파형이 끊기지 않는다.
        private static double Advance(double phase, double frequency)
        {
            phase += 2 * Math.PI * frequency / SampleRate;
            return phase >= 2 * Math.PI ? phase - 2 * Math.PI : phase;
        }
    }
}
