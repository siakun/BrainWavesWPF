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
        /// 즐겨찾기에 넣은 프리셋의 Id(PresetData.Id). Favorites 묶음은 이 순서대로 보이고, 새로 넣은 프리셋은 맨 뒤에 붙는다.
        /// </summary>
        public IReadOnlyList<string> FavoritePresets { get; init; } = Array.Empty<string>();

        /// <summary>
        /// 사용자가 끌어서 정한 프리셋 순서(PresetData.Id). 대역 묶음 안에서 이 순서대로 보인다.
        /// 여기 없는 프리셋은 같은 묶음의 뒤쪽에 비트가 낮은 순으로 놓인다.
        /// </summary>
        // INTENT: 묶음마다 따로 저장하지 않고 한 줄로 저장한다. 묶음은 대역 경계로 정해지므로, 경계가 바뀌어 프리셋이
        // 다른 묶음으로 옮겨 가도 묶음별 목록처럼 갈 곳 없는 항목이 생기지 않고 새 묶음 안에서 순서를 이어 쓴다.
        public IReadOnlyList<string> PresetOrder { get; init; } = Array.Empty<string>();

        /// <summary>
        /// 전체 음량. 0에서 100 사이의 백분율이다.
        /// </summary>
        public double MasterVolume { get; init; } = 50;

        /// <summary>
        /// 창을 닫을 때 앱을 끝내지 않고 알림 영역(트레이)으로 숨길지 여부. 숨긴 동안에도 소리는 계속 난다.
        /// </summary>
        public bool CloseToTray { get; init; } = true;

        // INTENT: 이 버전이 모르는 항목도 저장할 때 그대로 다시 쓴다. 설정 화면에서 예전 버전으로 되돌릴 수 있으므로,
        // 새 버전이 더한 항목이 예전 버전에서 설정을 한 번 저장하는 것만으로 사라지면 안 된다.
        [JsonExtensionData]
        public Dictionary<string, JsonElement>? UnknownProperties { get; init; }
    }
}
