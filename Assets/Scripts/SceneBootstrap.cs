using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

[DefaultExecutionOrder(-100)]
public class SceneBootstrap : MonoBehaviour
{
    [Header("Layout")]
    public float auToUnits = 6f;
    public float probeStartOffset = 10f;

    [Header("Planet sizes (diameter in world units)")]
    public float sunScale = 10f;
    public float minPlanetScale = 3.0f;
    public float massToScale = 2.5f;

    static readonly Dictionary<string, (float au, float mass, Color color)> defaults = new()
    {
        { "Mercury", (0.4f,  0.055f, new Color(0.70f, 0.70f, 0.70f)) },
        { "Venus",   (0.7f,  0.815f, new Color(0.90f, 0.75f, 0.40f)) },
        { "Earth",   (1.0f,  1.000f, new Color(0.30f, 0.55f, 0.90f)) },
        { "Mars",    (1.5f,  0.107f, new Color(0.80f, 0.35f, 0.25f)) },
        { "Jupiter", (5.2f, 317.8f,  new Color(0.85f, 0.70f, 0.50f)) },
        { "Saturn",  (9.5f,  95.2f,  new Color(0.90f, 0.80f, 0.55f)) },
        { "Uranus",  (19.2f, 14.5f,  new Color(0.60f, 0.85f, 0.90f)) },
        { "Neptune", (30.1f, 17.1f,  new Color(0.30f, 0.45f, 0.85f)) },
    };

    static readonly Dictionary<string, float> smoothnessMap = new()
    {
        { "Mercury", 0.05f },
        { "Venus",   0.12f },
        { "Earth",   0.18f },
        { "Mars",    0.05f },
        { "Jupiter", 0.25f },
        { "Saturn",  0.22f },
        { "Uranus",  0.30f },
        { "Neptune", 0.28f },
    };

    void Awake()
    {
        LayoutSolarSystem();
        ConfigureLighting();
        ConfigureUI();
        gameObject.AddComponent<Starfield>();
        if (GetComponent<RandomEventManager>() == null) gameObject.AddComponent<RandomEventManager>();
        if (GetComponent<MissionLog>() == null) gameObject.AddComponent<MissionLog>();
    }

    void ConfigureLighting()
    {
        foreach (var l in Object.FindObjectsByType<Light>(FindObjectsInactive.Include))
        {
            if (l.type == LightType.Directional) l.enabled = false;
        }

        var sun = GameObject.Find("Sun");
        if (sun != null)
        {
            var sunLight = sun.GetComponent<Light>();
            if (sunLight == null) sunLight = sun.AddComponent<Light>();
            sunLight.type = LightType.Point;
            sunLight.color = new Color(1f, 0.96f, 0.82f);
            sunLight.intensity = 65f;
            sunLight.range = 8000f;
            sunLight.shadows = LightShadows.Hard;
            sunLight.shadowStrength = 0.9f;
            }

            foreach (var c in Object.FindObjectsByType<Camera>(FindObjectsInactive.Include))
            {
            c.allowHDR = true;
            var urp = c.GetUniversalAdditionalCameraData();
            if (urp != null)
            {
                urp.renderPostProcessing = true;
                urp.antialiasing = AntialiasingMode.FastApproximateAntialiasing;
                }
                }

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.05f, 0.05f, 0.08f); // Much darker ambient

        // Subtle fill light from camera direction so planet textures are always visible
        var fillGO = new GameObject("FillLight");
        fillGO.transform.SetParent(transform, false);
        var fill = fillGO.AddComponent<Light>();
        fill.type = LightType.Directional;
        fill.color = new Color(0.75f, 0.80f, 1.0f);
        fill.intensity = 0.45f;
        fill.shadows = LightShadows.None;
        fillGO.transform.rotation = Quaternion.Euler(40f, 18f, 0f);

        // Second fill from opposite side for wrap-around lighting
        var fill2GO = new GameObject("FillLight2");
        fill2GO.transform.SetParent(transform, false);
        var fill2 = fill2GO.AddComponent<Light>();
        fill2.type = LightType.Directional;
        fill2.color = new Color(0.40f, 0.45f, 0.65f);
        fill2.intensity = 0.15f;
        fill2.shadows = LightShadows.None;
        fill2GO.transform.rotation = Quaternion.Euler(145f, 195f, 0f);

        var bloomProfile = ScriptableObject.CreateInstance<UnityEngine.Rendering.VolumeProfile>();
        bloomProfile.name = "RuntimePostProfile";

        var tone = bloomProfile.Add<UnityEngine.Rendering.Universal.Tonemapping>(true);
        tone.active = true;
        tone.mode.Override(UnityEngine.Rendering.Universal.TonemappingMode.ACES);

        var colorAdj = bloomProfile.Add<UnityEngine.Rendering.Universal.ColorAdjustments>(true);
        colorAdj.active = true;
        colorAdj.postExposure.Override(0.25f);
        colorAdj.contrast.Override(18f);
        colorAdj.saturation.Override(45f); // Increased saturation for vibrant nebulae
        colorAdj.colorFilter.Override(Color.white);

        var vignette = bloomProfile.Add<UnityEngine.Rendering.Universal.Vignette>(true);
        vignette.active = true;
        vignette.intensity.Override(0.30f);
        vignette.smoothness.Override(0.5f);

        var bloom = bloomProfile.Add<UnityEngine.Rendering.Universal.Bloom>(true);
        bloom.active = true;
        bloom.threshold.Override(1.02f); // Lower threshold so stars and textures can glow slightly
        bloom.intensity.Override(0.7f);  // Slightly higher intensity
        bloom.scatter.Override(0.7f);
        bloom.tint.Override(new Color(1f, 0.95f, 0.85f));
bloom.highQualityFiltering.Override(true);

        var volGO = new GameObject("RuntimeBloomVolume");
        volGO.transform.SetParent(transform, false);
        var vol = volGO.AddComponent<UnityEngine.Rendering.Volume>();
        vol.isGlobal = true;
        vol.priority = 100f;
        vol.weight = 1f;
        vol.sharedProfile = bloomProfile;
    }

    void LayoutSolarSystem()
    {
        var sun = GameObject.Find("Sun");
        if (sun != null)
        {
            sun.transform.position = Vector3.zero;
            sun.transform.localScale = Vector3.one * sunScale;
            var sunRealTex = Resources.Load<Texture2D>("PlanetTextures/Sun");
            if (sunRealTex != null)
                ApplyRealTexture(sun, sunRealTex, emissive: true);
            else
                ApplyTexture(sun, ProceduralPlanetTexture.GenerateSun(), emissive: true);
            AddSunGlow(sun);
            var sunSpin = sun.GetComponent<SelfRotation>() ?? sun.AddComponent<SelfRotation>();
            sunSpin.degreesPerSecond = 8f;
        }

        var planets = Object.FindObjectsByType<Planet>(FindObjectsInactive.Exclude);
        float angle = Mathf.PI * 1.05f;  // Start spread so planets fan toward camera
        float angleStep = Mathf.PI * 2f / Mathf.Max(1, planets.Length);
        Transform sunT = sun != null ? sun.transform : null;

        foreach (var p in planets)
        {
            if (p == null) continue;
            if (string.IsNullOrEmpty(p.data.planetName) || p.data.planetName == "Planet")
                p.data.planetName = p.gameObject.name;

            if (!defaults.TryGetValue(p.data.planetName, out var def))
            {
                def = (p.data.distanceFromSun > 0 ? p.data.distanceFromSun : 1f,
                       p.data.relativeMass > 0 ? p.data.relativeMass : 1f,
                       Color.gray);
            }

            p.data.distanceFromSun = def.au;
            p.data.relativeMass = def.mass;

            float r = sunScale * 0.55f + Mathf.Pow(def.au, 0.55f) * auToUnits * 4.5f;
            Vector3 pos = new Vector3(Mathf.Cos(angle) * r, 0f, Mathf.Sin(angle) * r);
            p.transform.position = pos;
            angle += angleStep;

            float s = Mathf.Max(4.0f, Mathf.Log10(def.mass * 10f + 1f) * 4.2f);
            p.transform.localScale = Vector3.one * s;

            var realTex = Resources.Load<Texture2D>($"PlanetTextures/{p.data.planetName}");
            if (realTex != null)
                ApplyRealTexture(p.gameObject, realTex);
            else
            {
                var maps = ProceduralPlanetTexture.Generate(p.data.planetName, def.color);
                ApplyTexture(p.gameObject, maps, emissive: false);
            }
            if (smoothnessMap.TryGetValue(p.data.planetName, out float sm))
                SetSmoothness(p.gameObject, sm);
            AddAtmosphere(p.gameObject, def.color);

            var orbit = p.gameObject.GetComponent<OrbitalMotion>() ?? p.gameObject.AddComponent<OrbitalMotion>();
            orbit.center = sunT;
            orbit.angularSpeedDeg = 2f / Mathf.Pow(def.au, 1.5f);

            var spin = p.gameObject.GetComponent<SelfRotation>() ?? p.gameObject.AddComponent<SelfRotation>();
            spin.degreesPerSecond = 30f + Random.Range(-10f, 10f);

            var orbitLineGO = new GameObject($"{p.data.planetName}_Orbit");
            orbitLineGO.transform.SetParent(transform, false);
            var lr = orbitLineGO.AddComponent<LineRenderer>();
            var path = orbitLineGO.AddComponent<OrbitPathRenderer>();
            path.center = sunT;
            path.orbitRadius = r;
            lr.positionCount = 0;

            if (p.data.planetName == "Saturn")
                p.gameObject.AddComponent<SaturnRings>();

            if (p.GetComponent<ScanEffect>() == null)
                p.gameObject.AddComponent<ScanEffect>();
            }

        var probe = Object.FindAnyObjectByType<ProbeController>();
        if (probe != null)
        {
            Vector3 safe = new Vector3(0f, 0f, -(30.1f * auToUnits + probeStartOffset));
            probe.transform.position = safe;
            probe.transform.localScale = Vector3.one * 1.2f;
            if (probe.GetComponent<ProceduralRocket>() == null) probe.gameObject.AddComponent<ProceduralRocket>();
            if (probe.GetComponent<RocketExhaust>() == null) probe.gameObject.AddComponent<RocketExhaust>();
            var probeLight = probe.gameObject.GetComponent<Light>();
            if (probeLight == null) probeLight = probe.gameObject.AddComponent<Light>();
            probeLight.type = LightType.Point;
            probeLight.color = new Color(0.15f, 0.85f, 1f);
            probeLight.intensity = 8f;
            probeLight.range = 30f;
            probeLight.shadows = LightShadows.None;
        }

        var camCtrl = Object.FindAnyObjectByType<CameraController>();
        if (camCtrl != null) camCtrl.distanceMultiplier = 10f;

        var cam = Camera.main;
        if (cam != null)
        {
            cam.transform.position = new Vector3(20f, 120f, -170f);
            cam.transform.rotation = Quaternion.Euler(34f, -6f, 0f);
            cam.fieldOfView = 62f;
            cam.farClipPlane = 10000f;
            cam.nearClipPlane = 0.1f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            if (cam.GetComponent<FreeFlyCamera>() == null) cam.gameObject.AddComponent<FreeFlyCamera>();
            }
            }

    void ApplyRealTexture(GameObject go, Texture2D tex, bool emissive = false)
    {
        var r = go.GetComponentInChildren<Renderer>();
        if (r == null) return;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On; // Enable shadow casting
        r.receiveShadows = true;
        
        var litShader = Shader.Find("Universal Render Pipeline/Lit");
var m = new Material(litShader != null ? litShader : r.sharedMaterial.shader);
        r.material = m;

        if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", tex);
        if (m.HasProperty("_MainTex")) m.SetTexture("_MainTex", tex);
        m.mainTexture = tex;

        if (emissive)
        {
            m.EnableKeyword("_EMISSION");
            if (m.HasProperty("_EmissionMap")) m.SetTexture("_EmissionMap", tex);
            // High intensity for that "Sun" look, but tinted so texture is visible
            if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", new Color(1f, 0.8f, 0.4f) * 2.2f);
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", Color.white);
        }
        else
        {
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", Color.white);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0f);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.1f);
        }
    }

    static void SetSmoothness(GameObject go, float smoothness)
    {
        var r = go.GetComponentInChildren<Renderer>();
        if (r?.material == null) return;
        if (r.material.HasProperty("_Smoothness")) r.material.SetFloat("_Smoothness", smoothness);
        if (r.material.HasProperty("_Glossiness")) r.material.SetFloat("_Glossiness", smoothness);
    }

    void TintRenderer(GameObject go, Color c, bool emissive)
    {
        var r = go.GetComponentInChildren<Renderer>();
        if (r == null) return;
        var m = r.material;
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Color")) m.color = c;
        if (emissive && m.HasProperty("_EmissionColor"))
        {
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", c * 1.5f);
        }
    }

    void ApplyTexture(GameObject go, ProceduralPlanetTexture.PlanetMaps maps, bool emissive)
    {
        var r = go.GetComponentInChildren<Renderer>();
        if (r == null || maps.albedo == null) return;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        var litShader = Shader.Find("Universal Render Pipeline/Lit");
        var unlitShader = Shader.Find("Universal Render Pipeline/Unlit");
        var target = emissive && unlitShader != null ? unlitShader : litShader;
        var m = new Material(target != null ? target : r.sharedMaterial.shader);
        r.material = m;

        if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", maps.albedo);
        if (m.HasProperty("_MainTex")) m.SetTexture("_MainTex", maps.albedo);
        m.mainTexture = maps.albedo;
        Color baseColor = emissive ? new Color(2.5f, 2.3f, 1.8f, 1f) : new Color(0.85f, 0.85f, 0.85f, 1f);
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", baseColor);
        if (m.HasProperty("_Color")) m.color = baseColor;
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.08f);
        if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0f);
        if (maps.normal != null)
        {
            if (m.HasProperty("_BumpMap"))
            {
                m.SetTexture("_BumpMap", maps.normal);
                m.EnableKeyword("_NORMALMAP");
            }
            if (m.HasProperty("_BumpScale")) m.SetFloat("_BumpScale", 1.2f);
        }
    }

    void AddSunGlow(GameObject sun)
    {
        var glow = new GameObject("SunGlow");
        glow.transform.SetParent(sun.transform, false);
        glow.transform.localPosition = Vector3.zero;
        glow.transform.localScale = Vector3.one * 5.5f;

        var mf = glow.AddComponent<MeshFilter>();
        mf.mesh = BuildQuadMesh();
        var mr = glow.AddComponent<MeshRenderer>();
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;

        var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Transparent");
        var mat = new Material(shader);
        if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f);
        if (mat.HasProperty("_Blend")) mat.SetFloat("_Blend", 1f);
        if (mat.HasProperty("_SrcBlend")) mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        if (mat.HasProperty("_DstBlend")) mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
        if (mat.HasProperty("_ZWrite")) mat.SetFloat("_ZWrite", 0f);
        if (mat.HasProperty("_Cull")) mat.SetFloat("_Cull", 0f);
        mat.SetOverrideTag("RenderType", "Transparent");
        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent + 50;
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.EnableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.DisableKeyword("_ALPHATEST_ON");
        var glowTex = ProceduralPlanetTexture.GenerateSunGlow();
        if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", glowTex);
        if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", glowTex);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", new Color(1f, 0.88f, 0.58f, 0.35f));
        if (mat.HasProperty("_Color")) mat.color = new Color(1f, 0.88f, 0.58f, 0.35f);
        mr.material = mat;

        glow.AddComponent<Billboard>();

        // Second larger, dimmer halo for corona effect
        var corona = new GameObject("SunCorona");
        corona.transform.SetParent(sun.transform, false);
        corona.transform.localPosition = Vector3.zero;
        corona.transform.localScale = Vector3.one * 9.5f;
        var mf2 = corona.AddComponent<MeshFilter>();
        mf2.mesh = BuildQuadMesh();
        var mr2 = corona.AddComponent<MeshRenderer>();
        mr2.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr2.receiveShadows = false;
        var mat2 = new Material(shader ?? Shader.Find("Universal Render Pipeline/Unlit"));
        if (mat2.HasProperty("_Surface")) mat2.SetFloat("_Surface", 1f);
        if (mat2.HasProperty("_Blend")) mat2.SetFloat("_Blend", 1f);
        if (mat2.HasProperty("_SrcBlend")) mat2.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        if (mat2.HasProperty("_DstBlend")) mat2.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
        if (mat2.HasProperty("_ZWrite")) mat2.SetFloat("_ZWrite", 0f);
        if (mat2.HasProperty("_Cull")) mat2.SetFloat("_Cull", 0f);
        mat2.SetOverrideTag("RenderType", "Transparent");
        mat2.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent + 49;
        mat2.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat2.EnableKeyword("_ALPHAPREMULTIPLY_ON");
        var coronaTex = ProceduralPlanetTexture.GenerateSunGlow();
        if (mat2.HasProperty("_BaseMap")) mat2.SetTexture("_BaseMap", coronaTex);
        if (mat2.HasProperty("_MainTex")) mat2.SetTexture("_MainTex", coronaTex);
        if (mat2.HasProperty("_BaseColor")) mat2.SetColor("_BaseColor", new Color(1f, 0.75f, 0.35f, 0.15f));
        if (mat2.HasProperty("_Color")) mat2.color = new Color(1f, 0.75f, 0.35f, 0.15f);
        mr2.material = mat2;
corona.AddComponent<Billboard>();
    }

    void AddAtmosphere(GameObject planet, Color baseColor)
    {
        // Only add visible atmosphere glow for planets with significant atmospheres
        string name = planet.name;
        bool hasAtmo = name == "Earth" || name == "Venus" || name == "Uranus" || name == "Neptune";
        float scale = hasAtmo ? 1.08f : 1.05f;
        float alpha  = hasAtmo ? 0.055f : 0.025f;

        var atmo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        atmo.name = "Atmosphere";
        Object.Destroy(atmo.GetComponent<SphereCollider>());
        atmo.transform.SetParent(planet.transform, false);
        atmo.transform.localPosition = Vector3.zero;
        atmo.transform.localScale = Vector3.one * scale;

        var mr = atmo.GetComponent<MeshRenderer>();
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;

        var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Transparent");
        var mat = new Material(shader);
        if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f);
        if (mat.HasProperty("_Blend"))   mat.SetFloat("_Blend",   1f);
        if (mat.HasProperty("_SrcBlend")) mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        if (mat.HasProperty("_DstBlend")) mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
        if (mat.HasProperty("_ZWrite"))  mat.SetFloat("_ZWrite",   0f);
        if (mat.HasProperty("_Cull"))    mat.SetFloat("_Cull",     0f);
        mat.SetOverrideTag("RenderType", "Transparent");
        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.EnableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.DisableKeyword("_ALPHATEST_ON");
        Color atmoColor = new Color(
            Mathf.Clamp01(baseColor.r * 0.5f + 0.5f),
            Mathf.Clamp01(baseColor.g * 0.5f + 0.5f),
            Mathf.Clamp01(baseColor.b * 0.5f + 0.5f),
            alpha);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", atmoColor);
        if (mat.HasProperty("_Color")) mat.color = atmoColor;
        mr.material = mat;
    }

    static Mesh BuildQuadMesh()
    {
        var mesh = new Mesh { name = "GlowQuad" };
        mesh.vertices = new[] {
            new Vector3(-0.5f, -0.5f, 0f),
            new Vector3( 0.5f, -0.5f, 0f),
            new Vector3( 0.5f,  0.5f, 0f),
            new Vector3(-0.5f,  0.5f, 0f),
        };
        mesh.uv = new[] {
            new Vector2(0, 0), new Vector2(1, 0),
            new Vector2(1, 1), new Vector2(0, 1),
        };
        mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
        mesh.RecalculateBounds();
        return mesh;
    }

    void ConfigureUI()
    {
        // ── Destroy any existing TimeScaleController GameObjects from scene ──
        foreach (var old in Object.FindObjectsByType<TimeScaleController>(FindObjectsInactive.Include))
            Destroy(old.gameObject);

        // ── Clean every scene Canvas: destroy children that have no PlanetSelectionUI ──
        foreach (var cv in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include))
        {
            var rt = cv.GetComponent<RectTransform>();
            for (int i = rt.childCount - 1; i >= 0; i--)
            {
                var ch = rt.GetChild(i);
                if (ch.GetComponent<PlanetSelectionUI>() == null &&
                    ch.GetComponentInChildren<PlanetSelectionUI>(true) == null)
                    Destroy(ch.gameObject);
            }
        }

        // ── EventSystem (if not already present) ──
        if (Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var esGO = new GameObject("EventSystem");
            esGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esGO.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }

        // ── HUD ──
        var probe = Object.FindAnyObjectByType<ProbeController>();
        if (probe != null)
        {
            var hudGO = new GameObject("HUD", typeof(Canvas), typeof(CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
            var hudController = hudGO.AddComponent<HUDController>();
            hudController.Setup(probe);
        }

        // ── Target indicator ──
        var indicatorGO = new GameObject("TargetIndicator");
        indicatorGO.AddComponent<TargetIndicator>();

        // ── Start screen ──
        var ssGO = new GameObject("StartScreen", typeof(Canvas), typeof(CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
        var ss = ssGO.AddComponent<StartScreen>();
        ss.Setup();

        // ── Planet panel ──
        StylePlanetPanel();

        // ── Time Scale UI ──
        CreateTimeScaleUI();
    }

    void StylePlanetPanel()
    {
        var ui = Object.FindAnyObjectByType<PlanetSelectionUI>();
        if (ui == null) return;
        var existingCanvas = ui.GetComponentInParent<Canvas>();
        if (existingCanvas == null) return;

        // Hide original raw panel children
        var existingRT = existingCanvas.GetComponent<RectTransform>();
        for (int i = existingRT.childCount - 1; i >= 0; i--)
        {
            var ch = existingRT.GetChild(i);
            if (ch.name == "Panel") ch.gameObject.SetActive(false);
        }

        // Build our own canvas for the planet panel
        var cGO = new GameObject("PlanetCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
        var c = cGO.GetComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = 10;
        var cs = cGO.GetComponent<CanvasScaler>();
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920, 1080);
        cs.matchWidthOrHeight = 0.5f;
        var cRoot = cGO.GetComponent<RectTransform>();

        // Outer panel — top-left, 300×360
        var panel = UIRect("PlanetPanel", cRoot);
        SetCorner(panel, new Vector2(0,1), new Vector2(0,1), new Vector2(0,1), new Vector2(20,-20), new Vector2(300, 360));
        AddImage(panel, new Color(0.04f, 0.06f, 0.12f, 0.92f));

        // Cyan top accent
        var accent = UIRect("Accent", panel);
        SetCorner(accent, new Vector2(0,1), new Vector2(1,1), new Vector2(0.5f,1), Vector2.zero, new Vector2(0,3));
        AddImage(accent, new Color(0f, 0.85f, 1f, 0.9f));

        // Title
        var titleR = UIRect("PanelTitle", panel);
        SetCorner(titleR, new Vector2(0,1), new Vector2(1,1), new Vector2(0,1), new Vector2(12,-8), new Vector2(-12,22));
        var titleT = titleR.gameObject.AddComponent<TextMeshProUGUI>();
        titleT.text = "PLANET SELECT";
        titleT.fontSize = 13; titleT.fontStyle = FontStyles.Bold;
        titleT.color = new Color(0f, 0.85f, 1f);
        titleT.characterSpacing = 3f;

        // Dropdown re-parented
        if (ui.dropdown != null)
        {
            ui.dropdown.transform.SetParent(panel, false);
            var dRT = ui.dropdown.GetComponent<RectTransform>();
            SetCorner(dRT, new Vector2(0,1), new Vector2(1,1), new Vector2(0,1), new Vector2(10,-36), new Vector2(-10,38));
            StyleDropdown(ui.dropdown);
        }

        // Divider
        var div = UIRect("Divider", panel);
        SetCorner(div, new Vector2(0,1), new Vector2(1,1), new Vector2(0,1), new Vector2(0,-80), new Vector2(0,1));
        AddImage(div, new Color(0f, 0.85f, 1f, 0.3f));

        // Info text
        if (ui.infoText != null)
        {
            ui.infoText.transform.SetParent(panel, false);
            var iRT = ui.infoText.GetComponent<RectTransform>();
            SetCorner(iRT, new Vector2(0,1), new Vector2(1,1), new Vector2(0,1), new Vector2(0,-82), new Vector2(0,220));
            ui.infoText.textWrappingMode = TextWrappingModes.Normal;
            ui.infoText.fontSize = 18;
            ui.infoText.color = Color.white;
            ui.infoText.alignment = TextAlignmentOptions.TopLeft;
            ui.infoText.margin = new Vector4(14, 10, 14, 0);
        }

        // Divider 2
        var div2 = UIRect("Divider2", panel);
        SetCorner(div2, new Vector2(0,0), new Vector2(1,0), new Vector2(0,0), new Vector2(0,76), new Vector2(0,1));
        AddImage(div2, new Color(0f, 0.85f, 1f, 0.3f));

        // Send probe button
        if (ui.sendProbeButton != null)
        {
            ui.sendProbeButton.transform.SetParent(panel, false);
            var bRT = ui.sendProbeButton.GetComponent<RectTransform>();
            SetCorner(bRT, new Vector2(0,0), new Vector2(1,0), new Vector2(0,0), new Vector2(12,12), new Vector2(-12,52));
            StyleButton(ui.sendProbeButton, "SEND PROBE", new Color(0f, 0.85f, 1f));
        }
    }

    void CreateTimeScaleUI()
    {
        var canvasGO = new GameObject("TimeScaleCanvas",
            typeof(Canvas), typeof(CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20;
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        var root = canvasGO.GetComponent<RectTransform>();

        // ── Panel: bottom-right, 400×120 ──
        var panel = UIRect("TimeScalePanel", root);
        SetCorner(panel, new Vector2(1,0), new Vector2(1,0), new Vector2(1,0), new Vector2(-20,80), new Vector2(400,120));
        AddImage(panel, new Color(0.04f, 0.06f, 0.12f, 0.92f));

        // Top accent
        var accent2 = UIRect("Accent", panel);
        SetCorner(accent2, new Vector2(0,1), new Vector2(1,1), new Vector2(0.5f,1), Vector2.zero, new Vector2(0,2));
        AddImage(accent2, new Color(0f, 0.85f, 1f));

        // Title (top-left)
        var titleR2 = UIRect("Title", panel);
        SetCorner(titleR2, new Vector2(0,1), new Vector2(0,1), new Vector2(0,1), new Vector2(12,-7), new Vector2(160,20));
        Label(titleR2, "TIME CONTROL", 12, new Color(0f,0.85f,1f), FontStyles.Bold, 3f);

        // Speed label (top-right)
        var speedR2 = UIRect("SpeedLabel", panel);
        SetCorner(speedR2, new Vector2(1,1), new Vector2(1,1), new Vector2(1,1), new Vector2(-12,-7), new Vector2(100,20));
        var speedTMP = Label(speedR2, "1x", 15, Color.white, FontStyles.Bold);
        speedTMP.alignment = TextAlignmentOptions.Right;

        // Slider — middle strip, proper RectTransform from creation
        var sliderR2 = UIRect("TimeSlider", panel);
        SetCorner(sliderR2, new Vector2(0,1), new Vector2(1,1), new Vector2(0.5f,1), new Vector2(0,-33), new Vector2(-24, 22));
        var slider = sliderR2.gameObject.AddComponent<Slider>();
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 1f; slider.maxValue = 100f;
        slider.value = 1f; slider.wholeNumbers = true;

        var bgR2 = UIRect("Background", sliderR2);
        bgR2.anchorMin = new Vector2(0,0.35f); bgR2.anchorMax = new Vector2(1,0.65f);
        bgR2.offsetMin = bgR2.offsetMax = Vector2.zero;
        AddImage(bgR2, new Color(0.1f,0.14f,0.22f));

        var faR2 = UIRect("Fill Area", sliderR2);
        faR2.anchorMin = new Vector2(0,0.35f); faR2.anchorMax = new Vector2(1,0.65f);
        faR2.offsetMin = faR2.offsetMax = Vector2.zero;

        var fillR2 = UIRect("Fill", faR2);
        fillR2.anchorMin = Vector2.zero; fillR2.anchorMax = new Vector2(0,1);
        fillR2.offsetMin = fillR2.offsetMax = Vector2.zero;
        var fillImg2 = AddImage(fillR2, new Color(0f, 0.75f, 1f));

        var haR2 = UIRect("Handle Slide Area", sliderR2);
        haR2.anchorMin = Vector2.zero; haR2.anchorMax = Vector2.one;
        haR2.offsetMin = haR2.offsetMax = Vector2.zero;

        var handleR2 = UIRect("Handle", haR2);
        handleR2.anchorMin = new Vector2(0,0); handleR2.anchorMax = new Vector2(0,1);
        handleR2.pivot = new Vector2(0.5f,0.5f);
        handleR2.offsetMin = new Vector2(-10,0); handleR2.offsetMax = new Vector2(10,0);
        var handleImg2 = AddImage(handleR2, Color.white);

        slider.fillRect = fillR2; slider.handleRect = handleR2; slider.targetGraphic = handleImg2;

        // Tick labels
        var lbl1 = UIRect("Lbl1x", sliderR2);
        SetCorner(lbl1, new Vector2(0,0), new Vector2(0,0), new Vector2(0,1), new Vector2(0,-2), new Vector2(30,14));
        Label(lbl1, "1x", 10, new Color(0.55f,0.65f,0.75f));

        var lbl100 = UIRect("Lbl100x", sliderR2);
        SetCorner(lbl100, new Vector2(1,0), new Vector2(1,0), new Vector2(1,1), new Vector2(0,-2), new Vector2(40,14));
        Label(lbl100, "100x", 10, new Color(0.55f,0.65f,0.75f)).alignment = TextAlignmentOptions.Right;

        // Pause button (bottom-right of panel)
        var btnR2 = UIRect("PauseButton", panel);
        SetCorner(btnR2, new Vector2(1,0), new Vector2(1,0), new Vector2(1,0), new Vector2(-12,10), new Vector2(120,40));
        var btnImg2 = AddImage(btnR2, new Color(0.05f,0.15f,0.28f));
        var btn = btnR2.gameObject.AddComponent<Button>();
        var bc2 = btn.colors;
        bc2.normalColor = new Color(0.05f,0.15f,0.28f);
        bc2.highlightedColor = new Color(0.1f,0.28f,0.5f);
        bc2.pressedColor = new Color(0.02f,0.08f,0.18f);
        btn.colors = bc2; btn.targetGraphic = btnImg2;

        var btnTextR2 = UIRect("Text", btnR2);
        btnTextR2.anchorMin = Vector2.zero; btnTextR2.anchorMax = Vector2.one;
        btnTextR2.offsetMin = btnTextR2.offsetMax = Vector2.zero;
        var btnTMP = Label(btnTextR2, "|| PAUSE", 14, new Color(0f,0.85f,1f), FontStyles.Bold);
        btnTMP.alignment = TextAlignmentOptions.Center;

        var ctrl = canvasGO.AddComponent<TimeScaleController>();
        ctrl.timeSlider = slider; ctrl.pauseButton = btn;
        ctrl.speedLabel = speedTMP; ctrl.pauseButtonText = btnTMP;
    }

    static void ReparentAndAnchor(Transform t, Transform newParent, Vector2 pos, Vector2 size)
    {
        t.SetParent(newParent, false);
        var rt = t.GetComponent<RectTransform>();
        if (rt == null) return;
        AnchorRect(rt, pos, size);
    }

    static void AnchorRect(RectTransform rt, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        rt.localScale = Vector3.one;
        rt.localRotation = Quaternion.identity;
    }

    // ── UI build helpers ─────────────────────────────────────────

    static RectTransform UIRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
    }

    static void SetCorner(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    static Image AddImage(RectTransform rt, Color color)
    {
        var img = rt.gameObject.AddComponent<Image>();
        img.color = color;
        return img;
    }

    static TextMeshProUGUI Label(RectTransform rt, string text, float size, Color color,
        FontStyles style = FontStyles.Normal, float charSpacing = 0f)
    {
        var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
        tmp.text = text; tmp.fontSize = size; tmp.color = color;
        tmp.fontStyle = style; tmp.characterSpacing = charSpacing;
        return tmp;
    }

    static void StyleDropdown(TMPro.TMP_Dropdown dd)
    {
        var img = dd.GetComponent<Image>();
        if (img) img.color = new Color(0.06f, 0.1f, 0.18f);
        if (dd.captionText) { dd.captionText.color = Color.white; dd.captionText.fontSize = 16; }
    }

    static void StyleButton(Button btn, string text, Color accentColor)
    {
        var img = btn.GetComponent<Image>();
        if (img) img.color = new Color(0.04f, 0.1f, 0.2f);
        var bc = btn.colors;
        bc.normalColor = new Color(0.04f, 0.1f, 0.2f);
        bc.highlightedColor = new Color(0.08f, 0.22f, 0.42f);
        bc.pressedColor = new Color(0.02f, 0.06f, 0.14f);
        btn.colors = bc; btn.targetGraphic = img;
        var lbl = btn.GetComponentInChildren<TextMeshProUGUI>();
        if (lbl) { lbl.text = text; lbl.color = accentColor; lbl.fontStyle = FontStyles.Bold; lbl.fontSize = 16; lbl.alignment = TextAlignmentOptions.Center; }
    }
}
