using UnityEngine;

namespace SerenaysGambit
{
    [CreateAssetMenu(fileName = "RewardAnimationSettings", menuName = "Serenay's Gambit/Reward Animation Settings")]
    public sealed class RewardAnimationSettings : ScriptableObject
    {
        [SerializeField] private float _firstWinPulseDuration = 0.6f;
        [SerializeField] private float _minimumWinPulseDuration = 0.025f;
        [SerializeField] private int _maximumRewardPulseWaves = 25;
        [SerializeField] private int _maximumCoinFlightsPerCellPerPulse = 12;
        [SerializeField] private float _rewardPulseSpeedIncrease = 0.08f;

        public float FirstWinPulseDuration => _firstWinPulseDuration;
        public float MinimumWinPulseDuration => _minimumWinPulseDuration;
        public int MaximumRewardPulseWaves => _maximumRewardPulseWaves;
        public int MaximumCoinFlightsPerCellPerPulse => _maximumCoinFlightsPerCellPerPulse;
        public float RewardPulseSpeedIncrease => _rewardPulseSpeedIncrease;
    }
}
