using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace SerenaysGambit
{
    public class IntroDialogueController : MonoBehaviour
    {
        [Header("Dialogue Content")]
        [SerializeField] private IntroDialogue introDialogue;

        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI dialogueText;
        [SerializeField] private Image characterPlaceholder; // White rectangle placeholder
        [SerializeField] private GameObject gameplayContent; // MainContent of the Slot game
        [SerializeField] private SlotLever slotLever;
        [SerializeField] private CanvasGroup introCanvasGroup;

        [Header("Video Settings")]
        [SerializeField] private VideoPlayer videoPlayer;
        [SerializeField] private RawImage videoRawImage;

        [Header("Typewriter Settings")]
        [SerializeField] private float normalCharDelay = 0.04f;
        [SerializeField] private float commaDelay = 0.25f;
        [SerializeField] private float sentenceEndDelay = 0.5f;

        private int _currentLineIndex = 0;
        private bool _isVideoPlaying = false;
        private Coroutine _typewriterCoroutine;
        private bool _isTyping = false;
        private string _targetText = "";
        private bool _isTransitioning = false;

        private void Start()
        {
            _isTransitioning = false;

            if (introCanvasGroup == null)
            {
                introCanvasGroup = GetComponent<CanvasGroup>();
            }
            if (introCanvasGroup == null)
            {
                introCanvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
            introCanvasGroup.alpha = 1f;

            if (slotLever == null)
            {
                slotLever = FindObjectOfType<SlotLever>();
            }

            if (gameplayContent != null)
            {
                gameplayContent.SetActive(false); // Hide gameplay UI
            }

            if (introDialogue == null || introDialogue.lines == null || introDialogue.lines.Length == 0)
            {
                FinishIntro();
                return;
            }

            gameObject.SetActive(true);

            if (videoPlayer != null && videoRawImage != null && videoPlayer.clip != null)
            {
                if (dialogueText != null) dialogueText.gameObject.SetActive(false);
                if (characterPlaceholder != null) characterPlaceholder.gameObject.SetActive(false);
                videoRawImage.gameObject.SetActive(true);

                _isVideoPlaying = true;
                videoPlayer.prepareCompleted += OnVideoPrepared;
                videoPlayer.loopPointReached += OnVideoFinished;
                videoPlayer.Prepare();
            }
            else
            {
                if (videoRawImage != null) videoRawImage.gameObject.SetActive(false);
                StartDialogueLine(0);
            }
        }

        private void OnVideoPrepared(VideoPlayer vp)
        {
            vp.prepareCompleted -= OnVideoPrepared;
            if (videoRawImage != null)
            {
                videoRawImage.texture = vp.texture;
            }
            vp.Play();
        }

        private void OnVideoFinished(VideoPlayer vp)
        {
            if (videoPlayer != null)
            {
                videoPlayer.prepareCompleted -= OnVideoPrepared;
                videoPlayer.loopPointReached -= OnVideoFinished;
                videoPlayer.Stop();
            }

            if (_isVideoPlaying)
            {
                _isVideoPlaying = false;

                if (videoRawImage != null) videoRawImage.gameObject.SetActive(false);
                if (dialogueText != null) dialogueText.gameObject.SetActive(true);
                if (characterPlaceholder != null) characterPlaceholder.gameObject.SetActive(true);

                StartDialogueLine(0);
            }
        }

        private void Update()
        {
            if (_isTransitioning) return;

            if (_isVideoPlaying)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    OnVideoFinished(videoPlayer);
                }
                return;
            }

            // Detect mouse click or screen tap (LMB)
            if (Input.GetMouseButtonDown(0))
            {
                HandlePlayerClick();
            }
        }

        private void HandlePlayerClick()
        {
            if (_isTyping)
            {
                // Skip typewriter animation and show full text instantly
                if (_typewriterCoroutine != null)
                {
                    StopCoroutine(_typewriterCoroutine);
                }
                dialogueText.text = _targetText;
                _isTyping = false;
            }
            else
            {
                // Advance to next line
                _currentLineIndex++;
                if (_currentLineIndex < introDialogue.lines.Length)
                {
                    StartDialogueLine(_currentLineIndex);
                }
                else
                {
                    StartCoroutine(FinishIntroWithLeverPull());
                }
            }
        }

        private void StartDialogueLine(int index)
        {
            _targetText = introDialogue.lines[index].text;
            if (_typewriterCoroutine != null)
            {
                StopCoroutine(_typewriterCoroutine);
            }
            _typewriterCoroutine = StartCoroutine(TypeText(_targetText));
        }

        private IEnumerator TypeText(string fullText)
        {
            _isTyping = true;
            dialogueText.text = "";
            
            // Build the text character by character
            for (int i = 0; i < fullText.Length; i++)
            {
                char c = fullText[i];
                dialogueText.text += c;

                // Adjust delay based on punctuation
                float delay = normalCharDelay;
                if (c == ',' || c == ';')
                {
                    delay = commaDelay;
                }
                else if (c == '.' || c == '!' || c == '?')
                {
                    // Check if followed by ellipsis to avoid double/triple pausing
                    bool isNextEllipsisChar = (i + 1 < fullText.Length && fullText[i + 1] == '.');
                    if (!isNextEllipsisChar)
                    {
                        delay = sentenceEndDelay;
                    }
                }

                yield return new WaitForSeconds(delay);
            }

            _isTyping = false;
        }

        private IEnumerator FinishIntroWithLeverPull()
        {
            _isTransitioning = true;

            // Show gameplay UI so it's visible behind the fading intro panel
            if (gameplayContent != null)
            {
                gameplayContent.SetActive(true);
            }

            float duration = 0.5f; // Fallback duration
            Coroutine leverAnimCoroutine = null;

            if (slotLever != null)
            {
                var anim = slotLever.GetComponent<Animator>();
                if (anim != null && anim.runtimeAnimatorController != null && anim.runtimeAnimatorController.animationClips.Length > 0)
                {
                    duration = anim.runtimeAnimatorController.animationClips[0].length;
                }
                leverAnimCoroutine = StartCoroutine(slotLever.PlayPullAnimationOnly());
            }

            if (introCanvasGroup != null)
            {
                float elapsed = 0f;
                float startAlpha = introCanvasGroup.alpha;
                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    introCanvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, elapsed / duration);
                    yield return null;
                }
                introCanvasGroup.alpha = 0f;
            }

            if (leverAnimCoroutine != null)
            {
                yield return leverAnimCoroutine;
            }
            else if (introCanvasGroup == null)
            {
                yield return new WaitForSeconds(duration);
            }

            FinishIntro();
        }

        private void FinishIntro()
        {
            if (gameplayContent != null)
            {
                gameplayContent.SetActive(true); // Show gameplay UI
            }

            if (GameObject.Find("BgMusicPlayer") == null)
            {
                GameObject bgMusicObj = new GameObject("BgMusicPlayer");
                DontDestroyOnLoad(bgMusicObj);
                AudioSource audioSource = bgMusicObj.AddComponent<AudioSource>();
                audioSource.clip = Resources.Load<AudioClip>("BgMusic");
                if (audioSource.clip != null)
                {
                    audioSource.volume = 0.2f;
                    audioSource.loop = true;
                    audioSource.playOnAwake = false;
                    audioSource.Play();
                }
                else
                {
                    Debug.LogWarning("BgMusic clip not found in Resources!");
                }
            }

            gameObject.SetActive(false); // Disable the intro panel
        }
    }
}
