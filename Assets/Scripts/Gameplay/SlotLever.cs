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
        public SpriteRenderer leverSprite;

        [Header("Settings")]
        public float fadeDuration = 0.5f;
        public Color availableColor = new Color(0.2f, 0.2f, 0.2f, 0.2f); // Faded out black (light grey)
        public Color unavailableColor = Color.black; // Solid black

        public Action OnPulled;

        private bool _isAvailable = true;
        private float _fadeTimer = 0f;
        private Animator _animator;
        private bool _isAnimating = false;

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
                        // Reset immediately when unavailable
                        SetColor(unavailableColor);
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
            if (leverSprite == null)
            {
                leverSprite = GetComponent<SpriteRenderer>();
            }
            _animator = GetComponent<Animator>();
            if (_animator != null)
            {
                _animator.Play("LovelyArms", 0, 0f);
                _animator.Update(0f);
                _animator.speed = 0f;
            }

            SetColor(_isAvailable ? availableColor : unavailableColor);
        }

        private void SetColor(Color color)
        {
            if (leverImage != null)
            {
                leverImage.color = color;
            }
            if (leverSprite != null)
            {
                leverSprite.color = color;
            }
        }

        private void Update()
        {
            if (_isAvailable && !_isAnimating)
            {
                Color curColor = leverImage != null ? leverImage.color : (leverSprite != null ? leverSprite.color : Color.white);
                if (curColor != availableColor)
                {
                    _fadeTimer += Time.deltaTime;
                    float t = Mathf.Clamp01(_fadeTimer / fadeDuration);
                    SetColor(Color.Lerp(unavailableColor, availableColor, t));
                }
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            TryPull();
        }

        private void OnMouseDown()
        {
            TryPull();
        }

        private void TryPull()
        {
            if (_isAvailable && !_isAnimating && OnPulled != null)
            {
                StartCoroutine(PlayPullAnimation());
            }
        }

        private System.Collections.IEnumerator PlayPullAnimation()
        {
            _isAnimating = true;

            if (OnPulled != null)
            {
                OnPulled.Invoke();
            }

            if (_animator != null)
            {
                _animator.Play("LovelyArms", 0, 0f);
                _animator.speed = 1f;

                float duration = 0.4167f;
                if (_animator.runtimeAnimatorController != null && _animator.runtimeAnimatorController.animationClips.Length > 0)
                {
                    duration = _animator.runtimeAnimatorController.animationClips[0].length;
                }

                yield return new WaitForSeconds(duration);

                _animator.Play("LovelyArms", 0, 0f);
                _animator.Update(0f);
                _animator.speed = 0f;
            }
            else
            {
                yield return null;
            }

            _isAnimating = false;
        }
    }
}
