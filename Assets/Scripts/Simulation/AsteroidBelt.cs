using UnityEngine;

public class AsteroidBelt : MonoBehaviour
{
    public Transform center;           // Sun transform — set by SceneBootstrap
    public int   count     = 300;
    public float minRadius = 45f;
    public float maxRadius = 65f;
    public float height    = 3f;
    public float minSize   = 0.15f;
    public float maxSize   = 0.6f;

    void Start()
    {
        for (int i = 0; i < count; i++)
            SpawnAsteroid();
    }

    void SpawnAsteroid()
    {
        float angle  = Random.Range(0f, Mathf.PI * 2f);
        float radius = Random.Range(minRadius, maxRadius);
        Vector3 centerPos = center != null ? center.position : Vector3.zero;
        Vector3 pos = centerPos + new Vector3(
            Mathf.Cos(angle) * radius,
            Random.Range(-height, height),
            Mathf.Sin(angle) * radius);

        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "Asteroid";
        go.transform.SetParent(transform);
        go.transform.position = pos;

        float size = Random.Range(minSize, maxSize);
        go.transform.localScale = new Vector3(
            size * Random.Range(0.7f, 1.3f),
            size * Random.Range(0.7f, 1.3f),
            size * Random.Range(0.7f, 1.3f));
        go.transform.rotation = Random.rotation;

        Destroy(go.GetComponent<Collider>());

        var mr  = go.GetComponent<MeshRenderer>();
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        Color rockColor = new Color(
            Random.Range(0.18f, 0.38f),
            Random.Range(0.16f, 0.32f),
            Random.Range(0.12f, 0.26f));
        if (mat.HasProperty("_BaseColor"))  mat.SetColor("_BaseColor",  rockColor);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.05f);
        mr.material = mat;

        // Self-rotation (tumbling)
        var spin = go.AddComponent<SelfRotation>();
        spin.degreesPerSecond = Random.Range(8f, 40f);

        // Orbital motion around the sun — speed follows Kepler: v ∝ 1/√r
        // Asteroid belt is roughly at 2.2–3.2 AU; map radius units to AU proportionally
        var orbit = go.AddComponent<OrbitalMotion>();
        orbit.center = center;
        float au = radius / 30f; // rough AU mapping based on scene scale
        orbit.angularSpeedDeg = 1.5f / Mathf.Pow(Mathf.Max(au, 0.1f), 1.5f);
        orbit.angularSpeedDeg *= Random.Range(0.85f, 1.15f); // slight spread
    }
}
