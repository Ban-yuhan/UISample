using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace LogisticsTycoon.ClipBoard
{
    /// <summary>
    /// 반투명 돼지 저금통. 보유 자금 비율만큼 내부 금화(fillRect)의 높이가 게이지처럼 차오르고 내려간다.
    /// fillRect: 몸통(타원 Mask) 안에서 바닥에 붙어 있는 사각형. RectMask2D로 그 위의 동전을 잘라낸다.
    /// </summary>
    public class PiggyBankGauge : MonoBehaviour
    {
        public RectTransform pig;        // 흔들림/펀치 대상
        public RectTransform fillRect;   // 높이가 변하는 금화 영역
        public RectTransform surface;    // 금화 수면 하이라이트 (fillRect 상단)
        public float fillMaxHeight = 60f;
        public float minRatio = 0.08f;
        public RectTransform coinDropPrefabParent; // 떨어지는 동전이 생성될 부모
        public RectTransform slot;       // 동전 투입구
        public Sprite coinSprite;

        private float ratio = -1f;
        private Tween fillTween;

        public void SetRatio(float target, bool animate)
        {
            target = Mathf.Clamp(target, minRatio, 1f);
            float prev = ratio;
            ratio = target;
            float h = fillMaxHeight * target;

            fillTween?.Kill();
            if (!animate || prev < 0f)
            {
                fillRect.sizeDelta = new Vector2(fillRect.sizeDelta.x, h);
                return;
            }

            bool up = target > prev + 0.0001f;
            bool down = target < prev - 0.0001f;
            float delay = 0f;

            if (up)
            {
                // 동전이 투입구로 떨어진 뒤 수위가 오른다
                delay = 0.28f;
                DropCoin();
                pig.DOKill(true);
                pig.DOPunchScale(new Vector3(0.12f, -0.08f, 0f), 0.4f, 6, 0.6f).SetDelay(delay);
            }
            else if (down)
            {
                pig.DOKill(true);
                pig.DOPunchRotation(new Vector3(0f, 0f, 9f), 0.45f, 9, 0.7f);
            }

            // 액체처럼 살짝 넘쳤다가 자리 잡는 수위 변화
            fillTween = fillRect.DOSizeDelta(new Vector2(fillRect.sizeDelta.x, h), 0.7f)
                .SetDelay(delay)
                .SetEase(up ? Ease.OutBack : Ease.OutCubic, 1.6f);
            if (surface != null)
            {
                surface.DOKill(true);
                surface.DOPunchScale(new Vector3(0f, 1.2f, 0f), 0.5f, 5).SetDelay(delay + 0.1f);
            }
        }

        private void DropCoin()
        {
            if (slot == null || coinSprite == null) return;
            var parent = coinDropPrefabParent != null ? coinDropPrefabParent : (RectTransform)slot.parent;
            var coin = ClipUIRuntime.SimpleImage("DropCoin", parent, coinSprite, ClipUI.Hex("#facc15"));
            coin.rectTransform.sizeDelta = new Vector2(16, 16);
            coin.rectTransform.position = slot.position + new Vector3(0f, 34f * slot.lossyScale.y, 0f);
            coin.color = ClipUI.WithA(coin.color, 0f);
            var seq = DOTween.Sequence();
            seq.Append(coin.DOFade(1f, 0.08f));
            seq.Join(coin.rectTransform.DOMove(slot.position, 0.24f).SetEase(Ease.InQuad));
            seq.Join(coin.rectTransform.DOScaleX(0.35f, 0.24f).SetEase(Ease.InQuad)); // 세워서 투입구로 쏙
            seq.Append(coin.DOFade(0f, 0.06f));
            seq.OnComplete(() => Destroy(coin.gameObject));
        }
    }

    internal static class ClipUIRuntime
    {
        public static Image SimpleImage(string name, Transform parent, Sprite s, Color c)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.layer = 5;
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.sprite = s;
            img.color = c;
            img.raycastTarget = false;
            return img;
        }
    }
}
