using UnityEngine;

[RequireComponent(typeof(ProbeController))]
public class ProbeVFX : MonoBehaviour
{
    [Header("Thruster")]
    public ParticleSystem thrusterParticles;
    public Vector3 localPosition = new Vector3(0f, 0f, -0.9f);
    public float lifetime = 0.3f;
    public float emissionRate = 45f;

    ProbeController probe;
    ProbeController.State lastState;

    void Awake()
    {
        probe = GetComponent<ProbeController>();
        EnsureThruster();
        lastState = probe.CurrentState;
        ApplyState(lastState, immediate: true);
    }

    void OnEnable()
    {
        if (probe != null) probe.StateChanged += OnProbeStateChanged;
    }

    void OnDisable()
    {
        if (probe != null) probe.StateChanged -= OnProbeStateChanged;
    }

    void Update()
    {
        if (probe == null || probe.CurrentState == lastState) return;

        lastState = probe.CurrentState;
        ApplyState(lastState, immediate: false);
    }

    void OnProbeStateChanged(ProbeController.State state)
    {
        lastState = state;
        ApplyState(state, immediate: false);
    }

    void EnsureThruster()
    {
        if (thrusterParticles == null)
        {
            var existing = transform.Find("ThrusterParticles");
            if (existing != null) thrusterParticles = existing.GetComponent<ParticleSystem>();
        }

        if (thrusterParticles == null)
        {
            var go = new GameObject("ThrusterParticles");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = localPosition;
            go.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            thrusterParticles = go.AddComponent<ParticleSystem>();
        }

        ConfigureThruster(thrusterParticles);
    }

    void ConfigureThruster(ParticleSystem ps)
    {
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.loop = true;
        main.playOnAwake = false;
        main.startLifetime = lifetime;
        main.startSpeed = new ParticleSystem.MinMaxCurve(5f, 8f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.35f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.45f, 0.8f, 1f, 1f),
            new Color(1f, 1f, 1f, 0.9f));
        main.maxParticles = 80;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;

        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = emissionRate;

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 14f;
        shape.radius = 0.08f;
        shape.length = 0.2f;

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(new Color(0.2f, 0.65f, 1f), 0.45f),
                new GradientColorKey(new Color(0.05f, 0.25f, 1f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0.65f, 0.45f),
                new GradientAlphaKey(0f, 1f)
            });
        colorOverLifetime.color = gradient;

        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0.1f));

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.maxParticleSize = 0.5f;
        var material = CreateThrusterMaterial();
        if (material != null) renderer.material = material;
    }

    Material CreateThrusterMaterial()
    {
        var shader =
            Shader.Find("Universal Render Pipeline/Particles/Unlit") ??
            Shader.Find("Universal Render Pipeline/Unlit") ??
            Shader.Find("Particles/Standard Unlit") ??
            Shader.Find("Unlit/Color");

        if (shader == null) return null;

        var material = new Material(shader);
        var color = new Color(0.35f, 0.75f, 1f, 1f);

        if (material.HasProperty("_Surface")) material.SetFloat("_Surface", 1f);
        if (material.HasProperty("_Blend")) material.SetFloat("_Blend", 2f);
        if (material.HasProperty("_SrcBlend")) material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.One);
        if (material.HasProperty("_DstBlend")) material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
        if (material.HasProperty("_ZWrite")) material.SetFloat("_ZWrite", 0f);
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color")) material.SetColor("_Color", color);
        if (material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * 2.5f);
        }
        material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

        return material;
    }

    void ApplyState(ProbeController.State state, bool immediate)
    {
        if (thrusterParticles == null) return;

        if (IsThrusterState(state))
        {
            if (!thrusterParticles.isPlaying) thrusterParticles.Play(true);
            return;
        }

        var stopBehavior = immediate
            ? ParticleSystemStopBehavior.StopEmittingAndClear
            : ParticleSystemStopBehavior.StopEmitting;
        thrusterParticles.Stop(true, stopBehavior);
    }

    static bool IsThrusterState(ProbeController.State state)
    {
        return state == ProbeController.State.Travel ||
               state == ProbeController.State.AvoidCollision;
    }
}
