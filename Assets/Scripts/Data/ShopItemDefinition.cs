using UnityEngine;

namespace SerenaysGambit
{
    [CreateAssetMenu(fileName = "ShopItem", menuName = "Serenay's Gambit/Shop Item Definition")]
    public sealed class ShopItemDefinition : ScriptableObject
    {
        [SerializeField] private ShopOfferKind _kind;
        [SerializeField] private string _displayName;
        [SerializeField] private string _description;
        [SerializeField] private Sprite _icon;
        [SerializeField] private int _symbolImprovementDelta = 0;
        [SerializeField] private int _baseRollMultiplierValue = 0;
        [SerializeField] private int _costDivisor = 0;

        public ShopOfferKind Kind { get { return _kind; } }
        public string DisplayName { get { return _displayName; } }
        public string Description { get { return _description; } }
        public Sprite Icon { get { return _icon; } }
        public int SymbolImprovementDelta { get { return _symbolImprovementDelta; } }
        public int BaseRollMultiplierValue { get { return _baseRollMultiplierValue; } }
        public int CostDivisor { get { return _costDivisor; } }

        public void Initialize(ShopOfferKind kind, string displayName, string description)
        {
            _kind = kind;
            _displayName = displayName;
            _description = description;
        }

        public void Initialize(ShopOfferKind kind, string displayName, string description, Sprite icon)
        {
            Initialize(kind, displayName, description);
            _icon = icon;
        }

        public void Initialize(ShopOfferKind kind, string displayName, string description, int symbolImprovementDelta, int baseRollMultiplierValue, int costDivisor)
        {
            Initialize(kind, displayName, description);
            _symbolImprovementDelta = symbolImprovementDelta;
            _baseRollMultiplierValue = baseRollMultiplierValue;
            _costDivisor = costDivisor;
        }

        public void Initialize(ShopOfferKind kind, string displayName, string description, Sprite icon, int symbolImprovementDelta, int baseRollMultiplierValue, int costDivisor)
        {
            Initialize(kind, displayName, description, icon);
            _symbolImprovementDelta = symbolImprovementDelta;
            _baseRollMultiplierValue = baseRollMultiplierValue;
            _costDivisor = costDivisor;
        }
    }
}
