using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LogisticsTycoon.Settlement
{
    /// <summary>
    /// 일일 정산 영수증 출력기 (원본: end_of_day_settlement_receipt_ui.html).
    ///
    /// 사용법 (게임 코드):
    ///     receiptView.Print(new SettlementData { day = 14, income = 485000, expense = 112000, ... });
    ///     receiptView.PrintCompleted += ...;   // 출력 완료
    ///     receiptView.TornOff += ...;          // 플레이어가 영수증을 뜯음 → 다음 날 진행 등
    ///
    /// 타임라인 (기본값, 원본과 1:1):
    ///   행마다 [피드 0.34s, OutQuint] → 행 공개 연출. 피드 시작 시각 = feedStartTimes[i].
    ///   순수익 공개 0.22s 뒤 "정산 완료" 스탬프 슬램. 마지막 행 + 0.2s 에 출력 완료.
    /// 종이 상단은 <see cref="ReceiptCurl"/>로 롤 말림이 생기며, 많이 나올수록 더 말리고 피드마다 살짝 출렁인다.
    /// 출력 중 종이 클릭 = 스킵(즉시 완성), 출력 후 클릭 = 뜯기.
    /// </summary>
    public class SettlementReceiptView : MonoBehaviour
    {
        [Header("Paper")]
        public RectTransform viewport;
        public RectTransform paper;
        public CanvasGroup paperGroup;
        public ReceiptPaperGraphic paperGraphic;
        public ReceiptCurl curl;
        [Tooltip("뜯을 때 종이를 마스크 밖으로 옮겨 화면 위로 날려 보내는 레이어")]
        public RectTransform tearLayer;

        [Header("Rows (출력 순서)")]
        public ReceiptRow headerRow;
        public ReceiptRow incomeRow;
        public ReceiptRow expenseRow;
        public ReceiptRow deliveredRow;
        public ReceiptRow returnedRow;
        public ReceiptRow netRow;
        public ReceiptRow forecastRow;

        [Header("Texts")]
        public TMP_Text dateText;
        public RectTransform weatherChip;
        public Image weatherIcon;
        public TMP_Text weatherText;
        public TMP_Text eventText;

        [Header("Stamp")]
        public RectTransform stamp;
        public CanvasGroup stampGroup;

        [Header("Weather Icons")]
        public Sprite sunnyIcon;
        public Sprite cloudyIcon;
        public Sprite rainIcon;
        public Sprite snowIcon;

        [Header("FX")]
        public PrinterBodyFx printer;
        public ReceiptParticlePool particles;
        public ReceiptSfx sfx;

        [Header("Colors")]
        public Color incomeColor = new Color32(0x05, 0x96, 0x69, 0xFF);
        public Color expenseColor = new Color32(0xE1, 0x1D, 0x48, 0xFF);
        public Color deliveredColor = new Color32(0x03, 0x69, 0xA1, 0xFF);
        public Color returnedColor = new Color32(0xEA, 0x58, 0x0C, 0xFF);
        public Color netPositiveColor = new Color32(0x04, 0x78, 0x57, 0xFF);
        public Color netNegativeColor = new Color32(0xBE, 0x12, 0x3C, 0xFF);
        public Color[] netPositiveParticles = { new Color32(0x10, 0xB9, 0x81, 0xFF), new Color32(0xFB, 0xBF, 0x24, 0xFF), new Color32(0xFD, 0xE0, 0x47, 0xFF) };
        public Color[] netNegativeParticles = { new Color32(0xF4, 0x3F, 0x5E, 0xFF), new Color32(0xFB, 0x71, 0x85, 0xFF), new Color32(0xFD, 0xA4, 0xAF, 0xFF) };
        public Color sunnyColor = new Color32(0xF5, 0x9E, 0x0B, 0xFF);
        public Color cloudyColor = new Color32(0x6B, 0x72, 0x80, 0xFF);
        public Color rainColor = new Color32(0x02, 0x84, 0xC7, 0xFF);
        public Color snowColor = new Color32(0x38, 0xBD, 0xF8, 0xFF);

        [Header("Timeline (sec)")]
        [Tooltip("각 행의 피드 시작 시각 (헤더, 수입, 지출, 배송, 반품, 순수익, 예보)")]
        public float[] feedStartTimes = { 0.10f, 0.54f, 1.12f, 1.70f, 2.28f, 2.88f, 3.56f };
        public float feedDuration = 0.34f;
        public float stampDelay = 0.22f;
        public float finishDelay = 0.2f;

        [Header("Curl (롤 말림)")]
        [Tooltip("종이가 다 나왔을 때 윗부분이 앞으로 넘어오는 각도 (180° 근처 = 바닥 쪽으로 늘어짐)")]
        [Range(0f, 260f)] public float curlMaxDegrees = 190f;
        [Tooltip("이만큼(UI 유닛) 나와야 최대 말림에 도달")]
        public float curlRampDistance = 560f;
        [Tooltip("피드할 때마다 끝부분이 출렁이는 각도")]
        public float feedFlutterDegrees = 18f;
        [Tooltip("뜯을 때 추가로 말려 들어가는 각도")]
        public float tearCurlDegrees = 20f;

        [Header("Behaviour")]
        public bool tapPaperToSkipOrTear = true;
        [Tooltip("CSS px → UI 유닛 배율 (뜯기 이동량 등)")]
        public float unitScale = 1.4f;

        public event Action<SettlementData> PrintStarted;
        public event Action PrintCompleted;
        public event Action TornOff;

        public bool IsPrinting { get; private set; }
        public bool HasReceipt { get; private set; }
        public bool IsTearing { get; private set; }
        public SettlementData CurrentData { get; private set; }

        private Sequence master;
        private Sequence tearSeq;
        private Tween flutterTween;
        private float emerged;
        private float flutter;
        private float tearCurl;
        private float finalReveal;
        private Transform paperHomeParent;
        private int paperHomeSibling;
        private bool initialized;

        private ReceiptRow[] Rows => new[] { headerRow, incomeRow, expenseRow, deliveredRow, returnedRow, netRow, forecastRow };

        // ------------------------------------------------------------------ lifecycle

        private void Awake() => Init();

        private void OnDestroy()
        {
            if (paperGraphic != null) paperGraphic.Clicked -= OnPaperClicked;
            KillTweens();
        }

        private void Init()
        {
            if (initialized) return;
            initialized = true;
            paperHomeParent = paper.parent;
            paperHomeSibling = paper.GetSiblingIndex();
            if (paperGraphic != null) paperGraphic.Clicked += OnPaperClicked;
            ResetState();
            if (printer != null) printer.SetStatus("READY");
        }

        private void Update()
        {
            if (curl == null) return;
            float ramp = curlRampDistance > 0f ? Mathf.Clamp01(emerged / curlRampDistance) : 1f;
            ramp = ramp * ramp * (3f - 2f * ramp);
            curl.degrees = Mathf.Clamp((curlMaxDegrees + flutter) * ramp + tearCurl, 0f, 300f);
        }

        // ------------------------------------------------------------------ public API

        /// <summary>정산 데이터를 받아 영수증을 처음부터 출력한다. 진행 중이던 출력/뜯기는 취소된다.</summary>
        public void Print(SettlementData data)
        {
            Init();
            ResetState();
            CurrentData = (data ?? new SettlementData()).Clone();
            Bind(CurrentData);

            ReceiptRow[] rows = Rows;
            float[] reveals = new float[rows.Length];
            for (int i = 0; i < rows.Length; i++) reveals[i] = rows[i] != null ? RevealHeight(rows[i]) : (i > 0 ? reveals[i - 1] : 0f);
            finalReveal = reveals[rows.Length - 1];

            if (stamp != null)
            {
                stamp.gameObject.SetActive(true);
                stamp.localScale = Vector3.one * 2.4f;
                if (stampGroup != null) stampGroup.alpha = 0f;
            }

            IsPrinting = true;
            HasReceipt = true;
            if (printer != null) printer.SetPrinting(true);
            PrintStarted?.Invoke(CurrentData);

            master = DOTween.Sequence().SetUpdate(true).SetLink(gameObject);
            float prev = 0f;
            float lastStart = 0f;
            for (int i = 0; i < rows.Length; i++)
            {
                ReceiptRow row = rows[i];
                if (row == null) continue;
                float t = StartTime(i);
                float from = prev;
                float to = reveals[i];
                prev = to;
                lastStart = t;

                master.InsertCallback(t, () =>
                {
                    if (sfx != null) sfx.Play(ReceiptSound.Feed);
                    Flutter();
                });
                master.Insert(t, DOVirtual.Float(from, to, feedDuration, SetEmerged).SetEase(Ease.OutQuint));
                master.Insert(t + feedDuration, BuildReveal(row));

                if (row == netRow && stamp != null)
                    master.Insert(t + feedDuration + stampDelay, BuildStamp());
            }
            master.InsertCallback(lastStart + feedDuration + finishDelay, FinishPrinting);
        }

        /// <summary>출력 중이면 남은 연출을 건너뛰고 완성된 영수증 상태로 만든다.</summary>
        public void SkipToEnd() => SkipToEnd(true);

        /// <summary>영수증을 위로 뜯어낸다. 출력 중이면 먼저 완성시킨 뒤 뜯는다.</summary>
        public void TearOff()
        {
            if (!HasReceipt || IsTearing) return;
            if (IsPrinting) SkipToEnd(false);

            IsTearing = true;
            flutterTween?.Kill();
            flutter = 0f;
            if (sfx != null) sfx.Play(ReceiptSound.Tear);
            if (printer != null) printer.SetStatus("CUT");

            if (tearLayer != null) paper.SetParent(tearLayer, true);
            SetPivotKeepPosition(paper, new Vector2(0.5f, 0.5f));

            Vector2 p0 = paper.anchoredPosition;
            float u = unitScale;
            tearSeq = DOTween.Sequence().SetUpdate(true).SetLink(gameObject);
            // 0~26% : 살짝 들어 올리며 반시계 -2.2°(CSS) 기울기
            tearSeq.Append(paper.DOAnchorPos(p0 + new Vector2(0f, 14f * u), 0.156f).SetEase(Ease.InSine));
            tearSeq.Join(paper.DOLocalRotate(new Vector3(0f, 0f, 2.2f), 0.156f).SetEase(Ease.InSine));
            // 26~100% : 가속하며 위로 날아가고 시계 방향 10° 회전 + 페이드 아웃
            tearSeq.Append(paper.DOAnchorPos(p0 + new Vector2(0f, 760f * u), 0.444f).SetEase(Ease.InCubic));
            tearSeq.Join(paper.DOLocalRotate(new Vector3(0f, 0f, -10f), 0.444f).SetEase(Ease.InCubic));
            tearSeq.Join(DOVirtual.Float(1f, 0f, 0.444f, a => paperGroup.alpha = a).SetEase(Ease.InCubic));
            tearSeq.Insert(0f, DOVirtual.Float(0f, tearCurlDegrees, 0.6f, v => tearCurl = v).SetEase(Ease.OutQuad));
            tearSeq.AppendInterval(0.04f);
            tearSeq.AppendCallback(() =>
            {
                ResetState();
                if (printer != null) printer.SetStatus("READY");
                TornOff?.Invoke();
            });
        }

        /// <summary>영수증을 즉시 치우고 대기 상태로 되돌린다.</summary>
        public void ResetState()
        {
            KillTweens();
            if (particles != null) particles.KillAll();

            IsPrinting = false;
            HasReceipt = false;
            IsTearing = false;
            flutter = 0f;
            tearCurl = 0f;

            if (paperHomeParent != null && paper.parent != paperHomeParent)
            {
                paper.SetParent(paperHomeParent, false);
                paper.SetSiblingIndex(paperHomeSibling);
            }
            paper.anchorMin = paper.anchorMax = new Vector2(0.5f, 0f);
            paper.pivot = new Vector2(0.5f, 1f);
            paper.localRotation = Quaternion.identity;
            paper.localScale = Vector3.one;
            SetEmerged(0f);
            if (paperGroup != null) paperGroup.alpha = 1f;

            foreach (var row in Rows)
                if (row != null) row.HideImmediate();

            if (stamp != null)
            {
                if (stampGroup != null) stampGroup.alpha = 0f;
                stamp.gameObject.SetActive(false);
            }

            if (printer != null) printer.SetPrinting(false);
            if (curl != null) curl.degrees = 0f;
        }

        public void SetMuted(bool muted)
        {
            if (sfx != null) sfx.muted = muted;
        }

        // ------------------------------------------------------------------ build steps

        private float StartTime(int i)
        {
            if (feedStartTimes != null && i < feedStartTimes.Length) return feedStartTimes[i];
            float last = feedStartTimes != null && feedStartTimes.Length > 0 ? feedStartTimes[feedStartTimes.Length - 1] : 0f;
            int extra = i - (feedStartTimes?.Length ?? 0) + 1;
            return last + extra * 0.58f;
        }

        private void Bind(SettlementData d)
        {
            if (dateText != null) dateText.text = SettlementFormat.DateLine(d);

            if (incomeRow != null) incomeRow.SetValue(SettlementFormat.Income(Math.Abs(d.income)), incomeColor, true);
            if (expenseRow != null) expenseRow.SetValue(SettlementFormat.Expense(Math.Abs(d.expense)), expenseColor, false);
            if (deliveredRow != null) deliveredRow.SetValue(SettlementFormat.Count(d.delivered), deliveredColor, true);
            if (returnedRow != null) returnedRow.SetValue(SettlementFormat.Count(d.returned), returnedColor, false);

            long net = d.ResolvedNetProfit;
            bool netUp = net >= 0;
            if (netRow != null)
            {
                netRow.SetValue(SettlementFormat.Signed(net), netUp ? netPositiveColor : netNegativeColor, netUp);
                netRow.particleColors = netUp ? netPositiveParticles : netNegativeParticles;
                if (netRow.flashBackground != null)
                {
                    Color f = netUp ? new Color32(0xF5, 0x9E, 0x0B, 0x00) : new Color32(0xF4, 0x3F, 0x5E, 0x00);
                    netRow.flashBackground.color = f;
                }
            }

            if (weatherIcon != null)
            {
                weatherIcon.sprite = WeatherSprite(d.weather);
                weatherIcon.color = WeatherColor(d.weather);
            }
            if (weatherText != null)
            {
                weatherText.text = SettlementFormat.WeatherLine(d);
                if (weatherChip != null)
                {
                    RectTransform tr = weatherText.rectTransform;
                    float w = weatherText.preferredWidth + tr.offsetMin.x - tr.offsetMax.x;
                    weatherChip.sizeDelta = new Vector2(w, weatherChip.sizeDelta.y);
                }
            }
            if (eventText != null) eventText.text = SettlementFormat.EventLine(d);
        }

        private Sprite WeatherSprite(SettlementWeather w)
        {
            switch (w)
            {
                case SettlementWeather.Sunny: return sunnyIcon;
                case SettlementWeather.Cloudy: return cloudyIcon;
                case SettlementWeather.Snow: return snowIcon;
                default: return rainIcon;
            }
        }

        private Color WeatherColor(SettlementWeather w)
        {
            switch (w)
            {
                case SettlementWeather.Sunny: return sunnyColor;
                case SettlementWeather.Cloudy: return cloudyColor;
                case SettlementWeather.Snow: return snowColor;
                default: return rainColor;
            }
        }

        /// <summary>종이 상단부터 행 아래 끝까지 거리 + 여백 = 이 행을 배출구 밖으로 완전히 꺼내는 데 필요한 높이.</summary>
        private float RevealHeight(ReceiptRow row)
        {
            var corners = new Vector3[4];
            row.Rect.GetWorldCorners(corners);
            float minY = float.MaxValue;
            for (int k = 0; k < 4; k++) minY = Mathf.Min(minY, paper.InverseTransformPoint(corners[k]).y);
            float h = paper.rect.yMax - minY + row.revealPad;
            return Mathf.Clamp(h, 0f, paper.rect.height);
        }

        private Sequence BuildReveal(ReceiptRow row)
        {
            var s = DOTween.Sequence();
            switch (row.style)
            {
                case ReceiptRow.RevealStyle.Fade:
                    s.Insert(0f, Fade(row.group, 0f, 1f, 0.2f));
                    break;

                case ReceiptRow.RevealStyle.Calm:
                    s.InsertCallback(0f, () => { if (sfx != null) sfx.Play(ReceiptSound.CalmSlide); });
                    s.Insert(0f, Fade(row.group, 0f, 1f, 0.45f).SetEase(Ease.OutQuad));
                    break;

                default:
                    s.InsertCallback(0f, () =>
                    {
                        if (sfx != null) sfx.Play(row.positive ? ReceiptSound.PopPositive : ReceiptSound.PopNegative);
                        if (particles != null) particles.Burst(row.valueRect, row.particleColors, row.particleCount);
                    });
                    s.Insert(0f, Fade(row.group, 0f, 1f, 0.15f));

                    if (row.flashBackground != null)
                        s.Insert(0f, GraphicAlpha(row.flashBackground, row.flashAlpha, 0f, 0.65f).SetEase(Ease.OutQuad));

                    if (row.valueRect != null)
                    {
                        // metricPopEmphasize: 0.75 → 1.52 (42%) → 0.91 (72%) → 1, 0.56s, 구간마다 오버슈트 커브
                        s.Insert(0f, Scale(row.valueRect, 0.75f, 1.52f, 0.235f).SetEase(Ease.OutBack, 1.1f));
                        s.Insert(0.235f, Scale(row.valueRect, 1.52f, 0.91f, 0.17f).SetEase(Ease.OutBack, 1.1f));
                        s.Insert(0.405f, Scale(row.valueRect, 0.91f, 1f, 0.155f).SetEase(Ease.OutBack, 1.1f));
                    }
                    if (row.valueGlow != null)
                    {
                        s.Insert(0f, GraphicAlpha(row.valueGlow, 0f, 0.7f, 0.235f).SetEase(Ease.OutQuad));
                        s.Insert(0.235f, GraphicAlpha(row.valueGlow, 0.7f, 0f, 0.325f).SetEase(Ease.InOutSine));
                    }
                    if (row.valueText != null)
                    {
                        Color baseC = row.accent;
                        Color bright = Color.Lerp(baseC, Color.white, 0.35f);
                        TMP_Text txt = row.valueText;
                        s.Insert(0f, DOVirtual.Float(0f, 1f, 0.56f, t => txt.color = Color.Lerp(bright, baseC, t)).SetEase(Ease.OutQuad));
                    }
                    break;
            }
            return s;
        }

        private Sequence BuildStamp()
        {
            var s = DOTween.Sequence();
            s.AppendCallback(() =>
            {
                if (sfx != null) sfx.Play(ReceiptSound.Stamp);
            });
            // stampSlam: scale 2.4 → 0.92 → 1.06 → 1, rot(CSS) -18 → -7 → -10 → -8, alpha 0 → 1 → 0.95
            s.Append(StampSegment(2.4f, 0.92f, 18f, 7f, 0f, 1f, 0.204f));
            s.Append(StampSegment(0.92f, 1.06f, 7f, 10f, 1f, 1f, 0.075f));
            s.Append(StampSegment(1.06f, 1f, 10f, 8f, 1f, 0.95f, 0.061f));
            return s;
        }

        private Tween StampSegment(float s0, float s1, float r0, float r1, float a0, float a1, float d)
        {
            return DOVirtual.Float(0f, 1f, d, t =>
            {
                stamp.localScale = Vector3.one * Mathf.LerpUnclamped(s0, s1, t);
                stamp.localEulerAngles = new Vector3(0f, 0f, Mathf.LerpUnclamped(r0, r1, t));
                if (stampGroup != null) stampGroup.alpha = Mathf.LerpUnclamped(a0, a1, t);
            }).SetEase(Ease.OutQuint);
        }

        // ------------------------------------------------------------------ internals

        private void FinishPrinting()
        {
            IsPrinting = false;
            if (printer != null)
            {
                printer.SetPrinting(false);
                printer.SetStatus("DONE");
            }
            PrintCompleted?.Invoke();
        }

        private void SkipToEnd(bool playStampSound)
        {
            if (!IsPrinting) return;
            master?.Kill();
            flutterTween?.Kill();
            flutter = 0f;
            SetEmerged(finalReveal);

            foreach (var row in Rows)
                if (row != null) row.ShowFinal();

            if (stamp != null)
            {
                stamp.gameObject.SetActive(true);
                stamp.localScale = Vector3.one;
                stamp.localEulerAngles = new Vector3(0f, 0f, 8f);
                if (stampGroup != null) stampGroup.alpha = 0.95f;
                if (playStampSound && sfx != null) sfx.Play(ReceiptSound.Stamp);
            }
            FinishPrinting();
        }

        private void OnPaperClicked()
        {
            if (!tapPaperToSkipOrTear) return;
            if (IsPrinting) SkipToEnd();
            else if (HasReceipt) TearOff();
        }

        private void Flutter()
        {
            flutterTween?.Kill();
            flutterTween = DOVirtual.Float(0f, 1f, 0.6f, t =>
                {
                    float decay = (1f - t) * (1f - t);
                    flutter = feedFlutterDegrees * Mathf.Sin(t * Mathf.PI * 2f) * decay;
                })
                .SetEase(Ease.Linear)
                .SetUpdate(true)
                .SetLink(gameObject)
                .OnKill(() => flutter = 0f);
        }

        private void SetEmerged(float v)
        {
            emerged = v;
            paper.anchoredPosition = new Vector2(0f, v);
        }

        private void KillTweens()
        {
            master?.Kill();
            tearSeq?.Kill();
            flutterTween?.Kill();
            master = null;
            tearSeq = null;
            flutterTween = null;
        }

        private static Tween Fade(CanvasGroup g, float from, float to, float d)
        {
            if (g == null) return DOVirtual.DelayedCall(d, () => { });
            return DOVirtual.Float(from, to, d, a => g.alpha = a);
        }

        private static Tween GraphicAlpha(Graphic g, float from, float to, float d)
        {
            return DOVirtual.Float(from, to, d, a =>
            {
                Color c = g.color;
                c.a = a;
                g.color = c;
            });
        }

        private static Tween Scale(RectTransform rt, float from, float to, float d)
        {
            return DOVirtual.Float(from, to, d, s => rt.localScale = new Vector3(s, s, 1f));
        }

        private static void SetPivotKeepPosition(RectTransform rt, Vector2 pivot)
        {
            Vector2 size = rt.rect.size;
            Vector2 delta = pivot - rt.pivot;
            rt.pivot = pivot;
            rt.anchoredPosition += new Vector2(delta.x * size.x, delta.y * size.y);
        }
    }
}
