using UnityEngine;
using UnityEngine.UI;

namespace LogisticsTycoon.Settlement
{
    /// <summary>Graphic의 정점 색에 위→아래 그라데이션을 곱한다 (CSS linear-gradient 180deg 대응, 중간 색 1개 지원).</summary>
    [DisallowMultipleComponent]
    public class UIVerticalGradient : BaseMeshEffect
    {
        public Color top = Color.white;
        public Color bottom = Color.white;
        public bool useMiddle;
        public Color middle = Color.white;
        [Range(0.01f, 0.99f)] public float middlePosition = 0.45f;

        public override void ModifyMesh(VertexHelper vh)
        {
            if (!IsActive() || vh.currentVertCount == 0) return;
            Rect r = graphic.rectTransform.rect;
            UIVertex v = default;
            for (int i = 0; i < vh.currentVertCount; i++)
            {
                vh.PopulateUIVertex(ref v, i);
                float t = r.height > 0f ? Mathf.Clamp01((r.yMax - v.position.y) / r.height) : 0f; // 0 = 위
                Color g;
                if (useMiddle)
                    g = t < middlePosition
                        ? Color.Lerp(top, middle, t / middlePosition)
                        : Color.Lerp(middle, bottom, (t - middlePosition) / (1f - middlePosition));
                else
                    g = Color.Lerp(top, bottom, t);
                v.color = (Color32)((Color)v.color * g);
                vh.SetUIVertex(v, i);
            }
        }
    }
}
