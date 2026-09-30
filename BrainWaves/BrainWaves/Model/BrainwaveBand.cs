using System.Linq;

namespace BrainWaves.Model
{
    /// <summary>
    /// 바이노럴 비트(좌우 주파수의 차이)가 속하는 뇌파 대역.
    /// </summary>
    // INTENT: 대역 경계와 표기를 이 표 한 곳에 둔다. 파형 화면과 프리셋 목록이 각자 경계를 적어 두면
    // 한쪽만 고쳤을 때 같은 비트가 화면마다 다른 대역으로 표시된다.
    // 대역은 색이 아니라 뇌파 분야의 표기인 그리스 문자로 구분한다. 색은 좌우 채널을 구분하는 데 쓴다.
    public sealed record BrainwaveBand(string Name, string Symbol, string Description, string Range, double MinimumBeat)
    {
        // 하한이 높은 대역부터 적는다. FromBeat는 비트가 처음으로 하한 이상이 되는 대역을 고른다.
        private static readonly BrainwaveBand[] Bands =
        {
            new("Above gamma", "γ+", "Beyond the gamma band", "over 42 Hz", 42.01),
            new("Gamma", "γ", "High focus", "38-42 Hz", 38.01),
            new("Beta", "β", "Active thinking", "12-38 Hz", 12.01),
            new("Alpha", "α", "Relaxation", "8-12 Hz", 8.01),
            new("Theta", "θ", "Meditation", "3-8 Hz", 3.01),
            new("Delta", "δ", "Sleep", "0.5-3 Hz", 0.51),
            new("Infra-Low", "∿", "Deep healing", "under 0.5 Hz", 0.01),
        };

        /// <summary>
        /// 좌우 주파수가 같아 비트가 생기지 않는 상태.
        /// </summary>
        public static BrainwaveBand Mono { get; } = new("Mono", "=", "No beat", "0 Hz", 0);

        public static BrainwaveBand FromBeat(double beat) =>
            Bands.FirstOrDefault(band => beat >= band.MinimumBeat) ?? Mono;
    }
}
