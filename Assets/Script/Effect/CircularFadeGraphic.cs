using UnityEngine;
using UnityEngine.UI;

// 별도 셰이더나 텍스처 없이 원 바깥쪽을 검게 덮는 UI 메시.
public class CircularFadeGraphic : MaskableGraphic
{
    private const int Segments = 128;
    private float openness;
    public float Openness
    {
        get => openness;
        set { openness = Mathf.Clamp01(value); SetVerticesDirty(); }
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        Rect rect = GetPixelAdjustedRect();
        Vector2 center = rect.center;
        float diagonal = rect.size.magnitude * 0.5f;
        const float feather = 8f;
        float radius = openness * (diagonal + feather);
        if (openness <= 0f)
        {
            vh.AddVert(new Vector3(rect.xMin, rect.yMin), color, Vector2.zero);
            vh.AddVert(new Vector3(rect.xMin, rect.yMax), color, Vector2.zero);
            vh.AddVert(new Vector3(rect.xMax, rect.yMax), color, Vector2.zero);
            vh.AddVert(new Vector3(rect.xMax, rect.yMin), color, Vector2.zero);
            vh.AddTriangle(0, 1, 2); vh.AddTriangle(0, 2, 3);
            return;
        }
        Color transparent = color;
        transparent.a = 0f;
        for (int i = 0; i <= Segments; i++)
        {
            float angle = i * Mathf.PI * 2f / Segments;
            Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            vh.AddVert(center + direction * radius, transparent, Vector2.zero);
            vh.AddVert(center + direction * (radius + feather), color, Vector2.zero);
            vh.AddVert(center + direction * (diagonal + feather * 3f), color, Vector2.zero);
            if (i == Segments) continue;
            int index = i * 3;
            vh.AddTriangle(index, index + 3, index + 1);
            vh.AddTriangle(index + 1, index + 3, index + 4);
            vh.AddTriangle(index + 1, index + 4, index + 2);
            vh.AddTriangle(index + 2, index + 4, index + 5);
        }
    }
}
