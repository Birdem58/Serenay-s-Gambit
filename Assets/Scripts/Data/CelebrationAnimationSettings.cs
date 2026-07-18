using UnityEngine;

namespace SerenaysGambit
{
    [CreateAssetMenu(fileName = "CelebrationAnimationSettings", menuName = "Serenay's Gambit/Celebration Animation Settings")]
    public sealed class CelebrationAnimationSettings : ScriptableObject
    {
        [Header("Threshold Congratulations")]
        [SerializeField, Min(0f)] private float _thresholdCongratulationsDuration = 3f;
        [SerializeField, Min(0f)] private float _thresholdInitialScale = 0.78f;
        [SerializeField, Min(0f)] private float _thresholdOvershootScale = 1.08f;
        [SerializeField, Min(0f)] private float _thresholdScaleUpDuration = 0.42f;
        [SerializeField, Min(0f)] private float _thresholdSettleDuration = 0.28f;
        [SerializeField, Min(0f)] private float _thresholdPunchStrength = 0.12f;
        [SerializeField, Min(0f)] private float _thresholdPunchDuration = 2.30f;
        [SerializeField, Min(1)] private int _thresholdPunchVibrato = 6;
        [SerializeField, Range(0f, 1f)] private float _thresholdPunchElasticity = 0.55f;

        [Header("MAX PLUS WIN")]
        [SerializeField, Range(0f, 1f)] private float _maxPlusOverlayOpacity = 0.50f;
        [SerializeField, Min(0f)] private float _maxPlusWinDuration = 3f;
        [SerializeField, Min(0f)] private float _maxPlusItemInitialScale = 0.45f;
        [SerializeField, Min(0f)] private float _maxPlusTitleInitialScale = 0.55f;
        [SerializeField, Min(0f)] private float _maxPlusItemOvershootScale = 1.10f;
        [SerializeField, Min(0f)] private float _maxPlusItemScaleUpDuration = 0.55f;
        [SerializeField, Min(0f)] private float _maxPlusTitleScaleUpDuration = 0.45f;
        [SerializeField, Min(0f)] private float _maxPlusItemSettleDuration = 0.25f;
        [SerializeField, Min(0f)] private float _maxPlusPunchStrength = 0.14f;
        [SerializeField, Min(1)] private int _maxPlusPunchVibrato = 7;
        [SerializeField, Range(0f, 1f)] private float _maxPlusPunchElasticity = 0.65f;

        public float ThresholdCongratulationsDuration { get { return _thresholdCongratulationsDuration; } }
        public float ThresholdInitialScale { get { return _thresholdInitialScale; } }
        public float ThresholdOvershootScale { get { return _thresholdOvershootScale; } }
        public float ThresholdScaleUpDuration { get { return _thresholdScaleUpDuration; } }
        public float ThresholdSettleDuration { get { return _thresholdSettleDuration; } }
        public float ThresholdPunchStrength { get { return _thresholdPunchStrength; } }
        public float ThresholdPunchDuration { get { return _thresholdPunchDuration; } }
        public int ThresholdPunchVibrato { get { return _thresholdPunchVibrato; } }
        public float ThresholdPunchElasticity { get { return _thresholdPunchElasticity; } }

        public float MaxPlusOverlayOpacity { get { return _maxPlusOverlayOpacity; } }
        public float MaxPlusWinDuration { get { return _maxPlusWinDuration; } }
        public float MaxPlusItemInitialScale { get { return _maxPlusItemInitialScale; } }
        public float MaxPlusTitleInitialScale { get { return _maxPlusTitleInitialScale; } }
        public float MaxPlusItemOvershootScale { get { return _maxPlusItemOvershootScale; } }
        public float MaxPlusItemScaleUpDuration { get { return _maxPlusItemScaleUpDuration; } }
        public float MaxPlusTitleScaleUpDuration { get { return _maxPlusTitleScaleUpDuration; } }
        public float MaxPlusItemSettleDuration { get { return _maxPlusItemSettleDuration; } }
        public float MaxPlusPunchStrength { get { return _maxPlusPunchStrength; } }
        public int MaxPlusPunchVibrato { get { return _maxPlusPunchVibrato; } }
        public float MaxPlusPunchElasticity { get { return _maxPlusPunchElasticity; } }
    }
}
