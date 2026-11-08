using System;
using UnityEngine;
using DG.Tweening;

namespace LogisticsTycoon.Transitions
{
    public abstract class BaseTransition : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] protected CanvasGroup canvasGroup;
        [SerializeField] protected RectTransform container;

        public bool IsPlaying { get; protected set; }

        protected virtual void Awake()
        {
            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();
            if (container == null)
                container = GetComponent<RectTransform>();
        }

        /// <summary>
        /// 단일 씬 프리뷰용: In -> 대기 -> Out을 연속 실행
        /// </summary>
        public virtual void PlayTransition(Action onMidpoint = null, Action onComplete = null)
        {
            PlayIn(() =>
            {
                onMidpoint?.Invoke();
                StartWaitingLoop();
                DOVirtual.DelayedCall(0.35f, () =>
                {
                    StopWaitingLoop();
                    PlayOut(onComplete);
                });
            });
        }

        /// <summary>
        /// Phase 1: 화면을 완전히 덮는 In 애니메이션
        /// </summary>
        public abstract void PlayIn(Action onInComplete = null);

        /// <summary>
        /// 화면이 덮인 상태에서 씬 로딩 대기 중 실행할 무한 루프 애니메이션 (트럭 진동, 경광등 회전 등)
        /// </summary>
        public virtual void StartWaitingLoop() {}

        /// <summary>
        /// 로딩 완료 시 대기 루프 중지
        /// </summary>
        public virtual void StopWaitingLoop() {}

        /// <summary>
        /// Phase 2: 덮인 화면이 열리며 새 씬을 노출하는 Out 애니메이션
        /// </summary>
        public abstract void PlayOut(Action onOutComplete = null);

        public abstract void ResetState();
    }
}
