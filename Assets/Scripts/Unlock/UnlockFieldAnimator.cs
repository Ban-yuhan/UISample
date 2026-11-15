using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace UnlockSystem
{
    public enum UnlockAnimMode
    {
        Wave = 0,
        Expand = 1,
        Circle = 2,
        Drop = 3,
        Bounce = 4
    }

    /// <summary>
    /// 5종 필드 해금 애니메이션(타일 파도, 필드 확장, 원형 확산, 필드 낙하, 튕김)을 DOTween으로 실행합니다.
    /// </summary>
    public class UnlockFieldAnimator : MonoBehaviour
    {
        [Header("1. Wave (5x5 Tiles)")]
        [SerializeField] private GameObject waveRoot;
        [SerializeField] private Transform[] waveTiles;

        [Header("2. Expand (Left Pivot Slab)")]
        [SerializeField] private Transform expandPivot;
        [SerializeField] private Transform expandEdgeGlow;
        [SerializeField] private Renderer expandEdgeGlowRenderer;

        [Header("3. Circle (URP Shader Reveal)")]
        [SerializeField] private GameObject circleSlab;
        [SerializeField] private Material circleRevealMaterial;
        [SerializeField] private Transform circleGlowRing;
        [SerializeField] private Renderer circleGlowRingRenderer;

        [Header("4. Drop (Sky Fall + Camera Shake + Dust Ring)")]
        [SerializeField] private Transform dropSlab;
        [SerializeField] private Transform dropShadow;
        [SerializeField] private Renderer dropShadowRenderer;
        [SerializeField] private Transform dustRing;
        [SerializeField] private Renderer dustRingRenderer;
        [SerializeField] private Camera mainCamera;

        [Header("5. Bounce (Center Elastic Scale)")]
        [SerializeField] private Transform bounceSlab;

        [Header("Shared Origin & Locked Zone")]
        [SerializeField] private Vector3 revealOrigin = new Vector3(-0.1f, 0f, 0f);
        [SerializeField] private GameObject lockedZoneOutline;

        private static readonly int OriginPropId = Shader.PropertyToID("_Origin");
        private static readonly int RadiusPropId = Shader.PropertyToID("_RevealRadius");
        private static readonly int ColorPropId = Shader.PropertyToID("_BaseColor");
        private static readonly int LegacyColorPropId = Shader.PropertyToID("_Color");

        private readonly List<Tween> _activeTweens = new List<Tween>();
        private Vector3 _cameraInitialPos;
        private bool _cameraPosCached;

        private void Awake()
        {
            CacheCameraPosition();
            ResetField();
        }

        private void OnDestroy()
        {
            KillTweens();
        }

        private void CacheCameraPosition()
        {
            if (!_cameraPosCached && mainCamera != null)
            {
                _cameraInitialPos = mainCamera.transform.position;
                _cameraPosCached = true;
            }
        }

        public void KillTweens()
        {
            for (int i = 0; i < _activeTweens.Count; i++)
            {
                _activeTweens[i]?.Kill();
            }
            _activeTweens.Clear();

            if (mainCamera != null && _cameraPosCached)
            {
                mainCamera.transform.DOKill();
                mainCamera.transform.position = _cameraInitialPos;
            }
        }

        public void ResetField()
        {
            KillTweens();

            if (waveRoot != null)
                waveRoot.SetActive(false);
            if (waveTiles != null)
            {
                for (int i = 0; i < waveTiles.Length; i++)
                {
                    if (waveTiles[i] == null) continue;
                    waveTiles[i].localScale = Vector3.zero;
                    Vector3 lp = waveTiles[i].localPosition;
                    waveTiles[i].localPosition = new Vector3(lp.x, 0f, lp.z);
                }
            }

            if (expandPivot != null)
            {
                expandPivot.gameObject.SetActive(false);
                expandPivot.localScale = new Vector3(0.02f, 1f, 1f);
            }
            if (expandEdgeGlow != null)
            {
                expandEdgeGlow.gameObject.SetActive(false);
                expandEdgeGlow.position = new Vector3(1f, 0.04f, 0f);
                SetRendererAlpha(expandEdgeGlowRenderer, 1f);
            }

            if (circleSlab != null)
                circleSlab.SetActive(false);
            if (circleRevealMaterial != null)
            {
                circleRevealMaterial.SetVector(OriginPropId, new Vector4(revealOrigin.x, revealOrigin.y, revealOrigin.z, 0f));
                circleRevealMaterial.SetFloat(RadiusPropId, 0f);
            }
            if (circleGlowRing != null)
            {
                circleGlowRing.gameObject.SetActive(false);
                circleGlowRing.position = new Vector3(revealOrigin.x, 0.03f, revealOrigin.z);
                circleGlowRing.localScale = new Vector3(0.1f, 0.1f, 1f);
                SetRendererAlpha(circleGlowRingRenderer, 0.9f);
            }

            if (dropSlab != null)
            {
                dropSlab.gameObject.SetActive(false);
                dropSlab.localPosition = new Vector3(0f, 12f, 0f);
                dropSlab.localScale = Vector3.one;
            }
            if (dropShadow != null)
            {
                dropShadow.gameObject.SetActive(false);
                dropShadow.position = new Vector3(3.5f, -0.355f, 0f);
                dropShadow.localScale = new Vector3(0.6f, 0.6f, 1f);
                SetRendererAlpha(dropShadowRenderer, 0.35f);
            }
            if (dustRing != null)
            {
                dustRing.gameObject.SetActive(false);
                dustRing.position = new Vector3(3.5f, -0.35f, 0f);
                dustRing.localScale = new Vector3(4.9f, 4.9f, 1f);
                SetRendererAlpha(dustRingRenderer, 0.85f);
            }

            if (bounceSlab != null)
            {
                bounceSlab.gameObject.SetActive(false);
                bounceSlab.localScale = Vector3.one * 0.02f;
            }

            if (lockedZoneOutline != null)
            {
                lockedZoneOutline.SetActive(true);
            }
        }

        public float PlayUnlockAnimation(UnlockAnimMode mode, Action onDropImpact, Action onComplete)
        {
            ResetField();
            CacheCameraPosition();

            switch (mode)
            {
                case UnlockAnimMode.Wave:
                    return PlayWave(onComplete);
                case UnlockAnimMode.Expand:
                    return PlayExpand(onComplete);
                case UnlockAnimMode.Circle:
                    return PlayCircle(onComplete);
                case UnlockAnimMode.Drop:
                    return PlayDrop(onDropImpact, onComplete);
                case UnlockAnimMode.Bounce:
                    return PlayBounce(onComplete);
                default:
                    return PlayWave(onComplete);
            }
        }

        private float PlayWave(Action onComplete)
        {
            const float totalDuration = 1.25f;
            if (waveRoot == null || waveTiles == null || waveTiles.Length == 0)
            {
                ScheduleComplete(totalDuration, onComplete);
                return totalDuration;
            }

            waveRoot.SetActive(true);

            float maxDist = 0.01f;
            float[] distances = new float[waveTiles.Length];
            for (int i = 0; i < waveTiles.Length; i++)
            {
                if (waveTiles[i] == null) continue;
                Vector3 wp = waveTiles[i].position;
                float d = Vector2.Distance(new Vector2(wp.x, wp.z), new Vector2(revealOrigin.x, revealOrigin.z));
                distances[i] = d;
                if (d > maxDist) maxDist = d;
            }

            for (int i = 0; i < waveTiles.Length; i++)
            {
                Transform tile = waveTiles[i];
                if (tile == null) continue;

                float norm = distances[i] / maxDist;
                float delay = norm * 0.56f;

                tile.localScale = Vector3.zero;
                Vector3 lp = tile.localPosition;
                tile.localPosition = new Vector3(lp.x, 0f, lp.z);

                Tween scaleTween = tile.DOScale(Vector3.one, 0.56f)
                    .SetDelay(delay)
                    .SetEase(Ease.OutBack, 1.7f);
                _activeTweens.Add(scaleTween);

                Sequence hopSeq = DOTween.Sequence();
                hopSeq.SetDelay(delay);
                hopSeq.Append(tile.DOLocalMoveY(0.24f, 0.22f).SetEase(Ease.OutQuad));
                hopSeq.Append(tile.DOLocalMoveY(0f, 0.34f).SetEase(Ease.OutBounce));
                _activeTweens.Add(hopSeq);
            }

            ScheduleComplete(totalDuration, onComplete);
            return totalDuration;
        }

        private float PlayExpand(Action onComplete)
        {
            const float duration = 0.95f;
            if (expandPivot != null)
            {
                expandPivot.gameObject.SetActive(true);
                expandPivot.localScale = new Vector3(0.02f, 1f, 1f);

                Tween t = expandPivot.DOScaleX(1f, duration).SetEase(Ease.OutBack, 1.25f);
                _activeTweens.Add(t);
            }

            if (expandEdgeGlow != null)
            {
                expandEdgeGlow.gameObject.SetActive(true);
                expandEdgeGlow.position = new Vector3(1.0f, 0.04f, 0f);
                SetRendererAlpha(expandEdgeGlowRenderer, 0.9f);

                Tween moveGlow = expandEdgeGlow.DOMoveX(6.0f, duration * 0.85f).SetEase(Ease.OutCubic);
                _activeTweens.Add(moveGlow);

                if (expandEdgeGlowRenderer != null)
                {
                    Tween fadeGlow = DOVirtual.Float(0.9f, 0f, 0.25f, a => SetRendererAlpha(expandEdgeGlowRenderer, a))
                        .SetDelay(duration * 0.72f)
                        .OnComplete(() =>
                        {
                            if (expandEdgeGlow != null)
                                expandEdgeGlow.gameObject.SetActive(false);
                        });
                    _activeTweens.Add(fadeGlow);
                }
            }

            ScheduleComplete(duration, onComplete);
            return duration;
        }

        private float PlayCircle(Action onComplete)
        {
            const float duration = 1.15f;
            const float maxRadius = 6.9f;

            if (circleSlab != null)
                circleSlab.SetActive(true);

            if (circleRevealMaterial != null)
            {
                circleRevealMaterial.SetVector(OriginPropId, new Vector4(revealOrigin.x, revealOrigin.y, revealOrigin.z, 0f));
                circleRevealMaterial.SetFloat(RadiusPropId, 0f);

                Tween rTween = DOTween.To(
                        () => circleRevealMaterial.GetFloat(RadiusPropId),
                        val => circleRevealMaterial.SetFloat(RadiusPropId, val),
                        maxRadius,
                        duration)
                    .SetEase(Ease.OutCubic);
                _activeTweens.Add(rTween);
            }

            if (circleGlowRing != null)
            {
                circleGlowRing.gameObject.SetActive(true);
                circleGlowRing.position = new Vector3(revealOrigin.x, 0.03f, revealOrigin.z);
                circleGlowRing.localScale = new Vector3(0.1f, 0.1f, 1f);
                SetRendererAlpha(circleGlowRingRenderer, 0.85f);

                Tween ringScale = circleGlowRing.DOScale(new Vector3(maxRadius * 2f, maxRadius * 2f, 1f), duration)
                    .SetEase(Ease.OutCubic);
                _activeTweens.Add(ringScale);

                Tween ringFade = DOVirtual.Float(0.85f, 0f, 0.35f, a => SetRendererAlpha(circleGlowRingRenderer, a))
                    .SetDelay(duration - 0.35f)
                    .OnComplete(() =>
                    {
                        if (circleGlowRing != null)
                            circleGlowRing.gameObject.SetActive(false);
                    });
                _activeTweens.Add(ringFade);
            }

            ScheduleComplete(duration, onComplete);
            return duration;
        }

        private float PlayDrop(Action onDropImpact, Action onComplete)
        {
            const float telegraphDuration = 0.35f;
            const float dropStartDelay = 0.22f;
            const float fallDuration = 0.46f;
            const float impactTime = dropStartDelay + fallDuration; // 0.68s
            const float shadowGrowDuration = impactTime - telegraphDuration; // 0.33s
            const float squashDuration = 0.10f;
            const float reboundDuration = 0.14f;
            const float settleDuration = 0.16f;
            const float totalDuration = 1.10f;

            if (dropShadow != null)
            {
                dropShadow.gameObject.SetActive(true);
                dropShadow.position = new Vector3(3.5f, -0.355f, 0f);
                dropShadow.localScale = new Vector3(0.6f, 0.6f, 1f);
                SetRendererAlpha(dropShadowRenderer, 0.35f);

                Sequence shadowSeq = DOTween.Sequence();
                // Phase 1 (0.00s ~ 0.35s): 그림자 전조 — 필드가 떨어지기 전에 먼저 확 커지며 낙하 위치 예고
                shadowSeq.Append(dropShadow.DOScale(new Vector3(4.6f, 4.6f, 1f), telegraphDuration).SetEase(Ease.OutQuad));
                shadowSeq.Join(DOVirtual.Float(0.35f, 0.62f, telegraphDuration, a => SetRendererAlpha(dropShadowRenderer, a)).SetEase(Ease.OutQuad));
                // Phase 2 (0.35s ~ 0.68s): 착지 타이밍에 맞춰 최종 크기(6.4) 및 알파(0.72)로 가속 도달
                shadowSeq.Append(dropShadow.DOScale(new Vector3(6.4f, 6.4f, 1f), shadowGrowDuration).SetEase(Ease.InQuad));
                shadowSeq.Join(DOVirtual.Float(0.62f, 0.72f, shadowGrowDuration, a => SetRendererAlpha(dropShadowRenderer, a)).SetEase(Ease.InQuad));
                // Phase 3 (0.68s ~): 착지 후 서서히 소멸
                shadowSeq.Append(DOVirtual.Float(0.72f, 0f, 0.35f, a => SetRendererAlpha(dropShadowRenderer, a)).SetEase(Ease.OutQuad));
                shadowSeq.OnComplete(() =>
                {
                    if (dropShadow != null)
                        dropShadow.gameObject.SetActive(false);
                });
                _activeTweens.Add(shadowSeq);
            }

            if (dropSlab != null)
            {
                dropSlab.gameObject.SetActive(true);
                dropSlab.localPosition = new Vector3(0f, 12f, 0f);
                dropSlab.localScale = Vector3.one;

                Sequence seq = DOTween.Sequence();
                // Phase 1~2 진입 대기 (0.00s ~ 0.22s): 상공 y = 12f 대기
                seq.AppendInterval(dropStartDelay);

                // Phase 2 (0.22s ~ 0.68s): 가속 낙하 + 세로 스트레치
                seq.Append(dropSlab.DOLocalMoveY(0f, fallDuration).SetEase(Ease.InQuad));
                seq.Join(dropSlab.DOScale(new Vector3(0.90f, 1.20f, 0.90f), fallDuration).SetEase(Ease.InQuad));

                // 착지 순간 임팩트 (0.68s: 카메라 쉐이크 + 먼지 링 + 스파클/사운드)
                seq.AppendCallback(() =>
                {
                    onDropImpact?.Invoke();

                    if (mainCamera != null)
                    {
                        Tween shake = mainCamera.DOShakePosition(0.30f, 0.26f, 18, 90f, true)
                            .OnComplete(() =>
                            {
                                if (mainCamera != null && _cameraPosCached)
                                    mainCamera.transform.position = _cameraInitialPos;
                            });
                        _activeTweens.Add(shake);
                    }

                    if (dustRing != null)
                    {
                        dustRing.gameObject.SetActive(true);
                        dustRing.position = new Vector3(3.5f, -0.35f, 0f);
                        dustRing.localScale = new Vector3(4.9f, 4.9f, 1f);
                        SetRendererAlpha(dustRingRenderer, 0.85f);

                        Tween dustScale = dustRing.DOScale(new Vector3(6.8f, 6.8f, 1f), 0.36f).SetEase(Ease.OutCubic);
                        Tween dustFade = DOVirtual.Float(0.85f, 0f, 0.36f, a => SetRendererAlpha(dustRingRenderer, a))
                            .OnComplete(() =>
                            {
                                if (dustRing != null)
                                    dustRing.gameObject.SetActive(false);
                            });
                        _activeTweens.Add(dustScale);
                        _activeTweens.Add(dustFade);
                    }
                });

                // Phase 3 (0.68s ~ 1.08s): 착지 스쿼시 -> 반동 -> 안착
                seq.Append(dropSlab.DOScale(new Vector3(1.18f, 0.58f, 1.18f), squashDuration).SetEase(Ease.OutQuad));
                seq.Append(dropSlab.DOScale(new Vector3(0.94f, 1.14f, 0.94f), reboundDuration).SetEase(Ease.OutBack));
                seq.Join(dropSlab.DOLocalMoveY(0.18f, reboundDuration).SetEase(Ease.OutQuad));
                seq.Append(dropSlab.DOScale(Vector3.one, settleDuration).SetEase(Ease.OutBounce));
                seq.Join(dropSlab.DOLocalMoveY(0f, settleDuration).SetEase(Ease.OutBounce));

                _activeTweens.Add(seq);
            }

            ScheduleComplete(totalDuration, onComplete);
            return totalDuration;
        }

        private float PlayBounce(Action onComplete)
        {
            const float duration = 1.05f;
            if (bounceSlab != null)
            {
                bounceSlab.gameObject.SetActive(true);
                bounceSlab.localScale = Vector3.one * 0.02f;

                Tween t = bounceSlab.DOScale(Vector3.one, duration).SetEase(Ease.OutElastic, 1.15f, 0.45f);
                _activeTweens.Add(t);
            }

            ScheduleComplete(duration, onComplete);
            return duration;
        }

        private void ScheduleComplete(float duration, Action onComplete)
        {
            Tween wait = DOVirtual.DelayedCall(duration, () =>
            {
                if (lockedZoneOutline != null)
                    lockedZoneOutline.SetActive(false);
                onComplete?.Invoke();
            });
            _activeTweens.Add(wait);
        }

        private static void SetRendererAlpha(Renderer r, float alpha)
        {
            if (r == null || r.sharedMaterial == null) return;
            Material mat = r.material;
            if (mat.HasProperty(ColorPropId))
            {
                Color c = mat.GetColor(ColorPropId);
                c.a = alpha;
                mat.SetColor(ColorPropId, c);
            }
            else if (mat.HasProperty(LegacyColorPropId))
            {
                Color c = mat.GetColor(LegacyColorPropId);
                c.a = alpha;
                mat.SetColor(LegacyColorPropId, c);
            }
        }
    }
}
