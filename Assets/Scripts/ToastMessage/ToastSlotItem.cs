using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ToastMessageSample
{
    [RequireComponent(typeof(RectTransform), typeof(LayoutElement), typeof(CanvasGroup))]
    public sealed class ToastSlotItem : MonoBehaviour
    {
        private const float CapsuleSlotHeight = 63f;  // 54px pill + 9px bottom gap
        private const float SnackbarSlotHeight = 65f; // 56px card + 9px bottom gap

        private LayoutElement _layoutElement;
        private CanvasGroup _canvasGroup;
        private ToastAlertState _state;
        private CapsuleToastView _capsuleView;
        private SnackbarToastView _snackbarView;
        private Tween _collapseTween;

        public ToastAlertState State => _state;
        public CapsuleToastView CapsuleView => _capsuleView;
        public SnackbarToastView SnackbarView => _snackbarView;

        public void Initialize(
            ToastAlertState state,
            ToastVersion version,
            TMP_FontAsset font,
            float pulseDuration,
            bool v2AutoDismiss,
            bool playEnterAnim,
            Action<ToastAlertState> onCapsuleClick,
            Action<ToastAlertState> onSnackbarClick,
            Action<ToastAlertState> onDismiss)
        {
            _state = state;
            _layoutElement = GetComponent<LayoutElement>();
            _canvasGroup = GetComponent<CanvasGroup>();

            var rect = GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);

            _layoutElement.minWidth = 0f;
            _layoutElement.preferredWidth = 350f;
            _layoutElement.flexibleWidth = 0f;
            _layoutElement.minHeight = 0f;
            _layoutElement.flexibleHeight = 0f;

            _canvasGroup.alpha = 1f;
            _canvasGroup.blocksRaycasts = true;

            SetVersionView(
                version,
                font,
                pulseDuration,
                v2AutoDismiss,
                playEnterAnim,
                onCapsuleClick,
                onSnackbarClick,
                onDismiss);
        }

        public void SetVersionView(
            ToastVersion version,
            TMP_FontAsset font,
            float pulseDuration,
            bool v2AutoDismiss,
            bool playEnterAnim,
            Action<ToastAlertState> onCapsuleClick,
            Action<ToastAlertState> onSnackbarClick,
            Action<ToastAlertState> onDismiss)
        {
            ClearChildViews();

            if (version == ToastVersion.Capsule)
            {
                _layoutElement.preferredHeight = CapsuleSlotHeight;
                var go = new GameObject("CapsuleView", typeof(RectTransform));
                go.transform.SetParent(transform, false);
                _capsuleView = go.AddComponent<CapsuleToastView>();
                _capsuleView.BuildAndBind(
                    _state,
                    font,
                    pulseDuration,
                    playEnterAnim,
                    onCapsuleClick,
                    onDismiss);
            }
            else
            {
                _layoutElement.preferredHeight = SnackbarSlotHeight;
                var go = new GameObject("SnackbarView", typeof(RectTransform));
                go.transform.SetParent(transform, false);
                _snackbarView = go.AddComponent<SnackbarToastView>();
                _snackbarView.BuildAndBind(
                    _state,
                    font,
                    v2AutoDismiss,
                    playEnterAnim,
                    onSnackbarClick,
                    onDismiss);
            }
        }

        public void CollapseAndDestroy(Action<ToastSlotItem> onCompleted)
        {
            _collapseTween?.Kill();
            _canvasGroup.blocksRaycasts = false;

            if (_snackbarView != null)
            {
                _snackbarView.PlayExitAnimation();
            }

            _collapseTween = DOTween.Sequence()
                .Join(DOTween.To(
                    () => _layoutElement.preferredHeight,
                    v => _layoutElement.preferredHeight = v,
                    0f,
                    0.38f).SetEase(Ease.OutCubic))
                .Join(_canvasGroup.DOFade(0f, 0.28f).SetEase(Ease.OutCubic))
                .OnComplete(() =>
                {
                    onCompleted?.Invoke(this);
                    if (this != null && gameObject != null)
                    {
                        Destroy(gameObject);
                    }
                })
                .SetLink(gameObject);
        }

        private void ClearChildViews()
        {
            if (_capsuleView != null)
            {
                Destroy(_capsuleView.gameObject);
                _capsuleView = null;
            }
            if (_snackbarView != null)
            {
                Destroy(_snackbarView.gameObject);
                _snackbarView = null;
            }
        }

        private void OnDestroy()
        {
            _collapseTween?.Kill();
        }
    }
}
