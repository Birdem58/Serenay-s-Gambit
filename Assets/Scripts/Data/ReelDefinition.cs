using UnityEngine;

namespace SerenaysGambit
{
    [CreateAssetMenu(fileName = "Reel", menuName = "Serenay's Gambit/Reel Definition")]
    public sealed class ReelDefinition : ScriptableObject
    {
        [SerializeField] private SymbolKind[] _faces = new SymbolKind[GameBalance.ReelLength];

        public SymbolKind[] Faces { get { return _faces; } }

        public void Initialize(SymbolKind[] faces)
        {
            if (faces == null || faces.Length != GameBalance.ReelLength)
            {
                throw new System.ArgumentException("A reel definition must contain exactly five faces.", nameof(faces));
            }

            _faces = (SymbolKind[])faces.Clone();
        }
    }
}
