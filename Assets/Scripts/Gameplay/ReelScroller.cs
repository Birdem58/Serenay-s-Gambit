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

        private RectTransform _container;
        private readonly List<Image> _cellImages = new List<Image>();
        private readonly List<TextMeshProUGUI> _cellTexts = new List<TextMeshProUGUI>();

        private float _offset;
        private float _currentScrollY;
        private bool _isSpinning;
        private float _spinSpeed;
        private int _stopIndex;

        public bool IsAnimating { get; private set; }
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
            Func<SymbolKind, Color> colorFunc)
        {
            _column = column;
            _strip = (SymbolKind[])strip.Clone();
            _labelFunc = labelFunc;
            _colorFunc = colorFunc;

            // 1. Add RectMask2D to this Reel GameObject to clip overflow
            if (gameObject.GetComponent<RectMask2D>() == null)
            {
                gameObject.AddComponent<RectMask2D>();
            }

            // 2. Create the scrolling container
            var containerObj = new GameObject("ReelStripContent", typeof(RectTransform));
            containerObj.transform.SetParent(transform, false);
            _container = containerObj.GetComponent<RectTransform>();

            // Setup container anchors to stretch horizontally, anchor to top vertically
            _container.anchorMin = new Vector2(0f, 1f);
            _container.anchorMax = new Vector2(1f, 1f);
            _container.pivot = new Vector2(0.5f, 1f);
            _container.anchoredPosition = Vector2.zero;
            _container.sizeDelta = new Vector2(0f, 1000f); // Arbitrary height, will position children manually

            // 3. Find existing 3 cells
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

            // Move existing cells to the container and duplicate to get continuous strip cells
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

                // Position cell at pos1 - i * _offset
                cell.anchoredPosition = new Vector2(0f, pos1 - i * _offset);

                // Cache image and text components
                var image = cell.GetComponent<Image>();
                var text = cell.Find("SymbolText")?.GetComponent<TextMeshProUGUI>();

                _cellImages.Add(image);
                _cellTexts.Add(text);
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
            _currentScrollY = 0f;
            _container.anchoredPosition = Vector2.zero;
            _isSpinning = false;
            IsAnimating = false;
            _stopIndex = 0;

            for (int i = 0; i < _cellTexts.Count; i++)
            {
                if (_cellTexts[i] != null) _cellTexts[i].text = "?";
                if (_cellImages[i] != null) _cellImages[i].color = new Color(0.88f, 0.88f, 0.88f, 1f);
            }
        }

        public void UpdateCellVisuals()
        {
            if (_strip == null || _strip.Length == 0) return;

            for (int i = 0; i < _cellTexts.Count; i++)
            {
                var symbol = _strip[i % _strip.Length];
                if (_cellTexts[i] != null && _labelFunc != null)
                {
                    _cellTexts[i].text = _labelFunc(symbol);
                }
                if (_cellImages[i] != null && _colorFunc != null)
                {
                    _cellImages[i].color = _colorFunc(symbol);
                }
            }
        }

        public void StartSpin(float speed)
        {
            _spinSpeed = speed;
            _isSpinning = true;
            IsAnimating = true;
            UpdateCellVisuals(); // Ensure latest upgrades/colors are visible
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

                // Cubic ease out deceleration
                float easedT = 1f - Mathf.Pow(1f - t, 3f);
                float y = Mathf.Lerp(startY, totalTargetY, easedT);

                _currentScrollY = y % (_strip.Length * _offset);
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
