using System;

namespace BrainWaves.Audio
{
    /// <summary>
    /// 목표값을 향해 정해진 샘플 수에 걸쳐 일정한 폭으로 옮겨 가는 값. 소리를 만드는 스레드 하나에서만 쓴다.
    /// 옮겨 가는 도중에 목표가 바뀌면 지금 값에서 새 목표까지 다시 같은 샘플 수에 걸쳐 옮겨 간다.
    /// </summary>
    // INTENT: 음량이나 주파수를 샘플 한 개 사이에 바꾸면 파형이 그 자리에서 계단처럼 꺾여 "틱" 소리가 난다.
    // 슬라이더를 천천히 끌면 이 계단이 잇달아 생겨 "띠띠띠" 하는 잡음(zipper noise)이 된다.
    // 소리에 닿는 값은 모두 이 타입을 거쳐 샘플마다 조금씩 옮겨 가게 해, 값을 바꾸는 쪽이 따로 신경 쓰지 않아도 끊기지 않게 한다.
    // 목표까지 남은 거리에 비례해 다가가는 방식(지수 감쇠)은 목표에 끝내 닿지 않아 비트 주파수가 정확히 맞지 않으므로,
    // 정해진 샘플 수 안에 목표에 정확히 닿는 직선 이동을 쓴다.
    internal sealed class SmoothedValue
    {
        private readonly int _rampSamples;
        private double _target;
        private double _step;
        private int _remaining;

        public SmoothedValue(double initial, TimeSpan rampTime, int sampleRate)
        {
            Current = initial;
            _target = initial;
            _rampSamples = Math.Max(1, (int)Math.Round(rampTime.TotalSeconds * sampleRate));
        }

        public double Current { get; private set; }

        public bool IsSettled => _remaining == 0;

        public void SetTarget(double target)
        {
            if (target == _target) return;

            _target = target;
            _remaining = _rampSamples;
            _step = (target - Current) / _rampSamples;
        }

        /// <summary>
        /// 한 샘플만큼 옮겨 간 값을 돌려준다. 마지막 샘플에서는 계산 오차 없이 목표값이 된다.
        /// </summary>
        public double Next()
        {
            if (_remaining == 0) return Current;

            _remaining--;
            Current = _remaining == 0 ? _target : Current + _step;
            return Current;
        }
    }
}
