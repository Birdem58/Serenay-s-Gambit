using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SerenaysGambit
{
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class UpgradeTooltip : MonoBehaviour
    {
        private const float PointerOffset = 16f;

        private CanvasGroup _canvasGroup;
        private Canvas _canvas;
        private RectTransform _canvasRect;
        private RectTransform _panel;
        private TextMeshProUGUI _titleText;
        private TextMeshProUGUI _descriptionText;
        private TextMeshProUGUI _detailsText;
        private UpgradeTooltipTrigger _source;
        private Vector2 _pointerPosition;

        private void Awake()
        {
            EnsureReferences();
        }

        public void Show(UpgradeTooltipTrigger source, string title, string description, string details, Vector2 pointerPosition)
        {
            EnsureReferences();
            if (_canvas == null || _panel == null)
            {
                return;
            }

            _source = source;
            _pointerPosition = pointerPosition;
            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }

            _canvasGroup.alpha = 1f;
            UpdateText(title, description, details);

            LayoutRebuilder.ForceRebuildLayoutImmediate(_panel);
            PositionAt(pointerPosition);
        }

        public void Refresh(UpgradeTooltipTrigger source, string title, string description, string details)
        {
            EnsureReferences();
            if (source == null || source != _source || !gameObject.activeSelf || _panel == null)
            {
                return;
            }

            UpdateText(title, description, details);
            LayoutRebuilder.ForceRebuildLayoutImmediate(_panel);
            PositionAt(_pointerPosition);
        }

        private void UpdateText(string title, string description, string details)
        {
            if (_titleText != null)
            {
                _titleText.text = title ?? string.Empty;
            }

            if (_descriptionText != null)
            {
                _descriptionText.text = description ?? string.Empty;
            }

            if (_detailsText != null)
            {
                _detailsText.text = details ?? string.Empty;
            }
        }

        public void Hide(UpgradeTooltipTrigger source = null)
        {
            if (source != null && source != _source)
            {
                return;
            }

            _source = null;
            if (gameObject.activeSelf)
            {
                gameObject.SetActive(false);
            }
        }

        private void PositionAt(Vector2 screenPosition)
        {
            Camera eventCamera = _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera;
            Vector2 localPointer;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, screenPosition, eventCamera, out localPointer))
            {
                return;
            }

            var desired = localPointer + new Vector2(PointerOffset, -PointerOffset);
            var panelSize = _panel.rect.size;
            var canvasRect = _canvasRect.rect;
            desired.x = Mathf.Clamp(desired.x, canvasRect.xMin, canvasRect.xMax - panelSize.x);
            desired.y = Mathf.Clamp(desired.y, canvasRect.yMin + panelSize.y, canvasRect.yMax);
            _panel.anchoredPosition = desired;
        }

        private void EnsureReferences()
        {
            if (_panel == null)
            {
                _panel = transform as RectTransform;
            }

            if (_canvasGroup == null)
            {
                _canvasGroup = GetComponent<CanvasGroup>();
            }

            if (_canvas == null)
            {
                _canvas = GetComponentInParent<Canvas>();
                _canvasRect = _canvas == null ? null : _canvas.transform as RectTransform;
            }

            if (_titleText == null)
            {
                _titleText = FindText("Title");
            }

            if (_descriptionText == null)
            {
                _descriptionText = FindText("Description");
            }

            if (_detailsText == null)
            {
                _detailsText = FindText("Details");
            }
        }

        private TextMeshProUGUI FindText(string name)
        {
            var child = transform.Find(name);
            return child == null ? null : child.GetComponent<TextMeshProUGUI>();
        }
    }
}
