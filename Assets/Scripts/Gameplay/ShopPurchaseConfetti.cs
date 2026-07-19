using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

namespace SerenaysGambit
{
    public sealed class ShopPurchaseConfetti : MonoBehaviour
    {
        [Header("Prefab & Pool Settings")]
        [SerializeField] private GameObject _confettiPrefab;
        [SerializeField] private int _poolSize = 15;

        [Header("Animation Settings")]
        [SerializeField] private float _minDuration = 0.8f;
        [SerializeField] private float _maxDuration = 1.4f;
        [SerializeField] private float _minDistance = 1.5f;
        [SerializeField] private float _maxDistance = 3.5f;
        [SerializeField] private float _minScale = 0.4f;
        [SerializeField] private float _maxScale = 1.0f;

        private List<GameObject> _pool = new List<GameObject>();
        private readonly List<Tween> _activeTweens = new List<Tween>();

        private void Start()
        {
            InitializePool();
            SlotGameController.OnShopPurchaseSuccess += PlayConfettiEffect;
        }

        private void OnDestroy()
        {
            SlotGameController.OnShopPurchaseSuccess -= PlayConfettiEffect;
            KillAllTweens();
        }

        private void InitializePool()
        {
            if (_confettiPrefab == null)
            {
                Debug.LogWarning("ShopPurchaseConfetti: Confetti prefab is not assigned.");
                return;
            }

            for (int i = 0; i < _poolSize; i++)
            {
                CreateNewPoolObject();
            }
        }

        private GameObject CreateNewPoolObject()
        {
            if (_confettiPrefab == null) return null;

            GameObject obj = Instantiate(_confettiPrefab, transform.parent);
            obj.SetActive(false);
            _pool.Add(obj);
            return obj;
        }

        private GameObject GetPooledObject()
        {
            for (int i = 0; i < _pool.Count; i++)
            {
                if (_pool[i] != null && !_pool[i].activeSelf)
                {
                    return _pool[i];
                }
            }

            // Grow pool if exhausted
            return CreateNewPoolObject();
        }

        private void KillAllTweens()
        {
            for (int i = _activeTweens.Count - 1; i >= 0; i--)
            {
                var tween = _activeTweens[i];
                if (tween != null && tween.IsActive())
                {
                    tween.Kill();
                }
            }
            _activeTweens.Clear();
        }

        [ContextMenu("Test Confetti")]
        public void PlayConfettiEffect()
        {
            if (_confettiPrefab == null)
            {
                Debug.LogError("ShopPurchaseConfetti: Prefab reference is missing!");
                return;
            }

            int spawnCount = _poolSize;
            for (int i = 0; i < spawnCount; i++)
            {
                GameObject confetti = GetPooledObject();
                if (confetti == null) continue;

                // Set initial position to this object's position (Serenay transform)
                confetti.transform.position = transform.position;
                confetti.transform.localScale = Vector3.zero;
                confetti.transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
                
                confetti.SetActive(true);

                // Calculate random destination
                float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                float distance = Random.Range(_minDistance, _maxDistance);
                Vector3 offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * distance;
                Vector3 targetPosition = transform.position + offset;

                float duration = Random.Range(_minDuration, _maxDuration);
                float targetScale = Random.Range(_minScale, _maxScale);

                // Create DOTween sequence
                Sequence seq = DOTween.Sequence();
                
                // Scale up
                seq.Join(confetti.transform.DOScale(targetScale, duration * 0.25f).SetEase(Ease.OutBack));
                
                // Move outward
                seq.Join(confetti.transform.DOMove(targetPosition, duration).SetEase(Ease.OutQuad));
                
                // Rotate
                float rotationAmount = Random.Range(180f, 540f) * (Random.value > 0.5f ? 1f : -1f);
                seq.Join(confetti.transform.DORotate(new Vector3(0f, 0f, confetti.transform.eulerAngles.z + rotationAmount), duration, RotateMode.FastBeyond360).SetEase(Ease.OutQuad));

                // Scale down near the end
                seq.Insert(duration * 0.7f, confetti.transform.DOScale(0f, duration * 0.3f).SetEase(Ease.InQuad));

                // Clean up when done
                seq.OnComplete(() =>
                {
                    confetti.SetActive(false);
                    _activeTweens.Remove(seq);
                });

                seq.SetLink(confetti);
                _activeTweens.Add(seq);
                seq.Play();
            }
        }
    }
}
