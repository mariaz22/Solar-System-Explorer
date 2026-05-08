using UnityEngine;

public class ProceduralRocket : MonoBehaviour
{
    void Awake()
    {
        // Hide original sphere mesh, keep collider
        var rend = GetComponent<Renderer>();
        if (rend != null) rend.enabled = false;

        // Container rotated so rocket nose (+Y) maps to probe forward (+Z)
        var visual = new GameObject("RocketVisual");
        visual.transform.SetParent(transform, false);
        visual.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
        visual.transform.localScale = Vector3.one * 0.45f;

        Build(visual.transform);
    }

    void Build(Transform root)
    {
        // Body
        var body = Part(PrimitiveType.Cylinder, root, Vector3.zero, new Vector3(0.35f, 0.6f, 0.35f));
        Solid(body, new Color(0.92f, 0.92f, 0.95f), metallic: 0.3f);

        // Nose cone
        var nose = Part(PrimitiveType.Sphere, root, new Vector3(0f, 0.75f, 0f), new Vector3(0.35f, 0.45f, 0.35f));
        Solid(nose, new Color(0.95f, 0.32f, 0.18f));

        // Blue stripe on body
        var stripe = Part(PrimitiveType.Cylinder, root, new Vector3(0f, 0.1f, 0f), new Vector3(0.36f, 0.08f, 0.36f));
        Solid(stripe, new Color(0.20f, 0.55f, 0.95f));

        // Porthole window
        var window = Part(PrimitiveType.Sphere, root, new Vector3(0f, 0.28f, 0.32f), new Vector3(0.16f, 0.16f, 0.08f));
        Emissive(window, new Color(0.45f, 0.90f, 1.00f), 1.8f);

        // Engine bell
        var bell = Part(PrimitiveType.Cylinder, root, new Vector3(0f, -0.75f, 0f), new Vector3(0.30f, 0.18f, 0.30f));
        Solid(bell, new Color(0.22f, 0.22f, 0.26f), metallic: 0.7f);

        // Engine exhaust glow (internal)
        var exhaust = Part(PrimitiveType.Sphere, root, new Vector3(0f, -0.9f, 0f), new Vector3(0.25f, 0.15f, 0.25f));
        Emissive(exhaust, new Color(1.00f, 0.6f, 0.1f), 6f);

        // 3 fins
        for (int i = 0; i < 3; i++)
        {
            float angle = i * 120f;
            float rad = angle * Mathf.Deg2Rad;
            var fin = Part(PrimitiveType.Cube, root,
                new Vector3(Mathf.Sin(rad) * 0.38f, -0.46f, Mathf.Cos(rad) * 0.38f),
                new Vector3(0.07f, 0.34f, 0.26f));
            fin.transform.localRotation = Quaternion.Euler(0f, angle, 0f);
            Solid(fin, new Color(0.88f, 0.22f, 0.18f));
        }
    }

    static GameObject Part(PrimitiveType type, Transform parent, Vector3 pos, Vector3 scale)
    {
        var go = GameObject.CreatePrimitive(type);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale = scale;
        // Remove colliders — probe's own collider handles interaction
        var col = go.GetComponent<Collider>();
        if (col != null) Destroy(col);
        return go;
    }

    static void Solid(GameObject go, Color color, float metallic = 0f)
    {
        var r = go.GetComponent<Renderer>();
        if (r == null) return;
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? r.sharedMaterial.shader);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.45f);
        if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
        r.material = mat;
    }

    static void Emissive(GameObject go, Color color, float intensity)
    {
        var r = go.GetComponent<Renderer>();
        if (r == null) return;
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? r.sharedMaterial.shader);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        mat.EnableKeyword("_EMISSION");
        if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", color * intensity);
        r.material = mat;
    }
}
