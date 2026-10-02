using System.Windows;

namespace BrainWaves.View
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // INTENT: 화면은 하단 탭으로만 고른다. Frame은 방문 기록을 남겨 Alt+←, Backspace, 마우스의 뒤로 가기 버튼으로
            // 이전 화면을 다시 띄우는데, 그러면 고른 탭과 보이는 화면이 어긋난다. 기록을 남기지 않아 돌아갈 화면이 없게 한다.
            PageFrame.Navigated += (_, _) =>
            {
                while (PageFrame.CanGoBack) PageFrame.RemoveBackEntry();
            };
        }
    }
}
