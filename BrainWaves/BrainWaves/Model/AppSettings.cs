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
    }
}
