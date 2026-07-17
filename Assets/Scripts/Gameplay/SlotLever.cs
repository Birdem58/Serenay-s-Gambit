using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace SerenaysGambit
{
    public class SlotLever : MonoBehaviour, IPointerClickHandler
    {
        [Header("References")]
        public Image leverImage;

        [Header("Settings")]
        public float fadeDuration = 0.5f;
        public Color availableColor = new Color(0.2f, 0.2f, 0.2f, 0.2f); // Faded out black (light grey)
        public Color unavailableColor = Color.black; // Solid black

        public Action OnPulled;

        private bool _isAvailable = true;
        private float _fadeTimer = 0f;

        public bool IsAvailable
        {
            get => _isAvailable;
            set
            {
                if (_isAvailable != value)
                {
                    _isAvailable = value;
                    if (!_isAvailable)
                    {
                        // Reset to solid black immediately when unavailable
                        if (leverImage != null)
                        {
                            leverImage.color = unavailableColor;
                        }
                    }
                    else
                    {
                        // Start fading out when it becomes available
                        _fadeTimer = 0f;
                    }
                }
            }
        }

        private void Start()
        {
            if (leverImage == null)
            {
                leverImage = GetComponent<Image>();
            }

            if (leverImage != null)
            {
                leverImage.color = _isAvailable ? availableColor : unavailableColor;
            }
        }

        private void Update()
        {
            if (_isAvailable && leverImage != null && leverImage.color != availableColor)
            {
                _fadeTimer += Time.deltaTime;
                float t = Mathf.Clamp01(_fadeTimer / fadeDuration);
                leverImage.color = Color.Lerp(unavailableColor, availableColor, t);
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_isAvailable && OnPulled != null)
            {
                OnPulled.Invoke();
            }
        }
    }
}
