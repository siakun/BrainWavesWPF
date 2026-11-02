using System;
using System.Windows.Threading;
using BrainWaves.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BrainWaves.ViewModel
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly SettingsStore _settings = SettingsStore.Instance;

        [ObservableProperty]
        private Uri showingPageName;

        /// <summary>
        /// 창 아래쪽 업데이트 알림 막대가 보는 상태
        /// </summary>
        public UpdatesViewModel Updates => UpdatesViewModel.Instance;

        /// <summary>
        /// 트레이 메뉴의 재생 항목이 보는 소리 상태
        /// </summary>
        public AudioService Audio => AudioService.Instance;

        /// <summary>
        /// 창을 닫으면 트레이로 숨기는 설정. 트레이 아이콘은 이 설정이 켜져 있을 때만 보인다.
        /// </summary>
        public bool CloseToTray => _settings.Current.CloseToTray;

        public MainViewModel()
        {
            showingPageName = new Uri("pack://application:,,,/View/Waves.xaml");

            // 메인 창은 앱 수명 동안 하나라 구독을 풀지 않는다. 설정 저장소는 저장한 스레드에서 알린다.
            var dispatcher = Dispatcher.CurrentDispatcher;
            _settings.Changed += (_, _) => dispatcher.BeginInvoke(() => OnPropertyChanged(nameof(CloseToTray)));
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

        [RelayCommand]
        private void TogglePlayback()
        {
            if (Audio.IsPlaying) Audio.Stop();
            else Audio.Play();
        }
    }
}
