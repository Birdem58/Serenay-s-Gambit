using UnityEngine;

namespace SerenaysGambit
{
    [CreateAssetMenu(fileName = "Symbol", menuName = "Serenay's Gambit/Symbol Definition")]
    public sealed class SymbolDefinition : ScriptableObject
    {
        [SerializeField] private SymbolKind _symbol;
        [SerializeField] private string _displayName;
        [SerializeField] private int _startingValue;

        public SymbolKind Symbol { get { return _symbol; } }
        public string DisplayName { get { return _displayName; } }
        public int StartingValue { get { return _startingValue; } }

        public void Initialize(SymbolKind symbol, string displayName, int startingValue)
        {
            _symbol = symbol;
            _displayName = displayName;
            _startingValue = startingValue;
        }
    }
}
