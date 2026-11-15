using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace LogisticsTycoon.Transitions
{
    /// <summary>
    /// 물리 3회 바운스 + 지나간 경로 점선 궤적 + 웅크림(Charge) + 화면 돌진(Launch) + 중앙 축소(PlayOut) 트랜지션
    /// </summary>
    public class BouncingBallTransition : BaseTransition
    {
        private enum SegmentType
        {
            Drop,
            Contact,
            Flight,
            Charge
        }

        private struct Segment
        {
            public SegmentType Type;
            public float T0;
            public float T1;
            public float V;
        }

        private struct TrailPoint
        {
            public float Time;
            public Vector2 Position;
        }

        [Header("Visual References")]
        [SerializeField] private Image bgPanel;
        [SerializeField] private Image floorLine;
        [SerializeField] private RectTransform trailContainer;
        [SerializeField] private Image[] trailDots;
        [SerializeField] private RectTransform ballRect;
        [SerializeField] private Image ballImage;

        [Header("Palette")]
        [SerializeField] private Color bgColor = new Color(0.937f, 0.918f, 0.878f, 1f);     // #EFEAE0
        [SerializeField] private Color floorColor = new Color(0.863f, 0.831f, 0.773f, 1f);  // #DCD4C5
        [SerializeField] private Color trailColor = new Color(0.769f, 0.722f, 0.647f, 1f);  // #C4B8A5
        [SerializeField] private Color ballColor = new Color(0.894f, 0.275f, 0.169f, 1f);   // #E4462B

        // HTML (1920x1080) -> Unity Center-Origin (1920x1080) 물리 상수
        private const float FloorY = -400f;      // HTML FLOOR = 940 -> 540 - 940 = -400
        private const float R0 = 50f;            // 시작 반지름 (지름 100)
        private const float Gravity = 5200f;     // 중력 가속도
        private const float H0 = 520f;           // 초기 낙하 높이
        private const float StartX = -700f;      // HTML X0 = 260 -> 260 - 960 = -700
        private const float EndX = 290f;         // HTML 1250 -> 1250 - 960 = +290
        private const float TAppear = 0.12f;     // 팝업 등장 시간
        private const float TDrop = 0.15f;       // 낙하 시작 시점
        private static readonly float[] Restitution = { 0.72f, 0.80f, 0.66f };
        private const float ChargeDuration = 0.26f;
        private const float ChargeSquash = 0.46f;
        private const float LaunchDuration = 0.34f;
        private const float REnd = 2200f;        // 최종 반지름 (지름 4400)

        private const float TrailStep = 1f / 480f;
        private const float TrailDotSpacing = 20f;
        private const float TrailEndOffset = 0.12f;
        private const float OutDuration = 0.42f;

        private readonly List<Segment> segments = new List<Segment>();
        private readonly List<TrailPoint> trailPoints = new List<TrailPoint>();

        private bool initialized;
        private float v0;
        private float tLand;
        private float tLaunch;
        private float totalInDuration;
        private float rLaunch;
        private float syCharged;
        private float elapsed;

        private Sequence inSequence;
        private Sequence outSequence;
        private Tween waitingTween;

        protected override void Awake()
        {
            base.Awake();
            EnsureInitialized();
        }

        private void OnDestroy()
        {
            KillAllTweens();
        }

        private void KillAllTweens()
        {
            inSequence?.Kill();
            inSequence = null;

            outSequence?.Kill();
            outSequence = null;

            waitingTween?.Kill();
            waitingTween = null;
        }

        private void EnsureInitialized()
        {
            if (initialized) return;
            BuildSchedule();
            BuildTrailPoints();
            initialized = true;
        }

        private void BuildSchedule()
        {
            segments.Clear();
            v0 = Mathf.Sqrt(2f * Gravity * H0);
            float t = TDrop;
            float v = v0;
            float tf = v0 / Gravity;

            segments.Add(new Segment { Type = SegmentType.Drop, T0 = t, T1 = t + tf, V = v });
            t += tf;

            for (int i = 0; i < Restitution.Length; i++)
            {
                float dc = 0.035f + 0.03f * (v / v0);
                segments.Add(new Segment { Type = SegmentType.Contact, T0 = t, T1 = t + dc, V = v });
                t += dc;

                v *= Restitution[i];
                float ft = 2f * v / Gravity;
                segments.Add(new Segment { Type = SegmentType.Flight, T0 = t, T1 = t + ft, V = v });
                t += ft;
            }

            segments.Add(new Segment { Type = SegmentType.Charge, T0 = t, T1 = t + ChargeDuration, V = v });

            tLand = t;
            tLaunch = tLand + ChargeDuration;
            totalInDuration = tLaunch + LaunchDuration;
            rLaunch = RAt(tLaunch);
            syCharged = 1f - ChargeSquash - 0.03f;
        }

        private void BuildTrailPoints()
        {
            trailPoints.Clear();
            float tEnd = tLaunch + TrailEndOffset;

            EvaluateBall(TDrop, out Vector2 prevPos, out _, out _);
            trailPoints.Add(new TrailPoint { Time = TDrop, Position = prevPos });

            float accDist = 0f;
            int steps = Mathf.FloorToInt((tEnd - TDrop) / TrailStep);
            for (int k = 1; k <= steps; k++)
            {
                float t = TDrop + k * TrailStep;
                EvaluateBall(t, out Vector2 pos, out _, out _);
                accDist += Vector2.Distance(prevPos, pos);
                if (accDist >= TrailDotSpacing)
                {
                    trailPoints.Add(new TrailPoint { Time = t, Position = pos });
                    accDist -= TrailDotSpacing;
                }
                prevPos = pos;
            }
        }

        public override void ResetState()
        {
            KillAllTweens();
            EnsureInitialized();

            if (container != null)
            {
                container.anchorMin = Vector2.zero;
                container.anchorMax = Vector2.one;
                container.offsetMin = Vector2.zero;
                container.offsetMax = Vector2.zero;
                container.localScale = Vector3.one;
            }

            if (bgPanel != null)
            {
                bgPanel.color = new Color(bgColor.r, bgColor.g, bgColor.b, 0f);
                bgPanel.gameObject.SetActive(false);
            }

            if (floorLine != null)
            {
                floorLine.color = new Color(floorColor.r, floorColor.g, floorColor.b, 0f);
                floorLine.gameObject.SetActive(false);
            }

            if (trailContainer != null)
            {
                trailContainer.gameObject.SetActive(false);
            }

            DeactivateAllTrailDots();

            if (ballRect != null)
            {
                ballRect.anchoredPosition = new Vector2(StartX, FloorY + H0 + R0);
                ballRect.localScale = Vector3.zero;
                ballRect.gameObject.SetActive(false);
            }

            if (ballImage != null)
            {
                ballImage.color = ballColor;
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.blocksRaycasts = false;
            }

            elapsed = 0f;
            IsPlaying = false;
        }

        public override void PlayIn(Action onInComplete = null)
        {
            if (IsPlaying) return;
            IsPlaying = true;

            KillAllTweens();
            EnsureInitialized();

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.blocksRaycasts = true;
            }

            if (bgPanel != null)
            {
                bgPanel.gameObject.SetActive(true);
                bgPanel.color = new Color(bgColor.r, bgColor.g, bgColor.b, 0f);
            }

            if (floorLine != null)
            {
                floorLine.gameObject.SetActive(true);
                floorLine.color = new Color(floorColor.r, floorColor.g, floorColor.b, 0f);
            }

            if (trailContainer != null)
            {
                trailContainer.gameObject.SetActive(true);
            }
            DeactivateAllTrailDots();

            if (ballRect != null)
            {
                ballRect.gameObject.SetActive(true);
            }
            if (ballImage != null)
            {
                ballImage.color = ballColor;
            }

            elapsed = 0f;
            UpdateMotionAtTime(0f);

            inSequence = DOTween.Sequence();

            if (bgPanel != null)
            {
                inSequence.Insert(0f, bgPanel.DOFade(1f, 0.12f).SetEase(Ease.OutQuad));
            }
            if (floorLine != null)
            {
                inSequence.Insert(0f, floorLine.DOFade(1f, 0.12f).SetEase(Ease.OutQuad));
            }

            inSequence.Insert(0f, DOTween.To(
                () => elapsed,
                t =>
                {
                    elapsed = t;
                    UpdateMotionAtTime(t);
                },
                totalInDuration,
                totalInDuration
            ).SetEase(Ease.Linear));

            inSequence.OnComplete(() =>
            {
                UpdateMotionAtTime(totalInDuration);
                onInComplete?.Invoke();
            });
        }

        public override void StartWaitingLoop()
        {
            waitingTween?.Kill();
            if (ballRect != null)
            {
                float fullScale = REnd / R0;
                ballRect.anchoredPosition = Vector2.zero;
                ballRect.localScale = new Vector3(fullScale, fullScale, 1f);
                waitingTween = ballRect.DOScale(fullScale * 1.02f, 0.55f)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo);
            }
        }

        public override void StopWaitingLoop()
        {
            waitingTween?.Kill();
            waitingTween = null;

            if (ballRect != null)
            {
                float fullScale = REnd / R0;
                ballRect.anchoredPosition = Vector2.zero;
                ballRect.localScale = new Vector3(fullScale, fullScale, 1f);
            }
        }

        public override void PlayOut(Action onOutComplete = null)
        {
            StopWaitingLoop();
            outSequence?.Kill();

            // 배경, 바닥 라인, 점선 궤적을 즉시 숨겨 새 씬을 뒤로 노출
            if (bgPanel != null) bgPanel.gameObject.SetActive(false);
            if (floorLine != null) floorLine.gameObject.SetActive(false);
            if (trailContainer != null) trailContainer.gameObject.SetActive(false);
            DeactivateAllTrailDots();

            if (ballRect != null)
            {
                ballRect.gameObject.SetActive(true);
                ballRect.anchoredPosition = Vector2.zero;
                float fullScale = REnd / R0;
                ballRect.localScale = new Vector3(fullScale, fullScale, 1f);
            }

            outSequence = DOTween.Sequence();
            if (ballRect != null)
            {
                outSequence.Append(ballRect.DOScale(Vector3.zero, OutDuration).SetEase(Ease.InBack));
            }

            outSequence.OnComplete(() =>
            {
                if (ballRect != null)
                {
                    ballRect.gameObject.SetActive(false);
                }
                if (canvasGroup != null)
                {
                    canvasGroup.blocksRaycasts = false;
                }
                IsPlaying = false;
                onOutComplete?.Invoke();
            });
        }

        private void UpdateMotionAtTime(float t)
        {
            EvaluateBall(t, out Vector2 center, out float rx, out float ry);

            if (ballRect != null)
            {
                ballRect.anchoredPosition = center;
                ballRect.localScale = rx > 0.01f
                    ? new Vector3(rx / R0, ry / R0, 1f)
                    : Vector3.zero;
            }

            UpdateTrail(t);
        }

        private void UpdateTrail(float t)
        {
            if (trailDots == null || trailDots.Length == 0) return;

            float tEnd = Mathf.Min(t, tLaunch + TrailEndOffset);
            if (tEnd <= TDrop)
            {
                DeactivateAllTrailDots();
                return;
            }

            int dotCount = Mathf.Min(trailDots.Length, trailPoints.Count);
            for (int i = 0; i < dotCount; i++)
            {
                Image dot = trailDots[i];
                if (dot == null) continue;

                TrailPoint pt = trailPoints[i];
                bool active = pt.Time <= tEnd;
                if (dot.gameObject.activeSelf != active)
                {
                    dot.gameObject.SetActive(active);
                }
                if (active)
                {
                    dot.rectTransform.anchoredPosition = pt.Position;
                }
            }

            for (int i = dotCount; i < trailDots.Length; i++)
            {
                if (trailDots[i] != null && trailDots[i].gameObject.activeSelf)
                {
                    trailDots[i].gameObject.SetActive(false);
                }
            }
        }

        private void DeactivateAllTrailDots()
        {
            if (trailDots == null) return;
            for (int i = 0; i < trailDots.Length; i++)
            {
                if (trailDots[i] != null && trailDots[i].gameObject.activeSelf)
                {
                    trailDots[i].gameObject.SetActive(false);
                }
            }
        }

        private void EvaluateBall(float t, out Vector2 center, out float rx, out float ry)
        {
            float appear = t < TAppear ? Mathf.Max(0f, EaseOutBack(Mathf.Clamp01(t / TAppear))) : 1f;

            if (t < tLaunch)
            {
                float r = RAt(t) * appear;
                BounceState(t, out float h, out float sy);
                float sx = 1f / sy;
                float cx = XAt(t);
                float cy = FloorY + h + r * sy;
                center = new Vector2(cx, cy);
                rx = r * sx;
                ry = r * sy;
                return;
            }

            float p = Mathf.Clamp01((t - tLaunch) / LaunchDuration);
            float launchSy = p < 0.14f
                ? Mathf.Lerp(syCharged, 1.45f, EaseOutCubic(p / 0.14f))
                : Mathf.Lerp(1.45f, 1f, SmoothStep(0.14f, 0.7f, p));
            float launchSx = 1f / launchSy;
            float launchR = rLaunch * Mathf.Pow(REnd / rLaunch, Mathf.Pow(p, 1.7f));
            float startX = XAt(tLaunch);
            float startY = FloorY + rLaunch * syCharged;
            float m = EaseOutCubic(p);

            float outX = Mathf.Lerp(startX, 0f, m);
            float outY = Mathf.Lerp(startY, 0f, m) + 260f * Mathf.Sin(Mathf.PI * Mathf.Clamp01(p / 0.6f)) * (1f - p);

            center = new Vector2(outX, outY);
            rx = launchR * launchSx;
            ry = launchR * launchSy;
        }

        private void BounceState(float t, out float h, out float sy)
        {
            if (t < TDrop)
            {
                h = H0;
                sy = 1f;
                return;
            }

            for (int i = 0; i < segments.Count; i++)
            {
                Segment s = segments[i];
                if (t >= s.T1) continue;

                float tau = t - s.T0;
                switch (s.Type)
                {
                    case SegmentType.Drop:
                    {
                        float w = SmoothStep(0f, 0.03f, s.T1 - t);
                        h = H0 - 0.5f * Gravity * tau * tau;
                        sy = 1f + (StretchFromVelocity(-Gravity * tau) - 1f) * w;
                        return;
                    }
                    case SegmentType.Flight:
                    {
                        float w = SmoothStep(0f, 0.03f, Mathf.Min(tau, s.T1 - t));
                        h = Mathf.Max(0f, s.V * tau - 0.5f * Gravity * tau * tau);
                        sy = 1f + (StretchFromVelocity(s.V - Gravity * tau) - 1f) * w;
                        return;
                    }
                    case SegmentType.Contact:
                    {
                        float amt = 0.34f * (s.V / v0) + 0.04f;
                        h = 0f;
                        sy = 1f - amt * Mathf.Sin(Mathf.PI * tau / (s.T1 - s.T0));
                        return;
                    }
                    case SegmentType.Charge:
                    {
                        float k = tau / ChargeDuration;
                        h = 0f;
                        sy = 1f - ChargeSquash * EaseOutCubic(Mathf.Clamp01(k / 0.55f)) - 0.03f * SmoothStep(0.55f, 1f, k);
                        return;
                    }
                }
            }

            h = 0f;
            sy = syCharged;
        }

        private float StretchFromVelocity(float vel)
        {
            return vel > 0f ? 1f + 0.26f * (vel / v0) : 1f + 0.14f * (-vel / v0);
        }

        private float XAt(float t)
        {
            float tt = Mathf.Clamp(t, TDrop, tLand) - TDrop;
            return Mathf.Lerp(StartX, EndX, 1f - Mathf.Exp(-tt / 1.4f));
        }

        private float RAt(float t)
        {
            return R0 * (1f + 0.22f * Mathf.Clamp(t, 0f, tLaunch));
        }

        private static float SmoothStep(float a, float b, float x)
        {
            float k = Mathf.Clamp01((x - a) / (b - a));
            return k * k * (3f - 2f * k);
        }

        private static float EaseOutBack(float x)
        {
            const float c1 = 1.6f;
            const float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
        }

        private static float EaseOutCubic(float x)
        {
            return 1f - Mathf.Pow(1f - x, 3f);
        }
    }
}
