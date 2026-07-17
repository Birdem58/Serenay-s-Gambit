using UnityEngine;

namespace SerenaysGambit
{
    [CreateAssetMenu(fileName = "ShopItem", menuName = "Serenay's Gambit/Shop Item Definition")]
    public sealed class ShopItemDefinition : ScriptableObject
    {
        [SerializeField] private ShopOfferKind _kind;
        [SerializeField] private string _displayName;
        [SerializeField] private string _description;

        public ShopOfferKind Kind { get { return _kind; } }
        public string DisplayName { get { return _displayName; } }
        public string Description { get { return _description; } }

        public void Initialize(ShopOfferKind kind, string displayName, string description)
        {
            _kind = kind;
            _displayName = displayName;
            _description = description;
        }
    }
}
