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
        // Slight brightness boost so the milky way is clearly visible
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", new Color(1.4f, 1.4f, 1.4f, 1f));
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
