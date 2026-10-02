using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BrainWaves.Model
{
    /// <summary>
    /// 실행 사이에 유지하는 사용자 설정. 값을 바꿀 때는 with 식으로 새 인스턴스를 만든다.
    /// </summary>
    // INTENT: 불변 레코드로 둔다. 업데이트 라이브러리가 버전 전환 중 백그라운드 스레드에서
    // 자동 업데이트 설정을 끄므로, 읽는 쪽이 잠금 없이 항상 완성된 스냅숏을 보게 한다.
    public sealed record AppSettings
    {
        public bool AutoUpdateEnabled { get; init; } = true;

        public bool PrereleaseEnabled { get; init; }

        /// <summary>
        /// 즐겨찾기에 넣은 프리셋의 Id(PresetData.Id). 순서에는 뜻이 없다.
        /// </summary>
        public IReadOnlyList<string> FavoritePresets { get; init; } = Array.Empty<string>();

        /// <summary>
        /// 전체 음량. 0에서 100 사이의 백분율이다.
        /// </summary>
        public double MasterVolume { get; init; } = 50;

        // INTENT: 이 버전이 모르는 항목도 저장할 때 그대로 다시 쓴다. 설정 화면에서 예전 버전으로 되돌릴 수 있으므로,
        // 새 버전이 더한 항목이 예전 버전에서 설정을 한 번 저장하는 것만으로 사라지면 안 된다.
        [JsonExtensionData]
        public Dictionary<string, JsonElement>? UnknownProperties { get; init; }
    }
}
