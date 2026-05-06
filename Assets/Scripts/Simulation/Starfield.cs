using UnityEngine;

public class Starfield : MonoBehaviour
{
    public float skyRadius = 4000f;
    public int sparkleCount = 1500;

    void Start()
    {
        var oldBackdrop = GameObject.Find("SpaceBackdrop");
        if (oldBackdrop != null) Destroy(oldBackdrop);
        var oldStarfield = GameObject.Find("Starfield");
        if (oldStarfield != null) Destroy(oldStarfield);

        BuildSkySphere();
        BuildSparkles();
    }

    void BuildNebulae()
    {
        var root = new GameObject("Nebulae");
        // Add a few large, very faint colorful clouds
        AddNebula(root, new Color(0.1f, 0.2f, 0.5f, 0.05f), 1200f, 3);
        AddNebula(root, new Color(0.4f, 0.1f, 0.3f, 0.04f), 1500f, 2);
        AddNebula(root, new Color(0.1f, 0.4f, 0.2f, 0.03f), 1000f, 2);
    }

    void AddNebula(GameObject root, Color color, float size, int count)
    {
        for (int i = 0; i < count; i++)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = "NebulaCloud";
            go.transform.SetParent(root.transform);
            go.transform.position = Random.onUnitSphere * skyRadius * 0.9f;
            go.transform.localScale = Vector3.one * size * Random.Range(0.8f, 1.5f);
            go.transform.LookAt(Vector3.zero);
            
            Destroy(go.GetComponent<Collider>());
            
            var mr = go.GetComponent<MeshRenderer>();
            var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            mat.SetFloat("_Surface", 1f); // Transparent
            mat.SetFloat("_Blend", 0f);   // Alpha
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One); // Additive for glow
            mat.SetInt("_ZWrite", 0);
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent - 10;
            
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            
            // We need a soft texture. Since we don't have one, we'll generate a simple radial gradient.
            mat.mainTexture = GenerateSoftDot();
            mr.material = mat;
        }
    }

    Texture2D GenerateSoftDot()
    {
        int res = 64;
        var tex = new Texture2D(res, res);
        for (int y = 0; y < res; y++)
        {
            for (int x = 0; x < res; x++)
            {
                float dx = (x / (float)res) - 0.5f;
                float dy = (y / (float)res) - 0.5f;
                float d = Mathf.Sqrt(dx * dx + dy * dy) * 2f;
                float a = Mathf.Clamp01(1f - d);
                a = Mathf.Pow(a, 3f); // Softer falloff
                tex.SetPixel(x, y, new Color(1, 1, 1, a));
            }
        }
        tex.Apply();
        return tex;
    }

    void BuildSkySphere()
    {
        var sky = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sky.name = "SkySphere";
        Destroy(sky.GetComponent<SphereCollider>());
        sky.transform.position = Vector3.zero;
        // Negative X scale flips the sphere inside-out so it renders from within
        sky.transform.localScale = new Vector3(-skyRadius, skyRadius, skyRadius);

        var mr = sky.GetComponent<MeshRenderer>();
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;
        mr.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        mr.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;

        var shader = Shader.Find("Universal Render Pipeline/Unlit")
                  ?? Shader.Find("Unlit/Texture");
        var mat = new Material(shader);

        var tex = Resources.Load<Texture2D>("PlanetTextures/StarsBackground");
        if (tex != null)
        {
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
            if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", tex);
        }
        // Strong brightness boost to ensure the star background is clearly visible
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", new Color(2.8f, 2.8f, 2.8f, 1f));
if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 0f);
        if (mat.HasProperty("_Cull")) mat.SetFloat("_Cull", 0f); // Off — negative scale handles flip
        mat.SetOverrideTag("RenderType", "Opaque");
        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Background;

        mr.material = mat;
    }

    void BuildSparkles()
    {
        var root = new GameObject("StarSparkles");

        int white  = Mathf.RoundToInt(sparkleCount * 0.55f);
        int yellow = Mathf.RoundToInt(sparkleCount * 0.20f);
        int blue   = Mathf.RoundToInt(sparkleCount * 0.25f);

        AddBatch(root, white,  new Color(1.00f, 1.00f, 1.00f), 0.6f, 3.0f);
        AddBatch(root, yellow, new Color(1.00f, 0.90f, 0.55f), 0.6f, 3.5f);
        AddBatch(root, blue,   new Color(0.70f, 0.85f, 1.00f), 0.6f, 3.0f);
    }

    void AddBatch(GameObject root, int count, Color color, float minSize, float maxSize)
    {
        if (count <= 0) return;
        var go = new GameObject("Sparkles");
        go.transform.SetParent(root.transform, false);
        var mf = go.AddComponent<MeshFilter>();
        mf.mesh = BuildSpeckleMesh(count, minSize, maxSize);
        var mr = go.AddComponent<MeshRenderer>();
        mr.material = MakeUnlitMat(color);
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;
        mr.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        mr.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
    }

    static Material MakeUnlitMat(Color c)
    {
        var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
        var mat = new Material(shader);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", c);
        if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 0f);
        if (mat.HasProperty("_Cull")) mat.SetFloat("_Cull", 0f);
        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Background + 1;
        return mat;
    }

    Mesh BuildSpeckleMesh(int count, float minSize, float maxSize)
    {
        float r = skyRadius * 0.95f;
        var verts = new Vector3[count * 4];
        var uvs   = new Vector2[count * 4];
        var tris  = new int[count * 6];

        for (int i = 0; i < count; i++)
        {
            Vector3 dir = Random.onUnitSphere;
            Vector3 center = dir * r;

            Vector3 up = Vector3.Cross(dir, Vector3.up);
            if (up.sqrMagnitude < 0.001f) up = Vector3.Cross(dir, Vector3.right);
            up.Normalize();
            Vector3 right = Vector3.Cross(dir, up).normalized;

            float brightness = Random.Range(0.5f, 1f);
            float size = Mathf.Lerp(minSize, maxSize, brightness * brightness);

            int v = i * 4;
            verts[v]     = center + (-right - up) * size;
            verts[v + 1] = center + ( right - up) * size;
            verts[v + 2] = center + ( right + up) * size;
            verts[v + 3] = center + (-right + up) * size;
            uvs[v]     = new Vector2(0, 0);
            uvs[v + 1] = new Vector2(1, 0);
            uvs[v + 2] = new Vector2(1, 1);
            uvs[v + 3] = new Vector2(0, 1);

            int t = i * 6;
            tris[t]     = v;     tris[t + 1] = v + 1; tris[t + 2] = v + 2;
            tris[t + 3] = v;     tris[t + 4] = v + 2; tris[t + 5] = v + 3;
        }

        var mesh = new Mesh { name = "Sparkles", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
        mesh.vertices = verts;
        mesh.uv = uvs;
        mesh.triangles = tris;
        mesh.bounds = new Bounds(Vector3.zero, Vector3.one * r * 4f);
        return mesh;
    }
}
