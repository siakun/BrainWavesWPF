using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BrainWaves.Model
{
    /// <summary>
    /// 좌/우 주파수와 그 차이(공명)로 정해지는 뇌파 대역을 포함한 뇌파 프리셋 정의
    /// </summary>
    public partial class PresetData : ObservableObject
    {
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

        public PresetData(string presetName, double leftWave, double rightWave)
        {
            this.presetName = presetName;
            this.leftWave = leftWave;
            this.rightWave = rightWave;
        }
    }
}
