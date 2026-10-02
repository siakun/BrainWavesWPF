using System;
using BrainWaves.Services;
using CommunityToolkit.Mvvm.Input;

namespace BrainWaves.ViewModel
{
    /// <summary>
    /// Waves 화면의 조작. 주파수를 0.01 Hz씩 옮기는 버튼과 재생 버튼을 제공한다.
    /// 소리 값은 AudioService가 들고 있고, 화면은 Audio에 바로 바인딩한다.
    /// </summary>
    // INTENT: 이 ViewModel은 탭을 옮길 때마다 새로 만들어지므로 값을 들고 있지 않는다. 값을 여기 두면 재생하지 않는 동안
    // 바꾼 주파수와 음량이 화면을 떠나는 순간 기본값으로 돌아간다.
    public sealed partial class WavesViewModel
    {
        private const double FrequencyStep = 0.01;

        public AudioService Audio { get; } = AudioService.Instance;

        [RelayCommand]
        private void IncreaseLeftFrequency() => Audio.LeftFrequency = Step(Audio.LeftFrequency, FrequencyStep);

        [RelayCommand]
        private void DecreaseLeftFrequency() => Audio.LeftFrequency = Step(Audio.LeftFrequency, -FrequencyStep);

        [RelayCommand]
        private void IncreaseRightFrequency() => Audio.RightFrequency = Step(Audio.RightFrequency, FrequencyStep);

        [RelayCommand]
        private void DecreaseRightFrequency() => Audio.RightFrequency = Step(Audio.RightFrequency, -FrequencyStep);

        [RelayCommand]
        private void TogglePlay()
        {
            if (Audio.IsPlaying) Audio.Stop();
            else Audio.Play();
        }

        // 0.01을 더하고 빼는 사이 생기는 부동소수점 오차가 쌓여 화면의 값과 소리의 값이 어긋나지 않게 반올림한다.
        private static double Step(double value, double delta) => Math.Round(value + delta, 2);
    }
}
