using UnityEngine;

public class SolarWind : MonoBehaviour
{
    public Transform sun;
    public float windForce = 3f;        // units/s² push away from sun
    public float maxForceDistance = 200f;

    ParticleSystem windPS;
    ProbeController probe;

    void Start()
    {
        if (sun == null) sun = GameObject.Find("Sun")?.transform;
        probe = Object.FindAnyObjectByType<ProbeController>();
        BuildWindParticles();
    }

    void BuildWindParticles()
    {
        var go = new GameObject("SolarWindParticles");
        go.transform.SetParent(sun != null ? sun : transform, false);
        go.transform.localPosition = Vector3.zero;

        windPS = go.AddComponent<ParticleSystem>();
        windPS.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = windPS.main;
        main.loop            = true;
        main.duration        = 5f;
        main.startLifetime   = new ParticleSystem.MinMaxCurve(4f, 8f);
        main.startSpeed      = new ParticleSystem.MinMaxCurve(30f, 60f);
        main.startSize       = new ParticleSystem.MinMaxCurve(0.15f, 0.4f);
        main.startColor      = new ParticleSystem.MinMaxGradient(
            new Color(1f, 0.85f, 0.5f, 0.06f),
            new Color(0.9f, 0.6f, 0.3f, 0.03f));
        main.maxParticles    = 500;
        main.gravityModifier = 0f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.playOnAwake     = false;

        var em = windPS.emission;
        em.rateOverTime = 80f;

        var shape = windPS.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius    = sun != null ? sun.localScale.x * 0.5f : 8f;

        // Particles stream outward from sun center
        var vel = windPS.velocityOverLifetime;
        vel.enabled   = false; // radial speed set via startSpeed + shape

        var col = windPS.colorOverLifetime;
        col.enabled = true;
        var g = new Gradient();
        g.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(new Color(1f, 0.9f, 0.6f), 0f),
                new GradientColorKey(new Color(0.6f, 0.7f, 1f), 1f),
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(0.08f, 0f),
                new GradientAlphaKey(0.03f, 0.5f),
                new GradientAlphaKey(0f,    1f),
            });
        col.color = g;

        var sol = windPS.sizeOverLifetime;
        sol.enabled = true;
        sol.size = new ParticleSystem.MinMaxCurve(1f,
            new AnimationCurve(new Keyframe(0f, 0.5f), new Keyframe(1f, 2f)));

        var r = windPS.GetComponent<ParticleSystemRenderer>();
        r.renderMode        = ParticleSystemRenderMode.Stretch;
        r.velocityScale     = 0.04f;
        r.lengthScale       = 2.5f;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.material          = MakeWindMat();

        windPS.Play();
    }

    static Material MakeWindMat()
    {
        var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                  ?? Shader.Find("Sprites/Default");
        var mat = new Material(shader);
        mat.SetFloat("_Surface",  1f);
        mat.SetFloat("_Blend",    2f); // Additive
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
        mat.SetInt("_ZWrite", 0);
        mat.renderQueue = 3500;
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);
        return mat;
    }

    void Update()
    {
        // Reposition particle emitter with sun so wind always comes from it
        if (windPS != null && sun != null)
            windPS.transform.position = sun.position;
    }

    // Call this from ManualControlState to apply solar wind force
    public Vector3 GetWindForceAt(Vector3 pos)
    {
        if (sun == null) return Vector3.zero;
        float dist = Vector3.Distance(pos, sun.position);
        if (dist > maxForceDistance || dist < 0.1f) return Vector3.zero;
        // Force falls off with distance squared (like real solar wind pressure)
        float magnitude = windForce * (maxForceDistance / (dist * dist + 1f));
        magnitude = Mathf.Clamp(magnitude, 0f, windForce * 2f);
        return (pos - sun.position).normalized * magnitude;
    }
}
