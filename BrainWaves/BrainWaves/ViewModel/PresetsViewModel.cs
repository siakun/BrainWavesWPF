using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using BrainWaves.Behaviors;
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
        /// 화면에 보이는 목록. 대역이 낮은 순으로 묶는다. 묶음 안의 순서는 사용자가 끌어서 정하고, 정하지 않았으면 비트가 낮은 순이다.
        /// 대역 순서가 곧 수면에서 각성으로 가는 순서라서, 사용자가 원하는 상태의 자리를 바로 찾는다. 묶음끼리의 순서는 바꾸지 않는다.
        /// </summary>
        public IReadOnlyList<PresetGroup> PresetGroups { get; }

        /// <summary>
        /// 목록 맨 위에 모아 보이는 즐겨찾기. 대역별 묶음에서 빼지 않고 같은 항목을 한 번 더 보여 준다.
        /// </summary>
        // INTENT: 즐겨찾기를 대역 묶음에서 옮겨 오면 대역별 목록에 빈자리가 생겨, 대역을 보고 프리셋을 찾는 사용자가
        // 그 프리셋을 찾지 못한다. 같은 인스턴스를 두 곳에 보여 주므로 재생 표시와 별 표시는 두 곳이 함께 바뀐다.
        // 순서는 사용자가 끌어서 정한다. 새로 별표한 프리셋은 맨 뒤에 붙어 이미 정해 둔 순서를 흐트러뜨리지 않는다.
        public ObservableCollection<PresetDataViewModel> Favorites { get; } = new();

        [ObservableProperty]
        private bool hasFavorites;

        public PresetsViewModel()
        {
            PresetList = PresetCatalog.All.Select(preset => new PresetDataViewModel(preset)).ToList();

            // 묶음에 들어갈 프리셋과 그 순서는 ApplySettings가 저장된 순서로 채운다.
            PresetGroups = PresetList
                .Select(preset => preset.Band)
                .Distinct()
                .OrderBy(band => band.MinimumBeat)
                .Select(band => new PresetGroup(band))
                .ToList();

            ApplySettings();

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

            SaveSettings(settings => settings with
            {
                FavoritePresets = settings.FavoritePresets.Contains(preset.Id)
                    ? settings.FavoritePresets.Where(id => id != preset.Id).ToArray()
                    : settings.FavoritePresets.Append(preset.Id).ToArray()
            });
        }

        [RelayCommand]
        private void MoveFavorite(ReorderMove? move)
        {
            if (move?.Item is not PresetDataViewModel preset || !Favorites.Contains(preset)) return;

            var ids = Moved(Favorites, preset, move.NewIndex).Select(favorite => favorite.Id).ToList();
            SaveSettings(settings => settings with { FavoritePresets = KeepingUnknownIds(ids, settings.FavoritePresets) });
        }

        [RelayCommand]
        private void MovePreset(ReorderMove? move)
        {
            if (move?.Item is not PresetDataViewModel preset) return;
            var movedGroup = PresetGroups.FirstOrDefault(group => group.Presets.Contains(preset));
            if (movedGroup is null) return;

            // 모든 묶음의 지금 순서를 이어 저장한다. 앞서 다른 묶음에서 정해 둔 순서도 그대로 남는다.
            var ids = PresetGroups
                .SelectMany(group => group == movedGroup ? Moved(group.Presets, preset, move.NewIndex) : group.Presets.AsEnumerable())
                .Select(item => item.Id)
                .ToList();
            SaveSettings(settings => settings with { PresetOrder = KeepingUnknownIds(ids, settings.PresetOrder) });
        }

        private void SaveSettings(Func<AppSettings, AppSettings> change)
        {
            try
            {
                _settings.Update(change);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // 저장하지 못했으면 설정도 바뀌지 않았다. 아래에서 저장된 값으로 다시 그려 별 표시와 순서를 원래대로 돌린다.
                Debug.WriteLine($"[PresetsViewModel] Failed to save settings: {ex.Message}");
            }

            ApplySettings();
        }

        /// <summary>
        /// 저장된 설정으로 별 표시, 즐겨찾기 묶음, 묶음 안의 순서를 다시 맞춘다. 화면의 즐겨찾기와 순서는 항상 설정 저장소에서 읽는다.
        /// </summary>
        private void ApplySettings()
        {
            var settings = _settings.Current;
            foreach (var preset in PresetList)
                preset.IsFavorite = settings.FavoritePresets.Contains(preset.Id);

            SyncTo(Favorites, InSavedOrder(PresetList.Where(preset => preset.IsFavorite), settings.FavoritePresets));
            foreach (var group in PresetGroups)
                SyncTo(group.Presets, InSavedOrder(PresetList.Where(preset => preset.Band == group.Band), settings.PresetOrder));

            HasFavorites = Favorites.Count > 0;
        }

        // 저장된 순서에 있는 프리셋을 그 순서대로 앞에 두고, 없는 프리셋은 비트가 낮은 순으로 뒤에 둔다.
        private static List<PresetDataViewModel> InSavedOrder(IEnumerable<PresetDataViewModel> presets, IReadOnlyList<string> savedIds)
        {
            var rank = new Dictionary<string, int>();
            for (var i = 0; i < savedIds.Count; i++)
                rank.TryAdd(savedIds[i], i);

            return presets
                .OrderBy(preset => rank.TryGetValue(preset.Id, out var index) ? index : int.MaxValue)
                .ThenBy(preset => preset.Resonance)
                .ToList();
        }

        private static List<PresetDataViewModel> Moved(IEnumerable<PresetDataViewModel> presets, PresetDataViewModel preset, int newIndex)
        {
            var list = presets.ToList();
            list.Remove(preset);
            list.Insert(Math.Clamp(newIndex, 0, list.Count), preset);
            return list;
        }

        // 화면에 보이는 순서 뒤에 이 버전이 모르는 Id를 그대로 붙인다. 설정 화면에서 예전 버전으로 되돌린 사용자가
        // 순서를 한 번 바꾸는 것만으로, 새 버전에서 별표한 프리셋이나 정해 둔 순서가 사라지지 않게 한다.
        private static string[] KeepingUnknownIds(IEnumerable<string> visibleIds, IReadOnlyList<string> savedIds)
        {
            var visible = visibleIds.ToList();
            return visible.Concat(savedIds.Where(id => !visible.Contains(id))).Distinct().ToArray();
        }

        // 컬렉션을 원하는 순서로 맞춘다. 비우고 다시 채우지 않고 필요한 항목만 옮기고 넣고 빼서,
        // 자리를 지킨 줄은 화면 요소를 다시 만들지 않는다. 별 하나를 눌렀다고 Favorites의 모든 줄이 새로 그려지지 않는다.
        private static void SyncTo<T>(ObservableCollection<T> target, IReadOnlyList<T> desired)
        {
            for (var i = 0; i < desired.Count; i++)
            {
                var current = target.IndexOf(desired[i]);
                if (current == i) continue;

                if (current < 0) target.Insert(i, desired[i]);
                else target.Move(current, i);
            }

            while (target.Count > desired.Count)
                target.RemoveAt(target.Count - 1);
        }
    }

    /// <summary>
    /// 같은 뇌파 대역에 속하는 프리셋 묶음. 묶음 안의 순서는 사용자가 끌어서 바꾼다.
    /// </summary>
    public sealed class PresetGroup
    {
        public PresetGroup(BrainwaveBand band)
        {
            Band = band;
        }

        public BrainwaveBand Band { get; }

        public ObservableCollection<PresetDataViewModel> Presets { get; } = new();
    }

    // PresetData를 확장하여 UI 관련 속성 추가
    public partial class PresetDataViewModel : PresetData
    {
        [ObservableProperty]
        private bool isPlaying;

        [ObservableProperty]
        private bool isFavorite;

        public PresetDataViewModel(PresetData preset) : base(preset.Id, preset.LeftWave, preset.RightWave)
        {
        }
    }
}
