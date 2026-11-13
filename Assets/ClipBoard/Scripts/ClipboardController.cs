using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LogisticsTycoon.ClipBoard
{
    /// <summary>이력서 카드 1장의 런타임 뷰</summary>
    public class ResumeCardView
    {
        public CandidateData data;
        public RectTransform root;
        public Image border;
        public Image fill;
        public TextMeshProUGUI status;
        public bool hovered;
    }

    /// <summary>
    /// 사무실 클립보드 UI (office_clipboard_ui.html 의 uGUI + DOTween 구현).
    /// 정적 계층은 ClipBoardSceneBuilder가 만들고, 변하는 목록(이력서/명부/계약/업그레이드)은 여기서 런타임 생성한다.
    /// </summary>
    public class ClipboardController : MonoBehaviour
    {
        public ClipTheme theme;
        public ClipFx fx;

        [Header("HUD")]
        public TextMeshProUGUI goldText;
        public PiggyBankGauge piggy;
        public Image[] stars;
        public Button settleButton;
        public Button resetButton;
        public RectTransform resetIcon;
        public CanvasGroup toast;
        public TextMeshProUGUI toastText;

        [Header("Clipboard")]
        public RectTransform boardRoot;
        public RectTransform paperFront;
        public RectTransform paperBack1;
        public RectTransform paperBack2;
        public IndexTab[] tabs;
        public CanvasGroup[] panels;
        public TextMeshProUGUI sheetTitle;
        public TextMeshProUGUI sheetDesc;
        public TextMeshProUGUI sheetGlyph;
        public RectTransform sheetGlyphBadge;

        [Header("Tab 1: Hire")]
        public RectTransform resumeList;
        public HireStampTool stampTool;

        [Header("Tab 2: Staff")]
        public RectTransform staffContent;
        public TextMeshProUGUI staffSummary;

        [Header("Tab 3: Contract")]
        public RectTransform contractContent;
        public TextMeshProUGUI contractCount;

        [Header("Tab 4: Upgrade")]
        public RectTransform upgradeContent;

        [Header("Sign Modal")]
        public CanvasGroup signModal;
        public RectTransform signPanel;
        public TextMeshProUGUI modalClientName;
        public SignaturePad signPad;
        public CanvasGroup sealFx;
        public RectTransform sealMark;
        public Button autoSignButton;
        public Button clearSignButton;
        public Button confirmSignButton;
        public Button closeSignButton;

        // ------------------------------------------------------------------ state
        private long gold;
        private float shownGold;
        private int reputation;
        private List<CandidateData> candidates;
        private List<StaffData> staff;
        private List<ContractData> contracts;
        private List<UpgradeData> upgrades;

        private int currentTab;
        private bool stampInHand;
        private string activeSignId;
        private bool sealing;
        private readonly List<ResumeCardView> resumeViews = new List<ResumeCardView>();
        private readonly Dictionary<string, RectTransform> upgradeStatTexts = new Dictionary<string, RectTransform>();

        private static readonly string[] TabTitles = { "알바 이력서 심사", "알바 관리 & 인력 배치", "화주사 납품 계약서", "물류 역량 업그레이드" };
        private static readonly string[] TabDescs =
        {
            "지원자 카드를 확인하고 고용 도장을 끌어서 찍으세요.",
            "이름표를 끌어 작업 구역에 배치하고, 아래로 스크롤해 직원 상태를 관리하세요.",
            "납품 조건을 검토하고 서명란에 전자서명하여 계약을 체결하세요.",
            "플레이어와 알바의 적재량·이동속도·작업속도를 강화합니다."
        };
        private static readonly string[] TabGlyphs = { "印", "名", "約", "力" };

        private const string GoldCountId = "ClipGoldCount";
        private static Color H(string hex, float a = 1f) => ClipUI.Hex(hex, a);

        // ================================================================== lifecycle

        private void Awake()
        {
            ClipUI.Use(theme);
            DOTween.Init();
        }

        private void Start()
        {
            for (int i = 0; i < tabs.Length; i++) tabs[i].onClick = idx => SwitchTab(idx);
            settleButton.onClick.AddListener(DailySettlement);
            resetButton.onClick.AddListener(ResetDemo);
            autoSignButton.onClick.AddListener(() => signPad.AutoSign());
            clearSignButton.onClick.AddListener(() => signPad.Clear());
            confirmSignButton.onClick.AddListener(ConfirmSignature);
            closeSignButton.onClick.AddListener(CloseSignModal);

            signModal.gameObject.SetActive(false);
            toast.alpha = 0f;

            LoadSeed();
            SetGold(gold, false);
            RenderStars(-1);
            RebuildAll();
            SwitchTab(0, true);
        }

        private void LoadSeed()
        {
            gold = ClipboardSeed.InitialGold;
            reputation = ClipboardSeed.InitialReputation;
            candidates = ClipboardSeed.Candidates();
            staff = ClipboardSeed.Staff();
            contracts = ClipboardSeed.Contracts();
            upgrades = ClipboardSeed.Upgrades();
        }

        private void RebuildAll()
        {
            RebuildResumes();
            RebuildStaff();
            RebuildContracts(null);
            RebuildUpgrades();
        }

        // ================================================================== 골드 & 저금통

        private void SetGold(long value, bool animate)
        {
            gold = value;
            float ratio = gold / (float)ClipboardSeed.PiggyFullGold;
            piggy.SetRatio(ratio, animate);

            DOTween.Kill(GoldCountId);
            if (!animate)
            {
                shownGold = gold;
                goldText.text = ClipUI.Gold(gold);
                return;
            }
            // 숫자 카운팅 (모핑)
            DOTween.To(() => shownGold, v => { shownGold = v; goldText.text = ClipUI.Gold((long)Mathf.Round(v)); }, gold, 0.6f)
                .SetEase(Ease.OutCubic).SetId(GoldCountId);
            var rt = goldText.rectTransform;
            rt.DOKill(true);
            rt.DOPunchScale(Vector3.one * 0.3f, 0.45f, 6, 0.5f);
            goldText.DOKill();
            goldText.color = H("#fff7c2");
            goldText.DOColor(H("#fcd34d"), 0.5f);
        }

        private bool Spend(long amount, Vector2 screen)
        {
            if (gold < amount)
            {
                NotifyNotEnoughGold(screen);
                return false;
            }
            SetGold(gold - amount, true);
            return true;
        }

        public void NotifyNotEnoughGold(Vector2 screen)
        {
            fx.FloatText("보유 자금이 부족합니다!", screen + new Vector2(0, 20), H("#dc2626"));
            var rt = goldText.rectTransform;
            rt.DOKill(true);
            rt.DOShakeAnchorPos(0.35f, new Vector2(8f, 0f), 20, 0f);
            goldText.DOKill();
            goldText.color = H("#f87171");
            goldText.DOColor(H("#fcd34d"), 0.6f);
        }

        /// <summary>헤더 [일일 정산] — 계약 수익 - 알바 인건비(시급 합계) 를 정산해 저금통을 채우거나 비운다</summary>
        private void DailySettlement()
        {
            long income = contracts.Where(c => c.signed).Sum(c => (long)c.DailyTotal);
            long cost = staff.Sum(s => (long)s.wage);
            long net = income - cost;
            Vector2 at = RectTransformUtility.WorldToScreenPoint(null, settleButton.transform.position);

            foreach (var s in staff) s.stress = Mathf.Min(100, s.stress + UnityEngine.Random.Range(3, 11));
            RebuildStaff();

            SetGold(Math.Max(0, gold + net), true);
            string msg = net >= 0 ? $"일일 정산 +{net:N0}G" : $"일일 정산 {net:N0}G";
            fx.FloatText(msg, at + new Vector2(0, -60), net >= 0 ? H("#047857") : H("#dc2626"));
            fx.Burst(at, 16, net >= 0 ? new[] { H("#facc15"), H("#fde68a"), H("#10b981") } : new[] { H("#9ca3af"), H("#ef4444") });
            if (income == 0)
                fx.FloatText("계약된 화주사가 없어 수익이 없습니다", at + new Vector2(0, -110), H("#b45309"));
        }

        private void RenderStars(int punchIndex)
        {
            for (int i = 0; i < stars.Length; i++)
            {
                bool on = i < reputation;
                stars[i].DOKill();
                stars[i].DOColor(on ? H("#facc15") : H("#57534e"), 0.25f);
                if (i == punchIndex)
                {
                    stars[i].rectTransform.DOKill(true);
                    stars[i].rectTransform.DOPunchScale(Vector3.one * 0.8f, 0.5f, 6, 0.5f);
                    stars[i].rectTransform.DOPunchRotation(new Vector3(0, 0, 40f), 0.5f, 6);
                }
            }
        }

        // ================================================================== 인덱스 탭

        public void SwitchTab(int idx, bool instant = false)
        {
            if (idx == currentTab && !instant) return;
            if (stampInHand || draggingChip != null) return;
            int prev = currentTab;
            currentTab = idx;

            for (int i = 0; i < tabs.Length; i++) tabs[i].SetActive(i == idx, instant);

            sheetTitle.text = TabTitles[idx];
            sheetDesc.text = TabDescs[idx];
            sheetGlyph.text = TabGlyphs[idx];

            if (idx == 1 && tabs[1].badge != null) tabs[1].badge.SetActive(false);
            if (prev == 1 && !instant)
            {
                // 명부를 보고 나가면 NEW 표시 해제
                if (staff.Any(s => s.isNew))
                {
                    foreach (var s in staff) s.isNew = false;
                    RebuildStaff();
                }
            }

            if (instant)
            {
                for (int i = 0; i < panels.Length; i++)
                {
                    panels[i].gameObject.SetActive(i == idx);
                    panels[i].alpha = 1f;
                }
                RebuildPanel(idx);
                return;
            }

            // 이전 서류 내용 페이드 아웃 → 새 서류 내용 슬라이드 인
            var oldP = panels[prev];
            var newP = panels[idx];
            oldP.DOKill();
            newP.DOKill();
            oldP.DOFade(0f, 0.1f).OnComplete(() => oldP.gameObject.SetActive(false));
            newP.gameObject.SetActive(true);
            RebuildPanel(idx);
            newP.alpha = 0f;
            var newRt = (RectTransform)newP.transform;
            newRt.anchoredPosition = new Vector2(0f, -16f);
            newP.DOFade(1f, 0.22f).SetDelay(0.08f);
            newRt.DOAnchorPos(Vector2.zero, 0.3f).SetDelay(0.08f).SetEase(Ease.OutCubic);

            // 종이 셔플 (paperShuffle 키프레임)
            ShufflePaper(0.42f);

            // 헤더 아이콘 / 제목 팝
            sheetGlyphBadge.DOKill(true);
            sheetGlyphBadge.DOPunchRotation(new Vector3(0, 0, 18f), 0.4f, 6);
            sheetGlyphBadge.DOPunchScale(Vector3.one * 0.18f, 0.4f, 6);
            sheetTitle.rectTransform.DOKill(true);
            sheetTitle.rectTransform.DOPunchAnchorPos(new Vector2(6f, 0f), 0.3f, 6);
        }

        /// <summary>
        /// 탭이 활성화될 때 해당 서류 목록을 다시 그림.
        /// (비활성 계층에서 만든 TMP는 폭 측정이 0이 되어 칩 배치가 어긋나므로 활성 상태에서 재구성)
        /// </summary>
        private void RebuildPanel(int idx)
        {
            switch (idx)
            {
                case 0: RebuildResumes(); break;
                case 1: RebuildStaff(); break;
                case 2: RebuildContracts(null); break;
                case 3: RebuildUpgrades(); break;
            }
        }

        private void ShufflePaper(float dur)
        {
            paperFront.DOKill(true);
            var seq = DOTween.Sequence().SetTarget(paperFront);
            seq.Append(paperFront.DOLocalRotate(new Vector3(0, 0, -1.2f), dur * 0.3f).SetEase(Ease.OutQuad));
            seq.Join(paperFront.DOAnchorPosY(8f, dur * 0.3f).SetEase(Ease.OutQuad));
            seq.Append(paperFront.DOLocalRotate(new Vector3(0, 0, 0.8f), dur * 0.35f).SetEase(Ease.InOutQuad));
            seq.Join(paperFront.DOAnchorPosY(-3f, dur * 0.35f).SetEase(Ease.InOutQuad));
            seq.Append(paperFront.DOLocalRotate(Vector3.zero, dur * 0.35f).SetEase(Ease.OutBack, 2f));
            seq.Join(paperFront.DOAnchorPosY(0f, dur * 0.35f).SetEase(Ease.OutBack, 2f));

            paperBack1.DOKill(true);
            paperBack1.DOPunchRotation(new Vector3(0, 0, 0.9f), dur, 4);
            paperBack2.DOKill(true);
            paperBack2.DOPunchRotation(new Vector3(0, 0, -1.1f), dur, 4);
        }

        private void ShakeBoard(float strength = 9f)
        {
            boardRoot.DOComplete();
            boardRoot.DOShakeAnchorPos(0.28f, strength, 40, 90f, false, true);
        }

        // ================================================================== TAB 1: 이력서 + 도장

        public bool CanUseStamp => currentTab == 0 && !signModal.gameObject.activeSelf;

        public void OnStampPicked()
        {
            stampInHand = true;
        }

        public void OnStampReturned()
        {
            stampInHand = false;
        }

        public bool CanAfford(ResumeCardView v) => gold >= v.data.wage;

        public ResumeCardView ResumeAt(Vector2 screen)
        {
            foreach (var v in resumeViews)
                if (!v.data.hired && RectTransformUtility.RectangleContainsScreenPoint(v.root, screen, null))
                    return v;
            return null;
        }

        /// <summary>도장을 든 채 이력서 위에 올라가면 붉은 점선 테두리로 하이라이트</summary>
        public void HoverResume(Vector2 screen)
        {
            var target = ResumeAt(screen);
            foreach (var v in resumeViews)
            {
                bool h = v == target;
                if (h == v.hovered) continue;
                v.hovered = h;
                v.border.DOKill();
                v.fill.DOKill();
                v.root.DOKill(true);
                v.border.DOColor(h ? H("#ef4444") : H("#d4c3a3"), 0.12f);
                v.fill.DOColor(h ? H("#fef2f2") : H("#fffdfa"), 0.12f);
                v.root.DOScale(h ? 1.015f : 1f, 0.15f).SetEase(Ease.OutBack);
            }
        }

        /// <summary>도장이 쾅 찍힌 순간 (HireStampTool이 호출)</summary>
        public void ConfirmHire(ResumeCardView v, Vector2 screen)
        {
            var c = v.data;
            if (c.hired) return;
            SetGold(gold - c.wage, true);
            c.hired = true;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(v.root, screen, null, out var local);
            Rect r = v.root.rect;
            local.x = Mathf.Clamp(local.x, r.xMin + 100f, r.xMax - 100f);
            local.y = Mathf.Clamp(local.y, r.yMin + 40f, r.yMax - 40f);
            c.stampPos = local;
            c.stampRot = UnityEngine.Random.Range(-12f, -6f);

            ApplyResumeHiredLook(v, true);

            ShakeBoard();
            paperFront.DOComplete();
            paperFront.DOPunchScale(new Vector3(0.006f, -0.006f, 0f), 0.25f, 6);
            fx.Burst(screen, 32, H("#dc2626"), H("#991b1b"), H("#f59e0b"), H("#fef2f2"));
            fx.FloatText($"쾅! {c.name} 고용 완료", screen + new Vector2(0, 70), H("#b91c1c"));

            staff.Insert(0, new StaffData
            {
                id = "s_" + DateTime.Now.Ticks,
                name = c.name,
                trait = c.trait,
                isNew = true,
                stats = c.stats,
                stress = 10,
                wage = c.wage
            });
            RebuildStaff();

            var badge = tabs[1].badge;
            if (badge != null)
            {
                badge.SetActive(true);
                badge.transform.DOKill(true);
                badge.transform.localScale = Vector3.zero;
                badge.transform.DOScale(1f, 0.4f).SetEase(Ease.OutBack, 3f);
            }
        }

        private void RebuildResumes()
        {
            Clear(resumeList);
            resumeViews.Clear();
            foreach (var c in candidates) resumeViews.Add(BuildResumeCard(c));
        }

        private ResumeCardView BuildResumeCard(CandidateData c)
        {
            var v = new ResumeCardView { data = c };
            var fill = ClipUI.Card("Resume_" + c.name, resumeList, H("#fffdfa"), H("#d4c3a3"), 18, 2.5f, out var outer);
            ClipUI.Height(outer, 140);
            v.root = outer.rectTransform;
            v.border = outer;
            v.fill = fill;
            var t = fill.transform;

            // 좌측: 사진 + 성향 + 이름
            var avatar = ClipUI.Img("Avatar", t, theme.round, H(c.avatarBg), 14);
            ClipUI.MidLeft(avatar.rectTransform, 20, 0, 80, 80);
            var avBorder = ClipUI.Img("AvatarEdge", avatar.transform, theme.round, H("#000000", 0.08f), 14);
            ClipUI.Stretch(avBorder.rectTransform, -1, -1, -1, -1);
            avBorder.transform.SetAsFirstSibling();
            var init = ClipUI.Txt("Initial", avatar.transform, c.name.Substring(0, 1), 40, H(c.avatarFg), TextAlignmentOptions.Center, true);
            ClipUI.Stretch(init.rectTransform);

            var chipText = ClipUI.Txt("Trait", t, c.trait, 15, H("#78350f"), TextAlignmentOptions.Center, true);
            float chipW = ClipUI.PrefW(chipText) + 18f;
            var chip = ClipUI.Img("TraitChip", t, theme.round, H("#fef3c7", 0.95f), 6);
            ClipUI.TopLeft(chip.rectTransform, 118, 32, chipW, 26);
            chipText.transform.SetParent(chip.transform, false);
            ClipUI.Stretch(chipText.rectTransform);
            var name = ClipUI.Txt("Name", t, c.name, 28, H("#3b2513"), TextAlignmentOptions.TopLeft, true);
            ClipUI.TopLeft(name.rectTransform, 118, 64, 180, 40);

            // 중앙: 능력치 3종
            var d1 = ClipUI.DashV("DashL", t, H("#e4d5b9"));
            ClipUI.TopLeft(d1.rectTransform, 318, 18, 2, 104);
            float sx = 338, sw = 124, gap = 18, sy = 52;
            ClipUI.StatBar(t, "적재량", $"{c.stats.carry}/10", c.stats.carry / 10f, H("#d97706"), H("#78350f"), sx, sy, sw);
            ClipUI.StatBar(t, "이동속도", $"{c.stats.speed}/10", c.stats.speed / 10f, H("#0ea5e9"), H("#0c4a6e"), sx + sw + gap, sy, sw);
            ClipUI.StatBar(t, "작업속도", $"{c.stats.work}/10", c.stats.work / 10f, H("#10b981"), H("#064e3b"), sx + (sw + gap) * 2, sy, sw);
            var d2 = ClipUI.DashV("DashR", t, H("#e4d5b9"));
            ClipUI.TopLeft(d2.rectTransform, 760, 18, 2, 104);

            // 우측: 시급 + 상태
            var wl = ClipUI.Txt("WageLabel", t, "시급", 14, H("#a8a29e"), TextAlignmentOptions.Right, true);
            ClipUI.MidRight(wl.rectTransform, 24, 34, 160, 20);
            var wv = ClipUI.Txt("Wage", t, ClipUI.Gold(c.wage), 26, H("#4a2e16"), TextAlignmentOptions.Right, true);
            ClipUI.MidRight(wv.rectTransform, 24, 4, 170, 34);
            v.status = ClipUI.Txt("Status", t, "", 16, H("#b45309"), TextAlignmentOptions.Right);
            ClipUI.MidRight(v.status.rectTransform, 24, -30, 170, 22);

            ApplyResumeHiredLook(v, false);
            return v;
        }

        private void ApplyResumeHiredLook(ResumeCardView v, bool animate)
        {
            var c = v.data;
            if (!c.hired)
            {
                v.status.text = "도장 대기중";
                v.status.color = H("#b45309", 0.85f);
                v.status.fontStyle = FontStyles.Italic;
                return;
            }

            v.hovered = false;
            v.border.DOKill();
            v.fill.DOKill();
            v.root.DOKill(true);
            v.root.localScale = Vector3.one;
            v.border.color = H("#d6d3d1");
            v.fill.color = H("#fafaf9");
            v.status.text = "채용 확정";
            v.status.color = H("#b91c1c");
            v.status.fontStyle = FontStyles.Bold;

            var mark = BuildInkMark(v.fill.transform, "HIRED", "고용 승인", new Vector2(190, 70));
            mark.anchoredPosition = c.stampPos;
            if (animate)
            {
                ClipFx.SlamIn(mark, c.stampRot).SetLink(mark.gameObject);
                v.status.rectTransform.DOPunchScale(Vector3.one * 0.35f, 0.4f, 6).SetDelay(0.15f);
            }
            else
            {
                mark.localRotation = Quaternion.Euler(0, 0, c.stampRot);
                mark.gameObject.AddComponent<CanvasGroup>().alpha = 0.96f;
            }
        }

        /// <summary>붉은 이중 테두리 고무도장 자국</summary>
        private RectTransform BuildInkMark(Transform parent, string small, string big, Vector2 size)
        {
            var paperBg = ClipUI.Img("InkMark_" + big, parent, theme.round, H("#ffffff", 0.82f), 10);
            var rt = paperBg.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            var frame = ClipUI.Img("Frame", rt, theme.frame, H("#dc2626"));
            frame.type = Image.Type.Sliced;
            ClipUI.Stretch(frame.rectTransform);
            var s = ClipUI.Txt("Small", rt, small, 12, H("#dc2626"), TextAlignmentOptions.Center, true);
            s.characterSpacing = 18f;
            ClipUI.Stretch(s.rectTransform, 0, 8, 0, size.y * 0.58f);
            var b = ClipUI.Txt("Big", rt, big, size.y * 0.36f, H("#dc2626"), TextAlignmentOptions.Center, true);
            b.characterSpacing = 8f;
            ClipUI.Stretch(b.rectTransform, 0, size.y * 0.3f, 0, 4);
            return rt;
        }

        // ================================================================== TAB 2: 알바 관리 (배치 보드 + 직원 명부)

        private class ZoneView
        {
            public string id;
            public RectTransform rt;
            public Image border, fill;
            public Color baseBorder, baseFill;
            public bool hovered;
        }

        private struct ZoneInfo
        {
            public string label, glyph, title, badgeBg, badgeFg;
        }

        private static readonly string[] WorkZones = { "conveyor", "loading", "packing" };

        private static ZoneInfo Zone(string id)
        {
            switch (id)
            {
                case "conveyor": return new ZoneInfo { label = "1. 박스 분류기", glyph = "分", title = "#1e3a8a", badgeBg = "#dbeafe", badgeFg = "#1e40af" };
                case "loading": return new ZoneInfo { label = "2. 트럭 상차장", glyph = "車", title = "#064e3b", badgeBg = "#d1fae5", badgeFg = "#065f46" };
                case "packing": return new ZoneInfo { label = "3. 정밀 포장대", glyph = "包", title = "#78350f", badgeBg = "#fef3c7", badgeFg = "#92400e" };
                default: return new ZoneInfo { label = "대기실 대기중", glyph = "休", title = "#593a1e", badgeBg = "#e7e5e4", badgeFg = "#44403c" };
            }
        }

        private readonly List<ZoneView> zoneViews = new List<ZoneView>();
        private readonly Dictionary<StaffData, RectTransform> chipViews = new Dictionary<StaffData, RectTransform>();
        private StaffChip draggingChip;
        private RectTransform chipGhost;
        private Vector2 ghostGrabDelta;

        private void RebuildStaff()
        {
            Clear(staffContent);
            zoneViews.Clear();
            chipViews.Clear();
            int total = staff.Sum(s => s.wage);
            int working = staff.Count(s => s.zone != "waiting");
            staffSummary.text = $"총 <b>{staff.Count}</b>명 (배치 {working}) · 시간당 인건비 <b>{total:N0}G</b>";

            // ---- 상단: 인력 배치 보드 ----
            SectionTitle(staffContent, "인력 배치 보드", "이름표를 끌어서 작업 구역에 놓으세요", false);
            BuildWaitingDock();
            BuildZoneRow();

            // ---- 하단: 직원 상세 명부 (스크롤) ----
            SectionTitle(staffContent, "고용 직원 상세 명부 & 스트레스 관리", "아래로 스크롤하여 상세 확인 및 해고", true);
            if (staff.Count == 0)
            {
                Placeholder(staffContent, "현재 고용된 알바 직원이 없습니다. [알바 고용] 탭에서 도장을 찍어 고용하세요.");
                return;
            }
            foreach (var s in staff) BuildStaffRow(s);
        }

        private void SectionTitle(RectTransform parent, string title, string hint, bool dashed)
        {
            var rt = ClipUI.RT("Section_" + title, parent);
            ClipUI.Height(rt, dashed ? 46 : 32);
            if (dashed)
            {
                var dash = ClipUI.DashH("Dash", rt, H("#d4c2a3"));
                StretchTop(dash.rectTransform, 0, 4, 0, 2);
            }
            var tt = ClipUI.Txt("Title", rt, $"<color=#b45309>■</color> {title}", 18, H("#432b16"), TextAlignmentOptions.BottomLeft, true);
            ClipUI.Stretch(tt.rectTransform, 0, 0, 0, 4);
            var ht = ClipUI.Txt("Hint", rt, hint, 14, H("#876649"), TextAlignmentOptions.BottomRight);
            ClipUI.Stretch(ht.rectTransform, 0, 0, 0, 5);
        }

        private void BuildWaitingDock()
        {
            var waiting = staff.Where(s => s.zone == "waiting").ToList();
            const int perRow = 7;
            int rows = Mathf.Max(1, Mathf.CeilToInt(waiting.Count / (float)perRow));
            var fill = ClipUI.Card("WaitingDock", staffContent, H("#f4ead5"), H("#cbb58e"), 14, 2f, out var outer);
            ClipUI.Height(outer, 48 + rows * 48);
            var t = fill.transform;
            var title = ClipUI.Txt("Title", t, "<color=#b45309>休</color> 직원 대기실", 16, H("#593a1e"), TextAlignmentOptions.TopLeft, true);
            ClipUI.TopLeft(title.rectTransform, 14, 10, 300, 24);
            var count = ClipUI.Txt("Count", t, $"{waiting.Count}명 대기", 14, H("#856447"), TextAlignmentOptions.TopRight, true);
            ClipUI.TopRight(count.rectTransform, 14, 11, 160, 22);

            if (waiting.Count == 0)
            {
                var e = ClipUI.Txt("Empty", t, "<i>대기 중인 이름표가 없습니다. 구역의 이름표를 여기로 끌어오면 복귀합니다.</i>", 14, H("#a8a29e"), TextAlignmentOptions.Left);
                ClipUI.TopLeft(e.rectTransform, 16, 44, 800, 36);
            }
            for (int i = 0; i < waiting.Count; i++)
            {
                var chip = BuildChip(t, waiting[i], false);
                ClipUI.TopLeft(chip, 14 + (i % perRow) * 128, 40 + (i / perRow) * 48, 120, 40);
            }
            AddZoneView("waiting", outer, fill);
        }

        private void BuildZoneRow()
        {
            int maxN = Mathf.Max(1, WorkZones.Max(z => staff.Count(s => s.zone == z)));
            var row = ClipUI.RT("WorkZones", staffContent);
            ClipUI.Height(row, 56 + maxN * 48);
            var hl = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            hl.spacing = 12;
            hl.childControlWidth = hl.childControlHeight = true;
            hl.childForceExpandWidth = hl.childForceExpandHeight = true;

            foreach (var id in WorkZones)
            {
                var info = Zone(id);
                var fill = ClipUI.Card("Zone_" + id, row, H("#fffdfa"), H("#d4c3a3"), 14, 2f, out var outer);
                var t = fill.transform;
                var title = ClipUI.Txt("Title", t, info.label, 17, H(info.title), TextAlignmentOptions.Left, true);
                StretchTop(title.rectTransform, 14, 10, 40, 26);
                var glyph = ClipUI.Txt("Glyph", t, info.glyph, 18, H(info.title, 0.75f), TextAlignmentOptions.Right, true);
                StretchTop(glyph.rectTransform, 14, 10, 14, 26);
                var dash = ClipUI.DashH("Dash", t, H("#e5d6bc"));
                StretchTop(dash.rectTransform, 12, 42, 12, 2);

                var here = staff.Where(s => s.zone == id).ToList();
                if (here.Count == 0)
                {
                    var e = ClipUI.Txt("Empty", t, "<i>이름표를 여기로 끌어 놓으세요</i>", 14, H("#9b8265"), TextAlignmentOptions.Center);
                    StretchTop(e.rectTransform, 12, 56, 12, 36);
                }
                for (int i = 0; i < here.Count; i++)
                {
                    var chip = BuildChip(t, here[i], true);
                    StretchTop(chip, 12, 52 + i * 48, 12, 40);
                }
                AddZoneView(id, outer, fill);
            }
        }

        private void AddZoneView(string id, Image outer, Image fill)
        {
            zoneViews.Add(new ZoneView
            {
                id = id, rt = outer.rectTransform, border = outer, fill = fill,
                baseBorder = outer.color, baseFill = fill.color
            });
        }

        /// <summary>드래그 가능한 알바 이름표</summary>
        private RectTransform BuildChip(Transform parent, StaffData s, bool wide)
        {
            var fill = ClipUI.Card("Chip_" + s.name, parent, H("#fdf3d8"), s.isNew ? H("#d97706") : H("#cbb084"), 12, 2f, out var outer);
            outer.raycastTarget = true;
            var g = fill.gameObject.AddComponent<ClipGradient>();
            g.top = H("#fffbeb");
            g.bottom = H("#fde68a");
            var t = fill.transform;
            var grip = ClipUI.Txt("Grip", t, "≡", 18, H("#b45309", 0.6f), TextAlignmentOptions.Center, true);
            ClipUI.MidLeft(grip.rectTransform, 6, 0, 18, 30);
            var name = ClipUI.Txt("Name", t, s.name, 16, H("#452b14"), TextAlignmentOptions.Left, true);
            ClipUI.MidLeft(name.rectTransform, 26, 0, 80, 30);

            if (wide)
            {
                Color sc = s.stress >= 70 ? H("#dc2626") : s.stress >= 45 ? H("#d97706") : H("#78716c");
                var st = ClipUI.Txt("Stress", t, $"스트레스 {s.stress}%", 13, sc, TextAlignmentOptions.Right, s.stress >= 70);
                ClipUI.MidRight(st.rectTransform, 10, 0, 120, 24);
            }
            if (s.isNew)
            {
                var badge = ClipUI.Img("New", t, theme.round, H("#dc2626"), 5);
                ClipUI.MidRight(badge.rectTransform, wide ? 136 : 6, 0, 38, 18);
                var bt = ClipUI.Txt("T", badge.transform, "NEW", 11, Color.white, TextAlignmentOptions.Center, true);
                ClipUI.Stretch(bt.rectTransform);
                outer.DOColor(H("#fbbf24"), 0.6f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetLink(outer.gameObject);
            }

            var chip = outer.gameObject.AddComponent<StaffChip>();
            chip.controller = this;
            chip.staff = s;
            chipViews[s] = outer.rectTransform;
            return outer.rectTransform;
        }

        // ---------------- 이름표 드래그 앤 드롭

        public void HoverChip(StaffChip chip, bool on)
        {
            if (draggingChip != null) return;
            chip.transform.DOKill(true);
            chip.transform.DOScale(on ? 1.05f : 1f, 0.15f).SetEase(Ease.OutBack);
        }

        public void BeginChipDrag(StaffChip chip, Vector2 screen)
        {
            if (draggingChip != null || stampInHand) return;
            draggingChip = chip;
            var src = (RectTransform)chip.transform;
            src.DOKill(true);
            src.localScale = Vector3.one;
            var dragLayer = stampTool.dragLayer;

            // 원본은 흐리게, 고스트 복제본이 포인터를 따라다님
            var srcCg = chip.GetComponent<CanvasGroup>();
            if (srcCg == null) srcCg = chip.gameObject.AddComponent<CanvasGroup>();
            srcCg.alpha = 0.35f;

            var ghostGo = Instantiate(chip.gameObject, dragLayer);
            Destroy(ghostGo.GetComponent<StaffChip>());
            var gcg = ghostGo.GetComponent<CanvasGroup>();
            gcg.alpha = 1f;
            gcg.blocksRaycasts = false;
            chipGhost = (RectTransform)ghostGo.transform;
            chipGhost.anchorMin = chipGhost.anchorMax = new Vector2(0.5f, 0.5f);
            chipGhost.pivot = new Vector2(0.5f, 0.5f);
            chipGhost.sizeDelta = src.rect.size;
            chipGhost.position = src.TransformPoint(src.rect.center);

            var shadow = ClipUI.Img("DragShadow", chipGhost, theme.round, new Color(0, 0, 0, 0.3f), 12);
            ClipUI.Stretch(shadow.rectTransform, 4, 6, -4, -8);
            shadow.transform.SetAsFirstSibling();

            RectTransformUtility.ScreenPointToLocalPointInRectangle(dragLayer, screen, null, out var local);
            ghostGrabDelta = chipGhost.anchoredPosition - local;
            chipGhost.DOScale(1.1f, 0.18f).SetEase(Ease.OutBack, 2.5f);
            chipGhost.DOLocalRotate(new Vector3(0, 0, -4f), 0.18f);

            // 놓을 수 있는 구역 표시
            foreach (var z in zoneViews)
            {
                if (z.id == chip.staff.zone) continue;
                z.border.DOKill();
                z.border.DOColor(H("#e0b872"), 0.15f);
            }
        }

        public void DragChip(StaffChip chip, Vector2 screen)
        {
            if (chip != draggingChip || chipGhost == null) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(stampTool.dragLayer, screen, null, out var local);
            chipGhost.anchoredPosition = local + ghostGrabDelta;

            var target = ZoneAt(screen);
            foreach (var z in zoneViews)
            {
                bool h = z == target && z.id != chip.staff.zone;
                if (h == z.hovered) continue;
                z.hovered = h;
                z.border.DOKill();
                z.fill.DOKill();
                z.rt.DOKill(true);
                z.border.DOColor(h ? H("#f59e0b") : (z.id == chip.staff.zone ? z.baseBorder : H("#e0b872")), 0.12f);
                z.fill.DOColor(h ? H("#fffbeb") : z.baseFill, 0.12f);
                z.rt.DOScale(h ? 1.02f : 1f, 0.15f).SetEase(Ease.OutBack);
            }
        }

        public void EndChipDrag(StaffChip chip, Vector2 screen)
        {
            if (chip != draggingChip) return;
            draggingChip = null;
            var s = chip.staff;
            var target = ZoneAt(screen);
            var ghost = chipGhost;
            chipGhost = null;

            foreach (var z in zoneViews)
            {
                z.hovered = false;
                z.border.DOKill();
                z.fill.DOKill();
                z.rt.DOKill(true);
                z.rt.localScale = Vector3.one;
                z.border.color = z.baseBorder;
                z.fill.color = z.baseFill;
            }

            if (target == null || target.id == s.zone)
            {
                // 제자리로 돌아감
                var src = (RectTransform)chip.transform;
                ghost.DOKill();
                var seq = DOTween.Sequence().SetLink(ghost.gameObject);
                seq.Append(ghost.DOMove(src.TransformPoint(src.rect.center), 0.25f).SetEase(Ease.OutCubic));
                seq.Join(ghost.DOScale(1f, 0.25f));
                seq.Join(ghost.DOLocalRotate(Vector3.zero, 0.25f));
                seq.OnComplete(() =>
                {
                    Destroy(ghost.gameObject);
                    if (chip != null) chip.GetComponent<CanvasGroup>().alpha = 1f;
                });
                return;
            }

            // 배치 확정: 고스트가 구역으로 쏙 들어가고, 새 이름표가 튀어나옴
            s.zone = target.id;
            s.isNew = false;
            ghost.DOKill();
            var gseq = DOTween.Sequence().SetLink(ghost.gameObject);
            gseq.Append(ghost.DOScale(0.8f, 0.12f).SetEase(Ease.InQuad));
            gseq.Join(ghost.GetComponent<CanvasGroup>().DOFade(0f, 0.12f));
            gseq.OnComplete(() => { ghost.gameObject.SetActive(false); Destroy(ghost.gameObject); });

            RebuildStaff();
            if (chipViews.TryGetValue(s, out var placed))
            {
                placed.localScale = Vector3.one * 0.6f;
                placed.DOScale(1f, 0.35f).SetEase(Ease.OutBack, 3f);
            }
            var zv = zoneViews.FirstOrDefault(z => z.id == s.zone);
            if (zv != null) zv.rt.DOPunchScale(new Vector3(0.02f, 0.03f, 0f), 0.3f, 6);

            fx.Burst(screen, 16, H("#f59e0b"), H("#34d399"), H("#fffbeb"));
            fx.FloatText($"{s.name} → {Zone(s.zone).label}", screen + new Vector2(0, 40), H("#b45309"));
        }

        /// <summary>포인터 아래 구역 (스크롤 뷰포트 안에 보이는 부분만)</summary>
        private ZoneView ZoneAt(Vector2 screen)
        {
            var viewport = staffContent.parent as RectTransform;
            if (viewport != null && !RectTransformUtility.RectangleContainsScreenPoint(viewport, screen, null)) return null;
            foreach (var z in zoneViews)
                if (RectTransformUtility.RectangleContainsScreenPoint(z.rt, screen, null)) return z;
            return null;
        }

        private void BuildStaffRow(StaffData s)
        {
            var fill = ClipUI.Card("Staff_" + s.name, staffContent, H("#fffdfa"), H("#d8c7a8"), 14, 2f, out var outer);
            ClipUI.Height(outer, 124);
            var t = fill.transform;

            var av = ClipUI.Img("Avatar", t, theme.round, H("#efe1c6"), 12);
            ClipUI.MidLeft(av.rectTransform, 18, 0, 58, 58);
            var init = ClipUI.Txt("Initial", av.transform, s.name.Substring(0, 1), 26, H("#593a1e"), TextAlignmentOptions.Center, true);
            ClipUI.Stretch(init.rectTransform);

            var name = ClipUI.Txt("Name", t, s.name, 22, H("#3b2513"), TextAlignmentOptions.Left, true);
            ClipUI.MidLeft(name.rectTransform, 90, 16, 120, 30);
            float nameW = ClipUI.PrefW(name);
            var trait = ClipUI.Txt("Trait", t, s.trait, 13, H("#a8a29e"), TextAlignmentOptions.Left);
            ClipUI.MidLeft(trait.rectTransform, 90 + nameW + 8, 14, 120, 22);

            // 현재 배치 구역 뱃지
            var zi = Zone(s.zone);
            var zt = ClipUI.Txt("ZoneText", t, zi.label, 13, H(zi.badgeFg), TextAlignmentOptions.Center, true);
            var zb = ClipUI.Img("ZoneBadge", t, theme.round, H(zi.badgeBg), 5);
            ClipUI.MidLeft(zb.rectTransform, 90, -16, ClipUI.PrefW(zt) + 14f, 24);
            zt.transform.SetParent(zb.transform, false);
            ClipUI.Stretch(zt.rectTransform);
            if (s.isNew)
            {
                var badge = ClipUI.Img("NewBadge", t, theme.round, H("#dc2626"), 6);
                ClipUI.MidLeft(badge.rectTransform, 90 + ClipUI.PrefW(zt) + 22, -16, 46, 22);
                var bt = ClipUI.Txt("T", badge.transform, "NEW", 13, Color.white, TextAlignmentOptions.Center, true);
                ClipUI.Stretch(bt.rectTransform);
                badge.rectTransform.DOScale(1.15f, 0.6f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetLink(badge.gameObject);
                var glow = ClipUI.Img("NewGlow", t, theme.round, H("#f59e0b", 0.22f), 14);
                glow.transform.SetAsFirstSibling();
                ClipUI.Stretch(glow.rectTransform, -2, -2, -2, -2);
                glow.DOFade(0f, 0.6f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetLink(glow.gameObject);
            }

            var d1 = ClipUI.DashV("DashL", t, H("#e5d6bc"));
            ClipUI.TopLeft(d1.rectTransform, 296, 16, 2, 88);
            float x0 = 314, cw = 186, gap = 20;
            ClipUI.StatBar(t, "적재량", $"{s.stats.carry}/10", s.stats.carry / 10f, H("#d97706"), H("#78350f"), x0, 18, cw);
            ClipUI.StatBar(t, "이동속도", $"{s.stats.speed}/10", s.stats.speed / 10f, H("#0ea5e9"), H("#0c4a6e"), x0 + cw + gap, 18, cw);
            ClipUI.StatBar(t, "작업속도", $"{s.stats.work}/10", s.stats.work / 10f, H("#10b981"), H("#064e3b"), x0, 66, cw);
            string stressText = s.stress >= 70 ? "위험" : s.stress >= 45 ? "주의" : "양호";
            Color stressColor = s.stress >= 70 ? H("#ef4444") : s.stress >= 45 ? H("#f59e0b") : H("#fb7185");
            var sb = ClipUI.StatBar(t, $"스트레스({stressText})", $"{s.stress}%", s.stress / 100f, stressColor, s.stress >= 70 ? H("#dc2626") : H("#44403c"), x0 + cw + gap, 66, cw);
            if (s.stress >= 70) sb.DOFade(0.45f, 0.5f).SetLoops(-1, LoopType.Yoyo).SetLink(sb.gameObject);

            var d2 = ClipUI.DashV("DashR", t, H("#e5d6bc"));
            ClipUI.TopLeft(d2.rectTransform, 724, 16, 2, 88);
            var wl = ClipUI.Txt("WageLabel", t, "시급", 13, H("#a8a29e"), TextAlignmentOptions.Right);
            ClipUI.MidRight(wl.rectTransform, 126, 12, 80, 18);
            var wv = ClipUI.Txt("Wage", t, ClipUI.Gold(s.wage), 18, H("#4a2e16"), TextAlignmentOptions.Right, true);
            ClipUI.MidRight(wv.rectTransform, 126, -10, 90, 26);

            var fire = ClipUI.Btn("FireButton", t, "해고", 18, H("#fef2f2"), H("#fca5a5"), H("#b91c1c"), 10, out _);
            ClipUI.MidRight((RectTransform)fire.transform, 16, 0, 96, 46);
            var row = outer.rectTransform;
            fire.onClick.AddListener(() => FireStaff(s, row, fill, fire));
        }

        private void FireStaff(StaffData s, RectTransform row, Image fill, Button btn)
        {
            btn.interactable = false;
            Vector2 at = RectTransformUtility.WorldToScreenPoint(null, btn.transform.position);
            fx.Burst(at, 16, H("#9ca3af"), H("#ef4444"), H("#fca5a5"));
            fx.FloatText($"{s.name} 해고 처리", at + new Vector2(0, 40), H("#dc2626"));
            StampAndCollapse(row, fill, "FIRED", "해  고", () =>
            {
                staff.Remove(s);
                RebuildStaff();
            });
        }

        /// <summary>행 위에 붉은 도장을 쾅 찍고 → 흔들린 뒤 → 높이가 접히며 사라짐</summary>
        private void StampAndCollapse(RectTransform row, Image fill, string small, string big, Action onDone)
        {
            ShakeBoard(5f);
            var mark = BuildInkMark(fill.transform, small, big, new Vector2(170, 66));
            mark.anchoredPosition = new Vector2(60f, 0f);
            var seq = DOTween.Sequence().SetLink(row.gameObject);
            seq.Append(ClipFx.SlamIn(mark, -8f));
            seq.Join(fill.DOColor(H("#fee2e2"), 0.25f));
            seq.Append(fill.rectTransform.DOShakeAnchorPos(0.25f, new Vector2(8f, 0f), 24, 0f));
            seq.AppendInterval(0.15f);

            var le = row.GetComponent<LayoutElement>();
            var cg = row.gameObject.AddComponent<CanvasGroup>();
            float h0 = le.preferredHeight;
            seq.Append(DOTween.To(() => h0, h => { le.preferredHeight = h; le.minHeight = h; }, 0f, 0.3f).SetEase(Ease.InBack));
            seq.Join(cg.DOFade(0f, 0.25f));
            seq.Join(row.DOScaleY(0.2f, 0.3f).SetEase(Ease.InQuad));
            seq.OnComplete(() => onDone());
        }

        // ================================================================== TAB 3: 회사 계약

        private void RebuildContracts(string animateSealId)
        {
            Clear(contractContent);
            contractCount.text = $"체결: {contracts.Count(c => c.signed)}/{contracts.Count}";

            // 상단 계약서 카드 3장
            var rowRt = ClipUI.RT("ContractCards", contractContent);
            ClipUI.Height(rowRt, 336);
            var hl = rowRt.gameObject.AddComponent<HorizontalLayoutGroup>();
            hl.spacing = 16;
            hl.childControlWidth = true;
            hl.childControlHeight = true;
            hl.childForceExpandWidth = true;
            hl.childForceExpandHeight = true;
            foreach (var c in contracts) BuildContractCard(rowRt, c, c.id == animateSealId);

            // 하단: 체결 완료 화주사 명부
            var header = ClipUI.RT("SignedHeader", contractContent);
            ClipUI.Height(header, 44);
            var dash = ClipUI.DashH("Dash", header, H("#d4c2a3"));
            ClipUI.TopLeft(dash.rectTransform, 0, 4, 0, 2);
            dash.rectTransform.anchorMax = new Vector2(1f, 1f);
            dash.rectTransform.sizeDelta = new Vector2(0f, 2f);
            var ht = ClipUI.Txt("Title", header, "체결 완료 화주사 세부 명부 & 계약 관리", 19, H("#432b16"), TextAlignmentOptions.Left, true);
            ClipUI.Stretch(ht.rectTransform, 0, 12, 0, 0);
            var hs = ClipUI.Txt("Hint", header, "아래로 스크롤하여 계약 상세 및 해지 관리", 14, H("#876649"), TextAlignmentOptions.Right);
            ClipUI.Stretch(hs.rectTransform, 0, 12, 0, 0);

            var signed = contracts.Where(c => c.signed).ToList();
            if (signed.Count == 0)
                Placeholder(contractContent, "현재 계약 체결된 화주사가 없습니다. 위 계약서 하단 서명란을 눌러 계약을 진행하세요.");
            else
                foreach (var c in signed) BuildSignedRow(c);
        }

        private void BuildContractCard(RectTransform parent, ContractData c, bool animateSeal)
        {
            var fill = ClipUI.Card("Contract_" + c.client, parent,
                c.signed ? H("#f0fdfa") : H("#fffdfa"), c.signed ? H("#0d9488") : H("#d4c3a3"), 18, 2.5f, out var outer);
            var t = fill.transform;

            var chip = ClipUI.Img("Chip", t, theme.round, H("#ccfbf1"), 5);
            ClipUI.TopLeft(chip.rectTransform, 16, 16, 92, 22);
            var ct = ClipUI.Txt("T", chip.transform, "CONTRACT", 12, H("#115e59"), TextAlignmentOptions.Center, true);
            ct.characterSpacing = 6f;
            ClipUI.Stretch(ct.rectTransform);
            var name = ClipUI.Txt("Client", t, c.client, 24, H("#3b2513"), TextAlignmentOptions.TopLeft, true);
            ClipUI.TopLeft(name.rectTransform, 16, 44, 240, 34);
            var bld = ClipUI.Txt("Glyph", t, "社", 22, H("#9c7854"), TextAlignmentOptions.Center, true);
            ClipUI.TopRight(bld.rectTransform, 14, 22, 32, 32);

            var dash = ClipUI.DashH("Dash", t, H("#e2d3b7"));
            dash.rectTransform.anchorMin = new Vector2(0f, 1f);
            dash.rectTransform.anchorMax = new Vector2(1f, 1f);
            dash.rectTransform.pivot = new Vector2(0.5f, 1f);
            dash.rectTransform.offsetMin = new Vector2(16f, -88f);
            dash.rectTransform.offsetMax = new Vector2(-16f, -86f);

            InfoLine(t, 100, "취급 제품", c.category, H("#4a2e16"));
            InfoLine(t, 130, "일일 납품량", $"{c.dailyBoxes} 박스/일", H("#4a2e16"));
            InfoLine(t, 160, "박스당 단가", ClipUI.Gold(c.revenuePerBox), H("#115e59"));
            var line = ClipUI.Img("Line", t, null, H("#efe4cf"));
            line.rectTransform.anchorMin = new Vector2(0f, 1f);
            line.rectTransform.anchorMax = new Vector2(1f, 1f);
            line.rectTransform.offsetMin = new Vector2(16f, -196f);
            line.rectTransform.offsetMax = new Vector2(-16f, -194f);
            var el = ClipUI.Txt("ExpLabel", t, "예상 일일 수익", 15, H("#57534e"), TextAlignmentOptions.Left, true);
            StretchTop(el.rectTransform, 16, 204, 16, 28);
            var ev = ClipUI.Txt("ExpValue", t, "+" + ClipUI.Gold(c.DailyTotal), 21, H("#92400e"), TextAlignmentOptions.Right, true);
            StretchTop(ev.rectTransform, 16, 202, 16, 30);

            // 카드 하단 서명란
            if (!c.signed)
            {
                var sign = ClipUI.Btn("SignBox", t, "클릭하여 대표자 서명", 17, H("#faf4e6"), H("#bfa682"), H("#7c5c3e"), 12, out var lbl);
                var srt = (RectTransform)sign.transform;
                srt.anchorMin = new Vector2(0f, 0f);
                srt.anchorMax = new Vector2(1f, 0f);
                srt.pivot = new Vector2(0.5f, 0f);
                srt.offsetMin = new Vector2(14f, 14f);
                srt.offsetMax = new Vector2(-14f, 92f);
                var cols = sign.colors;
                cols.highlightedColor = H("#fde68a");
                sign.colors = cols;
                // 서명 유도: 펜이 살짝 흔들리는 느낌
                lbl.rectTransform.DOAnchorPosY(3f, 0.9f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetLink(lbl.gameObject);
                sign.onClick.AddListener(() => OpenSignModal(c));
            }
            else
            {
                var box = ClipUI.Card("SignedBox", t, H("#f4fbf8"), H("#0d9488", 0.5f), 12, 2f, out var bo);
                var brt = bo.rectTransform;
                brt.anchorMin = new Vector2(0f, 0f);
                brt.anchorMax = new Vector2(1f, 0f);
                brt.pivot = new Vector2(0.5f, 0f);
                brt.offsetMin = new Vector2(14f, 14f);
                brt.offsetMax = new Vector2(-14f, 92f);
                var sig = ClipUI.Txt("Signature", box.transform, "<i>센터장 김물류</i>", 26, H("#2d1e12"), TextAlignmentOptions.Center, true);
                ClipUI.Stretch(sig.rectTransform, 0, 0, 74, 0);
                sig.rectTransform.localRotation = Quaternion.Euler(0, 0, 3f);
                var seal = BuildSeal(box.transform, 60f, "계약\n체결", 13);
                seal.anchorMin = seal.anchorMax = new Vector2(1f, 0.5f);
                seal.anchoredPosition = new Vector2(-40f, 0f);
                if (animateSeal)
                {
                    ClipFx.SlamIn(seal, -9f).SetDelay(0.15f).SetLink(seal.gameObject);
                    outer.rectTransform.DOPunchScale(Vector3.one * 0.04f, 0.4f, 6).SetDelay(0.2f);
                }
                else seal.localRotation = Quaternion.Euler(0, 0, -9f);
            }
        }

        private static void StretchTop(RectTransform rt, float l, float top, float r, float h)
        {
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.offsetMin = new Vector2(l, -top - h);
            rt.offsetMax = new Vector2(-r, -top);
        }

        private void InfoLine(Transform t, float y, string label, string value, Color valueColor)
        {
            var l = ClipUI.Txt("L_" + label, t, label, 15, H("#78716c"), TextAlignmentOptions.Left);
            StretchTop(l.rectTransform, 16, y, 16, 24);
            var v = ClipUI.Txt("V_" + label, t, value, 15, valueColor, TextAlignmentOptions.Right, true);
            StretchTop(v.rectTransform, 16, y, 16, 24);
        }

        private RectTransform BuildSeal(Transform parent, float size, string text, float fontSize)
        {
            var bg = ClipUI.Img("Seal", parent, theme.circle, H("#ffffff", 0.88f));
            var rt = bg.rectTransform;
            rt.sizeDelta = new Vector2(size, size);
            var ring = ClipUI.Img("Ring", rt, theme.ring, H("#dc2626"));
            ClipUI.Stretch(ring.rectTransform);
            var tx = ClipUI.Txt("Text", rt, text, fontSize, H("#dc2626"), TextAlignmentOptions.Center, true);
            tx.lineSpacing = -18f;
            ClipUI.Stretch(tx.rectTransform);
            return rt;
        }

        private void BuildSignedRow(ContractData c)
        {
            var fill = ClipUI.Card("Signed_" + c.client, contractContent, H("#fffdfa"), H("#d8c7a8"), 14, 2f, out var outer);
            ClipUI.Height(outer, 118);
            var t = fill.transform;

            var av = ClipUI.Img("Icon", t, theme.round, H("#ccfbf1"), 10);
            ClipUI.MidLeft(av.rectTransform, 16, 0, 56, 56);
            var at = ClipUI.Txt("G", av.transform, "社", 24, H("#115e59"), TextAlignmentOptions.Center, true);
            ClipUI.Stretch(at.rectTransform);
            var name = ClipUI.Txt("Client", t, c.client, 21, H("#3b2513"), TextAlignmentOptions.Left, true);
            ClipUI.MidLeft(name.rectTransform, 86, 16, 200, 28);
            var cycleT = ClipUI.Txt("Cycle", t, c.cycle, 13, H("#115e59"), TextAlignmentOptions.Center, true);
            var cyc = ClipUI.Img("CycleChip", t, theme.round, H("#ccfbf1"), 5);
            ClipUI.MidLeft(cyc.rectTransform, 86, -16, Mathf.Min(200f, ClipUI.PrefW(cycleT) + 14f), 24);
            cycleT.transform.SetParent(cyc.transform, false);
            ClipUI.Stretch(cycleT.rectTransform);

            var d1 = ClipUI.DashV("DashL", t, H("#e5d6bc"));
            ClipUI.TopLeft(d1.rectTransform, 296, 14, 2, 86);
            float x0 = 314, cw = 186, gap = 20;
            ClipUI.StatBar(t, "취급 화물", Short(c.category, 6), 1f, H("#0d9488"), H("#4a2e16"), x0, 14, cw);
            ClipUI.StatBar(t, "일일 물량", $"{c.dailyBoxes}박스", Mathf.Min(1f, c.dailyBoxes / 300f), H("#d97706"), H("#78350f"), x0 + cw + gap, 14, cw);
            ClipUI.StatBar(t, "박스당 단가", ClipUI.Gold(c.revenuePerBox), Mathf.Min(1f, c.revenuePerBox / 1000f), H("#0ea5e9"), H("#0c4a6e"), x0, 62, cw);
            ClipUI.StatBar(t, "화주 신뢰도", $"{c.trust}%", c.trust / 100f, H("#10b981"), H("#047857"), x0 + cw + gap, 62, cw);

            var d2 = ClipUI.DashV("DashR", t, H("#e5d6bc"));
            ClipUI.TopLeft(d2.rectTransform, 714, 14, 2, 86);
            var dl = ClipUI.Txt("DailyLabel", t, "일일 정산액", 12, H("#a8a29e"), TextAlignmentOptions.Right);
            ClipUI.MidRight(dl.rectTransform, 130, 12, 90, 18);
            var dv = ClipUI.Txt("Daily", t, "+" + ClipUI.Gold(c.DailyTotal), 16, H("#78350f"), TextAlignmentOptions.Right, true);
            ClipUI.MidRight(dv.rectTransform, 130, -10, 100, 24);

            var btn = ClipUI.Btn("TerminateButton", t, "계약 해지", 16, H("#fef2f2"), H("#fca5a5"), H("#b91c1c"), 10, out _);
            ClipUI.MidRight((RectTransform)btn.transform, 16, 0, 104, 46);
            var row = outer.rectTransform;
            btn.onClick.AddListener(() => TerminateContract(c, row, fill, btn));
        }

        private static string Short(string s, int n) => s.Length <= n ? s : s.Substring(0, n) + "…";

        private void TerminateContract(ContractData c, RectTransform row, Image fill, Button btn)
        {
            btn.interactable = false;
            Vector2 at = RectTransformUtility.WorldToScreenPoint(null, btn.transform.position);
            fx.Burst(at, 18, H("#9ca3af"), H("#ef4444"), H("#fca5a5"));
            fx.FloatText($"{c.client} 계약 해지됨", at + new Vector2(0, 40), H("#dc2626"));
            int lost = reputation - 1;
            reputation = Mathf.Max(1, reputation - 1);
            RenderStars(lost);
            StampAndCollapse(row, fill, "CANCELED", "계약 해지", () =>
            {
                c.signed = false;
                RebuildContracts(null);
            });
        }

        // ---------------- 전자서명 모달

        private void OpenSignModal(ContractData c)
        {
            if (c.signed || stampInHand) return;
            activeSignId = c.id;
            sealing = false;
            modalClientName.text = $"{c.client} 물류 대행 계약";
            signPad.Clear();
            sealFx.gameObject.SetActive(false);

            signModal.gameObject.SetActive(true);
            signModal.DOKill();
            signPanel.DOKill();
            signModal.alpha = 0f;
            signModal.DOFade(1f, 0.2f);
            signPanel.localScale = Vector3.one * 0.85f;
            signPanel.anchoredPosition = new Vector2(0f, -30f);
            signPanel.DOScale(1f, 0.35f).SetEase(Ease.OutBack, 1.6f);
            signPanel.DOAnchorPos(Vector2.zero, 0.35f).SetEase(Ease.OutCubic);
        }

        private void CloseSignModal()
        {
            if (sealing) return;
            HideSignModal();
        }

        private void HideSignModal()
        {
            activeSignId = null;
            signModal.DOKill();
            signPanel.DOKill();
            signPanel.DOScale(0.92f, 0.18f).SetEase(Ease.InQuad);
            signModal.DOFade(0f, 0.18f).OnComplete(() => signModal.gameObject.SetActive(false));
        }

        private void ConfirmSignature()
        {
            if (sealing) return;
            var c = contracts.FirstOrDefault(x => x.id == activeSignId);
            if (c == null) return;
            sealing = true;

            float wait = 0.2f;
            if (!signPad.HasStroke)
            {
                signPad.AutoSign(0.6f);
                wait = 0.75f;
            }

            DOVirtual.DelayedCall(wait, () =>
            {
                // 직인 쾅
                sealFx.gameObject.SetActive(true);
                sealFx.alpha = 0f;
                sealFx.DOFade(1f, 0.12f);
                ClipFx.SlamIn(sealMark, -9f);
                DOVirtual.DelayedCall(0.2f, () =>
                {
                    signPanel.DOComplete();
                    signPanel.DOShakeAnchorPos(0.25f, 10f, 40);
                    Vector2 sp = RectTransformUtility.WorldToScreenPoint(null, sealMark.position);
                    fx.Burst(sp, 30, H("#dc2626"), H("#f59e0b"), H("#10b981"));
                });

                c.signed = true;
                int gained = reputation;
                reputation = Mathf.Min(5, reputation + 1);
                RenderStars(gained);

                DOVirtual.DelayedCall(0.85f, () =>
                {
                    sealing = false;
                    HideSignModal();
                    RebuildContracts(c.id);
                    fx.FloatText("계약 체결! 평판 +1 ★", new Vector2(Screen.width * 0.5f, Screen.height * 0.55f), H("#0f766e"));
                });
            });
        }

        // ================================================================== TAB 4: 업그레이드

        private void RebuildUpgrades()
        {
            Clear(upgradeContent);
            upgradeStatTexts.Clear();
            SectionHeader(upgradeContent, "플레이어 강화");
            foreach (var u in upgrades.Where(x => !x.staffGroup)) BuildUpgradeRow(u);
            SectionHeader(upgradeContent, "알바 전체 강화");
            foreach (var u in upgrades.Where(x => x.staffGroup)) BuildUpgradeRow(u);
        }

        private void SectionHeader(RectTransform parent, string title)
        {
            var t = ClipUI.Txt("Section_" + title, parent, title, 18, H("#5b3a1e"), TextAlignmentOptions.BottomLeft, true);
            ClipUI.Height(t, 34);
        }

        private void BuildUpgradeRow(UpgradeData u)
        {
            bool isMax = u.level >= u.maxLevel;
            var fill = ClipUI.Card("Upgrade_" + u.id, upgradeContent, H("#fffdfa"), H("#d4c3a3"), 16, 2f, out var outer);
            ClipUI.Height(outer, 100);
            var t = fill.transform;

            var ic = ClipUI.Card("Icon", t, H("#fef3c7"), H("#fcd34d"), 12, 1.5f, out var icOuter);
            ClipUI.MidLeft(icOuter.rectTransform, 16, 0, 60, 60);
            var g = ClipUI.Txt("G", ic.transform, u.glyph, 26, H("#92400e"), TextAlignmentOptions.Center, true);
            ClipUI.Stretch(g.rectTransform);

            var title = ClipUI.Txt("Title", t, u.title, 20, H("#3b2513"), TextAlignmentOptions.Left, true);
            ClipUI.TopLeft(title.rectTransform, 92, 12, 200, 28);
            var lv = ClipUI.Img("Lv", t, theme.round, H("#92400e"), 5);
            ClipUI.TopLeft(lv.rectTransform, 92 + ClipUI.PrefW(title) + 10, 16, 82, 22);
            var lvt = ClipUI.Txt("T", lv.transform, $"LV.{u.level}/{u.maxLevel}", 13, H("#fef3c7"), TextAlignmentOptions.Center, true);
            ClipUI.Stretch(lvt.rectTransform);
            var desc = ClipUI.Txt("Desc", t, u.desc, 14, H("#78716c"), TextAlignmentOptions.Left);
            ClipUI.TopLeft(desc.rectTransform, 92, 42, 520, 22);

            var track = ClipUI.Img("Track", t, theme.round, H("#e7e5e4"), 5);
            ClipUI.TopLeft(track.rectTransform, 92, 72, 440, 10);
            var bar = ClipUI.Img("Fill", track.transform, theme.round, H("#f59e0b"), 5);
            ClipUI.Stretch(bar.rectTransform);
            bar.rectTransform.anchorMax = new Vector2(u.level / (float)u.maxLevel, 1f);
            var stat = ClipUI.Txt("Stat", t, isMax ? $"{u.statValue} (MAX)" : $"{u.statValue} → {u.nextValue}", 16, H("#78350f"), TextAlignmentOptions.Left, true);
            ClipUI.TopLeft(stat.rectTransform, 548, 64, 200, 26);
            upgradeStatTexts[u.id] = stat.rectTransform;
            upgradeStatTexts[u.id + "_lv"] = lv.rectTransform;
            upgradeStatTexts[u.id + "_bar"] = bar.rectTransform;

            var btn = ClipUI.Btn("BuyButton", t, isMax ? "최고 레벨" : $"강화\n<size=14>{u.cost:N0}G</size>", 18,
                isMax ? H("#e7e5e4") : H("#f59e0b"), isMax ? H("#d6d3d1") : H("#b45309"), isMax ? H("#a8a29e") : Color.white, 12, out var bl);
            bl.lineSpacing = -12f;
            ClipUI.MidRight((RectTransform)btn.transform, 16, 0, 130, 68);
            btn.interactable = !isMax;
            if (!isMax) btn.onClick.AddListener(() => PurchaseUpgrade(u, btn));
        }

        private void PurchaseUpgrade(UpgradeData u, Button btn)
        {
            if (u.level >= u.maxLevel) return;
            Vector2 at = RectTransformUtility.WorldToScreenPoint(null, btn.transform.position);
            if (!Spend(u.cost, at)) return;

            float prevFill = (u.level) / (float)u.maxLevel;
            u.level++;
            u.cost = Mathf.RoundToInt(u.cost * 1.45f / 500f) * 500;
            u.statValue = u.nextValue;
            int num = int.Parse(new string(u.statValue.Where(char.IsDigit).ToArray()));
            if (u.statValue.Contains("박스"))
                u.nextValue = (u.statValue.StartsWith("+") ? "+" : "") + (num + 1) + "박스";
            else
            {
                int step = u.id.Contains("work") ? 20 : (u.id.StartsWith("s_") ? 10 : 15);
                u.nextValue = (num + step) + "%";
            }

            RebuildUpgrades();

            // 수치 모핑 팝 + 게이지 차오름
            if (upgradeStatTexts.TryGetValue(u.id, out var st))
                st.DOPunchScale(Vector3.one * 0.38f, 0.45f, 6, 0.5f);
            if (upgradeStatTexts.TryGetValue(u.id + "_lv", out var lv))
                lv.DOPunchScale(Vector3.one * 0.38f, 0.45f, 6, 0.5f);
            if (upgradeStatTexts.TryGetValue(u.id + "_bar", out var bar))
            {
                float target = bar.anchorMax.x;
                bar.anchorMax = new Vector2(prevFill, 1f);
                DOTween.To(() => bar.anchorMax.x, x => bar.anchorMax = new Vector2(x, 1f), target, 0.45f).SetEase(Ease.OutBack).SetLink(bar.gameObject);
            }
            fx.Burst(at, 26, H("#f59e0b"), H("#fbbf24"), H("#10b981"), Color.white);
            fx.FloatText($"LEVEL UP! {u.bonusText}", at + new Vector2(0, 50), H("#b45309"));
        }

        // ================================================================== 시연 초기화

        private void ResetDemo()
        {
            if (stampInHand || sealing) return;
            if (signModal.gameObject.activeSelf) HideSignModal();

            resetIcon.DOKill(true);
            resetIcon.DOLocalRotate(new Vector3(0, 0, 360f), 0.5f, RotateMode.FastBeyond360).SetEase(Ease.OutCubic)
                .OnComplete(() => resetIcon.localRotation = Quaternion.identity);

            LoadSeed();
            SetGold(gold, true);
            RenderStars(-1);
            if (tabs[1].badge != null) tabs[1].badge.SetActive(false);
            RebuildAll();
            ShufflePaper(0.42f);

            Vector2 at = RectTransformUtility.WorldToScreenPoint(null, resetButton.transform.position);
            fx.Burst(at, 20, H("#fbbf24"), H("#f59e0b"), H("#fef3c7"));

            toastText.text = $"시연 상태가 초기값({ClipboardSeed.InitialGold:N0}G / LV.1 강화)으로 리셋되었습니다!";
            var trt = (RectTransform)toast.transform;
            toast.DOKill();
            trt.DOKill();
            float baseY = -118f;
            trt.anchoredPosition = new Vector2(0f, baseY + 10f);
            var seq = DOTween.Sequence().SetTarget(toast);
            seq.Append(toast.DOFade(1f, 0.25f));
            seq.Join(trt.DOAnchorPosY(baseY, 0.3f).SetEase(Ease.OutBack));
            seq.AppendInterval(2.0f);
            seq.Append(toast.DOFade(0f, 0.3f));
            seq.Join(trt.DOAnchorPosY(baseY + 10f, 0.3f));
        }

        // ================================================================== util

        private void Placeholder(RectTransform parent, string text)
        {
            var fill = ClipUI.Card("Empty", parent, H("#f6efe0"), H("#d4c2a3"), 14, 1.5f, out var outer);
            ClipUI.Height(outer, 84);
            var t = ClipUI.Txt("Text", fill.transform, text, 15, H("#a8a29e"), TextAlignmentOptions.Center);
            ClipUI.Stretch(t.rectTransform, 16, 0, 16, 0);
            t.textWrappingMode = TextWrappingModes.Normal;
        }

        private static void Clear(RectTransform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                var child = parent.GetChild(i).gameObject;
                child.SetActive(false);
                Destroy(child);
            }
        }
    }
}
