using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BrainWaves.Model
{
    /// <summary>
    /// 좌/우 주파수와 그 차이(공명)로 정해지는 뇌파 대역을 포함한 뇌파 프리셋 정의
    /// </summary>
    public partial class PresetData : ObservableObject
    {
        /// <summary>
        /// 즐겨찾기처럼 설정 파일에 저장할 때 쓰는 식별자.
        /// </summary>
        // INTENT: 표시 이름과 따로 둔다. 이름을 저장 키로 쓰면 이름의 오타를 고치거나 문구를 다듬는 순간
        // 사용자가 저장해 둔 즐겨찾기가 조용히 풀린다. 한 번 정한 Id는 바꾸지 않는다.
        public string Id { get; }

        [ObservableProperty]
        private string presetName;

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

        public PresetData(string id, string presetName, double leftWave, double rightWave)
        {
            Id = id;
            this.presetName = presetName;
            this.leftWave = leftWave;
            this.rightWave = rightWave;
        }
    }
}
