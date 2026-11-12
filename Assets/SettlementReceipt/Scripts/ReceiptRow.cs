using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LogisticsTycoon.Settlement
{
    /// <summary>
    /// 영수증의 한 "출력 단계(행)". 종이가 이 행의 아래 끝 + revealPad 만큼 올라온 뒤 공개 연출이 실행된다.
    /// </summary>
    public class ReceiptRow : MonoBehaviour
    {
        public enum RevealStyle
        {
            /// <summary>헤더: 짧은 페이드만</summary>
            Fade,
            /// <summary>수치 행: 플래시 + 스프링 팝 + 글로우 + 파티클</summary>
            Pop,
            /// <summary>익일 예보: 확대/파티클 없이 차분한 페이드</summary>
            Calm
        }

        public RevealStyle style = RevealStyle.Pop;
        public CanvasGroup group;

        [Tooltip("종이가 행 아래 끝보다 추가로 올라오는 여백 (UI 유닛)")]
        public float revealPad = 17f;

        [Header("Pop Style")]
        public Image flashBackground;
        [Range(0f, 1f)] public float flashAlpha = 0.24f;
        public RectTransform valueRect;
        public TMP_Text valueText;
        public Image valueGlow;
        public Color accent = new Color(0.02f, 0.59f, 0.41f);
        public bool positive = true;
        public int particleCount = 22;
        public Color[] particleColors = { new Color(0.06f, 0.73f, 0.51f), new Color(0.2f, 0.83f, 0.6f), new Color(0.98f, 0.75f, 0.14f) };

        public RectTransform Rect => (RectTransform)transform;

        /// <summary>값 텍스트·강조색을 갱신하고 글로우 크기를 텍스트 폭에 맞춘다.</summary>
        public void SetValue(string text, Color accentColor, bool isPositive)
        {
            accent = accentColor;
            positive = isPositive;
            if (valueText != null)
            {
                valueText.text = text;
                valueText.color = accentColor;
            }
            if (flashBackground != null)
            {
                var c = accentColor;
                c.a = 0f;
                flashBackground.color = c;
            }
            if (valueGlow != null && valueText != null)
            {
                var c = accentColor;
                c.a = 0f;
                valueGlow.color = c;
                float w = valueText.preferredWidth;
                float h = valueText.rectTransform.rect.height;
                valueGlow.rectTransform.sizeDelta = new Vector2(w + h * 1.6f, h * 2.2f);
                valueGlow.rectTransform.anchoredPosition = new Vector2(-w * 0.5f, 0f);
            }
        }

        public void HideImmediate()
        {
            if (group != null) group.alpha = 0f;
            if (flashBackground != null) SetAlpha(flashBackground, 0f);
            if (valueGlow != null) SetAlpha(valueGlow, 0f);
            if (valueRect != null) valueRect.localScale = Vector3.one;
            if (valueText != null) valueText.color = accent;
        }

        public void ShowFinal()
        {
            if (group != null) group.alpha = 1f;
            if (flashBackground != null) SetAlpha(flashBackground, 0f);
            if (valueGlow != null) SetAlpha(valueGlow, 0f);
            if (valueRect != null) valueRect.localScale = Vector3.one;
            if (valueText != null) valueText.color = accent;
        }

        private static void SetAlpha(Graphic g, float a)
        {
            var c = g.color;
            c.a = a;
            g.color = c;
        }
    }
}
