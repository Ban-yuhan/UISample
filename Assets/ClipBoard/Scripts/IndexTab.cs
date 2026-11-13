using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LogisticsTycoon.ClipBoard
{
    /// <summary>
    /// 우측 인덱스 테이프. 활성 탭은 앞 종이 위(frontLayer)로, 비활성 탭은 앞 종이 아래(backLayer)로 끼워진다.
    /// </summary>
    public class IndexTab : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        public int index;
        public Image body;
        public Image border;
        public TextMeshProUGUI label;
        public Color bodyColor;
        public Color borderColor;
        public RectTransform frontLayer;
        public RectTransform backLayer;
        public GameObject badge;
        public float baseX;
        public Action<int> onClick;

        private bool active;
        private bool hover;
        private RectTransform rt;

        private RectTransform Rt => rt != null ? rt : (rt = (RectTransform)transform);

        public void SetActive(bool value, bool instant)
        {
            active = value;
            // 활성/비활성 탭의 깊이(레이어) 전환
            Rt.SetParent(active ? frontLayer : backLayer, true);
            Apply(instant ? 0f : 0.25f);
        }

        private void Apply(float dur)
        {
            float x = active ? baseX : baseX - (hover ? 2f : 7f);
            float s = active ? 1.03f : (hover ? 0.99f : 0.97f);
            float bright = active ? 1.03f : (hover ? 0.95f : 0.84f);
            Color bc = new Color(bodyColor.r * bright, bodyColor.g * bright, bodyColor.b * bright, 1f);
            Color brc = new Color(borderColor.r * bright, borderColor.g * bright, borderColor.b * bright, 1f);

            Rt.DOKill();
            body.DOKill();
            border.DOKill();
            if (dur <= 0f)
            {
                Rt.anchoredPosition = new Vector2(x, Rt.anchoredPosition.y);
                Rt.localScale = Vector3.one * s;
                body.color = bc;
                border.color = brc;
                return;
            }
            Rt.DOAnchorPosX(x, dur).SetEase(Ease.OutBack, 2.2f);
            Rt.DOScale(s, dur).SetEase(Ease.OutBack, 2.2f);
            body.DOColor(bc, dur * 0.8f);
            border.DOColor(brc, dur * 0.8f);
        }

        public void OnPointerClick(PointerEventData e)
        {
            if (active) { Rt.DOPunchScale(Vector3.one * 0.05f, 0.25f, 8); return; }
            onClick?.Invoke(index);
        }

        public void OnPointerEnter(PointerEventData e)
        {
            hover = true;
            if (!active) Apply(0.2f);
        }

        public void OnPointerExit(PointerEventData e)
        {
            hover = false;
            if (!active) Apply(0.2f);
        }
    }
}
