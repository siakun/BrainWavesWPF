using System;
using BrainWaves.Model;
using BrainWaves.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

namespace BrainWaves.ViewModel
{
    public partial class WavesViewModel : ObservableObject
    {
        private readonly AudioService _audioService;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Resonance))]
        [NotifyPropertyChangedFor(nameof(Band))]
        private double leftFrequency = 75.0;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Resonance))]
        [NotifyPropertyChangedFor(nameof(Band))]
        private double rightFrequency = 73.0;

        [ObservableProperty]
        private double leftGain = 50.0;

        [ObservableProperty]
        private double rightGain = 50.0;

        [ObservableProperty]
        private double masterVolume = 50.0;

        [ObservableProperty]
        private string playButtonText = "Play";

        [ObservableProperty]
        private string playButtonIcon = "Play";

        [ObservableProperty]
        private bool isPlaying;

        public double Resonance => Math.Abs(LeftFrequency - RightFrequency);

        public BrainwaveBand Band => BrainwaveBand.FromBeat(Resonance);

        public WavesViewModel()
        {
            _audioService = AudioService.Instance;

            // 프리셋 선택 메시지 수신
            WeakReferenceMessenger.Default.Register<PresetSelectedMessage>(this, (r, m) =>
            {
                LeftFrequency = m.LeftFrequency;
                RightFrequency = m.RightFrequency;
                LeftGain = m.LeftGain;
                RightGain = m.RightGain;

                // 자동으로 재생 시작
                PlaySound();
            });

            // 재생 상태 변경 메시지 수신
            WeakReferenceMessenger.Default.Register<PlaybackStateChangedMessage>(this, (r, m) =>
            {
                UpdatePlayButtonState(m.IsPlaying);
            });

            // 오디오 파라미터 변경 메시지 수신  
            WeakReferenceMessenger.Default.Register<AudioParametersChangedMessage>(this, (r, m) =>
            {
                // 다른 곳에서 파라미터가 변경되면 UI 업데이트
                LeftFrequency = m.LeftFrequency;
                RightFrequency = m.RightFrequency;
                LeftGain = m.LeftGain;
                RightGain = m.RightGain;
                MasterVolume = m.MasterVolume;
            });

            // 초기 상태 설정
            UpdatePlayButtonState(_audioService.IsPlaying);

            // 현재 재생 중인 값으로 초기화
            if (_audioService.IsPlaying)
            {
                LeftFrequency = _audioService.CurrentLeftFrequency;
                RightFrequency = _audioService.CurrentRightFrequency;
                LeftGain = _audioService.CurrentLeftGain;
                RightGain = _audioService.CurrentRightGain;
                MasterVolume = _audioService.CurrentMasterVolume;
            }
        }

        partial void OnMasterVolumeChanged(double value)
        {
            if (_audioService.IsPlaying)
            {
                // 마스터 볼륨 변경 시 즉시 적용
                _audioService.SetMasterVolume(value / 100.0);
            }
        }

        [RelayCommand]
        private void IncreaseLeftFrequency()
        {
            if (LeftFrequency < 1000)
                LeftFrequency = Math.Round(LeftFrequency + 0.01, 2);
        }

        [RelayCommand]
        private void DecreaseLeftFrequency()
        {
            if (LeftFrequency > 0.01)
                LeftFrequency = Math.Round(LeftFrequency - 0.01, 2);
        }

        [RelayCommand]
        private void IncreaseRightFrequency()
        {
            if (RightFrequency < 1000)
                RightFrequency = Math.Round(RightFrequency + 0.01, 2);
        }

        [RelayCommand]
        private void DecreaseRightFrequency()
        {
            if (RightFrequency > 0.01)
                RightFrequency = Math.Round(RightFrequency - 0.01, 2);
        }

        [RelayCommand]
        private void TogglePlay()
        {
            if (_audioService.IsPlaying)
            {
                StopSound();
            }
            else
            {
                PlaySound();
            }
        }

        private void PlaySound()
        {
            _audioService.Play(LeftFrequency, RightFrequency, LeftGain / 100.0, RightGain / 100.0, MasterVolume / 100.0);
        }

        private void StopSound()
        {
            _audioService.Stop();
        }

        private void UpdatePlayButtonState(bool playing)
        {
            IsPlaying = playing;
            PlayButtonText = playing ? "Stop" : "Play";
            PlayButtonIcon = playing ? "Stop" : "Play";
        }
    }
}
