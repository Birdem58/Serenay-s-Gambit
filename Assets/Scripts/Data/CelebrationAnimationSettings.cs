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
        [SerializeField, Min(0f)] private float _thresholdHoldDuration = 0.28f;
        [SerializeField, Min(0f)] private float _thresholdPunchScaleUpAmount = 0.12f;
        [SerializeField, Min(0f)] private float _thresholdPunchScaleUpDuration = 2.30f;

        [Header("MAX PLUS WIN")]
        [SerializeField, Range(0f, 1f)] private float _maxPlusOverlayOpacity = 0.50f;
        [SerializeField, Min(0f)] private float _maxPlusWinDuration = 3f;
        [SerializeField, Min(0f)] private float _maxPlusItemInitialScale = 0.45f;
        [SerializeField, Min(0f)] private float _maxPlusTitleInitialScale = 0.55f;
        [SerializeField, Min(0f)] private float _maxPlusItemOvershootScale = 1.10f;
        [SerializeField, Min(0f)] private float _maxPlusItemScaleUpDuration = 0.55f;
        [SerializeField, Min(0f)] private float _maxPlusTitleScaleUpDuration = 0.45f;
        [SerializeField, Min(0f)] private float _maxPlusItemHoldDuration = 0.25f;
        [SerializeField, Min(0f)] private float _maxPlusPunchScaleUpAmount = 0.14f;

        public float ThresholdCongratulationsDuration { get { return _thresholdCongratulationsDuration; } }
        public float ThresholdInitialScale { get { return _thresholdInitialScale; } }
        public float ThresholdOvershootScale { get { return _thresholdOvershootScale; } }
        public float ThresholdScaleUpDuration { get { return _thresholdScaleUpDuration; } }
        public float ThresholdHoldDuration { get { return _thresholdHoldDuration; } }
        public float ThresholdPunchScaleUpAmount { get { return _thresholdPunchScaleUpAmount; } }
        public float ThresholdPunchScaleUpDuration { get { return _thresholdPunchScaleUpDuration; } }

        public float MaxPlusOverlayOpacity { get { return _maxPlusOverlayOpacity; } }
        public float MaxPlusWinDuration { get { return _maxPlusWinDuration; } }
        public float MaxPlusItemInitialScale { get { return _maxPlusItemInitialScale; } }
        public float MaxPlusTitleInitialScale { get { return _maxPlusTitleInitialScale; } }
        public float MaxPlusItemOvershootScale { get { return _maxPlusItemOvershootScale; } }
        public float MaxPlusItemScaleUpDuration { get { return _maxPlusItemScaleUpDuration; } }
        public float MaxPlusTitleScaleUpDuration { get { return _maxPlusTitleScaleUpDuration; } }
        public float MaxPlusItemHoldDuration { get { return _maxPlusItemHoldDuration; } }
        public float MaxPlusPunchScaleUpAmount { get { return _maxPlusPunchScaleUpAmount; } }
    }
}
