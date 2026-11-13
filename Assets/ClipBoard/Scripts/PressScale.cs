using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

namespace LogisticsTycoon.ClipBoard
{
    /// <summary>버튼 누름 시 살짝 눌렸다 튀어오르는 DOTween 피드백</summary>
    public class PressScale : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public float pressed = 0.94f;
        private Vector3 baseScale = Vector3.one;
        private bool init;

        private void Init()
        {
            if (init) return;
            init = true;
            baseScale = transform.localScale;
        }

        public void OnPointerDown(PointerEventData e)
        {
            Init();
            transform.DOKill();
            transform.DOScale(baseScale * pressed, 0.08f).SetEase(Ease.OutQuad);
        }

        public void OnPointerUp(PointerEventData e)
        {
            Init();
            transform.DOKill();
            transform.DOScale(baseScale, 0.32f).SetEase(Ease.OutBack, 3f);
        }

        private void OnDisable()
        {
            transform.DOKill();
            if (init) transform.localScale = baseScale;
        }
    }
}
