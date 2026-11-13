using UnityEngine;
using UnityEngine.UI;

namespace LogisticsTycoon.ClipBoard
{
    /// <summary>세로 그라디언트 (CSS linear-gradient 대용)</summary>
    public class ClipGradient : BaseMeshEffect
    {
        public Color top = Color.white;
        public Color bottom = Color.white;

        public override void ModifyMesh(VertexHelper vh)
        {
            if (!IsActive() || vh.currentVertCount == 0) return;
            var v = new UIVertex();
            float min = float.MaxValue, max = float.MinValue;
            for (int i = 0; i < vh.currentVertCount; i++)
            {
                vh.PopulateUIVertex(ref v, i);
                min = Mathf.Min(min, v.position.y);
                max = Mathf.Max(max, v.position.y);
            }
            float h = Mathf.Max(0.0001f, max - min);
            for (int i = 0; i < vh.currentVertCount; i++)
            {
                vh.PopulateUIVertex(ref v, i);
                v.color *= Color.Lerp(bottom, top, (v.position.y - min) / h);
                vh.SetUIVertex(v, i);
            }
        }
    }
}
