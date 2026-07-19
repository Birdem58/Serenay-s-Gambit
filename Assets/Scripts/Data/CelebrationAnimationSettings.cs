using UnityEngine;

namespace SerenaysGambit
{
    [CreateAssetMenu(fileName = "CelebrationAnimationSettings", menuName = "Serenay's Gambit/Celebration Animation Settings")]
    public sealed class CelebrationAnimationSettings : ScriptableObject
    {
        [Header("MAX PLUS WIN")]
        [SerializeField, Range(0f, 1f)] private float _maxPlusOverlayOpacity = 0.50f;
        [SerializeField, Min(0f)] private float _maxPlusWinDuration = 3f;
        [SerializeField, Min(0f)] private float _maxPlusItemInitialScale = 0.45f;
        [SerializeField, Min(0f)] private float _maxPlusTitleInitialScale = 0.55f;
        [SerializeField, Min(0f)] private float _maxPlusItemOvershootScale = 1.10f;
        [SerializeField, Min(0f)] private float _maxPlusItemScaleUpDuration = 0.55f;
        [SerializeField, Min(0f)] private float _maxPlusTitleScaleUpDuration = 0.45f;
        [SerializeField, Min(0f)] private float _maxPlusItemHoldDuration = 0.25f;
        [SerializeField, Min(0f)] private float _maxPlusItemScaleDownDuration = 0.30f;
        [SerializeField, Min(0f)] private float _maxPlusPunchScaleUpAmount = 0.14f;

        public float MaxPlusOverlayOpacity { get { return _maxPlusOverlayOpacity; } }
        public float MaxPlusWinDuration { get { return _maxPlusWinDuration; } }
        public float MaxPlusItemInitialScale { get { return _maxPlusItemInitialScale; } }
        public float MaxPlusTitleInitialScale { get { return _maxPlusTitleInitialScale; } }
        public float MaxPlusItemOvershootScale { get { return _maxPlusItemOvershootScale; } }
        public float MaxPlusItemScaleUpDuration { get { return _maxPlusItemScaleUpDuration; } }
        public float MaxPlusTitleScaleUpDuration { get { return _maxPlusTitleScaleUpDuration; } }
        public float MaxPlusItemHoldDuration { get { return _maxPlusItemHoldDuration; } }
        public float MaxPlusItemScaleDownDuration { get { return _maxPlusItemScaleDownDuration; } }
        public float MaxPlusPunchScaleUpAmount { get { return _maxPlusPunchScaleUpAmount; } }
    }
}
