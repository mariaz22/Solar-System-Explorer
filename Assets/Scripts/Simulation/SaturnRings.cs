using UnityEngine;

public class SaturnRings : MonoBehaviour
{
    public float innerRatio = 1.3f;
    public float outerRatio = 2.2f;
    public int segments = 96;
    public Color color = new Color(0.85f, 0.75f, 0.55f, 0.85f);

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
        go.transform.localRotation = Quaternion.Euler(15f, 0f, 0f);

        var mf = go.AddComponent<MeshFilter>();
        var mr = go.AddComponent<MeshRenderer>();

        var mesh = new Mesh { name = "RingMesh" };
        int vertCount = (segments + 1) * 2;
        var verts = new Vector3[vertCount];
        var uvs = new Vector2[vertCount];
        var tris = new int[segments * 6 * 2];

        for (int i = 0; i <= segments; i++)
        {
            float t = i / (float)segments * Mathf.PI * 2f;
            float cos = Mathf.Cos(t);
            float sin = Mathf.Sin(t);
            verts[i * 2] = new Vector3(cos * innerRatio, 0f, sin * innerRatio);
            verts[i * 2 + 1] = new Vector3(cos * outerRatio, 0f, sin * outerRatio);
            uvs[i * 2] = new Vector2(i / (float)segments, 0f);
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

        var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
        var mat = new Material(shader) { color = color };
        mr.material = mat;
    }
}
