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
    public float sunScale = 12f;
    public float minPlanetScale = 1.2f;
    public float massToScale = 1.6f;

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

    void Awake()
    {
        LayoutSolarSystem();
        ConfigureLighting();
        ConfigureUI();
        gameObject.AddComponent<Starfield>();
    }

    void ConfigureLighting()
    {
        foreach (var l in Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None))
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
            sunLight.intensity = 18f;
            sunLight.range = 2500f;
            sunLight.shadows = LightShadows.None;
        }

        foreach (var c in Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            c.allowHDR = true;
            var urp = c.GetUniversalAdditionalCameraData();
            if (urp != null)
            {
                urp.renderPostProcessing = true;
                urp.antialiasing = AntialiasingMode.FastApproximateAntialiasing;
            }
        }

        var bloomProfile = ScriptableObject.CreateInstance<UnityEngine.Rendering.VolumeProfile>();
        bloomProfile.name = "RuntimeBloomProfile";
        var bloom = bloomProfile.Add<UnityEngine.Rendering.Universal.Bloom>(true);
        bloom.active = true;
        bloom.threshold.Override(1.1f);
        bloom.intensity.Override(0.8f);
        bloom.scatter.Override(0.75f);
        bloom.tint.Override(new Color(1f, 0.95f, 0.82f));
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
            ApplyTexture(sun, ProceduralPlanetTexture.GenerateSun(), emissive: true);
            AddSunGlow(sun);
            var sunSpin = sun.GetComponent<SelfRotation>() ?? sun.AddComponent<SelfRotation>();
            sunSpin.degreesPerSecond = 8f;
        }

        var planets = Object.FindObjectsByType<Planet>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        float angle = 0f;
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

            float r = def.au * auToUnits;
            Vector3 pos = new Vector3(Mathf.Cos(angle) * r, 0f, Mathf.Sin(angle) * r);
            p.transform.position = pos;
            angle += angleStep;

            float s = Mathf.Max(minPlanetScale, Mathf.Log10(def.mass * 10f + 1f) * massToScale);
            p.transform.localScale = Vector3.one * s;

            var maps = ProceduralPlanetTexture.Generate(p.data.planetName, def.color);
            ApplyTexture(p.gameObject, maps, emissive: false);

            var orbit = p.gameObject.GetComponent<OrbitalMotion>() ?? p.gameObject.AddComponent<OrbitalMotion>();
            orbit.center = sunT;
            orbit.angularSpeedDeg = 20f / Mathf.Pow(def.au, 1.5f);

            var spin = p.gameObject.GetComponent<SelfRotation>() ?? p.gameObject.AddComponent<SelfRotation>();
            spin.degreesPerSecond = 30f + Random.Range(-10f, 10f);

            var orbitLineGO = new GameObject($"{p.data.planetName}_Orbit");
            orbitLineGO.transform.SetParent(transform, false);
            var lr = orbitLineGO.AddComponent<LineRenderer>();
            var path = orbitLineGO.AddComponent<OrbitPathRenderer>();
            path.center = sunT;
            lr.positionCount = 0;

            if (p.data.planetName == "Saturn")
                p.gameObject.AddComponent<SaturnRings>();
        }

        var probe = Object.FindAnyObjectByType<ProbeController>();
        if (probe != null)
        {
            Vector3 safe = new Vector3(0f, 0f, -(30.1f * auToUnits + probeStartOffset));
            probe.transform.position = safe;
            probe.transform.localScale = Vector3.one * 0.6f;
            TintRenderer(probe.gameObject, new Color(0.9f, 0.2f, 0.9f), emissive: true);
            if (probe.GetComponent<ProbeTrail>() == null) probe.gameObject.AddComponent<ProbeTrail>();
            if (probe.GetComponent<ProbeVFX>() == null) probe.gameObject.AddComponent<ProbeVFX>();
        }

        var cam = Camera.main;
        if (cam != null)
        {
            cam.transform.position = new Vector3(0f, 120f, -200f);
            cam.transform.rotation = Quaternion.Euler(25f, 0f, 0f);
            cam.farClipPlane = 3000f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            if (cam.GetComponent<FreeFlyCamera>() == null) cam.gameObject.AddComponent<FreeFlyCamera>();
        }
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
        glow.transform.localScale = Vector3.one * 2.2f;

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
