using System.Collections;
using UnityEngine;

[ExecuteAlways]
public class SunEvolutionController : MonoBehaviour
{
    [System.Serializable]
public struct StageVisuals
    {
        public float scale;
        public float lightIntensity;
        public float lightRange;
        public Color lightColor;
        public Color glowColor;
    }

    public StageVisuals[] stages = new StageVisuals[]
    {
        // lightColor = Point Light tint on planets (keep near-white so planets aren't tinted).
        // glowColor  = visual glow/corona around the sun sphere (warm/coloured as desired).
        new StageVisuals { scale = 1.00f, lightIntensity =  200_000f, lightRange = 4000f, lightColor = new Color(1.00f, 0.98f, 0.96f), glowColor = new Color(1.00f, 0.95f, 0.60f, 0.70f) },
        new StageVisuals { scale = 1.60f, lightIntensity =  250_000f, lightRange = 5000f, lightColor = new Color(1.00f, 0.96f, 0.90f), glowColor = new Color(1.00f, 0.75f, 0.30f, 0.75f) },
        new StageVisuals { scale = 2.80f, lightIntensity =  540_000f, lightRange = 7000f, lightColor = new Color(1.00f, 0.28f, 0.08f), glowColor = new Color(1.00f, 0.18f, 0.04f, 0.80f) },
        new StageVisuals { scale = 1.50f, lightIntensity =  110_000f, lightRange = 3000f, lightColor = new Color(0.55f, 0.35f, 1.00f), glowColor = new Color(0.80f, 0.50f, 1.00f, 0.90f) },
        new StageVisuals { scale = 0.06f, lightIntensity = 1_100_000f, lightRange = 5000f, lightColor = new Color(0.90f, 0.96f, 1.00f), glowColor = new Color(0.96f, 0.98f, 1.00f, 1.00f) },
    };

    public AnimationCurve scaleCurve     = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    public AnimationCurve intensityCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    public AnimationCurve colorCurve     = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    static readonly Color[] AmbientPerStage =
    {
        new Color(0.13f, 0.13f, 0.16f),   // MainSequence
        new Color(0.14f, 0.11f, 0.08f),   // SubGiant
        new Color(0.28f, 0.06f, 0.01f),   // RedGiant — vivid red fill
        new Color(0.08f, 0.03f, 0.15f),   // PlanetaryNebula
        new Color(0.05f, 0.07f, 0.14f),   // WhiteDwarf
    };

    Light     sunLight;
    Material  sunMat;
    Color     sunMatOriginalColor;
    Color     sunMatOriginalEmission;
    Texture   sunMatOriginalTex;
    bool      sunMatHadEmission;
    Material  glowMat;
    Material  coronaMat;
    Transform _glowChild;
    Transform _coronaChild;
    Vector3   baseScale;
    Vector3   _lerpedScale;

    GameObject     _nebulaGO;
    ParticleSystem _outerPS;
    ParticleSystem _midPS;
    ParticleSystem _corePS;
    float          _currentNebulaRadius;

    SolarStage _prevStage = SolarStage.MainSequence;

    void Start()
    {
        sunLight     = GetComponent<Light>();
        baseScale    = transform.localScale;
        _lerpedScale = baseScale;

        var mr = GetComponent<MeshRenderer>();
        if (mr != null)
        {
            sunMat = mr.material;
            if (sunMat.HasProperty("_BaseColor"))  sunMatOriginalColor = sunMat.GetColor("_BaseColor");
            else if (sunMat.HasProperty("_Color")) sunMatOriginalColor = sunMat.GetColor("_Color");
            else                                   sunMatOriginalColor = Color.white;

            if (sunMat.HasProperty("_BaseMap"))      sunMatOriginalTex = sunMat.GetTexture("_BaseMap");
            else if (sunMat.HasProperty("_MainTex")) sunMatOriginalTex = sunMat.GetTexture("_MainTex");

            sunMatHadEmission = sunMat.IsKeywordEnabled("_EMISSION");
            if (sunMat.HasProperty("_EmissionColor"))
                sunMatOriginalEmission = sunMat.GetColor("_EmissionColor");
        }

        _glowChild = transform.Find("SunGlow");
        if (_glowChild != null)
        {
            var gmr = _glowChild.GetComponent<MeshRenderer>();
            if (gmr != null) glowMat = gmr.material;
        }

        _coronaChild = transform.Find("SunCorona");
        if (_coronaChild != null)
        {
            var cmr = _coronaChild.GetComponent<MeshRenderer>();
            if (cmr != null) coronaMat = cmr.material;
        }

        BuildNebulaParticles();

        if (CosmicTimelineManager.Instance != null)
        {
            CosmicTimelineManager.Instance.OnCosmicTimeChanged += OnCosmicTimeChanged;
            ApplyVisuals(CosmicTimelineManager.Instance.GetCurrentStage(),
                         CosmicTimelineManager.Instance.GetStageProgress());
        }
        }

        void OnEnable()
        {
        if (!Application.isPlaying && CosmicTimelineManager.Instance != null)
        {
            CosmicTimelineManager.Instance.OnCosmicTimeChanged -= OnCosmicTimeChanged;
            CosmicTimelineManager.Instance.OnCosmicTimeChanged += OnCosmicTimeChanged;
            ApplyVisuals(CosmicTimelineManager.Instance.GetCurrentStage(),
                         CosmicTimelineManager.Instance.GetStageProgress());
        }
        }

        void OnDestroy()
        {
        if (CosmicTimelineManager.Instance != null)
            CosmicTimelineManager.Instance.OnCosmicTimeChanged -= OnCosmicTimeChanged;
        if (_nebulaGO != null) DestroyImmediate(_nebulaGO);
        }

        void OnCosmicTimeChanged(float gyr, SolarStage stage)
        {
        if (Application.isPlaying && stage != _prevStage)
        {
            if (stage > _prevStage)
                StartCoroutine(CameraShake(0.7f, 2.0f));
            _prevStage = stage;
        }
        ApplyVisuals(stage, CosmicTimelineManager.Instance.GetStageProgress());
        }

        void Update()
        {
        if (!Application.isPlaying && CosmicTimelineManager.Instance != null)
        {
            ApplyVisuals(CosmicTimelineManager.Instance.GetCurrentStage(),
                         CosmicTimelineManager.Instance.GetStageProgress());
        }

        if (Application.isPlaying && CosmicTimelineManager.Instance?.GetCurrentStage() == SolarStage.RedGiant)
        {
            float pulse = 1f + Mathf.Sin(Time.time * 0.55f) * 0.05f
                             + Mathf.Sin(Time.time * 1.80f) * 0.02f;
            transform.localScale = _lerpedScale * pulse;
        }
        }

    void ApplyVisuals(SolarStage stage, float progress)
    {
        int cur  = (int)stage;
        int next = Mathf.Min(cur + 1, stages.Length - 1);

        StageVisuals a = stages[cur];
        StageVisuals b = stages[next];

        float st = scaleCurve.Evaluate(progress);
        float it = intensityCurve.Evaluate(progress);
        float ct = colorCurve.Evaluate(progress);

        _lerpedScale = baseScale * Mathf.Lerp(a.scale, b.scale, st);
        if (stage != SolarStage.RedGiant)
            transform.localScale = _lerpedScale;

        if (sunLight != null)
        {
            sunLight.intensity = Mathf.Lerp(a.lightIntensity, b.lightIntensity, it);
            sunLight.color     = Color.Lerp(a.lightColor, b.lightColor, ct);
            sunLight.range     = Mathf.Lerp(a.lightRange, b.lightRange, it);
        }

        Color gc = Color.Lerp(a.glowColor, b.glowColor, ct);

        // White Dwarf only: override sun sphere to pure white brilliant star
        if (sunMat != null)
        {
            if (stage == SolarStage.WhiteDwarf)
            {
                Color wdWhite = new Color(0.92f, 0.96f, 1.00f);
                if (sunMat.HasProperty("_BaseColor")) sunMat.SetColor("_BaseColor", wdWhite);
                if (sunMat.HasProperty("_Color"))     sunMat.SetColor("_Color",     wdWhite);
                // Replace orange texture with plain white so tint takes full effect
                if (sunMat.HasProperty("_BaseMap"))   sunMat.SetTexture("_BaseMap", Texture2D.whiteTexture);
                if (sunMat.HasProperty("_MainTex"))   sunMat.SetTexture("_MainTex", Texture2D.whiteTexture);
                // Bright emission so it looks self-luminous
                sunMat.EnableKeyword("_EMISSION");
                if (sunMat.HasProperty("_EmissionColor"))
                    sunMat.SetColor("_EmissionColor", wdWhite * (3f + ct * 4f));
            }
            else
            {
                // Restore original — all other stages look exactly as before
                if (sunMat.HasProperty("_BaseColor")) sunMat.SetColor("_BaseColor", sunMatOriginalColor);
                if (sunMat.HasProperty("_Color"))     sunMat.SetColor("_Color",     sunMatOriginalColor);
                if (sunMat.HasProperty("_BaseMap"))   sunMat.SetTexture("_BaseMap", sunMatOriginalTex);
                if (sunMat.HasProperty("_MainTex"))   sunMat.SetTexture("_MainTex", sunMatOriginalTex);
                if (sunMatHadEmission)
                {
                    sunMat.EnableKeyword("_EMISSION");
                    if (sunMat.HasProperty("_EmissionColor"))
                        sunMat.SetColor("_EmissionColor", sunMatOriginalEmission);
                }
                else
                {
                    sunMat.DisableKeyword("_EMISSION");
                }
            }
        }

        if (glowMat   != null && glowMat.HasProperty("_BaseColor"))
            glowMat.SetColor("_BaseColor", gc);
        if (coronaMat != null && coronaMat.HasProperty("_BaseColor"))
            coronaMat.SetColor("_BaseColor", new Color(gc.r, gc.g, gc.b, gc.a * 0.65f));

        // White Dwarf: sun sphere shrinks to near-nothing, so compensate glow/corona scale
        // so they remain visible as a brilliant white star point.
        float sunRelScale = _lerpedScale.x / baseScale.x;
        if (sunRelScale < 0.5f && (_glowChild != null || _coronaChild != null))
        {
            // Keep glow world-radius ≈ 20% of original base scale
            float desiredLocal = 0.20f / Mathf.Max(sunRelScale, 0.001f);
            float clamped      = Mathf.Clamp(desiredLocal, 1f, 25f);
            if (_glowChild   != null) _glowChild.localScale   = Vector3.one * clamped;
            if (_coronaChild != null) _coronaChild.localScale = Vector3.one * clamped * 1.4f;
        }
        else
        {
            if (_glowChild   != null) _glowChild.localScale   = Vector3.one;
            if (_coronaChild != null) _coronaChild.localScale = Vector3.one;
        }

        RenderSettings.ambientLight = Color.Lerp(AmbientPerStage[cur], AmbientPerStage[next], ct);

        UpdateNebulaParticles(stage, progress);
    }

    // ── Planetary Nebula — ReactiveNebula-style layers ────────────────

    void BuildNebulaParticles()
    {
        _nebulaGO = new GameObject("PlanetaryNebula");
        _nebulaGO.transform.position = Vector3.zero;

        var tex = GenerateNebulaTexture();
        var mat = MakeNebulaMat(tex);

        _outerPS = BuildLayer(_nebulaGO, "Outer", 500,
            minSize: 30f, maxSize: 80f, minSpeed: 1f, maxSpeed: 5f,
            minLife: 10f, maxLife: 20f,
            tint: new Color(0.45f, 0.15f, 0.85f, 0.22f),
            mat: mat, stretch: true, velScale: 0.9f);

        _midPS = BuildLayer(_nebulaGO, "Mid", 700,
            minSize: 20f, maxSize: 55f, minSpeed: 0.3f, maxSpeed: 2.5f,
            minLife: 12f, maxLife: 24f,
            tint: new Color(0.65f, 0.25f, 0.92f, 0.25f),
            mat: mat, stretch: false, velScale: 0f);

        _corePS = BuildLayer(_nebulaGO, "Core", 350,
            minSize: 12f, maxSize: 35f, minSpeed: 0.5f, maxSpeed: 3f,
            minLife: 7f,  maxLife: 14f,
            tint: new Color(0.88f, 0.65f, 1.00f, 0.30f),
            mat: mat, stretch: false, velScale: 0f);

        _nebulaGO.SetActive(false);
    }

    ParticleSystem BuildLayer(GameObject parent, string layerName,
        int count, float minSize, float maxSize,
        float minSpeed, float maxSpeed, float minLife, float maxLife,
        Color tint, Material mat, bool stretch, float velScale)
    {
        var go = new GameObject(layerName);
        go.transform.SetParent(parent.transform, false);
        go.transform.localPosition = Vector3.zero;

        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.loop            = true;
        main.duration        = 12f;
        main.startLifetime   = new ParticleSystem.MinMaxCurve(minLife, maxLife);
        main.startSpeed      = new ParticleSystem.MinMaxCurve(minSpeed, maxSpeed);
        main.startSize       = new ParticleSystem.MinMaxCurve(minSize, maxSize);
        main.startColor      = new ParticleSystem.MinMaxGradient(tint);
        main.maxParticles    = count * 3;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = 0f;

        var em = ps.emission;
        em.rateOverTime = count / 14f;

        var shape = ps.shape;
        shape.shapeType       = ParticleSystemShapeType.Sphere;
        shape.radius          = 200f;
        shape.radiusThickness = 1f;

        // Fade-in / fade-out over particle lifetime (colour × alpha only)
        var col = ps.colorOverLifetime;
        col.enabled = true;
        var g = new Gradient();
        g.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(Color.white, 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0f,   0f),
                new GradientAlphaKey(1f,   0.15f),
                new GradientAlphaKey(0.9f, 0.75f),
                new GradientAlphaKey(0f,   1f)
            });
        col.color = new ParticleSystem.MinMaxGradient(g);

        // Organic turbulence — same as ReactiveNebula
        var noise = ps.noise;
        noise.enabled     = true;
        noise.strength    = 0.45f;
        noise.frequency   = 0.12f;
        noise.scrollSpeed = 0.20f;
        noise.octaveCount = 2;

        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        sol.size = new ParticleSystem.MinMaxCurve(1f,
            new AnimationCurve(
                new Keyframe(0f,    0f),
                new Keyframe(0.12f, 1f),
                new Keyframe(0.80f, 0.85f),
                new Keyframe(1f,    0f)));

        var r = go.GetComponent<ParticleSystemRenderer>();
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.material          = mat;

        if (stretch)
        {
            r.renderMode    = ParticleSystemRenderMode.Stretch;
            r.velocityScale = velScale;
            r.lengthScale   = 3f;
        }
        else
        {
            r.renderMode = ParticleSystemRenderMode.Billboard;
        }

        return ps;
    }

    static Material MakeNebulaMat(Texture2D tex)
    {
        var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                  ?? Shader.Find("Sprites/Default");
        var mat = new Material(shader);
        mat.SetFloat("_Surface", 1f);
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.renderQueue = 3500;
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);
        if (mat.HasProperty("_BaseMap"))   mat.SetTexture("_BaseMap", tex);
        if (mat.HasProperty("_MainTex"))   mat.SetTexture("_MainTex", tex);
        return mat;
    }

    // Soft radial gradient with Perlin variation — identical to ReactiveNebula
    static Texture2D GenerateNebulaTexture()
    {
        const int res = 128;
        var tex = new Texture2D(res, res);
        for (int y = 0; y < res; y++)
        {
            for (int x = 0; x < res; x++)
            {
                float dx = (x / (float)res) - 0.5f;
                float dy = (y / (float)res) - 0.5f;
                float d  = Mathf.Sqrt(dx * dx + dy * dy) * 2f;
                float a  = Mathf.Clamp01(1f - d);
                float n  = Mathf.PerlinNoise(x * 0.07f, y * 0.07f) * 0.7f + 0.3f;
                a = Mathf.Pow(a, 2.5f) * n;
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }
        tex.Apply();
        return tex;
    }

    void UpdateNebulaParticles(SolarStage stage, float progress)
    {
        bool visible = stage == SolarStage.PlanetaryNebula || stage == SolarStage.WhiteDwarf;

        if (!visible)
        {
            if (_nebulaGO.activeSelf)
            {
                _outerPS.Stop(); _midPS.Stop(); _corePS.Stop();
                _nebulaGO.SetActive(false);
            }
            return;
        }

        if (!_nebulaGO.activeSelf)
        {
            _nebulaGO.SetActive(true);
            _outerPS.Play(); _midPS.Play(); _corePS.Play();
        }

        float t      = stage == SolarStage.PlanetaryNebula ? progress : 1f;
        float radius = Mathf.Lerp(55f, 680f, t);

        // During WhiteDwarf the nebula slowly disperses (but stays fairly visible)
        float fadeMult = stage == SolarStage.WhiteDwarf
            ? Mathf.Lerp(1f, 0.40f, progress)
            : 1f;

        if (Mathf.Abs(radius - _currentNebulaRadius) > 1f)
        {
            _currentNebulaRadius = radius;
            SetLayerRadius(_outerPS, radius);
            SetLayerRadius(_midPS,   radius * 0.60f);
            SetLayerRadius(_corePS,  radius * 0.28f);
        }

        // Higher base alphas so the nebula is clearly visible
        SetLayerAlpha(_outerPS, 0.45f * fadeMult);
        SetLayerAlpha(_midPS,   0.55f * fadeMult);
        SetLayerAlpha(_corePS,  0.65f * fadeMult);
    }

    static void SetLayerRadius(ParticleSystem ps, float r)
    {
        var shape = ps.shape;
        shape.radius = Mathf.Max(r, 1f);
    }

    static void SetLayerAlpha(ParticleSystem ps, float alpha)
    {
        var main  = ps.main;
        Color col = main.startColor.color;
        col.a     = alpha;
        main.startColor = new ParticleSystem.MinMaxGradient(col);
    }

    // ── Camera shake ──────────────────────────────────────────────────

    IEnumerator CameraShake(float intensity, float duration)
    {
        var cam = Camera.main;
        if (cam == null) yield break;

        Vector3 origin = cam.transform.localPosition;
        float   t      = 0f;

        while (t < duration)
        {
            float decay = 1f - (t / duration);
            cam.transform.localPosition = origin + (Vector3)(Random.insideUnitCircle * intensity * decay);
            t += Time.deltaTime;
            yield return null;
        }
        cam.transform.localPosition = origin;
    }
}
