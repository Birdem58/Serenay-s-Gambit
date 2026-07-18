using UnityEngine;

namespace SerenaysGambit
{
    [CreateAssetMenu(fileName = "ShopItem", menuName = "Serenay's Gambit/Shop Item Definition")]
    public sealed class ShopItemDefinition : ScriptableObject
    {
        [Tooltip("The unique category/kind of this shop offer.")]
        [SerializeField] private ShopOfferKind _kind;

        [Tooltip("The name of the item displayed in the shop interface.")]
        [SerializeField] private string _displayName;

        [Tooltip("The description details shown to the player in the shop.")]
        [SerializeField] private string _description;

        [Tooltip("The visual icon representing this shop item.")]
        [SerializeField] private Sprite _icon;

        [Tooltip("Amount to increase the corresponding symbol's payout multiplier upon purchase.")]
        [SerializeField] private int _symbolImprovementDelta = 0;

        [Tooltip("The exact multiplier value to set for base rolls (e.g., 2 for x2, 10 for x10).")]
        [SerializeField] private int _baseRollMultiplierValue = 0;

        [Tooltip("Divisor applied to the current target money to calculate the item's purchase cost (Cost = Target / CostDivisor).")]
        [SerializeField] private int _costDivisor = 0;

        /// <summary>
        /// The category or type of shop offer.
        /// </summary>
        public ShopOfferKind Kind { get { return _kind; } }

        /// <summary>
        /// The display name of the item in the shop.
        /// </summary>
        public string DisplayName { get { return _displayName; } }

        /// <summary>
        /// A text description of what the item does.
        /// </summary>
        public string Description { get { return _description; } }

        /// <summary>
        /// Sprite icon representing the item in the shop.
        /// </summary>
        public Sprite Icon { get { return _icon; } }

        /// <summary>
        /// The amount by which a symbol's multiplier/value increases when this upgrade is purchased.
        /// </summary>
        public int SymbolImprovementDelta { get { return _symbolImprovementDelta; } }

        /// <summary>
        /// The target base roll multiplier applied when buying a starting roll multiplier upgrade.
        /// </summary>
        /// <value>E.g. 2 for x2, 10 for x10</value>
        public int BaseRollMultiplierValue { get { return _baseRollMultiplierValue; } }

        /// <summary>
        /// The divisor used to calculate the cost based on target money (Cost = Target / CostDivisor).
        /// </summary>
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
