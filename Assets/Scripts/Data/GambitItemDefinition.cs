using UnityEngine;

namespace SerenaysGambit
{
    [CreateAssetMenu(fileName = "GambitItem", menuName = "Serenay's Gambit/Gambit Item Definition")]
    public sealed class GambitItemDefinition : ScriptableObject
    {
        [Tooltip("The unique kind of gambit and the gameplay effect it applies.")]
        [SerializeField] private GambitKind _kind;

        [Tooltip("The name shown on the gambit card and in the active gambits list.")]
        [SerializeField] private string _displayName;

        [Tooltip("The rules text shown to the player when choosing or inspecting this gambit.")]
        [TextArea(3, 8)]
        [SerializeField] private string _description;

        [Tooltip("Optional icon shown anywhere this gambit is represented by an icon.")]
        [SerializeField] private Sprite _icon;

        [Tooltip("Accent color used by the gambit card and active-gambit badge.")]
        [SerializeField] private Color _accentColor = Color.white;

        [Tooltip("Short label used when the gambit is displayed in a compact badge.")]
        [SerializeField] private string _shortLabel;

        [Tooltip("Payout multiplier applied by this gambit. Values stack multiplicatively per copy.")]
        [Min(1)]
        [SerializeField] private int _payoutMultiplier = 1;

        [Tooltip("Roll multiplier applied by this gambit. Used by batch gambits.")]
        [Min(1)]
        [SerializeField] private int _rollMultiplier = 1;

        [Tooltip("Output reduction percentage per matching sacrifice symbol when the gambit misses.")]
        [Range(0, 100)]
        [SerializeField] private int _sacrificePercent;

        [Tooltip("Chance percentage for the gambit's risk effect to trigger on each eligible copy.")]
        [Range(0, 100)]
        [SerializeField] private int _riskPercent;

        [Tooltip("Amount removed from the configured symbol value after a failed condition.")]
        [Min(0)]
        [SerializeField] private int _decayPerMiss = 1;

        public GambitKind Kind { get { return _kind; } }
        public string DisplayName { get { return _displayName; } }
        public string Description { get { return _description; } }
        public Sprite Icon { get { return _icon; } }
        public Color AccentColor { get { return _accentColor; } }
        public string ShortLabel { get { return _shortLabel; } }
        public int PayoutMultiplier { get { return _payoutMultiplier; } }
        public int RollMultiplier { get { return _rollMultiplier; } }
        public int SacrificePercent { get { return _sacrificePercent; } }
        public int RiskPercent { get { return _riskPercent; } }
        public int DecayPerMiss { get { return _decayPerMiss; } }

        public void Initialize(GambitKind kind, string displayName, string description)
        {
            _kind = kind;
            _displayName = displayName;
            _description = description;
        }

        public void Initialize(
            GambitKind kind,
            string displayName,
            string description,
            Sprite icon,
            Color accentColor,
            string shortLabel,
            int payoutMultiplier,
            int rollMultiplier,
            int sacrificePercent,
            int riskPercent,
            int decayPerMiss)
        {
            Initialize(kind, displayName, description);
            _icon = icon;
            _accentColor = accentColor;
            _shortLabel = shortLabel;
            _payoutMultiplier = payoutMultiplier;
            _rollMultiplier = rollMultiplier;
            _sacrificePercent = sacrificePercent;
            _riskPercent = riskPercent;
            _decayPerMiss = decayPerMiss;
        }
    }
}
