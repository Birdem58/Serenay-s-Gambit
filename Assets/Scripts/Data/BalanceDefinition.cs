using UnityEngine;

namespace SerenaysGambit
{
    [CreateAssetMenu(fileName = "Balance", menuName = "Serenay's Gambit/Balance Definition")]
    public sealed class BalanceDefinition : ScriptableObject
    {
        [SerializeField] private int _baseRolls = GameBalance.BaseRolls;
        [SerializeField] private int _organCount = GameBalance.OrganCount;
        [SerializeField] private int _thresholdCount = GameBalance.MaxThresholdLevel;
        [SerializeField] private int _freeSpinBundle = GameBalance.FreeSpinBundle;

        public int BaseRolls { get { return _baseRolls; } }
        public int OrganCount { get { return _organCount; } }
        public int ThresholdCount { get { return _thresholdCount; } }
        public int FreeSpinBundle { get { return _freeSpinBundle; } }

        public void Initialize(int baseRolls, int organCount, int thresholdCount, int freeSpinBundle)
        {
            _baseRolls = baseRolls;
            _organCount = organCount;
            _thresholdCount = thresholdCount;
            _freeSpinBundle = freeSpinBundle;
        }
    }
}
