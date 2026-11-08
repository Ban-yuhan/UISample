using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace LogisticsTycoon.UI
{
    /// <summary>
    /// 주문 폭주(Order Rush / Fever Time) 피버 모드 화면 외곽 붉은 라인 펄스 비네팅 오버레이
    /// 화면 4면 테두리에 긴장감 넘치는 네온 레드 펄스를 비동기 렌더링하며 클릭 입력을 절대 방해하지 않습니다.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class OrderRushOverlay : MonoBehaviour
    {
        [Header("UI Components")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Image borderImage;
        [SerializeField] private RectTransform borderTransform;

        [Header("Visual Settings")]
        [SerializeField] private Color rushColor = new Color(0.92f, 0.18f, 0.15f, 1f);
        [SerializeField] private float minAlpha = 0.25f;
        [SerializeField] private float maxAlpha = 0.85f;
        [SerializeField] private float pulseDuration = 0.45f;
        [SerializeField] private float pulseScale = 1.015f;

        private Tween pulseAlphaTween;
        private Tween pulseScaleTween;
        private Tween fadeTween;
        private bool isRushing = false;

        public bool IsActive => isRushing;

        private void Awake()
        {
            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }

            // 마우스 클릭 및 터치 입력을 절대 가로채지 않도록 엄격 차단
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;

            if (borderImage != null)
            {
                borderImage.raycastTarget = false;
                borderImage.color = rushColor;
            }

            if (borderTransform == null && borderImage != null)
            {
                borderTransform = borderImage.rectTransform;
            }

            // 초기 상태는 비활성화
            canvasGroup.alpha = 0f;
            gameObject.SetActive(false);
        }

        private void OnDisable()
        {
            KillTweens();
        }

        private void OnDestroy()
        {
            KillTweens();
        }

        /// <summary>
        /// 주문 폭주 붉은 펄스 오버레이 활성화 (Fade In 후 무한 펄스 루프 진입)
        /// </summary>
        public void Show(float duration = 0.35f)
        {
            isRushing = true;
            KillTweens();

            gameObject.SetActive(true);

            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }

            if (borderTransform != null)
            {
                borderTransform.localScale = Vector3.one;
            }

            if (canvasGroup != null)
            {
                fadeTween = canvasGroup.DOFade(maxAlpha, duration)
                    .SetEase(Ease.OutQuad)
                    .OnComplete(StartPulseLoop);
            }
        }

        /// <summary>
        /// 주문 폭주 붉은 펄스 오버레이 비활성화 (Fade Out 후 게임오브젝트 비활성화)
        /// </summary>
        public void Hide(float duration = 0.3f)
        {
            isRushing = false;
            KillTweens();

            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }

            if (canvasGroup != null)
            {
                fadeTween = canvasGroup.DOFade(0f, duration)
                    .SetEase(Ease.OutQuad)
                    .OnComplete(() =>
                    {
                        if (borderTransform != null)
                        {
                            borderTransform.localScale = Vector3.one;
                        }
                        gameObject.SetActive(false);
                    });
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// 주문 폭주 피버 모드 토글
        /// </summary>
        public void Toggle()
        {
            if (isRushing)
            {
                Hide();
            }
            else
            {
                Show();
            }
        }

        /// <summary>
        /// minAlpha <-> maxAlpha 사이를 부드럽게 점멸하는 무한 펄스 루프 시작
        /// </summary>
        private void StartPulseLoop()
        {
            KillTweens();

            if (canvasGroup == null) return;

            // 1. 알파 무한 요요 펄스 (0.85f -> 0.25f -> 0.85f ...)
            canvasGroup.alpha = maxAlpha;
            pulseAlphaTween = canvasGroup.DOFade(minAlpha, pulseDuration)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);

            // 2. 화면 스케일/두께 미세 호흡(Breathing) 펄스로 긴장감 극대화
            if (borderTransform != null)
            {
                borderTransform.localScale = Vector3.one;
                pulseScaleTween = borderTransform.DOScale(pulseScale, pulseDuration)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo);
            }
        }

        /// <summary>
        /// 활성화 중인 모든 DOTween 트윈 누수 방지 킬 처리
        /// </summary>
        private void KillTweens()
        {
            if (fadeTween != null && fadeTween.IsActive())
            {
                fadeTween.Kill();
                fadeTween = null;
            }

            if (pulseAlphaTween != null && pulseAlphaTween.IsActive())
            {
                pulseAlphaTween.Kill();
                pulseAlphaTween = null;
            }

            if (pulseScaleTween != null && pulseScaleTween.IsActive())
            {
                pulseScaleTween.Kill();
                pulseScaleTween = null;
            }
        }

        #region Setup Reference Helper (Editor / Builder)

        public void SetReferences(CanvasGroup cg, Image img, RectTransform trans)
        {
            canvasGroup = cg;
            borderImage = img;
            borderTransform = trans;

            if (canvasGroup != null)
            {
                canvasGroup.blocksRaycasts = false;
                canvasGroup.interactable = false;
            }

            if (borderImage != null)
            {
                borderImage.raycastTarget = false;
                borderImage.color = rushColor;
            }
        }

        /// <summary>
        /// 런타임/빌더용 프로시저럴 비네팅 테두리 텍스처/스프라이트 자동 생성
        /// 가장자리 네온 림 + 안쪽 부드러운 코사인 감쇄 비네팅
        /// </summary>
        public static Sprite CreateProceduralVignetteSprite(int size = 128, int borderThickness = 12, int fadeDepth = 48)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.name = "OrderRush_Vignette_Tex";
            Color[] pixels = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // 모서리로부터의 최소 거리
                    int distFromEdgeX = Mathf.Min(x, size - 1 - x);
                    int distFromEdgeY = Mathf.Min(y, size - 1 - y);
                    int distFromEdge = Mathf.Min(distFromEdgeX, distFromEdgeY);

                    float alpha = 0f;
                    if (distFromEdge < borderThickness)
                    {
                        // 최외곽 네온 테두리: 1.0f 완전 불투명
                        alpha = 1f;
                    }
                    else if (distFromEdge < fadeDepth)
                    {
                        // 안쪽 감쇄 영역: 코사인 커브 부드러운 페이드
                        float t = (float)(distFromEdge - borderThickness) / (fadeDepth - borderThickness);
                        alpha = 0.5f * (1f + Mathf.Cos(t * Mathf.PI));
                    }
                    else
                    {
                        alpha = 0f;
                    }

                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();

            // 9-sliced Sprite 생성 (border: fadeDepth)
            Vector4 border = new Vector4(fadeDepth, fadeDepth, fadeDepth, fadeDepth);
            Sprite sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
            sprite.name = "OrderRush_Vignette_Sprite";
            return sprite;
        }

        #endregion
    }
}
