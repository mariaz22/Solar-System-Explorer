using UnityEngine;

public class SaturnRings : MonoBehaviour
{
    public float innerRatio = 1.20f;
    public float outerRatio = 2.50f;
    public int segments = 256;

    void Start()
    {
        Build();
    }

    void Build()
    {
        var existing = transform.Find("Rings");
        if (existing != null) return;

        var go = new GameObject("Rings");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.Euler(20f, 0f, 0f);

        var mf = go.AddComponent<MeshFilter>();
        var mr = go.AddComponent<MeshRenderer>();

        var mesh = new Mesh { name = "RingMesh" };
        int vertCount = (segments + 1) * 2;
        var verts = new Vector3[vertCount];
        var uvs   = new Vector2[vertCount];
        var tris  = new int[segments * 6 * 2];

        // Multiply by 0.5 because Unity's unit sphere has radius 0.5 in local space,
        // so ratios relative to planet radius need this correction for the child GO.
        float rIn  = innerRatio * 0.5f;
        float rOut = outerRatio * 0.5f;
        for (int i = 0; i <= segments; i++)
        {
            float t = i / (float)segments * Mathf.PI * 2f;
            float cos = Mathf.Cos(t);
            float sin = Mathf.Sin(t);
            verts[i * 2]     = new Vector3(cos * rIn,  0f, sin * rIn);
            verts[i * 2 + 1] = new Vector3(cos * rOut, 0f, sin * rOut);
            uvs[i * 2]     = new Vector2(i / (float)segments, 0f);
            uvs[i * 2 + 1] = new Vector2(i / (float)segments, 1f);
        }

        int tri = 0;
        for (int i = 0; i < segments; i++)
        {
            int a = i * 2, b = i * 2 + 1, c = (i + 1) * 2, d = (i + 1) * 2 + 1;
            tris[tri++] = a; tris[tri++] = c; tris[tri++] = b;
            tris[tri++] = b; tris[tri++] = c; tris[tri++] = d;
            tris[tri++] = a; tris[tri++] = b; tris[tri++] = c;
            tris[tri++] = b; tris[tri++] = d; tris[tri++] = c;
        }

        mesh.vertices = verts;
        mesh.uv = uvs;
        mesh.triangles = tris;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        mf.mesh = mesh;

        var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Transparent");
        var mat = new Material(shader);
        if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f);
        if (mat.HasProperty("_Blend"))   mat.SetFloat("_Blend",   1f);
        if (mat.HasProperty("_SrcBlend")) mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        if (mat.HasProperty("_DstBlend")) mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        if (mat.HasProperty("_ZWrite"))  mat.SetFloat("_ZWrite",   0f);
        if (mat.HasProperty("_Cull"))    mat.SetFloat("_Cull",     0f);
        mat.SetOverrideTag("RenderType", "Transparent");
        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");

        var ringTex = Resources.Load<Texture2D>("PlanetTextures/SaturnRingAlpha") ?? BuildRingTexture();
        if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", ringTex);
        if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", ringTex);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);
        mr.material = mat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;
    }

    static Texture2D BuildRingTexture()
    {
        const int W = 512, H = 4;
        var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
        tex.name = "RingGradient";
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        var pixels = new Color32[W * H];
        for (int y = 0; y < H; y++)
        {
            for (int x = 0; x < W; x++)
            {
                float t = x / (float)(W - 1);
                Color32 c = RingColor(t);
                pixels[y * W + x] = c;
            }
        }
        tex.SetPixels32(pixels);
        tex.Apply();
        return tex;
    }

    static Color32 RingColor(float t)
    {
        Color rgb;
        float alpha;

        if (t < 0.06f)
        {
            // D ring — very faint inner ring
            alpha = 0.08f + t / 0.06f * 0.10f;
            rgb = new Color(0.72f, 0.65f, 0.48f);
        }
        else if (t < 0.22f)
        {
            // C ring — medium brightness
            float bt = (t - 0.06f) / 0.16f;
            alpha = 0.25f + Mathf.Sin(bt * Mathf.PI * 10f) * 0.08f;
            rgb = new Color(0.68f, 0.60f, 0.42f);
        }
        else if (t < 0.56f)
        {
            // B ring — brightest, most opaque
            float bt = (t - 0.22f) / 0.34f;
            alpha = 0.80f + Mathf.Sin(bt * Mathf.PI * 14f) * 0.12f;
            rgb = new Color(0.90f, 0.82f, 0.58f);
        }
        else if (t < 0.62f)
        {
            // Cassini Division — dark gap
            float bt = Mathf.Abs((t - 0.59f) / 0.03f);
            alpha = 0.04f + bt * 0.08f;
            rgb = new Color(0.25f, 0.22f, 0.18f);
        }
        else if (t < 0.90f)
        {
            // A ring — bright but slightly less than B
            float at = (t - 0.62f) / 0.28f;
            float encke = Mathf.Abs(at - 0.55f) < 0.04f ? 0.35f : 0f;
            alpha = (0.60f - at * 0.22f + Mathf.Sin(at * Mathf.PI * 8f) * 0.07f) - encke;
            alpha = Mathf.Clamp01(alpha);
            rgb = new Color(0.84f, 0.76f, 0.52f);
        }
        else
        {
            // F ring — faint wispy outer ring
            float ft = (t - 0.90f) / 0.10f;
            alpha = (1f - ft) * 0.12f;
            rgb = new Color(0.78f, 0.70f, 0.50f);
        }

        return new Color32(
            (byte)(rgb.r * 255),
            (byte)(rgb.g * 255),
            (byte)(rgb.b * 255),
            (byte)(Mathf.Clamp01(alpha) * 255));
    }
}
