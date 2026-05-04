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
            sunLight.intensity = 55f;
            sunLight.range = 8000f;
            sunLight.shadows = LightShadows.None;
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
        RenderSettings.ambientLight = new Color(0.22f, 0.22f, 0.30f);

        // Subtle fill light from camera direction so planet textures are always visible
        var fillGO = new GameObject("FillLight");
        fillGO.transform.SetParent(transform, false);
        var fill = fillGO.AddComponent<Light>();
        fill.type = LightType.Directional;
        fill.color = new Color(0.75f, 0.80f, 1.0f);
        fill.intensity = 0.75f;
        fill.shadows = LightShadows.None;
        fillGO.transform.rotation = Quaternion.Euler(40f, 18f, 0f);

        // Second fill from opposite side for wrap-around lighting
        var fill2GO = new GameObject("FillLight2");
        fill2GO.transform.SetParent(transform, false);
        var fill2 = fill2GO.AddComponent<Light>();
        fill2.type = LightType.Directional;
        fill2.color = new Color(0.40f, 0.45f, 0.65f);
        fill2.intensity = 0.25f;
        fill2.shadows = LightShadows.None;
        fill2GO.transform.rotation = Quaternion.Euler(145f, 195f, 0f);

        var bloomProfile = ScriptableObject.CreateInstance<UnityEngine.Rendering.VolumeProfile>();
        bloomProfile.name = "RuntimePostProfile";

        var tone = bloomProfile.Add<UnityEngine.Rendering.Universal.Tonemapping>(true);
        tone.active = true;
        tone.mode.Override(UnityEngine.Rendering.Universal.TonemappingMode.ACES);

        var colorAdj = bloomProfile.Add<UnityEngine.Rendering.Universal.ColorAdjustments>(true);
        colorAdj.active = true;
        colorAdj.postExposure.Override(0.55f);
        colorAdj.contrast.Override(14f);
        colorAdj.saturation.Override(28f);
        colorAdj.colorFilter.Override(new Color(0.96f, 0.97f, 1.0f));

        var vignette = bloomProfile.Add<UnityEngine.Rendering.Universal.Vignette>(true);
        vignette.active = true;
        vignette.intensity.Override(0.30f);
        vignette.smoothness.Override(0.5f);

        var bloom = bloomProfile.Add<UnityEngine.Rendering.Universal.Bloom>(true);
        bloom.active = true;
        bloom.threshold.Override(0.95f);
        bloom.intensity.Override(1.2f);
        bloom.scatter.Override(0.45f);
        bloom.tint.Override(new Color(1f, 0.95f, 0.88f));
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
            probe.transform.localScale = Vector3.one * 0.6f;
            TintRenderer(probe.gameObject, new Color(0.15f, 0.85f, 1f), emissive: true);
            if (probe.GetComponent<ProbeTrail>() == null) probe.gameObject.AddComponent<ProbeTrail>();
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
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        var litShader   = Shader.Find("Universal Render Pipeline/Lit");
        var unlitShader = Shader.Find("Universal Render Pipeline/Unlit");
        var shader = emissive && unlitShader != null ? unlitShader : litShader;
        var m = new Material(shader != null ? shader : r.sharedMaterial.shader);
        r.material = m;
        if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", tex);
        if (m.HasProperty("_MainTex")) m.SetTexture("_MainTex", tex);
        m.mainTexture = tex;
        Color baseColor = emissive ? new Color(2.2f, 2.0f, 1.6f) : Color.white;
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", baseColor);
        if (!emissive)
        {
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
        Color baseColor = emissive ? new Color(2.8f, 2.6f, 2.0f, 1f) : Color.white;
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
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", new Color(1f, 0.88f, 0.58f, 1f));
        if (mat.HasProperty("_Color")) mat.color = new Color(1f, 0.88f, 0.58f, 1f);
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
        if (mat2.HasProperty("_BaseColor")) mat2.SetColor("_BaseColor", new Color(1f, 0.75f, 0.35f, 0.4f));
        if (mat2.HasProperty("_Color")) mat2.color = new Color(1f, 0.75f, 0.35f, 0.4f);
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

        // ── Time Scale UI ──
        CreateTimeScaleUI();

        // ── PlanetSelectionUI canvas ──
        var ui = Object.FindAnyObjectByType<PlanetSelectionUI>();
        if (ui == null) return;

        var canvas = ui.GetComponentInParent<Canvas>();
        if (canvas == null) return;

        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler == null) scaler = canvas.gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        var canvasRT = canvas.GetComponent<RectTransform>();

        for (int i = canvasRT.childCount - 1; i >= 0; i--)
        {
            var child = canvasRT.GetChild(i);
            if (child.name == "Panel")
            {
                child.gameObject.SetActive(false);
            }
        }

        if (ui.infoText != null)
        {
            var bg = new GameObject("InfoBG", typeof(RectTransform), typeof(Image));
            bg.transform.SetParent(canvasRT, false);
            AnchorRect(bg.GetComponent<RectTransform>(), new Vector2(20, -90), new Vector2(320, 200));
            bg.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);

            ReparentAndAnchor(ui.infoText.transform, canvasRT, new Vector2(20, -90), new Vector2(320, 200));
            ui.infoText.textWrappingMode = TextWrappingModes.Normal;
            ui.infoText.fontSize = 22;
            ui.infoText.color = Color.white;
            ui.infoText.alignment = TextAlignmentOptions.TopLeft;
            ui.infoText.margin = new Vector4(10, 10, 10, 10);
        }

        if (ui.dropdown != null)
            ReparentAndAnchor(ui.dropdown.transform, canvasRT, new Vector2(20, -20), new Vector2(320, 60));

        if (ui.sendProbeButton != null)
        {
            ReparentAndAnchor(ui.sendProbeButton.transform, canvasRT, new Vector2(20, -310), new Vector2(320, 60));
            var label = ui.sendProbeButton.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null) label.text = "Send Probe Here";
            var legacy = ui.sendProbeButton.GetComponentInChildren<Text>();
            if (legacy != null) legacy.text = "Send Probe Here";
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

        // ── Panel: bottom-right, 380×130 ──
        var panel = MakeRect("TimeScalePanel", root);
        panel.anchorMin = panel.anchorMax = new Vector2(1, 0);
        panel.pivot     = new Vector2(1, 0);
        panel.anchoredPosition = new Vector2(-24, 24);
        panel.sizeDelta = new Vector2(380, 130);
        var panelImg = panel.gameObject.AddComponent<Image>();
        panelImg.color = new Color(0.04f, 0.06f, 0.12f, 0.92f);

        // Cyan accent bar on top
        var accent = MakeRect("Accent", panel);
        accent.anchorMin = Vector2.zero; accent.anchorMax = new Vector2(1, 1);
        accent.offsetMin = Vector2.zero; accent.offsetMax = Vector2.zero;
        accent.anchorMin = new Vector2(0, 1); accent.anchorMax = new Vector2(1, 1);
        accent.pivot = new Vector2(0.5f, 1f);
        accent.sizeDelta = new Vector2(0, 3);
        accent.anchoredPosition = Vector2.zero;
        accent.gameObject.AddComponent<Image>().color = new Color(0.0f, 0.85f, 1f, 0.9f);

        // ── Title row ──
        var title = MakeRect("Title", panel);
        title.anchorMin = new Vector2(0, 1); title.anchorMax = new Vector2(1, 1);
        title.pivot = new Vector2(0, 1);
        title.anchoredPosition = new Vector2(14, -8);
        title.sizeDelta = new Vector2(-14, 22);
        var titleTMP = title.gameObject.AddComponent<TextMeshProUGUI>();
        titleTMP.text = "TIME CONTROL";
        titleTMP.fontSize = 13;
        titleTMP.color = new Color(0.0f, 0.85f, 1f);
        titleTMP.fontStyle = FontStyles.Bold;
        titleTMP.characterSpacing = 3f;

        // ── Speed label (right-aligned, same row as title) ──
        var speedLabel = MakeRect("SpeedLabel", panel);
        speedLabel.anchorMin = new Vector2(0, 1); speedLabel.anchorMax = new Vector2(1, 1);
        speedLabel.pivot = new Vector2(1, 1);
        speedLabel.anchoredPosition = new Vector2(-14, -8);
        speedLabel.sizeDelta = new Vector2(-14, 22);
        var speedTMP = speedLabel.gameObject.AddComponent<TextMeshProUGUI>();
        speedTMP.text = "1×";
        speedTMP.fontSize = 15;
        speedTMP.color = Color.white;
        speedTMP.fontStyle = FontStyles.Bold;
        speedTMP.alignment = TextAlignmentOptions.Right;

        // ── Slider row ──
        var sliderGO = new GameObject("TimeSlider");
        sliderGO.transform.SetParent(panel, false);
        var sliderRT = sliderGO.AddComponent<RectTransform>();
        sliderRT.anchorMin = new Vector2(0, 1); sliderRT.anchorMax = new Vector2(1, 1);
        sliderRT.pivot = new Vector2(0.5f, 1f);
        sliderRT.anchoredPosition = new Vector2(0, -36);
        sliderRT.sizeDelta = new Vector2(-28, 24);
        var slider = sliderGO.AddComponent<Slider>();
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 1f;
        slider.maxValue = 1000f;
        slider.value = 1f;
        slider.wholeNumbers = true;

        // Track background
        var track = MakeRect("Background", sliderRT);
        track.anchorMin = new Vector2(0, 0.4f); track.anchorMax = new Vector2(1, 0.6f);
        track.offsetMin = track.offsetMax = Vector2.zero;
        var trackImg = track.gameObject.AddComponent<Image>();
        trackImg.color = new Color(0.12f, 0.16f, 0.24f);

        // Fill area
        var fillArea = MakeRect("Fill Area", sliderRT);
        fillArea.anchorMin = new Vector2(0, 0.4f); fillArea.anchorMax = new Vector2(1, 0.6f);
        fillArea.offsetMin = new Vector2(0, 0); fillArea.offsetMax = new Vector2(0, 0);

        // Fill
        var fill = MakeRect("Fill", fillArea);
        fill.anchorMin = Vector2.zero; fill.anchorMax = new Vector2(0, 1);
        fill.offsetMin = fill.offsetMax = Vector2.zero;
        var fillImg = fill.gameObject.AddComponent<Image>();
        fillImg.color = new Color(0.0f, 0.75f, 1f);

        // Handle slide area
        var handleArea = MakeRect("Handle Slide Area", sliderRT);
        handleArea.anchorMin = Vector2.zero; handleArea.anchorMax = Vector2.one;
        handleArea.offsetMin = Vector2.zero; handleArea.offsetMax = Vector2.zero;

        // Handle
        var handle = MakeRect("Handle", handleArea);
        handle.anchorMin = new Vector2(0, 0); handle.anchorMax = new Vector2(0, 1);
        handle.pivot = new Vector2(0.5f, 0.5f);
        handle.sizeDelta = new Vector2(18, 4);
        var handleImg = handle.gameObject.AddComponent<Image>();
        handleImg.color = Color.white;

        slider.fillRect      = fill;
        slider.handleRect    = handle;
        slider.targetGraphic = handleImg;

        // Tick labels: 1x and 1000x
        var minLbl = MakeRect("MinLbl", sliderRT);
        minLbl.anchorMin = new Vector2(0, 0); minLbl.anchorMax = new Vector2(0, 0);
        minLbl.pivot = new Vector2(0, 1); minLbl.anchoredPosition = new Vector2(0, -2); minLbl.sizeDelta = new Vector2(40, 16);
        var minT = minLbl.gameObject.AddComponent<TextMeshProUGUI>();
        minT.text = "1×"; minT.fontSize = 11; minT.color = new Color(0.6f, 0.7f, 0.8f);

        var maxLbl = MakeRect("MaxLbl", sliderRT);
        maxLbl.anchorMin = new Vector2(1, 0); maxLbl.anchorMax = new Vector2(1, 0);
        maxLbl.pivot = new Vector2(1, 1); maxLbl.anchoredPosition = new Vector2(0, -2); maxLbl.sizeDelta = new Vector2(50, 16);
        var maxT = maxLbl.gameObject.AddComponent<TextMeshProUGUI>();
        maxT.text = "1000×"; maxT.fontSize = 11; maxT.color = new Color(0.6f, 0.7f, 0.8f);
        maxT.alignment = TextAlignmentOptions.Right;

        // ── Pause button ──
        var btnGO = new GameObject("PauseButton", typeof(RectTransform), typeof(Image), typeof(Button));
        btnGO.transform.SetParent(panel, false);
        var btnRT = btnGO.GetComponent<RectTransform>();
        btnRT.anchorMin = new Vector2(1, 0); btnRT.anchorMax = new Vector2(1, 0);
        btnRT.pivot = new Vector2(1, 0);
        btnRT.anchoredPosition = new Vector2(-14, 12);
        btnRT.sizeDelta = new Vector2(110, 36);
        var btnImg = btnGO.GetComponent<Image>();
        btnImg.color = new Color(0.06f, 0.18f, 0.32f);
        var btn = btnGO.GetComponent<Button>();
        var btnColors = btn.colors;
        btnColors.normalColor      = new Color(0.06f, 0.18f, 0.32f);
        btnColors.highlightedColor = new Color(0.10f, 0.30f, 0.55f);
        btnColors.pressedColor     = new Color(0.02f, 0.10f, 0.22f);
        btn.colors = btnColors;
        btn.targetGraphic = btnImg;

        var btnTextGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        btnTextGO.transform.SetParent(btnGO.transform, false);
        var btnTextRT = btnTextGO.GetComponent<RectTransform>();
        btnTextRT.anchorMin = Vector2.zero; btnTextRT.anchorMax = Vector2.one;
        btnTextRT.offsetMin = btnTextRT.offsetMax = Vector2.zero;
        var btnTMP = btnTextGO.GetComponent<TextMeshProUGUI>();
        btnTMP.text = "⏸  PAUSE";
        btnTMP.fontSize = 14;
        btnTMP.color = new Color(0.0f, 0.85f, 1f);
        btnTMP.fontStyle = FontStyles.Bold;
        btnTMP.alignment = TextAlignmentOptions.Center;

        // ── Wire TimeScaleController ──
        var ctrl = canvasGO.AddComponent<TimeScaleController>();
        ctrl.timeSlider      = slider;
        ctrl.pauseButton     = btn;
        ctrl.speedLabel      = speedTMP;
        ctrl.pauseButtonText = btnTMP;
    }

    static RectTransform MakeRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
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
}
