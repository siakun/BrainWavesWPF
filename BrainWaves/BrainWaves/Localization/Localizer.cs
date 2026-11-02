using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Resources;
using System.Windows.Threading;
using BrainWaves.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BrainWaves.Localization
{
    /// <summary>
    /// 화면에 보이는 문자열을 지금 고른 언어로 돌려준다. 문자열은 Strings.resx(영어)와 Strings.&lt;언어&gt;.resx에 있고,
    /// 고른 언어에 없는 문자열은 영어로 보인다. 언어를 바꾸면 Culture가 바뀌었다고 알려, 이 값에 묶인 화면 문자열이 모두 다시 그려진다.
    /// </summary>
    // INTENT: 언어 설정의 원본은 설정 저장소다. 설정 화면은 저장소만 바꾸고, 이 객체는 저장소의 변경을 받아 언어를 맞춘다.
    // 화면이 직접 언어를 바꾸게 하면 저장에 실패했을 때 화면의 언어와 저장된 언어가 어긋난다.
    // 다시 시작하지 않고 바로 바뀌게 하려고, 문자열을 불러올 때 한 번 읽고 마는 x:Static 대신 Culture에 묶인 바인딩(TextExtension)으로 쓴다.
    public sealed class Localizer : ObservableObject
    {
        private static readonly ResourceManager Strings =
            new("BrainWaves.Localization.Strings", typeof(Localizer).Assembly);

        // 앱을 시작할 때의 Windows 표시 언어. "시스템 설정 따름"을 고르면 이 언어로 보이고, 번역이 없으면 영어로 보인다.
        private static readonly CultureInfo SystemCulture = CultureInfo.CurrentUICulture;

        private readonly SettingsStore _settings = SettingsStore.Instance;

        public static Localizer Instance { get; } = new();

        /// <summary>
        /// 설정 화면에서 고를 수 있는 언어. Code가 null이면 Windows 표시 언어를 따른다.
        /// </summary>
        public static IReadOnlyList<LanguageOption> Languages { get; } = new LanguageOption[]
        {
            new(null),
            new("en"),
            new("ko"),
        };

        private Localizer()
        {
            Culture = Resolve(_settings.Current.Language);

            // 설정 저장소는 저장한 스레드에서 알린다. 화면 바인딩은 UI 스레드에서 바꿔야 하므로 옮긴다.
            var dispatcher = Dispatcher.CurrentDispatcher;
            _settings.Changed += (_, _) => dispatcher.BeginInvoke(() => Apply(_settings.Current.Language));
        }

        public CultureInfo Culture { get; private set; }

        public string Get(string key) => Strings.GetString(key, Culture) ?? key;

        public string Format(string key, params object?[] args) => string.Format(Culture, Get(key), args);

        /// <summary>
        /// 키와 인자로 된 문장을 지금 언어로 만든다. 인자가 다시 LocalizedText면 그것도 지금 언어로 바꿔 넣는다.
        /// </summary>
        public string Format(LocalizedText text) =>
            Format(text.Key, text.Args.Select(arg => arg is LocalizedText inner ? Format(inner) : arg).ToArray());

        private void Apply(string? language)
        {
            var culture = Resolve(language);
            if (Equals(culture, Culture)) return;

            Culture = culture;
            OnPropertyChanged(nameof(Culture));
        }

        private static CultureInfo Resolve(string? language) =>
            string.IsNullOrEmpty(language) ? SystemCulture : CultureInfo.GetCultureInfo(language);
    }

    /// <summary>
    /// 설정 화면의 언어 목록 항목. 이름은 Language.&lt;Code&gt; 문자열이며, 각 언어 이름은 그 언어로 적어 어느 언어에서든 알아보게 한다.
    /// </summary>
    public sealed record LanguageOption(string? Code)
    {
        public LocalizedText Name => new($"Language.{Code ?? "System"}");
    }
}
