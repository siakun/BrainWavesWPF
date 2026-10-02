using System;
using System.Collections.ObjectModel;
using BrainWaves.Model;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BrainWaves.ViewModel
{
    public partial class MainViewModel : ObservableObject
    {
        [ObservableProperty]
        private Uri showingPageName;

        public ObservableCollection<PresetData> PresetList { get; set; }

        /// <summary>
        /// 창 아래쪽 업데이트 알림 막대가 보는 상태
        /// </summary>
        public UpdatesViewModel Updates => UpdatesViewModel.Instance;

        public MainViewModel()
        {
            showingPageName = new Uri("pack://application:,,,/View/Waves.xaml");
            PresetList = new ObservableCollection<PresetData>();

            // 첫 인자는 설정 파일에 저장되는 Id라 바꾸지 않는다. 표시 이름은 고쳐도 된다.
            PresetList.Add(new("visualization", "Visualization", 101.08, 109.75));
            PresetList.Add(new("creative", "Creative", 110.72, 138.85));
            PresetList.Add(new("focus", "Focus", 133.00, 162.00));
            PresetList.Add(new("work", "Work", 194.66, 181.33));
            PresetList.Add(new("concentrate", "Concentrate", 158.44, 144.98));
            PresetList.Add(new("healing", "Healing", 80.72, 82.22));
            PresetList.Add(new("sleep", "Sleep", 75.00, 73.00));
            PresetList.Add(new("deep-sleep", "Deep Sleep", 55.96, 54.33));
            PresetList.Add(new("perception", "Perception", 140.00, 100.43));
            PresetList.Add(new("cognitive-tasks", "Cognitive tasks", 340.00, 300.00));
            PresetList.Add(new("infra-low", "InfraLow", 89.00, 89.35));
            PresetList.Add(new("meditation", "Meditation", 85.25, 89.75));
            PresetList.Add(new("relax", "Relax", 95.66, 100.22));
        }

        [RelayCommand]
        private void NavigateFrame(string? pageName)
        {
            if (pageName is null) return;
            if (Uri.TryCreate(pageName, UriKind.Absolute, out Uri? result))
            {
                ShowingPageName = result;
            }
        }
    }
}
