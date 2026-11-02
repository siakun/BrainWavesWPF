namespace BrainWaves.Localization
{
    /// <summary>
    /// 아직 언어를 정하지 않은 문장. 문자열 키와 그 자리에 넣을 인자만 들고 있고, 화면에 그릴 때 지금 언어로 만든다.
    /// </summary>
    // INTENT: ViewModel이 상태를 알리는 문장을 완성된 문자열로 들고 있으면, 언어를 바꿔도 다음에 상태가 바뀔 때까지 옛 언어로 남는다.
    // 키와 인자를 들고 있으면 언어가 바뀌는 순간 화면이 다시 만든다.
    public sealed record LocalizedText(string Key, params object?[] Args);
}
