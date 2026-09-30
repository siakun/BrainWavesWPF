using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using BrainWaves.Model;
using BrainWaves.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

namespace BrainWaves.ViewModel
{
    public partial class PresetsViewModel : ObservableObject
    {
        private readonly MainViewModel _mainViewModel;
        private readonly AudioService _audioService;
        private PresetDataViewModel? _currentlyPlayingPreset;

        [ObservableProperty]
        private ObservableCollection<PresetDataViewModel> presetList;

        [ObservableProperty]
        private bool isPresetListEmpty;

        /// <summary>
        /// 화면에 보이는 목록. 대역이 낮은 순으로 묶고, 묶음 안은 비트가 낮은 순으로 늘어놓는다.
        /// 대역 순서가 곧 수면에서 각성으로 가는 순서라서, 사용자가 원하는 상태의 자리를 바로 찾는다.
        /// </summary>
        public IReadOnlyList<PresetGroup> PresetGroups { get; }

        public PresetsViewModel()
        {
            // MainViewModel에서 프리셋 리스트 가져오기
            _mainViewModel = new MainViewModel();
            _audioService = AudioService.Instance;

            // PresetData를 PresetDataViewModel로 변환
            PresetList = new ObservableCollection<PresetDataViewModel>(
                _mainViewModel.PresetList.Select(p => new PresetDataViewModel(p))
            );

            IsPresetListEmpty = PresetList.Count == 0;

            PresetGroups = PresetList
                .GroupBy(preset => preset.Band)
                .OrderBy(group => group.Key.MinimumBeat)
                .Select(group => new PresetGroup(group.Key, group.OrderBy(preset => preset.Resonance).ToList()))
                .ToList();

            // 재생 상태 변경 메시지 수신
            WeakReferenceMessenger.Default.Register<PlaybackStateChangedMessage>(this, (r, m) =>
            {
                // 재생이 중지되면 현재 프리셋 초기화
                if (!m.IsPlaying)
                {
                    if (_currentlyPlayingPreset != null)
                    {
                        _currentlyPlayingPreset.IsPlaying = false;
                    }
                    _currentlyPlayingPreset = null;
                }
            });
        }

        [RelayCommand]
        private void SelectPreset(PresetDataViewModel preset)
        {
            if (preset == null) return;

            // 같은 프리셋을 다시 클릭하면 정지
            if (_currentlyPlayingPreset == preset && _audioService.IsPlaying)
            {
                _audioService.Stop();
                preset.IsPlaying = false;
                _currentlyPlayingPreset = null;
            }
            else
            {
                // 이전 프리셋의 재생 상태 업데이트
                if (_currentlyPlayingPreset != null)
                {
                    _currentlyPlayingPreset.IsPlaying = false;
                }

                // 다른 프리셋 또는 정지 상태에서 클릭하면 재생
                _audioService.Play(preset.LeftWave, preset.RightWave, 0.5, 0.5);
                preset.IsPlaying = true;
                _currentlyPlayingPreset = preset;

                // Waves 페이지로 주파수 데이터 전송 (필요한 경우)
                WeakReferenceMessenger.Default.Send(new PresetSelectedMessage(preset.LeftWave, preset.RightWave, 50.0, 50.0));
            }
        }

        [RelayCommand]
        private void NavigateToWaves(PresetDataViewModel preset)
        {
            if (preset == null) return;

            // Waves 페이지로 이동
            _mainViewModel.NavigateFrameCommand.Execute("pack://application:,,,/View/Waves.xaml");
        }
    }

    /// <summary>
    /// 같은 뇌파 대역에 속하는 프리셋 묶음
    /// </summary>
    public sealed record PresetGroup(BrainwaveBand Band, IReadOnlyList<PresetDataViewModel> Presets);

    // PresetData를 확장하여 UI 관련 속성 추가
    public partial class PresetDataViewModel : PresetData
    {
        [ObservableProperty]
        private bool isPlaying;

        public PresetDataViewModel(PresetData preset) : base(preset.PresetName, preset.LeftWave, preset.RightWave)
        {
        }
    }

    // 프리셋 선택 메시지
    public class PresetSelectedMessage
    {
        public double LeftFrequency { get; }
        public double RightFrequency { get; }
        public double LeftGain { get; }
        public double RightGain { get; }

        public PresetSelectedMessage(double leftFrequency, double rightFrequency, double leftGain = 50.0, double rightGain = 50.0)
        {
            LeftFrequency = leftFrequency;
            RightFrequency = rightFrequency;
            LeftGain = leftGain;
            RightGain = rightGain;
        }
    }
}
