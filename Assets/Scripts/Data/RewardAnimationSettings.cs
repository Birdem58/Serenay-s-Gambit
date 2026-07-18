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
        [SerializeField] private float _rewardTextRiseDistance = 90f;
        [SerializeField] private float _rewardTextFadeDuration = 0.32f;
        [SerializeField] private float _rewardTextStartScale = 0.75f;
        [SerializeField] private float _rewardTextPeakScale = 1.08f;
        [SerializeField] private float _rewardTextHorizontalSpread = 28f;
        [SerializeField] private float _multiplierStepDuration = 0.24f;
        [SerializeField] private float _queueSpeedupPerEvent = 0.08f;

        public float FirstWinPulseDuration => _firstWinPulseDuration;
        public float MinimumWinPulseDuration => _minimumWinPulseDuration;
        public int MaximumRewardPulseWaves => _maximumRewardPulseWaves;
        public int MaximumCoinFlightsPerCellPerPulse => _maximumCoinFlightsPerCellPerPulse;
        public float RewardPulseSpeedIncrease => _rewardPulseSpeedIncrease;
        public float RewardTextRiseDistance => _rewardTextRiseDistance;
        public float RewardTextFadeDuration => _rewardTextFadeDuration;
        public float RewardTextStartScale => _rewardTextStartScale;
        public float RewardTextPeakScale => _rewardTextPeakScale;
        public float RewardTextHorizontalSpread => _rewardTextHorizontalSpread;
        public float MultiplierStepDuration => _multiplierStepDuration;
        public float QueueSpeedupPerEvent => _queueSpeedupPerEvent;
    }
}
