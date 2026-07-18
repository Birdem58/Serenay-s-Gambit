using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SerenaysGambit
{
    public sealed class ReelScroller : MonoBehaviour
    {
        private int _column;
        private SymbolKind[] _strip;
        private Func<SymbolKind, string> _labelFunc;
        private Func<SymbolKind, Color> _colorFunc;
        private Func<SymbolKind, Sprite> _rotationImageFunc;
        private Func<SymbolKind, AnimationClip> _scoreAnimationFunc;

        private RectTransform _container;
        private readonly List<Image> _cellImages = new List<Image>();
        private readonly List<TextMeshProUGUI> _cellTexts = new List<TextMeshProUGUI>();
        private readonly List<Image> _symbolIcons = new List<Image>();
        private readonly List<SymbolScoreAnimationPlayer> _scoreAnimationPlayers = new List<SymbolScoreAnimationPlayer>();

        private float _offset;
        private float _currentScrollY;
        private bool _isSpinning;
        private float _spinSpeed;
        private int _stopIndex;

        public bool IsAnimating { get; private set; }

        public bool AreScoreAnimationsPlaying
        {
            get
            {
                for (var index = 0; index < _scoreAnimationPlayers.Count; index++)
                {
                    if (_scoreAnimationPlayers[index] != null && _scoreAnimationPlayers[index].IsPlaying)
                    {
                        return true;
                    }
                }

                return false;
            }
        }
        public int StopIndex => _stopIndex;

        public Image GetCellImage(int row)
        {
            int index = _stopIndex + row;
            if (index >= 0 && index < _cellImages.Count)
            {
                return _cellImages[index];
            }
            return null;
        }

        public TextMeshProUGUI GetCellText(int row)
        {
            int index = _stopIndex + row;
            if (index >= 0 && index < _cellTexts.Count)
            {
                return _cellTexts[index];
            }
            return null;
        }

        public void Initialize(
            int column,
            SymbolKind[] strip,
            Func<SymbolKind, string> labelFunc,
            Func<SymbolKind, Color> colorFunc,
            Func<SymbolKind, Sprite> rotationImageFunc,
            Func<SymbolKind, AnimationClip> scoreAnimationFunc = null)
        {
            _column = column;
            _strip = (SymbolKind[])strip.Clone();
            _labelFunc = labelFunc;
            _colorFunc = colorFunc;
            _rotationImageFunc = rotationImageFunc;
            _scoreAnimationFunc = scoreAnimationFunc;

            // 1. Find existing 3 cells first
            var originalCells = new List<RectTransform>();
            for (int r = 1; r <= 3; r++)
            {
                var cellTransform = transform.Find("Cell_R" + (_column + 1) + "_" + r);
                if (cellTransform != null)
                {
                    originalCells.Add(cellTransform.GetComponent<RectTransform>());
                }
            }

            if (originalCells.Count < 3)
            {
                Debug.LogError("ReelScroller: Could not find 3 original cells on " + name);
                return;
            }

            // Calculate spacing offset from original cells local positions
            float pos1 = originalCells[0].localPosition.y;
            float pos2 = originalCells[1].localPosition.y;
            _offset = pos1 - pos2;

            // Remove RectMask2D from main Reel GameObject to avoid nested masking issues
            var mainMask = gameObject.GetComponent<RectMask2D>();
            if (mainMask != null)
            {
                Destroy(mainMask);
            }

            // 2. Create Viewport child GameObject to constrain visible area to exactly 3 cells
            var viewportObj = new GameObject("Viewport", typeof(RectTransform));
            viewportObj.transform.SetParent(transform, false);
            var viewportRect = viewportObj.GetComponent<RectTransform>();
            viewportRect.anchorMin = new Vector2(0f, 0.5f);
            viewportRect.anchorMax = new Vector2(1f, 0.5f);
            viewportRect.pivot = new Vector2(0.5f, 0.5f);
            viewportRect.sizeDelta = new Vector2(0f, 3f * _offset);
            viewportRect.anchoredPosition = new Vector2(0f, pos1 - _offset);

            // Add RectMask2D to viewport so only these 3 cells are shown
            viewportObj.AddComponent<RectMask2D>();

            // 3. Create the scrolling container under viewport
            var containerObj = new GameObject("ReelStripContent", typeof(RectTransform));
            containerObj.transform.SetParent(viewportObj.transform, false);
            _container = containerObj.GetComponent<RectTransform>();

            // Setup container anchors to stretch horizontally, anchor to top vertically relative to Viewport
            _container.anchorMin = new Vector2(0f, 1f);
            _container.anchorMax = new Vector2(1f, 1f);
            _container.pivot = new Vector2(0.5f, 1f);
            _container.anchoredPosition = Vector2.zero;
            _container.sizeDelta = new Vector2(0f, 1000f);

            // 4. Move existing cells to the container and duplicate to get continuous strip cells
            int numCells = _strip.Length * 2;
            for (int i = 0; i < numCells; i++)
            {
                RectTransform cell;
                if (i < 3)
                {
                    cell = originalCells[i];
                    cell.SetParent(_container, false);
                }
                else
                {
                    cell = Instantiate(originalCells[0], _container);
                    cell.name = "Cell_R" + (_column + 1) + "_Copy" + (i + 1);
                }

                // Set pivot to center, and anchor to top-stretch relative to container
                cell.anchorMin = new Vector2(0.06f, 1f);
                cell.anchorMax = new Vector2(0.94f, 1f);
                cell.pivot = new Vector2(0.5f, 0.5f);
                // The scene cells get their height from their original vertical anchors.
                // After moving them under the top-anchored strip content those anchors no
                // longer contribute any height, so keep one reel step as an explicit size.
                cell.sizeDelta = new Vector2(0f, _offset);

                // Position cell at (-0.5f - i) * _offset so it aligns with rows 0, 1, 2
                cell.anchoredPosition = new Vector2(0f, (-0.5f - i) * _offset);

                // Cache image and text components
                var image = cell.GetComponent<Image>();
                var text = cell.Find("SymbolText")?.GetComponent<TextMeshProUGUI>();

                _cellImages.Add(image);
                _cellTexts.Add(text);

                // Add or find SymbolImage component for custom sprites
                var iconTrans = cell.Find("SymbolImage");
                Image iconImg = null;
                if (iconTrans == null)
                {
                    var iconObj = new GameObject("SymbolImage", typeof(RectTransform));
                    iconObj.transform.SetParent(cell, false);
                    var rect = iconObj.GetComponent<RectTransform>();
                    rect.anchorMin = new Vector2(0f, 0f);
                    rect.anchorMax = new Vector2(1f, 1f);
                    rect.offsetMin = new Vector2(15f, 15f);
                    rect.offsetMax = new Vector2(-15f, -15f);
                    iconImg = iconObj.AddComponent<Image>();
                    iconImg.preserveAspect = true;
                }
                else
                {
                    iconImg = iconTrans.GetComponent<Image>();
                }

                if (iconImg == null)
                {
                    iconImg = iconTrans.gameObject.AddComponent<Image>();
                }

                iconImg.preserveAspect = true;
                _symbolIcons.Add(iconImg);

                var animationPlayer = iconImg.GetComponent<SymbolScoreAnimationPlayer>();
                if (animationPlayer == null)
                {
                    animationPlayer = iconImg.gameObject.AddComponent<SymbolScoreAnimationPlayer>();
                }
                _scoreAnimationPlayers.Add(animationPlayer);
            }

            ResetToStart();
        }

        public void ResetToStart()
        {
            _currentScrollY = 0f;
            _container.anchoredPosition = Vector2.zero;
            _isSpinning = false;
            IsAnimating = false;
            _stopIndex = 0;
            UpdateCellVisuals();
        }

        public void Clear()
        {
            StopScoreAnimations();
            _currentScrollY = 0f;
            _container.anchoredPosition = Vector2.zero;
            _isSpinning = false;
            IsAnimating = false;
            _stopIndex = 0;

            for (int i = 0; i < _cellTexts.Count; i++)
            {
                if (_cellTexts[i] != null)
                {
                    _cellTexts[i].text = "?";
                    _cellTexts[i].gameObject.SetActive(true);
                }
                if (_cellImages[i] != null) _cellImages[i].color = new Color(0.88f, 0.88f, 0.88f, 1f);
                if (i < _symbolIcons.Count && _symbolIcons[i] != null) _symbolIcons[i].gameObject.SetActive(false);
            }
        }

        public void UpdateStrip(SymbolKind[] newStrip)
        {
            StopScoreAnimations();
            _strip = (SymbolKind[])newStrip.Clone();
            UpdateCellVisuals();
        }

        public void UpdateCellVisuals()
        {
            if (_strip == null || _strip.Length == 0) return;

            for (int i = 0; i < _cellTexts.Count; i++)
            {
                var symbol = _strip[i % _strip.Length];
                var sprite = _rotationImageFunc?.Invoke(symbol);
                var scoreAnimation = _scoreAnimationFunc?.Invoke(symbol);

                if (i < _scoreAnimationPlayers.Count && _scoreAnimationPlayers[i] != null)
                {
                    _scoreAnimationPlayers[i].Configure(sprite, scoreAnimation);
                }

                if (sprite != null)
                {
                    if (_cellTexts[i] != null) _cellTexts[i].gameObject.SetActive(false);
                    if (i < _symbolIcons.Count && _symbolIcons[i] != null)
                    {
                        _symbolIcons[i].sprite = sprite;
                        _symbolIcons[i].gameObject.SetActive(true);
                    }
                }
                else
                {
                    if (_cellTexts[i] != null)
                    {
                        _cellTexts[i].text = _labelFunc != null ? _labelFunc(symbol) : symbol.ToString();
                        _cellTexts[i].gameObject.SetActive(true);
                    }
                    if (i < _symbolIcons.Count && _symbolIcons[i] != null)
                    {
                        _symbolIcons[i].gameObject.SetActive(false);
                    }
                }

                if (_cellImages[i] != null && _colorFunc != null)
                {
                    _cellImages[i].color = _colorFunc(symbol);
                }
            }
        }

        public void StartSpin(float speed)
        {
            StopScoreAnimations();
            _spinSpeed = speed;
            _isSpinning = true;
            IsAnimating = true;
            UpdateCellVisuals(); // Ensure latest upgrades/colors are visible
        }

        public void PlayScoreAnimation(int row)
        {
            int index = _stopIndex + row;
            if (index >= 0 && index < _scoreAnimationPlayers.Count)
            {
                _scoreAnimationPlayers[index].PlayOneShot();
            }
        }

        public void StopScoreAnimations()
        {
            for (var index = 0; index < _scoreAnimationPlayers.Count; index++)
            {
                if (_scoreAnimationPlayers[index] != null)
                {
                    _scoreAnimationPlayers[index].StopAndRestore();
                }
            }
        }

        public void StopSpin(int targetStopIndex, float duration, Action onComplete)
        {
            StartCoroutine(StopCoroutine(targetStopIndex, duration, onComplete));
        }

        private IEnumerator StopCoroutine(int targetStopIndex, float duration, Action onComplete)
        {
            _isSpinning = false;
            _stopIndex = targetStopIndex;

            float startY = _currentScrollY;
            float targetY = targetStopIndex * _offset;

            // Compute distance to target Y, ensuring we move forward in Y
            float diff = targetY - startY;
            if (diff < 0)
            {
                diff += _strip.Length * _offset;
            }

            // Add 2 full spins of padding for a nice rolling feel
            float totalTargetY = startY + diff + 2f * (_strip.Length * _offset);

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                // Custom EaseOutBack function for elastic snapping feel (overshoot then pull back)
                const float c1 = 1.2f; 
                const float c3 = c1 + 1f;
                float easedT = 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);

                float y = Mathf.LerpUnclamped(startY, totalTargetY, easedT);

                float maxScroll = _strip.Length * _offset;
                _currentScrollY = ((y % maxScroll) + maxScroll) % maxScroll;
                _container.anchoredPosition = new Vector2(0f, _currentScrollY);

                yield return null;
            }

            // Hard snap to final target to ensure precision
            _currentScrollY = targetY;
            _container.anchoredPosition = new Vector2(0f, targetY);
            IsAnimating = false;

            onComplete?.Invoke();
        }

        private void Update()
        {
            if (_isSpinning)
            {
                _currentScrollY += _spinSpeed * Time.deltaTime;
                _currentScrollY = _currentScrollY % (_strip.Length * _offset);
                _container.anchoredPosition = new Vector2(0f, _currentScrollY);
            }
        }
    }
}
