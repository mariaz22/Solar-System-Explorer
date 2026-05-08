using UnityEngine;

public class SunLensFlare : MonoBehaviour
{
    public Transform sun;
    public Color flareColor = new Color(1f, 0.8f, 0.5f, 0.4f);
    
    GameObject[] ghosts;
    float[] ghostDistances = { -0.5f, 0.1f, 0.2f, 0.4f, 0.6f, 1.2f, 1.5f };
    float[] ghostScales = { 1.5f, 0.2f, 0.15f, 0.5f, 0.3f, 0.8f, 1.2f };

    void Start()
    {
        if (sun == null) sun = GameObject.Find("Sun")?.transform;
        if (sun == null) return;

        ghosts = new GameObject[ghostDistances.Length];
        var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        mat.SetFloat("_Surface", 1f);
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
        mat.SetInt("_ZWrite", 0);
        
        var tex = GenerateFlareTex();

        for (int i = 0; i < ghosts.Length; i++)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Quad);
            g.name = "FlareGhost";
            Destroy(g.GetComponent<Collider>());
            g.transform.SetParent(transform);
            var mr = g.GetComponent<MeshRenderer>();
            mr.material = new Material(mat);
            mr.material.mainTexture = tex;
            mr.material.SetColor("_BaseColor", flareColor * (1f - Mathf.Abs(ghostDistances[i] - 0.5f)));
            ghosts[i] = g;
        }
    }

    Texture2D GenerateFlareTex()
    {
        int res = 128;
        var tex = new Texture2D(res, res);
        for (int y = 0; y < res; y++)
        {
            for (int x = 0; x < res; x++)
            {
                float dx = (x / (float)res) - 0.5f;
                float dy = (y / (float)res) - 0.5f;
                float d = Mathf.Sqrt(dx * dx + dy * dy) * 2f;
                float a = Mathf.Clamp01(1f - d);
                a = Mathf.Pow(a, 4f);
                tex.SetPixel(x, y, new Color(1, 1, 1, a));
            }
        }
        tex.Apply();
        return tex;
    }

    void Update()
    {
        if (sun == null) return;

        Camera cam = Camera.main;
        Vector3 sunScreenPos = cam.WorldToScreenPoint(sun.position);
        
        // Hide if behind camera
        if (sunScreenPos.z < 0)
        {
            foreach (var g in ghosts) g.SetActive(false);
            return;
        }

        foreach (var g in ghosts) g.SetActive(true);

        Vector2 center = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        Vector2 sunToCenter = center - (Vector2)sunScreenPos;

        for (int i = 0; i < ghosts.Length; i++)
        {
            Vector2 pos = (Vector2)sunScreenPos + sunToCenter * ghostDistances[i];
            Vector3 worldPos = cam.ScreenToWorldPoint(new Vector3(pos.x, pos.y, 1f));
            ghosts[i].transform.position = worldPos;
            ghosts[i].transform.localScale = Vector3.one * ghostScales[i] * 0.2f;
            ghosts[i].transform.LookAt(cam.transform.position);
        }
    }
}
