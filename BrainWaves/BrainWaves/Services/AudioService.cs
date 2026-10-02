using System;
using System.Diagnostics;
using BrainWaves.Model;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BrainWaves.Services
{
    /// <summary>
    /// 좌우 주파수, 채널 음량, 전체 음량과 재생 여부를 앱 전체에 하나만 두고 소리에 반영한다.
    /// 화면은 이 인스턴스에 바인딩한다. 값을 바꾸면 재생 중인 소리에 바로 적용되고, 멈춰 있으면 다음 재생에 쓰인다.
    /// </summary>
    // INTENT: 페이지와 그 ViewModel은 탭을 옮길 때마다 새로 만들어진다. 소리 값을 페이지마다 두고 메시지로 맞추면
    // 재생하지 않는 동안 바꾼 값이 페이지와 함께 사라지고, 사본마다 값이 달라 어느 값으로 재생할지가 경우에 따라 바뀐다.
    // 그래서 값의 원본을 여기 하나로 두고 화면은 읽고 쓰기만 한다. 업데이트 상태를 UpdatesViewModel 하나에 두는 것과 같은 원칙이다.
    // UI 스레드에서만 쓴다. 소리를 만드는 스레드와는 PlaySound가 잠금으로 값을 주고받는다.
    public sealed class AudioService : ObservableObject
    {
        /// <summary>채널 주파수의 하한(Hz). 슬라이더와 0.01 Hz 버튼이 같은 범위를 쓴다.</summary>
        public const double MinFrequency = 20;

        /// <summary>채널 주파수의 상한(Hz).</summary>
        public const double MaxFrequency = 500;

        /// <summary>채널 음량과 전체 음량의 최댓값. 음량은 0에서 이 값 사이의 백분율이다.</summary>
        public const double MaxLevel = 100;

        public static AudioService Instance { get; } = new();

        private readonly PlaySound _sound = new();

        private double _leftFrequency = 75;
        private double _rightFrequency = 73;
        private double _leftGain = 50;
        private double _rightGain = 50;
        private double _masterVolume = 50;
        private bool _isPlaying;

        private AudioService()
        {
            _sound.SetFrequencies(_leftFrequency, _rightFrequency);
            ApplyLevels();
        }

        public double LeftFrequency
        {
            get => _leftFrequency;
            set
            {
                if (SetProperty(ref _leftFrequency, ClampFrequency(value))) OnFrequencyChanged();
            }
        }

        public double RightFrequency
        {
            get => _rightFrequency;
            set
            {
                if (SetProperty(ref _rightFrequency, ClampFrequency(value))) OnFrequencyChanged();
            }
        }

        /// <summary>왼쪽 채널 음량(백분율).</summary>
        public double LeftGain
        {
            get => _leftGain;
            set
            {
                if (SetProperty(ref _leftGain, ClampLevel(value))) ApplyLevels();
            }
        }

        /// <summary>오른쪽 채널 음량(백분율).</summary>
        public double RightGain
        {
            get => _rightGain;
            set
            {
                if (SetProperty(ref _rightGain, ClampLevel(value))) ApplyLevels();
            }
        }

        /// <summary>두 채널에 함께 곱하는 전체 음량(백분율).</summary>
        public double MasterVolume
        {
            get => _masterVolume;
            set
            {
                if (SetProperty(ref _masterVolume, ClampLevel(value))) ApplyLevels();
            }
        }

        /// <summary>두 채널의 주파수 차이. 들리는 바이노럴 비트의 주파수다.</summary>
        public double Beat => Math.Abs(_leftFrequency - _rightFrequency);

        public BrainwaveBand Band => BrainwaveBand.FromBeat(Beat);

        public bool IsPlaying
        {
            get => _isPlaying;
            private set => SetProperty(ref _isPlaying, value);
        }

        public void Play()
        {
            if (IsPlaying) return;

            try
            {
                _sound.Play();
                IsPlaying = true;
            }
            catch (Exception ex)
            {
                // 출력 장치가 없거나 쓸 수 없으면 재생하지 않은 상태로 남긴다.
                Debug.WriteLine($"[AudioService] Failed to start playback: {ex.Message}");
                _sound.Stop();
            }
        }

        /// <summary>
        /// 두 채널의 주파수를 바꾸고 재생한다. 프리셋을 고를 때 쓰며, 채널 음량과 전체 음량은 그대로 둔다.
        /// 이미 재생 중이면 소리를 끊지 않고 주파수만 옮긴다.
        /// </summary>
        public void Play(double leftFrequency, double rightFrequency)
        {
            LeftFrequency = leftFrequency;
            RightFrequency = rightFrequency;
            Play();
        }

        public void Stop()
        {
            _sound.Stop();
            IsPlaying = false;
        }

        private void OnFrequencyChanged()
        {
            _sound.SetFrequencies(_leftFrequency, _rightFrequency);
            OnPropertyChanged(nameof(Beat));
            OnPropertyChanged(nameof(Band));
        }

        private void ApplyLevels()
        {
            _sound.SetGains(_leftGain / MaxLevel, _rightGain / MaxLevel);
            _sound.SetMasterVolume(_masterVolume / MaxLevel);
        }

        private static double ClampFrequency(double value) => Math.Clamp(value, MinFrequency, MaxFrequency);

        private static double ClampLevel(double value) => Math.Clamp(value, 0, MaxLevel);
    }
}
