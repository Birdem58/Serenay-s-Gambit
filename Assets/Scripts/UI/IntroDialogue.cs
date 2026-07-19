using UnityEngine;

namespace SerenaysGambit
{
    [System.Serializable]
    public class DialogueLine
    {
        [TextArea(3, 10)]
        public string text;
    }

    [CreateAssetMenu(fileName = "NewIntroDialogue", menuName = "Serenay's Gambit/Intro Dialogue")]
    public class IntroDialogue : ScriptableObject
    {
        public DialogueLine[] lines;
    }
}
