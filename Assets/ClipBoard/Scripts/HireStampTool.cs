using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LogisticsTycoon.ClipBoard
{
    /// <summary>
    /// 고용 승인 고무도장.
    ///  - 거치대에 놓여 있을 때: Resources/Stamp_Front
    ///  - 집어 들었을 때: Resources/Stamp_Top 으로 교체 + 들어 올린 스케일/그림자
    ///  - 이력서 위에서 놓으면: 위로 살짝 들었다가(스케일 ↑) 쾅 내려찍고(스케일 ↓ + 찌그러짐) 튀어 오른 뒤 거치대로 복귀
    /// 이 컴포넌트는 거치대(raycast 대상)에 붙고, 실제 도장 그래픽(stampRoot)을 드래그 레이어로 옮겨 다닌다.
    /// </summary>
    public class HireStampTool : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler, IBeginDragHandler, IEndDragHandler
    {
        public ClipboardController controller;
        public RectTransform stampRoot;   // 그림자 + 도장 이미지를 묶은 루트
        public Image stampImage;
        public Image shadow;
        public RectTransform holder;      // 평소 stampRoot의 부모
        public RectTransform dragLayer;   // 드래그 중 stampRoot의 부모 (최상단)
        public Sprite frontSprite;
        public Sprite topSprite;

        [Header("Feel")]
        public float heldScale = 1.18f;
        public float liftScale = 1.42f;
        public float impactScale = 0.86f;
        public Vector2 grabOffset = new Vector2(0f, 10f);

        private enum State { Idle, Held, Busy }
        private State state = State.Idle;
        private Vector2 homePos;
        private Vector2 homeSize;
        private Vector2 shadowBasePos;
        private Vector3 lastScreen;

        private void Awake()
        {
            if (frontSprite == null) frontSprite = LoadSprite("Stamp_Front");
            if (topSprite == null) topSprite = LoadSprite("Stamp_Top");
            stampImage.sprite = frontSprite;
            homePos = stampRoot.anchoredPosition;
            homeSize = stampRoot.sizeDelta;
            shadowBasePos = shadow.rectTransform.anchoredPosition;
            shadow.color = ClipUI.WithA(shadow.color, 0.35f);
        }

        private static Sprite LoadSprite(string name)
        {
            var s = Resources.Load<Sprite>(name);
            if (s == null)
            {
                // 텍스처 타입이 Sprite가 아닐 경우 대비
                var tex = Resources.Load<Texture2D>(name);
                if (tex != null) s = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
            }
            if (s == null) Debug.LogWarning("[HireStampTool] Resources에서 스프라이트를 찾을 수 없습니다: " + name);
            return s;
        }

        // ------------------------------------------------------------------ 입력

        public void OnPointerDown(PointerEventData e)
        {
            if (state != State.Idle || !controller.CanUseStamp) return;
            state = State.Held;
            lastScreen = e.position;
            PickUp(e.position);
        }

        public void OnBeginDrag(PointerEventData e) { }

        public void OnDrag(PointerEventData e)
        {
            if (state != State.Held) return;
            lastScreen = e.position;
            FollowPointer(e.position, 0.06f);
            controller.HoverResume(e.position);
        }

        public void OnEndDrag(PointerEventData e) { }

        public void OnPointerUp(PointerEventData e)
        {
            if (state != State.Held) return;
            Drop(e.position);
        }

        // ------------------------------------------------------------------ 동작

        private void PickUp(Vector2 screen)
        {
            stampRoot.DOKill();
            stampImage.rectTransform.DOKill();
            shadow.DOKill();
            shadow.rectTransform.DOKill();

            // 최상단 드래그 레이어로 이동 (화면상 위치 유지)
            stampRoot.SetParent(dragLayer, true);

            // 놓인 모습(Front) → 위에서 본 모습(Top) 교체 + 손에 들린 느낌
            stampImage.sprite = topSprite;
            stampImage.rectTransform.localScale = new Vector3(0.9f, 1.1f, 1f);
            stampImage.rectTransform.DOScale(1f, 0.3f).SetEase(Ease.OutElastic, 1.1f, 0.4f);

            stampRoot.DOScale(heldScale, 0.22f).SetEase(Ease.OutBack, 2f);
            stampRoot.DOLocalRotate(new Vector3(0, 0, -6f), 0.22f).SetEase(Ease.OutQuad);
            // 높이 = 그림자가 멀어지고 흐려짐
            shadow.rectTransform.DOAnchorPos(shadowBasePos + new Vector2(14f, -22f), 0.22f);
            shadow.rectTransform.DOScale(0.85f, 0.22f);
            shadow.DOFade(0.22f, 0.22f);

            FollowPointer(screen, 0.12f);
            controller.OnStampPicked();
        }

        private void FollowPointer(Vector2 screen, float dur)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(dragLayer, screen, null, out var local);
            stampRoot.DOAnchorPos(local + grabOffset, dur).SetEase(Ease.OutQuad).SetId(this);
        }

        private void Drop(Vector2 screen)
        {
            state = State.Busy;
            DOTween.Kill(this);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(dragLayer, screen, null, out var local);
            stampRoot.anchoredPosition = local + grabOffset;

            var target = controller.ResumeAt(screen);
            controller.HoverResume(new Vector2(-9999, -9999));

            if (target == null)
            {
                ReturnHome(0f);
                return;
            }

            if (!controller.CanAfford(target))
            {
                controller.NotifyNotEnoughGold(screen);
                stampRoot.DOShakeAnchorPos(0.3f, new Vector2(10f, 0f), 18, 0f).OnComplete(() => ReturnHome(0f));
                return;
            }

            Slam(screen, target);
        }

        private void Slam(Vector2 screen, ResumeCardView target)
        {
            var img = stampImage.rectTransform;
            var seq = DOTween.Sequence();

            // 1) 힘을 모으며 들어 올림 (스케일 ↑, 그림자 멀어짐)
            seq.Append(stampRoot.DOScale(liftScale, 0.14f).SetEase(Ease.OutQuad));
            seq.Join(stampRoot.DOLocalRotate(new Vector3(0, 0, -10f), 0.14f));
            seq.Join(shadow.rectTransform.DOAnchorPos(shadowBasePos + new Vector2(22f, -34f), 0.14f));
            seq.Join(shadow.DOFade(0.12f, 0.14f));

            // 2) 쾅! 내려찍기 (스케일 ↓ 급가속, 그림자 붙음)
            seq.Append(stampRoot.DOScale(impactScale, 0.075f).SetEase(Ease.InQuad));
            seq.Join(stampRoot.DOLocalRotate(new Vector3(0, 0, 0f), 0.075f).SetEase(Ease.InQuad));
            seq.Join(shadow.rectTransform.DOAnchorPos(shadowBasePos, 0.075f).SetEase(Ease.InQuad));
            seq.Join(shadow.rectTransform.DOScale(1.05f, 0.075f));
            seq.Join(shadow.DOFade(0.5f, 0.075f));

            // 3) 임팩트: 고무가 눌려 찌그러짐 + 고용 확정 (자국/흔들림/파티클)
            seq.AppendCallback(() =>
            {
                img.localScale = new Vector3(1.12f, 0.86f, 1f);
                controller.ConfirmHire(target, screen);
            });
            seq.Append(img.DOScale(1f, 0.22f).SetEase(Ease.OutElastic, 1.2f, 0.35f));
            seq.Join(stampRoot.DOScale(1f, 0.18f).SetEase(Ease.OutBack, 3f));

            // 4) 잠깐 눌러둔 뒤 들어 올림
            seq.AppendInterval(0.12f);
            seq.Append(stampRoot.DOScale(heldScale, 0.14f).SetEase(Ease.OutQuad));
            seq.Join(shadow.rectTransform.DOAnchorPos(shadowBasePos + new Vector2(14f, -22f), 0.14f));
            seq.Join(shadow.DOFade(0.22f, 0.14f));
            seq.OnComplete(() => ReturnHome(0.05f));
        }

        private void ReturnHome(float delay)
        {
            state = State.Busy;
            Vector3 homeWorld = holder.TransformPoint(homePos);
            var seq = DOTween.Sequence().SetDelay(delay);
            seq.Append(stampRoot.DOMove(homeWorld, 0.32f).SetEase(Ease.OutCubic));
            seq.Join(stampRoot.DOScale(1.05f, 0.32f));
            seq.Join(stampRoot.DOLocalRotate(Vector3.zero, 0.32f));
            seq.AppendCallback(() =>
            {
                // 거치대에 내려놓기: Top → Front 복귀
                stampRoot.SetParent(holder, false);
                stampRoot.anchoredPosition = homePos;
                stampRoot.sizeDelta = homeSize;
                stampImage.sprite = frontSprite;
                stampImage.rectTransform.localScale = new Vector3(1.08f, 0.9f, 1f);
            });
            seq.Append(stampRoot.DOScale(1f, 0.2f).SetEase(Ease.OutBack, 3f));
            seq.Join(stampImage.rectTransform.DOScale(1f, 0.25f).SetEase(Ease.OutElastic, 1f, 0.4f));
            seq.Join(shadow.rectTransform.DOAnchorPos(shadowBasePos, 0.2f));
            seq.Join(shadow.rectTransform.DOScale(1f, 0.2f));
            seq.Join(shadow.DOFade(0.35f, 0.2f));
            seq.OnComplete(() =>
            {
                state = State.Idle;
                controller.OnStampReturned();
            });
        }

        private void OnDisable()
        {
            // 탭 전환 등으로 비활성화되면 즉시 거치대로 복귀
            if (state == State.Idle) return;
            DOTween.Kill(this);
            stampRoot.DOKill();
            stampImage.rectTransform.DOKill();
            shadow.DOKill();
            shadow.rectTransform.DOKill();
            stampRoot.SetParent(holder, false);
            stampRoot.anchoredPosition = homePos;
            stampRoot.sizeDelta = homeSize;
            stampRoot.localScale = Vector3.one;
            stampRoot.localRotation = Quaternion.identity;
            stampImage.sprite = frontSprite;
            stampImage.rectTransform.localScale = Vector3.one;
            shadow.rectTransform.anchoredPosition = shadowBasePos;
            shadow.rectTransform.localScale = Vector3.one;
            shadow.color = ClipUI.WithA(shadow.color, 0.35f);
            state = State.Idle;
        }
    }
}
