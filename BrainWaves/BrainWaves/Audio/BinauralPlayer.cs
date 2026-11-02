using System;
using System.Diagnostics;
using System.Threading;
using NAudio.Wave;

namespace BrainWaves.Audio
{
    /// <summary>
    /// BinauralBeatProvider를 출력 장치에 연결해 소리를 낸다. 마지막으로 정한 주파수와 음량을 기억해 두었다가 재생할 때 쓴다.
    /// 재생은 소리를 키우며 시작하고, 정지는 소리를 줄인 뒤 출력 장치를 닫는다. UI 스레드에서만 쓴다.
    /// </summary>
    // INTENT: 출력 장치를 바로 멈추면 재생 중이던 버퍼가 잘려 "딱" 소리가 난다. 그래서 정지는 소리를 줄이라고만 알리고,
    // 장치는 소리가 다 줄어 스트림이 끝난 뒤 스스로 멈추면 닫는다. 멈춘 동안에도 무음을 흘려보내며 장치를 계속 돌리면
    // 다시 재생할 때 장치를 열 필요가 없지만, 소리가 없어도 재생 중인 스트림이 있으면 Windows가 오디오 사용 중으로 보고
    // 절전 진입을 막을 수 있어 그 방법은 쓰지 않는다.
    // WaveOutEvent는 자기를 만든 스레드의 SynchronizationContext로 PlaybackStopped를 보내므로,
    // UI 스레드에서 연 장치의 정리도 UI 스레드에서 돌아 잠금 없이 필드를 바꾼다.
    internal sealed class BinauralPlayer : IDisposable
    {
        // 출력 장치가 미리 채워 두는 소리의 길이(밀리초). 값을 바꾸면 그만큼 늦게 들린다.
        private const int OutputLatency = 100;

        private double _leftFrequency = 75.0;
        private double _rightFrequency = 73.0;
        private double _leftGain = 0.5;
        private double _rightGain = 0.5;
        private double _masterVolume = 0.5;
        private WaveOutEvent? _output;
        private BinauralBeatProvider? _provider;

        public void SetFrequencies(double left, double right)
        {
            _leftFrequency = left;
            _rightFrequency = right;
            _provider?.SetFrequencies(left, right);
        }

        /// <summary>채널 음량. 0에서 1 사이 값이다.</summary>
        public void SetGains(double left, double right)
        {
            _leftGain = Math.Clamp(left, 0, 1);
            _rightGain = Math.Clamp(right, 0, 1);
            ApplyLevels();
        }

        /// <summary>두 채널에 함께 곱하는 전체 음량. 0에서 1 사이 값이다.</summary>
        public void SetMasterVolume(double masterVolume)
        {
            _masterVolume = Math.Clamp(masterVolume, 0, 1);
            ApplyLevels();
        }

        public void Play()
        {
            // 소리를 줄이는 중이면 같은 장치에서 다시 키운다.
            if (_provider?.FadeIn() == true) return;

            // 스트림을 이미 끝낸 장치는 남은 버퍼를 다 들려준 뒤 스스로 멈추고 닫힌다.
            // 여기서 닫으면 아직 들리는 중인 페이드의 끝이 잘리므로 놓아 두고 새 장치를 연다.
            _output = null;
            _provider = null;

            var provider = new BinauralBeatProvider(
                _leftFrequency, _rightFrequency, _leftGain * _masterVolume, _rightGain * _masterVolume);
            var output = new WaveOutEvent { DesiredLatency = OutputLatency };
            output.PlaybackStopped += (_, _) => OnPlaybackStopped(output);
            try
            {
                output.Init(provider);
                output.Play();
            }
            catch
            {
                output.Dispose();
                throw;
            }

            _provider = provider;
            _output = output;
        }

        public void Stop() => _provider?.FadeOut();

        /// <summary>
        /// 앱을 끝낼 때 부른다. 소리가 다 줄어 출력 장치가 멈출 때까지 잠깐 기다린 뒤 장치를 닫는다.
        /// </summary>
        public void Dispose()
        {
            if (_output is { } output && _provider is { } provider)
            {
                provider.FadeOut();

                // 줄이는 시간에 장치가 미리 채워 둔 버퍼를 다 들려줄 시간을 더한 만큼만 기다린다.
                var limit = BinauralBeatProvider.FadeTime + TimeSpan.FromMilliseconds(OutputLatency * 3);
                var elapsed = Stopwatch.StartNew();
                while (output.PlaybackState != PlaybackState.Stopped && elapsed.Elapsed < limit)
                    Thread.Sleep(10);
            }

            _output?.Dispose();
            _output = null;
            _provider = null;
        }

        private void ApplyLevels() => _provider?.SetLevels(_leftGain * _masterVolume, _rightGain * _masterVolume);

        // 소리를 다 줄여 스트림이 끝났거나 장치에 문제가 생겨 멈춘 장치를 닫는다. 그 사이 새로 연 장치는 건드리지 않는다.
        private void OnPlaybackStopped(WaveOutEvent output)
        {
            if (ReferenceEquals(_output, output))
            {
                _output = null;
                _provider = null;
            }
            output.Dispose();
        }
    }
}
