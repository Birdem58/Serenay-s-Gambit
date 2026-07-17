using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SerenaysGambit
{
    [RequireComponent(typeof(Image))]
    [RequireComponent(typeof(CanvasGroup))]
    [RequireComponent(typeof(UpgradeTooltipTrigger))]
    public sealed class OwnedUpgradeView : MonoBehaviour
    {
        private Image _icon;
        private TextMeshProUGUI _fallbackLabel;
        private TextMeshProUGUI _countLabel;
        private CanvasGroup _canvasGroup;
        private UpgradeTooltipTrigger _tooltipTrigger;
        private Sprite _fallbackSprite;
        private Coroutine _fadeRoutine;

        private void Awake()
        {
            EnsureReferences();
        }

        public void Bind(
            Sprite icon,
            string fallbackLabel,
            Color fallbackColor,
            int ownedCount,
            string title,
            string description,
            string currentEffect,
            UpgradeTooltip tooltip)
        {
            EnsureReferences();
            CancelFade();

            var hasCustomIcon = icon != null;
            _icon.sprite = hasCustomIcon ? icon : _fallbackSprite;
            _icon.color = hasCustomIcon ? Color.white : fallbackColor;

            if (_fallbackLabel != null)
            {
                _fallbackLabel.gameObject.SetActive(!hasCustomIcon);
                _fallbackLabel.text = fallbackLabel;
            }

            if (_countLabel != null)
            {
                _countLabel.text = "x" + ownedCount;
            }

            if (_tooltipTrigger != null)
            {
                _tooltipTrigger.Bind(tooltip, title, description, "Owned: x" + ownedCount + "\n" + currentEffect);
            }
        }

        public void FadeOutAndDestroy(float duration)
        {
            EnsureReferences();
            if (_fadeRoutine != null)
            {
                StopCoroutine(_fadeRoutine);
            }

            _fadeRoutine = StartCoroutine(FadeOut(duration));
        }

        private IEnumerator FadeOut(float duration)
        {
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.interactable = false;
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                _canvasGroup.alpha = 1f - Mathf.Clamp01(elapsed / duration);
                yield return null;
            }

            Destroy(gameObject);
        }

        private void CancelFade()
        {
            if (_fadeRoutine != null)
            {
                StopCoroutine(_fadeRoutine);
                _fadeRoutine = null;
            }

            _canvasGroup.alpha = 1f;
            _canvasGroup.blocksRaycasts = true;
            _canvasGroup.interactable = true;
        }

        private void EnsureReferences()
        {
            if (_icon == null)
            {
                _icon = GetComponent<Image>();
                _fallbackSprite = _icon.sprite;
            }

            if (_canvasGroup == null)
            {
                _canvasGroup = GetComponent<CanvasGroup>();
            }

            if (_tooltipTrigger == null)
            {
                _tooltipTrigger = GetComponent<UpgradeTooltipTrigger>();
            }

            if (_fallbackLabel == null)
            {
                _fallbackLabel = FindText("FallbackLabel");
            }

            if (_countLabel == null)
            {
                _countLabel = FindText("CountBadge/CountLabel");
            }
        }

        private TextMeshProUGUI FindText(string path)
        {
            var child = transform.Find(path);
            return child == null ? null : child.GetComponent<TextMeshProUGUI>();
        }
    }
}
