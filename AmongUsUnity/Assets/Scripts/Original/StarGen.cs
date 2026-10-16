using UnityEngine;

/// <summary>Static starfield mesh used behind the dropship.</summary>
public class StarGen : MonoBehaviour
{
    public int NumStars = 500;
    public float Length = 25;
    public float Width = 25;
    public float MaxStarRadius = 0.05f;
    public Vector2 Direction = new Vector2(0, -60);
    public FloatRange Rates;

    void Start()
    {
        var mf = GetComponent<MeshFilter>(); var mr = GetComponent<MeshRenderer>();
        if (mf == null || mr == null) return;
        var rng = new System.Random(5);
        var v = new Vector3[NumStars * 4]; var c = new Color[NumStars * 4]; var t = new int[NumStars * 6];
        for (int i = 0; i < NumStars; i++)
        {
            float x = (float)(rng.NextDouble() - 0.5) * Width * 2, y = (float)(rng.NextDouble() - 0.5) * Length * 2, r = 0.01f + (float)rng.NextDouble() * MaxStarRadius;
            v[i * 4] = new Vector3(x - r, y - r); v[i * 4 + 1] = new Vector3(x + r, y - r); v[i * 4 + 2] = new Vector3(x + r, y + r); v[i * 4 + 3] = new Vector3(x - r, y + r);
            var col = new Color(1, 1, 1, 0.3f + (float)rng.NextDouble() * 0.7f);
            for (int k = 0; k < 4; k++) c[i * 4 + k] = col;
            t[i * 6] = i * 4; t[i * 6 + 1] = i * 4 + 2; t[i * 6 + 2] = i * 4 + 1; t[i * 6 + 3] = i * 4; t[i * 6 + 4] = i * 4 + 3; t[i * 6 + 5] = i * 4 + 2;
        }
        var m = new Mesh { name = "Stars", vertices = v, colors = c, triangles = t };
        m.RecalculateBounds();
        mf.sharedMesh = m;
        mr.sortingOrder = -200;
    }
}
