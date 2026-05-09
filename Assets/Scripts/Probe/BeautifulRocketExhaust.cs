using UnityEngine;
using System.Collections.Generic;

[ExecuteInEditMode]
public class BeautifulRocketExhaust : MonoBehaviour
{
    [Header("Setup")]
    public Transform nozzleTransform;

    ParticleSystem mainFlame;
    ParticleSystem innerFlame;
    ParticleSystem coreGlow;
    Light thrustLight;
    ProbeController probe;
    GameObject emitterGo;   // rotates each frame to face backward

    Vector3 _prevPos;
    Vector3 _lastMoveDir = Vector3.down;

    void Awake()
    {
        if (Application.isPlaying)
        {
            BuildExhaust();
            probe = GetComponentInParent<ProbeController>() ?? GetComponent<ProbeController>();
            _prevPos = transform.position;
        }
    }

    [ContextMenu("BUILD EXHAUST")]
    public void BuildExhaust()
    {
        Transform root = nozzleTransform != null ? nozzleTransform : transform;

        var toDestroy = new List<GameObject>();
        foreach (Transform child in root)
            if (child.name == "ExhaustRoot" || child.name == "ThrustLight")
                toDestroy.Add(child.gameObject);
        foreach (var go in toDestroy)
            DestroyImmediate(go);

        var exhaustRoot = new GameObject("ExhaustRoot");
        exhaustRoot.transform.SetParent(root, false);
        exhaustRoot.transform.localPosition = Vector3.zero;

        // One emitter object shared by both flame layers.
        // Rotated every Update() so particles always point BACKWARD from movement.
        emitterGo = new GameObject("FlameEmitter");
        emitterGo.transform.SetParent(exhaustRoot.transform, false);
        emitterGo.transform.localPosition = new Vector3(0f, -0.25f, 0f);
        emitterGo.transform.localRotation = Quaternion.Euler(180f, 0f, 0f);

        mainFlame  = BuildMainFlame(emitterGo);

        var innerEmitterGo = new GameObject("InnerFlameEmitter");
        innerEmitterGo.transform.SetParent(emitterGo.transform, false);
        innerEmitterGo.transform.localPosition = Vector3.zero;
        innerEmitterGo.transform.localRotation = Quaternion.identity;
        innerFlame = BuildInnerFlame(innerEmitterGo);
        coreGlow   = BuildCoreGlow(exhaustRoot.transform);

        var lightGo = new GameObject("ThrustLight");
        lightGo.transform.SetParent(root, false);
        lightGo.transform.localPosition = new Vector3(0f, -0.3f, 0f);
        thrustLight = lightGo.AddComponent<Light>();
        thrustLight.type      = LightType.Point;
        thrustLight.color     = new Color(1f, 0.5f, 0.1f);
        thrustLight.intensity = 5f;
        thrustLight.range     = 4f;
        thrustLight.shadows   = LightShadows.None;
        thrustLight.enabled   = false;
    }

    // ── Main outer flame ──────────────────────────────────────────

    ParticleSystem BuildMainFlame(GameObject go)
    {
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.loop            = true;
        main.duration        = 1f;
        main.startLifetime   = new ParticleSystem.MinMaxCurve(0.07f, 0.12f);
        main.startSpeed      = new ParticleSystem.MinMaxCurve(10f, 20f);
        main.startSize       = new ParticleSystem.MinMaxCurve(0.04f, 0.08f);
        main.startColor      = Color.white;
        main.maxParticles    = 120;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = 0f;
        main.playOnAwake     = false;

        var em = ps.emission;
        em.rateOverTime = 80f;
        em.enabled      = false;

        var shape = ps.shape;
        shape.enabled   = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle     = 8f;
        shape.radius    = 0.02f;

        var col = ps.colorOverLifetime;
        col.enabled = true;
        var g = new Gradient();
        g.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(new Color(1f,    1f,    0.85f), 0f),
                new GradientColorKey(new Color(1f,    0.55f, 0.05f), 0.3f),
                new GradientColorKey(new Color(0.8f,  0.15f, 0f),   0.7f),
                new GradientColorKey(new Color(0.3f,  0.05f, 0f),   1f),
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(0.9f, 0f),
                new GradientAlphaKey(0.55f, 0.35f),
                new GradientAlphaKey(0.15f, 0.75f),
                new GradientAlphaKey(0f,   1f),
            }
        );
        col.color = g;

        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        sol.size = new ParticleSystem.MinMaxCurve(1f,
            new AnimationCurve(new Keyframe(0f, 0.5f), new Keyframe(0.4f, 1f), new Keyframe(1f, 0.3f)));

        var r = go.GetComponent<ParticleSystemRenderer>();
        r.renderMode        = ParticleSystemRenderMode.Billboard;
        r.maxParticleSize   = 0.05f;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.material          = MakeGlowMat(new Color(1f, 0.5f, 0.1f));
        return ps;
    }

    // ── Inner hot core ────────────────────────────────────────────

    ParticleSystem BuildInnerFlame(GameObject go)
    {
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.loop            = true;
        main.duration        = 1f;
        main.startLifetime   = new ParticleSystem.MinMaxCurve(0.04f, 0.07f);
        main.startSpeed      = new ParticleSystem.MinMaxCurve(14f, 24f);
        main.startSize       = new ParticleSystem.MinMaxCurve(0.015f, 0.03f);
        main.startColor      = Color.white;
        main.maxParticles    = 60;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = 0f;
        main.playOnAwake     = false;

        var em = ps.emission;
        em.rateOverTime = 50f;
        em.enabled      = false;

        var shape = ps.shape;
        shape.enabled   = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle     = 3f;
        shape.radius    = 0.01f;

        var col = ps.colorOverLifetime;
        col.enabled = true;
        var g = new Gradient();
        g.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(new Color(1f, 1f,   0.95f), 0f),
                new GradientColorKey(new Color(1f, 0.9f, 0.4f),  0.5f),
                new GradientColorKey(new Color(1f, 0.7f, 0.1f),  1f),
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(1f,   0f),
                new GradientAlphaKey(0.6f, 0.5f),
                new GradientAlphaKey(0f,   1f),
            }
        );
        col.color = g;

        var r = go.GetComponent<ParticleSystemRenderer>();
        r.renderMode        = ParticleSystemRenderMode.Billboard;
        r.maxParticleSize   = 0.03f;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.material          = MakeGlowMat(new Color(1f, 0.9f, 0.5f));
        return ps;
    }

    // ── Tiny nozzle glow dot ──────────────────────────────────────

    ParticleSystem BuildCoreGlow(Transform parent)
    {
        var go = new GameObject("CoreGlow");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(0f, -0.3f, 0f);
        go.transform.localRotation = Quaternion.Euler(180f, 0f, 0f);

        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.loop            = true;
        main.duration        = 1f;
        main.startLifetime   = new ParticleSystem.MinMaxCurve(0.05f, 0.09f);
        main.startSpeed      = new ParticleSystem.MinMaxCurve(4f, 8f);
        main.startSize       = new ParticleSystem.MinMaxCurve(0.03f, 0.06f);
        main.startColor      = new Color(1f, 1f, 0.9f, 1f);
        main.maxParticles    = 40;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = 0f;
        main.playOnAwake     = false;

        var em = ps.emission;
        em.rateOverTime = 50f;
        em.enabled      = false;

        var shape = ps.shape;
        shape.enabled   = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle     = 4f;
        shape.radius    = 0.015f;

        var col = ps.colorOverLifetime;
        col.enabled = true;
        var g = new Gradient();
        g.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(new Color(1f, 1f,   0.9f), 0f),
                new GradientColorKey(new Color(1f, 0.8f, 0.2f), 0.5f),
                new GradientColorKey(new Color(1f, 0.5f, 0f),   1f),
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(1f,   0f),
                new GradientAlphaKey(0.5f, 0.6f),
                new GradientAlphaKey(0f,   1f),
            }
        );
        col.color = g;

        var r = go.GetComponent<ParticleSystemRenderer>();
        r.renderMode        = ParticleSystemRenderMode.Billboard;
        r.maxParticleSize   = 0.008f;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.material          = MakeGlowMat(new Color(1f, 0.8f, 0.3f));
        return ps;
    }

    // ── Runtime control ───────────────────────────────────────────

    void Update()
    {
        if (!Application.isPlaying) return;

        if (probe == null)
        {
            probe = GetComponentInParent<ProbeController>();
            if (probe == null) return;
        }

        bool moving;
        if (probe.FSM?.CurrentState is ManualControlState)
            moving = probe.IsThrusting;
        else
            moving = probe.FSM?.CurrentState is TravelState        ||
                     probe.FSM?.CurrentState is ReturnState         ||
                     probe.FSM?.CurrentState is AvoidCollisionState;

        // Rotate emitter each frame so particles always emit BACKWARD from movement.
        // This replaces the old TrailRenderer approach that recorded path history
        // and could appear in front of the rocket after direction changes.
        Vector3 curPos = probe.transform.position;
        if (moving)
        {
            Vector3 disp = curPos - _prevPos;
            if (disp.sqrMagnitude > 0.0001f)
                _lastMoveDir = disp.normalized;

            if (emitterGo != null && _lastMoveDir.sqrMagnitude > 0.01f)
            {
                Vector3 exhaustDir = -_lastMoveDir;
                emitterGo.transform.rotation = Quaternion.Slerp(
                    emitterGo.transform.rotation,
                    Quaternion.FromToRotation(Vector3.up, exhaustDir),
                    Time.deltaTime * 25f);
            }
        }
        _prevPos = curPos;

        SetFlameActive(mainFlame,  moving);
        SetFlameActive(innerFlame, moving);

        if (coreGlow != null)
        {
            var e = coreGlow.emission;
            if (moving && !e.enabled) { e.enabled = true;  coreGlow.Play(); }
            else if (!moving && e.enabled)
            {
                e.enabled = false;
                coreGlow.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        if (thrustLight != null)
        {
            thrustLight.enabled = moving;
            if (moving)
            {
                thrustLight.intensity = 4f + Mathf.PingPong(Time.time * 20f, 2f);
                thrustLight.range     = 3.5f + Random.Range(-0.2f, 0.2f);
            }
        }
    }

    void SetFlameActive(ParticleSystem ps, bool active)
    {
        if (ps == null) return;
        var e = ps.emission;
        if (active && !e.enabled)  { e.enabled = true;  ps.Play(); }
        else if (!active && e.enabled)
        {
            e.enabled = false;
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    // ── Material helpers ──────────────────────────────────────────

    static Material MakeGlowMat(Color tint)
    {
        var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                  ?? Shader.Find("Legacy Shaders/Particles/Additive")
                  ?? Shader.Find("Sprites/Default");

        var mat = new Material(shader);

        // Force additive transparent blend regardless of shader defaults
        mat.SetFloat("_Surface",  1f);   // Transparent
        mat.SetFloat("_Blend",    2f);   // Additive
        mat.SetFloat("_SrcBlend", 1f);   // One
        mat.SetFloat("_DstBlend", 1f);   // One
        mat.SetFloat("_ZWrite",   0f);
        mat.renderQueue = 3500;
        mat.SetOverrideTag("RenderType", "Transparent");
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");

        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", tint);

        var tex = BuildSoftCircle(32);
        if (mat.HasProperty("_BaseMap"))       mat.SetTexture("_BaseMap",   tex);
        else if (mat.HasProperty("_MainTex"))  mat.SetTexture("_MainTex",   tex);

        return mat;
    }

    static Texture2D BuildSoftCircle(int res)
    {
        var tex = new Texture2D(res, res, TextureFormat.RGBA32, false);
        tex.wrapMode   = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        var c = new Vector2(res * 0.5f, res * 0.5f);
        float r = res * 0.48f;
        for (int y = 0; y < res; y++)
            for (int x = 0; x < res; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), c) / r;
                float a = Mathf.Clamp01(1f - d);
                a = a * a * a;
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        tex.Apply();
        return tex;
    }
}
