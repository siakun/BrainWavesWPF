using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace BrainWaves.View
{
    public partial class ChannelCard : UserControl
    {
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(nameof(Title), typeof(string), typeof(ChannelCard), new PropertyMetadata(""));

        public static readonly DependencyProperty ChannelBrushProperty =
            DependencyProperty.Register(nameof(ChannelBrush), typeof(Brush), typeof(ChannelCard), new PropertyMetadata(null));

        public static readonly DependencyProperty FrequencyProperty =
            DependencyProperty.Register(nameof(Frequency), typeof(double), typeof(ChannelCard),
                new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public static readonly DependencyProperty GainProperty =
            DependencyProperty.Register(nameof(Gain), typeof(double), typeof(ChannelCard),
                new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public static readonly DependencyProperty DecreaseCommandProperty =
            DependencyProperty.Register(nameof(DecreaseCommand), typeof(ICommand), typeof(ChannelCard), new PropertyMetadata(null));

        public static readonly DependencyProperty IncreaseCommandProperty =
            DependencyProperty.Register(nameof(IncreaseCommand), typeof(ICommand), typeof(ChannelCard), new PropertyMetadata(null));

        public ChannelCard()
        {
            InitializeComponent();
        }

        /// <summary>카드 위쪽의 채널 이름. 라벨 규칙에 따라 대문자로 적는다.</summary>
        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        /// <summary>채널을 표시하는 색. 점과 주파수 슬라이더를 이 색으로 칠한다.</summary>
        public Brush? ChannelBrush
        {
            get => (Brush?)GetValue(ChannelBrushProperty);
            set => SetValue(ChannelBrushProperty, value);
        }

        public double Frequency
        {
            get => (double)GetValue(FrequencyProperty);
            set => SetValue(FrequencyProperty, value);
        }

        /// <summary>채널 음량. 0에서 100 사이다.</summary>
        public double Gain
        {
            get => (double)GetValue(GainProperty);
            set => SetValue(GainProperty, value);
        }

        public ICommand? DecreaseCommand
        {
            get => (ICommand?)GetValue(DecreaseCommandProperty);
            set => SetValue(DecreaseCommandProperty, value);
        }

        public ICommand? IncreaseCommand
        {
            get => (ICommand?)GetValue(IncreaseCommandProperty);
            set => SetValue(IncreaseCommandProperty, value);
        }
    }
}
