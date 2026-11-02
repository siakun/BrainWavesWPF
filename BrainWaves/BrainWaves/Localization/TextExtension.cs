using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace BrainWaves.Localization
{
    /// <summary>
    /// XAML에서 지금 언어의 문자열을 쓰는 마크업 확장. 언어가 바뀌면 다시 계산된다.
    /// <list type="bullet">
    /// <item><c>{loc:Text Settings.Title}</c>: 키의 문자열</item>
    /// <item><c>{loc:Text Updates.Ready, Arg={Binding Version}}</c>: 문자열의 {0} 자리에 바인딩 값을 넣는다. ArgKeyFormat을 주면 그 값으로 만든 키의 문자열을 넣는다.</item>
    /// <item><c>{loc:Text Source={Binding StatusText}}</c>: 바인딩 값이 LocalizedText면 그 문장을 만든다.</item>
    /// <item><c>{loc:Text Source={Binding Band.Id}, KeyFormat=Band.{0}.Name}</c>: 바인딩 값으로 키를 만들어 그 문자열을 쓴다.</item>
    /// </list>
    /// </summary>
    // INTENT: 바인딩 하나에 지금 언어(Localizer.Culture)와 필요한 값을 함께 묶는다. 언어가 바뀌면 Culture가 바뀌었다는 알림으로
    // 화면의 문자열이 다시 계산되므로, 화면마다 언어 변경을 구독하거나 페이지를 다시 만들지 않아도 된다.
    [MarkupExtensionReturnType(typeof(object))]
    public sealed class TextExtension : MarkupExtension
    {
        public TextExtension()
        {
        }

        public TextExtension(string key)
        {
            Key = key;
        }

        [ConstructorArgument("key")]
        public string? Key { get; set; }

        public Binding? Arg { get; set; }

        public string? ArgKeyFormat { get; set; }

        public Binding? Source { get; set; }

        public string? KeyFormat { get; set; }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            var binding = new MultiBinding { Converter = new TextConverter(this), Mode = BindingMode.OneWay };
            binding.Bindings.Add(new Binding(nameof(Localizer.Culture)) { Source = Localizer.Instance });
            if (Source is not null) binding.Bindings.Add(Source);
            if (Arg is not null) binding.Bindings.Add(Arg);

            // 스타일의 Setter나 템플릿처럼 붙을 대상이 아직 없는 자리에서는 바인딩 자체를 돌려주고, 그 자리가 대상에 붙일 때 쓰게 한다.
            if (serviceProvider.GetService(typeof(IProvideValueTarget)) is IProvideValueTarget { TargetObject: not DependencyObject })
                return binding;

            return binding.ProvideValue(serviceProvider);
        }

        private sealed class TextConverter : IMultiValueConverter
        {
            private readonly TextExtension _text;

            public TextConverter(TextExtension text)
            {
                _text = text;
            }

            public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
            {
                var localizer = Localizer.Instance;
                var index = 1;
                var key = _text.Key;

                if (_text.Source is not null)
                {
                    var value = Value(values[index++]);
                    if (value is LocalizedText localized) return localizer.Format(localized);
                    if (value is null) return string.Empty;
                    key = _text.KeyFormat is null ? value.ToString() : string.Format(CultureInfo.InvariantCulture, _text.KeyFormat, value);
                }

                if (key is null) return string.Empty;
                if (_text.Arg is null) return localizer.Get(key);

                var arg = Value(values[index]);
                if (_text.ArgKeyFormat is not null && arg is not null)
                    arg = localizer.Get(string.Format(CultureInfo.InvariantCulture, _text.ArgKeyFormat, arg));
                return localizer.Format(key, arg);
            }

            public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
                throw new NotSupportedException();

            // 바인딩이 아직 값을 얻지 못했으면 빈 값으로 본다.
            private static object? Value(object? value) =>
                value == DependencyProperty.UnsetValue || value == BindingOperations.DisconnectedSource ? null : value;
        }
    }
}
