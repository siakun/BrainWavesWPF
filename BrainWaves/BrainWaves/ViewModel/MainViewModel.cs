using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BrainWaves.ViewModel
{
    public partial class MainViewModel : ObservableObject
    {
        [ObservableProperty]
        private Uri showingPageName;

        /// <summary>
        /// 창 아래쪽 업데이트 알림 막대가 보는 상태
        /// </summary>
        public UpdatesViewModel Updates => UpdatesViewModel.Instance;

        public MainViewModel()
        {
            showingPageName = new Uri("pack://application:,,,/View/Waves.xaml");
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
