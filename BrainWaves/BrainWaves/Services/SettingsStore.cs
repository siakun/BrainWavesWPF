using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using BrainWaves.Model;

namespace BrainWaves.Services
{
    /// <summary>
    /// 사용자 설정을 JSON 파일로 읽고 저장한다. 설정을 바꾸는 쪽은 Update를 거치고, 화면은 Changed로 갱신한다.
    /// </summary>
    // INTENT: 설정 값의 원본을 여기 한 곳에 둔다. 화면이 값을 따로 들고 있다가 저장할 때 덮어쓰면,
    // 업데이트 라이브러리가 구버전 설치 중에 끈 자동 업데이트 설정이 화면에 남은 옛 값으로 되살아난다.
    public sealed class SettingsStore
    {
        private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

        private readonly string _path;
        private readonly object _saveLock = new();
        // 쓰기는 잠금 안에서만 하고, 읽기는 잠금 없이 마지막으로 저장된 스냅숏을 본다.
        private volatile AppSettings _current;

        public static SettingsStore Instance { get; } = new(Path.Combine(AppInfo.DataDirectory, "settings.json"));

        private SettingsStore(string path)
        {
            _path = path;
            _current = Load(path);
        }

        public AppSettings Current => _current;

        /// <summary>
        /// 저장을 마친 뒤 호출한 스레드에서 발생한다. UI 갱신은 받는 쪽에서 UI 스레드로 옮긴다.
        /// </summary>
        public event EventHandler? Changed;

        /// <summary>
        /// 설정을 바꾸고 파일에 저장한 뒤 반환한다. 저장에 실패하면 예외를 던지고 값도 바꾸지 않는다.
        /// </summary>
        public void Update(Func<AppSettings, AppSettings> change)
        {
            lock (_saveLock)
            {
                var next = change(_current);
                if (next == _current) return;

                Save(next);
                _current = next;
            }
            Changed?.Invoke(this, EventArgs.Empty);
        }

        private static AppSettings Load(string path)
        {
            try
            {
                if (!File.Exists(path)) return new AppSettings();
                return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(path)) ?? new AppSettings();
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
            {
                // 읽지 못한 설정 때문에 앱이 뜨지 않으면 안 된다. 기본값으로 시작하고 다음 저장이 파일을 고친다.
                Debug.WriteLine($"[SettingsStore] Failed to read {path}: {ex.Message}");
                return new AppSettings();
            }
        }

        private void Save(AppSettings settings)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);

            // 쓰는 도중 앱이 꺼져도 설정 파일이 반쯤 쓰인 채 남지 않도록 임시 파일에 쓴 뒤 교체한다.
            var temporaryPath = _path + ".tmp";
            File.WriteAllText(temporaryPath, JsonSerializer.Serialize(settings, JsonOptions));
            File.Move(temporaryPath, _path, overwrite: true);
        }
    }
}
