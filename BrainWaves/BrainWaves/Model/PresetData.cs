using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BrainWaves.Model
{
    /// <summary>
    /// 좌/우 주파수와 그 차이(공명)로 정해지는 뇌파 대역을 포함한 뇌파 프리셋 정의.
    /// 화면에 보이는 이름은 문자열 리소스의 Preset.&lt;Id&gt;에 있다.
    /// </summary>
    public partial class PresetData : ObservableObject
    {
        /// <summary>
        /// 즐겨찾기와 순서를 설정 파일에 저장할 때, 그리고 이름을 문자열 리소스에서 찾을 때 쓰는 식별자.
        /// </summary>
        // INTENT: 표시 이름과 따로 둔다. 이름을 저장 키로 쓰면 이름의 오타를 고치거나 문구를 다듬는 순간,
        // 그리고 언어를 바꾸는 순간 사용자가 저장해 둔 즐겨찾기가 조용히 풀린다. 한 번 정한 Id는 바꾸지 않는다.
        public string Id { get; }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Resonance))]
        [NotifyPropertyChangedFor(nameof(Band))]
        private double leftWave;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Resonance))]
        [NotifyPropertyChangedFor(nameof(Band))]
        private double rightWave;

        public double Resonance => Math.Abs(LeftWave - RightWave);

        public BrainwaveBand Band => BrainwaveBand.FromBeat(Resonance);

        public PresetData(string id, double leftWave, double rightWave)
        {
            Id = id;
            this.leftWave = leftWave;
            this.rightWave = rightWave;
        }
    }
}
