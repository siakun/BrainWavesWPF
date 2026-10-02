using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using BrainWaves.Model;
using BrainWaves.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BrainWaves.ViewModel
{
    public partial class PresetsViewModel : ObservableObject
    {
        // 주파수는 0.01 Hz 단위로 움직이므로 그 절반보다 가까우면 같은 소리로 본다.
        private const double SameFrequencyTolerance = 0.005;

        private readonly AudioService _audio = AudioService.Instance;
        private readonly SettingsStore _settings = SettingsStore.Instance;

        public IReadOnlyList<PresetDataViewModel> PresetList { get; }

        public bool IsPresetListEmpty => PresetList.Count == 0;

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
            PresetList = PresetCatalog.All.Select(preset => new PresetDataViewModel(preset)).ToList();

            PresetGroups = PresetList
                .GroupBy(preset => preset.Band)
                .OrderBy(group => group.Key.MinimumBeat)
                .Select(group => new PresetGroup(group.Key, group.OrderBy(preset => preset.Resonance).ToList()))
                .ToList();

            ApplyFavorites();

            // 약한 구독으로 받아, 탭을 옮겨 이 ViewModel이 버려지면 함께 정리되게 한다.
            PropertyChangedEventManager.AddHandler(_audio, OnAudioChanged, string.Empty);
            ShowPlayingPreset();
        }

        [RelayCommand]
        private void SelectPreset(PresetDataViewModel? preset)
        {
            if (preset is null) return;

            // 지금 들리는 프리셋을 다시 누르면 멈춘다. 다른 프리셋은 소리를 끊지 않고 주파수만 옮긴다.
            if (preset.IsPlaying) _audio.Stop();
            else _audio.Play(preset.LeftWave, preset.RightWave);
        }

        private void OnAudioChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(AudioService.IsPlaying)
                or nameof(AudioService.LeftFrequency)
                or nameof(AudioService.RightFrequency)
                or null or "")
            {
                ShowPlayingPreset();
            }
        }

        /// <summary>
        /// 재생 표시를 지금 들리는 소리에서 정한다. 재생 중이고 두 채널 주파수가 프리셋과 같으면 그 프리셋이 재생 중이다.
        /// </summary>
        // INTENT: 재생 중인 프리셋을 이 ViewModel에 기억하면, 탭을 옮겨 다시 만들어진 화면은 그것을 모르고
        // Waves에서 주파수를 바꿔도 표시가 남는다. 소리에서 매번 다시 읽으면 어느 경로로 바뀌어도 표시가 맞는다.
        private void ShowPlayingPreset()
        {
            foreach (var preset in PresetList)
            {
                preset.IsPlaying = _audio.IsPlaying
                    && Math.Abs(preset.LeftWave - _audio.LeftFrequency) < SameFrequencyTolerance
                    && Math.Abs(preset.RightWave - _audio.RightFrequency) < SameFrequencyTolerance;
            }
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
}
