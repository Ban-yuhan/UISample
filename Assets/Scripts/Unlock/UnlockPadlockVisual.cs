using System;
using DG.Tweening;
using UnityEngine;

namespace UnlockSystem
{
    /// <summary>
    /// 미해금 구역 중앙(3.5, 0.25, 0)의 자물쇠 + Lv.4 배지 비주얼 및 열림 연출을 제어합니다.
    /// </summary>
    public class UnlockPadlockVisual : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform visualRoot;
        [SerializeField] private RectTransform shackleRect;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Idle Motion")]
        [SerializeField] private float idleBobAmplitude = 0.12f;
        [SerializeField] private float idleBobDuration = 1.2f;

        [Header("Unlock Animation")]
        [SerializeField] private float shackleLiftPixels = 36f;
        [SerializeField] private float shackleTiltAngle = -18f;

        private Vector3 _rootInitialLocalPos;
        private Vector3 _rootInitialLocalScale = Vector3.one;
        private Vector2 _shackleInitialPos;
        private bool _initialized;
        private Tween _idleTween;
        private Sequence _unlockSeq;

        private void Awake()
        {
            EnsureInitialized();
            StartIdleBob();
        }

        private void EnsureInitialized()
        {
            if (_initialized) return;
            _initialized = true;

            if (visualRoot == null)
                visualRoot = transform;

            _rootInitialLocalPos = visualRoot.localPosition;
            _rootInitialLocalScale = visualRoot.localScale;

            if (shackleRect != null)
                _shackleInitialPos = shackleRect.anchoredPosition;
        }

        private void OnDestroy()
        {
            KillTweens();
        }

        public void KillTweens()
        {
            _idleTween?.Kill();
            _idleTween = null;
            _unlockSeq?.Kill();
            _unlockSeq = null;
        }

        public void StartIdleBob()
        {
            EnsureInitialized();
            _idleTween?.Kill();
            if (visualRoot == null) return;

            visualRoot.localPosition = _rootInitialLocalPos;
            _idleTween = visualRoot
                .DOLocalMoveY(_rootInitialLocalPos.y + idleBobAmplitude, idleBobDuration)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);
        }

        private void RestoreInitialVisualState()
        {
            gameObject.SetActive(true);

            if (visualRoot != null)
            {
                visualRoot.localPosition = _rootInitialLocalPos;
                visualRoot.localScale = _rootInitialLocalScale;
            }
            if (shackleRect != null)
            {
                shackleRect.anchoredPosition = _shackleInitialPos;
                shackleRect.localRotation = Quaternion.identity;
            }
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
            }
        }

        /// <summary>
        /// 자물쇠 걸이가 튀어오르고 전체가 축소/페이드아웃되는 0.52초 시퀀스를 실행합니다.
        /// </summary>
        public Sequence PlayUnlockSequence(Action onShacklePop = null)
        {
            EnsureInitialized();
            KillTweens();
            RestoreInitialVisualState();

            _unlockSeq = DOTween.Sequence();

            // 0.00s ~ 0.28s: U자 걸이 팝업 + 살짝 기울어짐
            _unlockSeq.AppendCallback(() => onShacklePop?.Invoke());

            if (shackleRect != null)
            {
                _unlockSeq.Append(
                    shackleRect.DOAnchorPosY(_shackleInitialPos.y + shackleLiftPixels, 0.28f)
                        .SetEase(Ease.OutBack)
                );
                _unlockSeq.Join(
                    shackleRect.DOLocalRotate(new Vector3(0f, 0f, shackleTiltAngle), 0.28f)
                        .SetEase(Ease.OutBack)
                );
            }
            else
            {
                _unlockSeq.AppendInterval(0.28f);
            }

            // 0.28s ~ 0.52s: 전체 축소 + 페이드아웃
            if (visualRoot != null)
            {
                _unlockSeq.Append(
                    visualRoot.DOScale(Vector3.zero, 0.24f)
                        .SetEase(Ease.InBack)
                );
            }
            else
            {
                _unlockSeq.AppendInterval(0.24f);
            }

            if (canvasGroup != null)
            {
                _unlockSeq.Join(canvasGroup.DOFade(0f, 0.24f).SetEase(Ease.InQuad));
            }

            _unlockSeq.OnComplete(() =>
            {
                gameObject.SetActive(false);
            });

            return _unlockSeq;
        }

        public void ResetLock()
        {
            EnsureInitialized();
            KillTweens();
            RestoreInitialVisualState();
            StartIdleBob();
        }
    }
}
