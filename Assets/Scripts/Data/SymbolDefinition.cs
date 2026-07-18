using UnityEngine;

namespace SerenaysGambit
{
    [CreateAssetMenu(fileName = "Symbol", menuName = "Serenay's Gambit/Symbol Definition")]
    public sealed class SymbolDefinition : ScriptableObject
    {
        [SerializeField] private SymbolKind _symbol;
        [SerializeField] private string _displayName;
        [SerializeField] private int _startingValue;

        [Tooltip("The single static sprite shown for this symbol on the reels.")]
        [SerializeField] private Sprite _rotationImage;

        [Tooltip("Optional one-shot clip played on this symbol when it contributes to a score. The clip should target the SymbolImage UI object.")]
        [SerializeField] private AnimationClip _scoreAnimation;

        // Kept for backwards compatibility with older symbol assets and callers. New symbol
        // assets should use RotationImage for their reel presentation.
        [SerializeField] private Sprite _icon;

        public SymbolKind Symbol { get { return _symbol; } }
        public string DisplayName { get { return _displayName; } }
        public int StartingValue { get { return _startingValue; } }
        public Sprite Icon { get { return _icon; } }
        public Sprite RotationImage { get { return _rotationImage; } }
        public AnimationClip ScoreAnimation { get { return _scoreAnimation; } }

        public void Initialize(SymbolKind symbol, string displayName, int startingValue)
        {
            _symbol = symbol;
            _displayName = displayName;
            _startingValue = startingValue;
        }

        public void Initialize(SymbolKind symbol, string displayName, int startingValue, Sprite icon)
        {
            Initialize(symbol, displayName, startingValue);
            _icon = icon;
        }

        public void Initialize(
            SymbolKind symbol,
            string displayName,
            int startingValue,
            Sprite rotationImage,
            AnimationClip scoreAnimation)
        {
            Initialize(symbol, displayName, startingValue);
            _rotationImage = rotationImage;
            _scoreAnimation = scoreAnimation;
        }

        public void Initialize(
            SymbolKind symbol,
            string displayName,
            int startingValue,
            Sprite icon,
            Sprite rotationImage,
            AnimationClip scoreAnimation)
        {
            Initialize(symbol, displayName, startingValue, rotationImage, scoreAnimation);
            _icon = icon;
        }
    }
}
