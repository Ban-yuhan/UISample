using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UnlockSystem
{
    /// <summary>
    /// 지역 해금 전체 시퀀스(일반 모드 vs 즉시 해금 모드), HUD 버튼, 파티클 풀을 총괄합니다.
    /// </summary>
    public class UnlockZoneController : MonoBehaviour
    {
        public enum UnlockPhase
        {
            Locked,
            MovingToPad,
            ChargingPad,
            UnlockingLock,
            AnimatingField,
            Unlocked
        }

        [Header("Core Components")]
        [SerializeField] private UnlockPadUI padUI;
        [SerializeField] private UnlockPadlockVisual padlockVisual;
        [SerializeField] private UnlockFieldAnimator fieldAnimator;
        [SerializeField] private UnlockProceduralAudio proceduralAudio;

        [Header("Player Visual")]
        [SerializeField] private Transform playerRoot;
        [SerializeField] private Transform playerBodyVisual;
        [SerializeField] private Vector3 playerIdlePos = new Vector3(-2.2f, 0f, 0.4f);
        [SerializeField] private Vector3 padStandPos = new Vector3(-0.1f, 0f, 0f);

        [Header("Particle Prefabs / Templates")]
        [SerializeField] private GameObject cashFlyTemplate;
        [SerializeField] private GameObject sparkleTemplate;

        [Header("HUD - Status Card")]
        [SerializeField] private TMP_Text statusBadgeText;
        [SerializeField] private Image statusBadgeBg;

        [Header("HUD - Top Right Buttons")]
        [SerializeField] private Button unlockButton;
        [SerializeField] private Button instantToggleButton;
        [SerializeField] private Image instantToggleBg;
        [SerializeField] private TMP_Text instantToggleText;
        [SerializeField] private Button resetButton;
        [SerializeField] private Button soundButton;
        [SerializeField] private Image soundIconImage;
        [SerializeField] private Sprite soundOnSprite;
        [SerializeField] private Sprite soundOffSprite;

        [Header("HUD - Bottom Mode Bar")]
        [SerializeField] private Button[] modeButtons;
        [SerializeField] private Image[] modeButtonBgs;
        [SerializeField] private TMP_Text[] modeButtonTexts;
        [SerializeField] private TMP_Text modeDescText;

        [Header("HUD - Toast")]
        [SerializeField] private CanvasGroup toastCanvasGroup;
        [SerializeField] private TMP_Text toastText;

        [Header("Palette")]
        [SerializeField] private Color creamColor = new Color(0.980f, 0.961f, 0.922f, 1f);     // #FAF5EB
        [SerializeField] private Color darkBrownColor = new Color(0.231f, 0.137f, 0.078f, 1f); // #3B2314
        [SerializeField] private Color limeActiveColor = new Color(0.478f, 0.780f, 0.118f, 1f); // #7AC71E
        [SerializeField] private Color yellowActiveColor = new Color(1.0f, 0.816f, 0.157f, 1f); // #FFD028
        [SerializeField] private Color badgeWaitColor = new Color(1.0f, 0.910f, 0.639f, 1f);    // #FFE8A3
        [SerializeField] private Color badgeDoneColor = new Color(0.757f, 0.941f, 0.510f, 1f);  // #C1F082

        private static readonly string[] ModeDescriptions =
        {
            "소단위 타일이 물결치듯 순차적으로 튀어오르며 해금",
            "기존 필드의 경계면이 해금 구역 끝까지 주욱 늘어나며 해금",
            "발판 기준 원형 파장이 퍼지며 원형 곡선에 맞춰 필드가 드러남",
            "미해금 구역 크기의 거대한 필드가 하늘에서 낙하해 쿵! 착지",
            "구역 중앙에서 필드가 탄성 있게 튕겨 나오듯 팝업되며 해금"
        };

        private static readonly string[] ModeNames =
        {
            "1. 타일 파도형식",
            "2. 필드확장",
            "3. 원형 확산",
            "4. 필드 낙하",
            "5. 튕김"
        };

        private UnlockAnimMode _currentMode = UnlockAnimMode.Wave;
        private bool _isInstantMode;
        private UnlockPhase _phase = UnlockPhase.Locked;

        public UnlockPhase CurrentPhase => _phase;

        private Sequence _masterSequence;
        private Tween _walkBounceTween;
        private Tween _toastTween;
        private readonly List<GameObject> _fxInstances = new List<GameObject>();

        private float _lastCashSpawnTime;
        private float _lastTickSoundTime;

        private void Awake()
        {
            BindButtons();
        }

        private void Start()
        {
            ResetAll(showToastMsg: false);
            UpdateModeButtonsUI();
            UpdateInstantToggleUI();
            UpdateSoundUI();
        }

        private void OnDestroy()
        {
            KillAllSequences();
        }

        private void BindButtons()
        {
            if (unlockButton != null)
                unlockButton.onClick.AddListener(OnClickUnlock);

            if (instantToggleButton != null)
                instantToggleButton.onClick.AddListener(OnClickInstantToggle);

            if (resetButton != null)
                resetButton.onClick.AddListener(OnClickReset);

            if (soundButton != null)
                soundButton.onClick.AddListener(OnClickSoundToggle);

            if (modeButtons != null)
            {
                for (int i = 0; i < modeButtons.Length; i++)
                {
                    int idx = i;
                    if (modeButtons[i] != null)
                        modeButtons[i].onClick.AddListener(() => OnClickModeButton(idx));
                }
            }
        }

        private void OnClickUnlock()
        {
            if (proceduralAudio != null)
                proceduralAudio.PlayButtonClick();

            if (_isInstantMode)
            {
                StartInstantUnlockSequence();
            }
            else
            {
                StartNormalUnlockSequence();
            }
        }

        private void OnClickInstantToggle()
        {
            if (proceduralAudio != null)
                proceduralAudio.PlayButtonClick();

            _isInstantMode = !_isInstantMode;
            UpdateInstantToggleUI();
            ShowToast(_isInstantMode
                ? "즉시 해금 ON: 모드 클릭만으로 바로 재생!"
                : "즉시 해금 OFF: 해금 버튼으로 전체 연출 재생");
        }

        private void OnClickReset()
        {
            if (proceduralAudio != null)
                proceduralAudio.PlayButtonClick();

            ResetAll(showToastMsg: true);
        }

        private void OnClickSoundToggle()
        {
            if (proceduralAudio == null) return;
            proceduralAudio.SoundEnabled = !proceduralAudio.SoundEnabled;
            if (proceduralAudio.SoundEnabled)
                proceduralAudio.PlayButtonClick();

            UpdateSoundUI();
            ShowToast(proceduralAudio.SoundEnabled ? "효과음 켜짐" : "효과음 꺼짐");
        }

        private void OnClickModeButton(int index)
        {
            if (index < 0 || index > 4) return;

            if (proceduralAudio != null)
                proceduralAudio.PlayButtonClick();

            _currentMode = (UnlockAnimMode)index;
            UpdateModeButtonsUI();

            if (modeButtons != null && index < modeButtons.Length && modeButtons[index] != null)
            {
                modeButtons[index].transform.DOKill();
                modeButtons[index].transform.localScale = Vector3.one;
                modeButtons[index].transform.DOPunchScale(Vector3.one * 0.08f, 0.22f, 4, 0.5f);
            }

            if (_isInstantMode)
            {
                StartInstantUnlockSequence();
            }
            else
            {
                ShowToast($"모드 선택: {ModeNames[index]}");
            }
        }

        /// <summary>
        /// 일반 모드: 캐릭터 이동(0.55s) -> 발판 충전+지폐비행(1.38s) -> 자물쇠 해제(0.52s) -> 필드 해금
        /// </summary>
        private void StartNormalUnlockSequence()
        {
            ResetAll(showToastMsg: false);

            _masterSequence = DOTween.Sequence();

            // Step 1: 캐릭터 이동 (0.55s)
            _masterSequence.AppendCallback(() =>
            {
                _phase = UnlockPhase.MovingToPad;
                UpdateStatusBadge("발판으로 이동 중...", badgeWaitColor);
                StartWalkBounce();

                if (playerRoot != null)
                {
                    Vector3 dir = (padStandPos - playerRoot.position);
                    dir.y = 0f;
                    if (dir.sqrMagnitude > 0.001f)
                    {
                        playerRoot.DORotateQuaternion(Quaternion.LookRotation(dir), 0.18f);
                    }
                }
            });

            if (playerRoot != null)
            {
                _masterSequence.Append(playerRoot.DOMove(padStandPos, 0.55f).SetEase(Ease.InOutSine));
            }
            else
            {
                _masterSequence.AppendInterval(0.55f);
            }

            _masterSequence.AppendCallback(() =>
            {
                StopWalkBounce();
                if (playerRoot != null)
                {
                    Vector3 lookRight = new Vector3(1f, 0f, 0f);
                    playerRoot.DORotateQuaternion(Quaternion.LookRotation(lookRight), 0.15f);
                }
            });

            // Step 2: 발판 게이지 충전 + 지폐 비행 (1.38s)
            _masterSequence.AppendCallback(() =>
            {
                _phase = UnlockPhase.ChargingPad;
                _lastCashSpawnTime = -1f;
                _lastTickSoundTime = -1f;
            });

            float chargeProgress = 0f;
            _masterSequence.Append(
                DOTween.To(() => chargeProgress, v =>
                {
                    chargeProgress = v;
                    if (padUI != null) padUI.SetProgress(v);
                    int pct = Mathf.RoundToInt(v * 100f);
                    UpdateStatusBadge($"해금 충전 중... {pct}%", badgeWaitColor);

                    float now = Time.time;
                    if (now - _lastCashSpawnTime >= 0.11f)
                    {
                        _lastCashSpawnTime = now;
                        SpawnCashFly();
                    }
                    if (now - _lastTickSoundTime >= 0.10f)
                    {
                        _lastTickSoundTime = now;
                        if (proceduralAudio != null)
                            proceduralAudio.PlayCashTick(v);
                    }
                }, 1f, 1.38f).SetEase(Ease.Linear)
            );

            _masterSequence.AppendCallback(() =>
            {
                if (padUI != null)
                    padUI.PlayChargeCompletePulse();
            });

            // Step 3 & Step 4 이어서 실행
            AppendLockAndFieldSteps(_masterSequence);
        }

        /// <summary>
        /// 즉시 해금 모드: 필드 리셋 후 곧바로 자물쇠 해제 -> 필드 해금 애니메이션 실행
        /// </summary>
        private void StartInstantUnlockSequence()
        {
            ResetAll(showToastMsg: false);
            if (padUI != null)
                padUI.SetProgress(1f);

            _masterSequence = DOTween.Sequence();
            AppendLockAndFieldSteps(_masterSequence);
        }

        private void AppendLockAndFieldSteps(Sequence seq)
        {
            // Step 3: 자물쇠 해제 (0.52s)
            seq.AppendCallback(() =>
            {
                _phase = UnlockPhase.UnlockingLock;
                UpdateStatusBadge("자물쇠 해제!", badgeWaitColor);

                if (padlockVisual != null)
                {
                    padlockVisual.PlayUnlockSequence(() =>
                    {
                        if (proceduralAudio != null)
                            proceduralAudio.PlayLockPop();
                        SpawnSparkleBurst(new Vector3(3.5f, 0.6f, 0f), 10);
                    });
                }
                else if (proceduralAudio != null)
                {
                    proceduralAudio.PlayLockPop();
                }
            });

            seq.AppendInterval(0.52f);

            // Step 4: 선택된 필드 해금 애니메이션
            seq.AppendCallback(() =>
            {
                _phase = UnlockPhase.AnimatingField;
                UpdateStatusBadge("필드 생성 중!", badgeWaitColor);

                if (_currentMode != UnlockAnimMode.Drop && proceduralAudio != null)
                {
                    proceduralAudio.PlayUnlockFanfare();
                }

                if (fieldAnimator != null)
                {
                    fieldAnimator.PlayUnlockAnimation(
                        _currentMode,
                        onDropImpact: () =>
                        {
                            if (proceduralAudio != null)
                                proceduralAudio.PlayDropImpact();
                            SpawnSparkleBurst(new Vector3(3.5f, 0.35f, 0f), 16);
                        },
                        onComplete: () =>
                        {
                            _phase = UnlockPhase.Unlocked;
                            UpdateStatusBadge("새 구역 해금 완료!", badgeDoneColor);
                            if (padUI != null)
                                padUI.SetPadDimmed(true);
                            SpawnSparkleBurst(new Vector3(3.5f, 0.5f, 0f), 14);
                        }
                    );
                }
                else
                {
                    _phase = UnlockPhase.Unlocked;
                    UpdateStatusBadge("새 구역 해금 완료!", badgeDoneColor);
                }
            });
        }

        public void ResetAll(bool showToastMsg)
        {
            KillAllSequences();

            _phase = UnlockPhase.Locked;

            if (proceduralAudio != null)
                proceduralAudio.StopAllPending();

            if (playerRoot != null)
            {
                playerRoot.position = playerIdlePos;
                playerRoot.rotation = Quaternion.LookRotation(new Vector3(1f, 0f, -0.2f));
            }
            StopWalkBounce();

            if (padUI != null)
                padUI.ResetPad();

            if (padlockVisual != null)
                padlockVisual.ResetLock();

            if (fieldAnimator != null)
                fieldAnimator.ResetField();

            ClearFxInstances();
            UpdateStatusBadge("대기 중 (미해금)", badgeWaitColor);

            if (showToastMsg)
                ShowToast("필드가 다시 잠겼습니다.");
        }

        private void KillAllSequences()
        {
            _masterSequence?.Kill();
            _masterSequence = null;

            StopWalkBounce();

            if (playerRoot != null)
                playerRoot.DOKill();

            if (padUI != null)
                padUI.KillTweens();

            if (padlockVisual != null)
                padlockVisual.KillTweens();

            if (fieldAnimator != null)
                fieldAnimator.KillTweens();
        }

        private void StartWalkBounce()
        {
            StopWalkBounce();
            if (playerBodyVisual == null) return;
            playerBodyVisual.localPosition = Vector3.zero;
            _walkBounceTween = playerBodyVisual
                .DOLocalMoveY(0.14f, 0.14f)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);
        }

        private void StopWalkBounce()
        {
            _walkBounceTween?.Kill();
            _walkBounceTween = null;
            if (playerBodyVisual != null)
                playerBodyVisual.localPosition = Vector3.zero;
        }

        private void SpawnCashFly()
        {
            if (cashFlyTemplate == null) return;

            Vector3 startPos = (playerRoot != null ? playerRoot.position : padStandPos) + new Vector3(0f, 0.7f, 0f);
            Vector3 endPos = new Vector3(padStandPos.x + Random.Range(-0.22f, 0.22f), 0.12f, padStandPos.z + Random.Range(-0.22f, 0.22f));

            GameObject bill = Instantiate(cashFlyTemplate, startPos, cashFlyTemplate.transform.rotation);
            bill.SetActive(true);
            _fxInstances.Add(bill);

            float dur = 0.36f;
            bill.transform.DOJump(endPos, 0.75f, 1, dur).SetEase(Ease.OutQuad);
            bill.transform.DOScale(Vector3.one * 0.65f, dur).SetEase(Ease.InQuad);
            bill.transform.DOLocalRotate(new Vector3(0f, 0f, Random.Range(-45f, 45f)), dur, RotateMode.FastBeyond360)
                .OnComplete(() =>
                {
                    if (bill != null)
                    {
                        _fxInstances.Remove(bill);
                        Destroy(bill);
                    }
                });
        }

        private void SpawnSparkleBurst(Vector3 center, int count)
        {
            if (sparkleTemplate == null) return;

            for (int i = 0; i < count; i++)
            {
                GameObject sp = Instantiate(sparkleTemplate, center, sparkleTemplate.transform.rotation);
                sp.SetActive(true);
                _fxInstances.Add(sp);

                float angle = (Mathf.PI * 2f * i) / count + Random.Range(-0.2f, 0.2f);
                float radius = Random.Range(0.7f, 1.85f);
                Vector3 target = center + new Vector3(Mathf.Cos(angle) * radius, Random.Range(0.35f, 1.1f), Mathf.Sin(angle) * radius);

                float dur = Random.Range(0.38f, 0.58f);
                sp.transform.localScale = Vector3.one * Random.Range(0.6f, 1.15f);
                sp.transform.DOMove(target, dur).SetEase(Ease.OutCubic);
                sp.transform.DOScale(Vector3.zero, dur * 0.6f)
                    .SetDelay(dur * 0.4f)
                    .SetEase(Ease.InQuad)
                    .OnComplete(() =>
                    {
                        if (sp != null)
                        {
                            _fxInstances.Remove(sp);
                            Destroy(sp);
                        }
                    });
            }
        }

        private void ClearFxInstances()
        {
            for (int i = _fxInstances.Count - 1; i >= 0; i--)
            {
                if (_fxInstances[i] != null)
                {
                    _fxInstances[i].transform.DOKill();
                    Destroy(_fxInstances[i]);
                }
            }
            _fxInstances.Clear();
        }

        private void UpdateStatusBadge(string message, Color bgColor)
        {
            if (statusBadgeText != null)
                statusBadgeText.text = message;
            if (statusBadgeBg != null)
                statusBadgeBg.color = bgColor;
        }

        private void UpdateModeButtonsUI()
        {
            int selectedIdx = (int)_currentMode;
            if (modeButtons == null) return;

            for (int i = 0; i < modeButtons.Length; i++)
            {
                bool active = (i == selectedIdx);
                if (modeButtonBgs != null && i < modeButtonBgs.Length && modeButtonBgs[i] != null)
                    modeButtonBgs[i].color = active ? limeActiveColor : creamColor;

                if (modeButtonTexts != null && i < modeButtonTexts.Length && modeButtonTexts[i] != null)
                    modeButtonTexts[i].color = active ? Color.white : darkBrownColor;
            }

            if (modeDescText != null && selectedIdx >= 0 && selectedIdx < ModeDescriptions.Length)
                modeDescText.text = ModeDescriptions[selectedIdx];
        }

        private void UpdateInstantToggleUI()
        {
            if (instantToggleBg != null)
                instantToggleBg.color = _isInstantMode ? yellowActiveColor : creamColor;

            if (instantToggleText != null)
                instantToggleText.text = _isInstantMode ? "즉시 해금: ON" : "즉시 해금: OFF";
        }

        private void UpdateSoundUI()
        {
            if (soundIconImage == null || proceduralAudio == null) return;
            soundIconImage.sprite = proceduralAudio.SoundEnabled ? soundOnSprite : soundOffSprite;
        }

        private void ShowToast(string msg)
        {
            if (toastCanvasGroup == null || toastText == null) return;

            toastText.text = msg;
            _toastTween?.Kill();
            toastCanvasGroup.alpha = 1f;
            _toastTween = toastCanvasGroup
                .DOFade(0f, 0.35f)
                .SetDelay(1.45f)
                .SetEase(Ease.InQuad);
        }
    }
}
