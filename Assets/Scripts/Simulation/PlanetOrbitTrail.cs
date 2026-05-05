using UnityEngine;

[RequireComponent(typeof(Planet))]
public class PlanetOrbitTrail : MonoBehaviour
{
    public float trailTime = 5f;
    public float startWidth = 0.12f;
    public Color planetColor = Color.white;

    TrailRenderer trail;

    void Awake()
    {
        trail = GetComponent<TrailRenderer>();
        if (trail == null) trail = gameObject.AddComponent<TrailRenderer>();
        trail.time = trailTime;
        trail.startWidth = startWidth;
        trail.endWidth = 0f;
        trail.minVertexDistance = 0.1f;
        trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        trail.receiveShadows = false;

        var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
        if (shader != null)
            trail.material = new Material(shader);
    }

    void Start()
    {
        Color c = Color.Lerp(planetColor, Color.white, 0.3f);
        trail.startColor = new Color(c.r, c.g, c.b, 0.8f);
        trail.endColor = new Color(c.r, c.g, c.b, 0f);
        if (trail.material != null)
            trail.material.color = c;
    }
}
