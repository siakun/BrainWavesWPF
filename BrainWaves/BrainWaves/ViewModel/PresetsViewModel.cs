using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
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
        private readonly SettingsStore _settings = SettingsStore.Instance;
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

        /// <summary>
        /// 목록 맨 위에 모아 보이는 즐겨찾기. 대역별 묶음에서 빼지 않고 같은 항목을 한 번 더 보여 준다.
        /// </summary>
        // INTENT: 즐겨찾기를 대역 묶음에서 옮겨 오면 대역별 목록에 빈자리가 생겨, 대역을 보고 프리셋을 찾는 사용자가
        // 그 프리셋을 찾지 못한다. 같은 인스턴스를 두 곳에 보여 주므로 재생 표시와 별 표시는 두 곳이 함께 바뀐다.
        // 순서는 대역 묶음과 같이 비트가 낮은 순으로 두어, 즐겨찾기한 순서를 기억하지 않아도 자리를 예상할 수 있게 한다.
        public ObservableCollection<PresetDataViewModel> Favorites { get; } = new();

        [ObservableProperty]
        private bool hasFavorites;

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

            ApplyFavorites();

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

        [RelayCommand]
        private void ToggleFavorite(PresetDataViewModel? preset)
        {
            if (preset is null) return;

            try
            {
                _settings.Update(settings => settings with
                {
                    FavoritePresets = settings.FavoritePresets.Contains(preset.Id)
                        ? settings.FavoritePresets.Where(id => id != preset.Id).ToArray()
                        : settings.FavoritePresets.Append(preset.Id).ToArray()
                });
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // 저장하지 못했으면 설정도 바뀌지 않았다. 아래에서 저장된 값으로 다시 그려 별 표시를 원래대로 돌린다.
                Debug.WriteLine($"[PresetsViewModel] Failed to save favorites: {ex.Message}");
            }

            ApplyFavorites();
        }

        /// <summary>
        /// 저장된 즐겨찾기로 별 표시와 즐겨찾기 묶음을 다시 만든다. 화면의 즐겨찾기 상태는 항상 설정 저장소에서 읽는다.
        /// </summary>
        private void ApplyFavorites()
        {
            var favoriteIds = _settings.Current.FavoritePresets;
            foreach (var preset in PresetList)
                preset.IsFavorite = favoriteIds.Contains(preset.Id);

            Favorites.Clear();
            foreach (var preset in PresetList.Where(preset => preset.IsFavorite).OrderBy(preset => preset.Resonance))
                Favorites.Add(preset);

            HasFavorites = Favorites.Count > 0;
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

        [ObservableProperty]
        private bool isFavorite;

        public PresetDataViewModel(PresetData preset) : base(preset.Id, preset.PresetName, preset.LeftWave, preset.RightWave)
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
