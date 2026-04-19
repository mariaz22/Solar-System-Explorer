using UnityEngine;

public class Starfield : MonoBehaviour
{
    public int starCount = 3000;
    public float radius = 1800f;
    public float minSize = 0.8f;
    public float maxSize = 3.2f;

    Transform starsT;

    void Start()
    {
        var oldBackdrop = GameObject.Find("SpaceBackdrop");
        if (oldBackdrop != null) Destroy(oldBackdrop);
        var oldStarfield = GameObject.Find("Starfield");
        if (oldStarfield != null) Destroy(oldStarfield);

        BuildStars();
    }

    void LateUpdate()
    {
        var cam = Camera.main;
        if (cam == null) return;
        if (starsT != null) starsT.position = cam.transform.position;
    }

    static Material MakeUnlitMaterial(Color c)
    {
        var shader = Shader.Find("Universal Render Pipeline/Unlit")
                  ?? Shader.Find("Unlit/Color");
        var mat = new Material(shader);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", c);
        if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 0f);
        if (mat.HasProperty("_Cull")) mat.SetFloat("_Cull", 0f);
        mat.doubleSidedGI = true;
        mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Background;
        return mat;
    }

    void BuildStars()
    {
        var root = new GameObject("Starfield");
        starsT = root.transform;

        var mf = root.AddComponent<MeshFilter>();
        mf.mesh = BuildStarMesh();

        var mr = root.AddComponent<MeshRenderer>();
        var mat = MakeUnlitMaterial(Color.white);
        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Background + 1;
        mr.material = mat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;
        mr.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        mr.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
    }

    Mesh BuildStarMesh()
    {
        var verts = new Vector3[starCount * 4];
        var uvs = new Vector2[starCount * 4];
        var tris = new int[starCount * 6];

        for (int i = 0; i < starCount; i++)
        {
            Vector3 dir = Random.onUnitSphere;
            float r = radius * Random.Range(0.9f, 1.0f);
            Vector3 center = dir * r;

            Vector3 up = Vector3.Cross(dir, Vector3.up);
            if (up.sqrMagnitude < 0.001f) up = Vector3.Cross(dir, Vector3.right);
            up.Normalize();
            Vector3 right = Vector3.Cross(dir, up).normalized;

            float brightness = Random.Range(0.4f, 1f);
            float size = Mathf.Lerp(minSize, maxSize, brightness * brightness);

            int v = i * 4;
            verts[v + 0] = center + (-right - up) * size;
            verts[v + 1] = center + ( right - up) * size;
            verts[v + 2] = center + ( right + up) * size;
            verts[v + 3] = center + (-right + up) * size;
            uvs[v + 0] = new Vector2(0, 0);
            uvs[v + 1] = new Vector2(1, 0);
            uvs[v + 2] = new Vector2(1, 1);
            uvs[v + 3] = new Vector2(0, 1);

            int t = i * 6;
            tris[t + 0] = v + 0; tris[t + 1] = v + 1; tris[t + 2] = v + 2;
            tris[t + 3] = v + 0; tris[t + 4] = v + 2; tris[t + 5] = v + 3;
        }

        var mesh = new Mesh { name = "Starfield", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
        mesh.vertices = verts;
        mesh.uv = uvs;
        mesh.triangles = tris;
        mesh.bounds = new Bounds(Vector3.zero, Vector3.one * radius * 4f);
        return mesh;
    }
}
