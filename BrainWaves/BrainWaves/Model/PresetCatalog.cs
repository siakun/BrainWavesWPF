using System.Collections.Generic;

namespace BrainWaves.Model
{
    /// <summary>
    /// 앱에 들어 있는 프리셋 목록.
    /// </summary>
    public static class PresetCatalog
    {
        // 첫 인자는 설정 파일에 저장되는 Id라 바꾸지 않는다. 표시 이름은 문자열 리소스의 Preset.<Id>에서 고친다.
        public static IReadOnlyList<PresetData> All { get; } = new PresetData[]
        {
            new("visualization", 101.08, 109.75),
            new("creative", 110.72, 138.85),
            new("focus", 133.00, 162.00),
            new("work", 194.66, 181.33),
            new("concentrate", 158.44, 144.98),
            new("healing", 80.72, 82.22),
            new("sleep", 75.00, 73.00),
            new("deep-sleep", 55.96, 54.33),
            new("perception", 140.00, 100.43),
            new("cognitive-tasks", 340.00, 300.00),
            new("infra-low", 89.00, 89.35),
            new("meditation", 85.25, 89.75),
            new("relax", 95.66, 100.22),
        };
    }
}
