using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.UI;

namespace SerenaysGambit
{
    // Plays a symbol's authored AnimationClip directly on the UI image. A PlayableGraph keeps
    // this independent of a shared Animator Controller while still allowing clips to animate
    // sprites, scale, rotation, or any other properties authored for the SymbolImage object.
    [RequireComponent(typeof(Image))]
    [RequireComponent(typeof(Animator))]
    [DisallowMultipleComponent]
    public sealed class SymbolScoreAnimationPlayer : MonoBehaviour
    {
        private Image _image;
        private SpriteRenderer _spriteRenderer;
        private Animator _animator;
        private AnimationClip _scoreAnimation;
        private Sprite _defaultSprite;
        private Vector3 _defaultLocalPosition;
        private Quaternion _defaultLocalRotation;
        private Vector3 _defaultLocalScale;
        private Color _defaultColor;
        private bool _hasDefaultVisual;
        private PlayableGraph _graph;
        private AnimationClipPlayable _clipPlayable;

        private void Awake()
        {
            EnsureReferences();
        }

        public void Configure(Sprite defaultSprite, AnimationClip scoreAnimation)
        {
            EnsureReferences();
            _defaultSprite = defaultSprite;
            _scoreAnimation = scoreAnimation;

            if (!_graph.IsValid())
            {
                RestoreDefaultVisual();
            }
        }

        public bool IsPlaying
        {
            get { return _graph.IsValid(); }
        }

        public void PlayOneShot()
        {
            EnsureReferences();
            if (_scoreAnimation == null || _animator == null)
            {
                return;
            }

            StopAndRestore();

            _graph = PlayableGraph.Create(name + " Score Animation");
            _graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);

            var output = AnimationPlayableOutput.Create(_graph, "Score Animation", _animator);
            _clipPlayable = AnimationClipPlayable.Create(_graph, _scoreAnimation);
            _clipPlayable.SetTime(0d);
            output.SetSourcePlayable(_clipPlayable);
            _graph.Play();
            _clipPlayable.Pause();
        }

        public void StopAndRestore()
        {
            if (_graph.IsValid())
            {
                _graph.Destroy();
            }

            _clipPlayable = default(AnimationClipPlayable);
            RestoreDefaultVisual();
        }

        private void Update()
        {
            if (!_graph.IsValid() || !_clipPlayable.IsValid() || _scoreAnimation == null)
            {
                return;
            }

            var nextTime = _clipPlayable.GetTime() + Time.deltaTime;
            if (nextTime >= _scoreAnimation.length)
            {
                StopAndRestore();
                return;
            }

            // Keep the playable paused and advance it manually so even a clip imported with
            // looping enabled still behaves as a single score animation.
            _clipPlayable.SetTime(nextTime);
            _graph.Evaluate(0f);

            // Some authored clips target SpriteRenderer while the reel presentation uses a
            // UI Image. Mirror that binding after evaluation so both clip formats are visible.
            if (_spriteRenderer != null && _spriteRenderer.sprite != null && _image != null)
            {
                _image.sprite = _spriteRenderer.sprite;
            }
        }

        private void RestoreDefaultVisual()
        {
            if (_image != null)
            {
                _image.sprite = _defaultSprite;
                _image.color = _defaultColor;
            }

            if (_spriteRenderer != null)
            {
                _spriteRenderer.sprite = null;
            }

            if (_hasDefaultVisual)
            {
                transform.localPosition = _defaultLocalPosition;
                transform.localRotation = _defaultLocalRotation;
                transform.localScale = _defaultLocalScale;
            }
        }

        private void EnsureReferences()
        {
            if (_image == null)
            {
                _image = GetComponent<Image>();
            }

            if (_spriteRenderer == null)
            {
                _spriteRenderer = GetComponent<SpriteRenderer>();
                if (_spriteRenderer == null)
                {
                    _spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
                }
                _spriteRenderer.enabled = false;
            }

            if (_animator == null)
            {
                _animator = GetComponent<Animator>();
            }

            if (_animator != null)
            {
                _animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            }

            if (!_hasDefaultVisual && _image != null)
            {
                _defaultLocalPosition = transform.localPosition;
                _defaultLocalRotation = transform.localRotation;
                _defaultLocalScale = transform.localScale;
                _defaultColor = _image.color;
                _hasDefaultVisual = true;
            }
        }

        private void OnDisable()
        {
            StopAndRestore();
        }

        private void OnDestroy()
        {
            StopAndRestore();
        }
    }
}
