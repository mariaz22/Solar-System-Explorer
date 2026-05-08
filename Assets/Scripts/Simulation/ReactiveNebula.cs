using UnityEngine;
using System.Collections.Generic;

[ExecuteInEditMode]
public class ReactiveNebula : MonoBehaviour
{
    public enum NebulaState { Calm, Warning, Danger }

    [Header("Visual Configuration")]
    public float radius = 50f;
    public NebulaState currentState = NebulaState.Calm;
    
    [Header("Density & Navigation")]
    [Range(0, 1)] public float overallDensity = 0.22f;
    public float noiseScale = 0.03f;       // lower = fewer but larger blobs
    public float tunnelThreshold = 0.65f;  // most space is open; only peaks are danger zones

    [Header("Colors")]
    public Color safeColor    = new Color(0.82f, 0.78f, 0.94f, 0.05f); // barely-there lavender
    public Color calmColor    = new Color(0.78f, 0.80f, 0.95f, 0.10f); // very light blue-purple
    public Color warningColor = new Color(0.90f, 0.65f, 0.88f, 0.25f); // soft pink-purple
    public Color dangerColor  = new Color(0.95f, 0.52f, 0.82f, 0.48f); // rosy pink-purple push zones

    [Header("Components")]
    private ParticleSystem outerLayer;
    private ParticleSystem innerLayer;
    private ParticleSystem coreLayer;
    private ParticleSystem sparkLayer;
    private ParticleSystem tendrilLayer;
    private ParticleSystem shockwaveLayer;
    private ParticleSystem energyWaveLayer;
    private Light internalLight;

    private ProbeController playerProbe;
    private Vector3 lastProbePos;
    private float probeSpeed;
    private float agitationLevel = 0f;
    private float visualPulse = 0f;
    private Vector3 noiseOffset;
    
    private static Texture2D sharedNebulaTex;

    void Awake()
    {
        RandomizeTunnels();
        InitializeLayers();
    }

    [ContextMenu("Randomize Tunnels")]
    public void RandomizeTunnels()
    {
        noiseOffset = new Vector3(Random.value * 1000, Random.value * 1000, Random.value * 1000);
        if (!Application.isPlaying) InitializeLayers();
    }

    void OnEnable()
    {
        InitializeLayers();
    }

    void Start()
    {
        if (Application.isPlaying)
        {
            playerProbe = Object.FindAnyObjectByType<ProbeController>();
            if (playerProbe != null) lastProbePos = playerProbe.transform.position;
        }
    }

    public void InitializeLayers()
    {
        var children = new List<GameObject>();
        foreach (Transform child in transform) children.Add(child.gameObject);
        foreach (var child in children) 
        {
            if (child.name.Contains("Layer") || child.name.Contains("Emitter") || child.name.Contains("Shockwave") || child.name.Contains("Light"))
            {
                if (Application.isPlaying) Destroy(child); else DestroyImmediate(child);
            }
        }

        if (sharedNebulaTex == null) sharedNebulaTex = GenerateNebulaTexture();

        outerLayer      = CreateLayer("OuterLayer",     80, 28f, 55f, 0.1f, 0.4f, false);
        innerLayer      = CreateLayer("InnerLayer",    120, 14f, 28f, 0.2f, 1.0f, false);
        coreLayer       = CreateLayer("CoreLayer",      60,  8f, 16f, 0.8f, 2.5f, false);
        sparkLayer      = CreateLayer("SparkLayer",     30,  1.0f, 3.5f, 0f, 0.2f, false);
        tendrilLayer    = CreateLayer("TendrilLayer",   40,  0.7f, 2.2f, 8f, 20f, true);
        shockwaveLayer  = CreateLayer("ShockwaveLayer", 20,  3f,  8f, 12f, 28f, true);
        energyWaveLayer = CreateLayer("EnergyWaveLayer",15, 16f, 32f,  0f, 0.4f, false);
        
        ConfigureSparks();
        ConfigureTendrils();
        ConfigureShockwave();
        ConfigureEnergyWaves();
        ConfigureLight();
        SetupTrigger();
        
        UpdateVisuals();

        if (!Application.isPlaying)
        {
            if (outerLayer) outerLayer.Play();
            if (innerLayer) innerLayer.Play();
            if (coreLayer) coreLayer.Play();
            if (sparkLayer) sparkLayer.Play();
            if (tendrilLayer) tendrilLayer.Play();
            if (shockwaveLayer) shockwaveLayer.Play();
            if (energyWaveLayer) energyWaveLayer.Play();
        }
    }

    void SetupTrigger()
    {
        var col = GetComponent<SphereCollider>();
        if (col == null) col = gameObject.AddComponent<SphereCollider>();
        col.isTrigger = true;
        col.radius = radius;
    }

    public float GetDensityAt(Vector3 worldPos)
    {
        float dist = Vector3.Distance(transform.position, worldPos);
        if (dist > radius) return 0f;

        float baseDensity = 1f - (dist / radius);
        Vector3 samplePos = (worldPos - transform.position) * noiseScale + noiseOffset;
        float n = Perlin3D(samplePos.x, samplePos.y, samplePos.z);
        
        if (n < tunnelThreshold) n *= 0.2f; 

        return Mathf.Clamp01(baseDensity * n * overallDensity * 2f);
    }

    float Perlin3D(float x, float y, float z)
    {
        float xy = Mathf.PerlinNoise(x, y);
        float yz = Mathf.PerlinNoise(y, z);
        float zx = Mathf.PerlinNoise(z, x);
        return (xy + yz + zx) / 3f;
    }

    void ConfigureSparks()
    {
        var main = sparkLayer.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.04f, 0.12f);
        var em = sparkLayer.emission;
        em.rateOverTime = 12f;
        var renderer = sparkLayer.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One); 
    }

    void ConfigureTendrils()
    {
        var main = tendrilLayer.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.2f, 2.8f);
        var renderer = tendrilLayer.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Stretch;
        renderer.velocityScale = 0.9f;
        renderer.lengthScale = 4.5f;
    }

    void ConfigureShockwave()
    {
        var main = shockwaveLayer.main;
        main.startLifetime = 0.45f;
        main.loop = false;
        var em = shockwaveLayer.emission;
        em.enabled = false;
        var renderer = shockwaveLayer.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Stretch;
        renderer.velocityScale = 1.5f;
    }

    void ConfigureEnergyWaves()
    {
        var main = energyWaveLayer.main;
        main.startLifetime = 2.0f;
        var em = energyWaveLayer.emission;
        em.rateOverTime = 2f;
        var col = energyWaveLayer.colorOverLifetime;
        col.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.3f, 0.5f), new GradientAlphaKey(0f, 1f) }
        );
        col.color = g;
    }

    void ConfigureLight()
    {
        GameObject lightGo = new GameObject("NebulaLight");
        lightGo.transform.SetParent(transform, false);
        internalLight = lightGo.AddComponent<Light>();
        internalLight.type = LightType.Point;
        internalLight.range = radius * 3.0f;
    }

    ParticleSystem CreateLayer(string name, int maxParticles, float minSize, float maxSize, float minSpeed, float maxSpeed, bool isStretched)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(transform, false);
        
        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.maxParticles = maxParticles;
        main.startSize = new ParticleSystem.MinMaxCurve(minSize, maxSize);
        main.startSpeed = new ParticleSystem.MinMaxCurve(minSpeed, maxSpeed);
        main.startLifetime = new ParticleSystem.MinMaxCurve(4f, 8f);
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.loop = true;
        main.playOnAwake = true;

        var emission = ps.emission;
        emission.rateOverTime = maxParticles / 9f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = radius * 0.55f;

        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.5f;
        noise.frequency = 0.18f;
        noise.scrollSpeed = 0.25f;

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = isStretched ? ParticleSystemRenderMode.Stretch : ParticleSystemRenderMode.Billboard;
        renderer.sharedMaterial = CreateNebulaMaterial();
        
        return ps;
    }

    Material CreateNebulaMaterial()
    {
        var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Particles/Standard Unlit");
        Material mat = new Material(shader);
        if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f); 
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", sharedNebulaTex);
        if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", sharedNebulaTex);
        return mat;
    }

    Texture2D GenerateNebulaTexture()
    {
        int res = 128;
        Texture2D tex = new Texture2D(res, res);
        for (int y = 0; y < res; y++)
        {
            for (int x = 0; x < res; x++)
            {
                float dx = (x / (float)res) - 0.5f;
                float dy = (y / (float)res) - 0.5f;
                float d = Mathf.Sqrt(dx * dx + dy * dy) * 2f;
                float a = Mathf.Clamp01(1f - d);
                float n = Mathf.PerlinNoise(x * 0.07f, y * 0.07f) * 0.7f + 0.3f;
                a = Mathf.Pow(a, 2.5f) * n;
                tex.SetPixel(x, y, new Color(1, 1, 1, a));
            }
        }
        tex.Apply();
        return tex;
    }

    void Update()
    {
        if (playerProbe == null)
        {
            if (Application.isPlaying) playerProbe = Object.FindAnyObjectByType<ProbeController>();
            else { UpdateVisuals(); return; } 
        }

        if (Application.isPlaying) CalculateProbeSpeed();
        float distance = playerProbe != null ? Vector3.Distance(transform.position, playerProbe.transform.position) : 999f;
        UpdateAgitation(distance);
        UpdateVisuals();
        if (Application.isPlaying) ApplyEnvironmentalEffects(distance);
    }

    void CalculateProbeSpeed()
    {
        Vector3 curPos = playerProbe.transform.position;
        probeSpeed = (curPos - lastProbePos).magnitude / Mathf.Max(0.001f, Time.deltaTime);
        lastProbePos = curPos;
    }

    void UpdateAgitation(float distance)
    {
        float targetAgitation = 0f;
        if (distance < radius * 2.2f)
        {
            float localDensity = GetDensityAt(playerProbe.transform.position);
            targetAgitation = localDensity;
            targetAgitation += Mathf.Clamp01(probeSpeed / 80f) * 0.2f;
        }

        float lerpSpeed = targetAgitation > agitationLevel ? 2.0f : 0.4f;
        agitationLevel = Mathf.Lerp(agitationLevel, targetAgitation, Time.deltaTime * lerpSpeed);

        if (agitationLevel < 0.3f) currentState = NebulaState.Calm;
        else if (agitationLevel < 0.7f) currentState = NebulaState.Warning;
        else currentState = NebulaState.Danger;

        if (Application.isPlaying && probeSpeed > 40f && agitationLevel > 0.6f)
        {
            var emitParams = new ParticleSystem.EmitParams();
            emitParams.position = playerProbe.transform.position - transform.position;
            shockwaveLayer.Emit(emitParams, 20);
        }
    }

    void UpdateVisuals()
    {
        visualPulse += Time.deltaTime * (1f + agitationLevel * 4f);
        float pulse = Mathf.Sin(visualPulse) * 0.1f + 0.9f;

        // Color driven by LOCAL density so danger zones (lilac) are visible regardless of distance
        float localD = Application.isPlaying && playerProbe != null
            ? GetDensityAt(playerProbe.transform.position) : agitationLevel;
        Color targetColor;
        if (localD < 0.35f)
            targetColor = Color.Lerp(safeColor, calmColor, localD / 0.35f);
        else if (localD < 0.65f)
            targetColor = Color.Lerp(calmColor, warningColor, (localD - 0.35f) / 0.30f);
        else
            targetColor = Color.Lerp(warningColor, dangerColor, (localD - 0.65f) / 0.35f);
        
        targetColor *= pulse;

        UpdateLayer(outerLayer, targetColor, 0.1f + agitationLevel * 0.7f, 0.1f);
        UpdateLayer(innerLayer, targetColor * 1.2f, 0.3f + agitationLevel * 1.5f, 0.2f);
        UpdateLayer(coreLayer, targetColor * 1.4f, 0.6f + agitationLevel * 2.5f, 0.4f);
        UpdateLayer(tendrilLayer, targetColor * 1.7f, 1.0f + agitationLevel * 4.0f, 0.7f);
        UpdateLayer(shockwaveLayer, Color.white * 0.9f, 2f, 1.5f);
        UpdateLayer(energyWaveLayer, targetColor * 0.5f, 0.2f, 0.1f);
        
        if (internalLight != null)
        {
            internalLight.color = targetColor;
            internalLight.intensity = (3f + agitationLevel * 15f) * pulse;
            if (agitationLevel > 0.65f && Random.value < 0.06f) internalLight.intensity *= 3.5f;
        }

        var sparkEm = sparkLayer.emission;
        sparkEm.enabled = agitationLevel > 0.4f;
        if (sparkEm.enabled)
        {
            sparkEm.rateOverTime = agitationLevel * 60f;
            var main = sparkLayer.main;
            main.startColor = Color.Lerp(Color.white, targetColor, 0.6f);
        }
    }

    void UpdateLayer(ParticleSystem ps, Color color, float noiseStrength, float scrollSpeed)
    {
        if (ps == null) return;
        var main = ps.main;
        main.startColor = color;
        var noise = ps.noise;
        noise.strength = noiseStrength;
        noise.frequency = 0.14f + agitationLevel * 0.4f;
        noise.scrollSpeed = scrollSpeed + agitationLevel * 1.2f;
    }

    void ApplyEnvironmentalEffects(float distance)
    {
        if (distance < radius)
        {
            float localDensity = GetDensityAt(playerProbe.transform.position);
            Vector3 centerDir = (playerProbe.transform.position - transform.position).normalized;
            Vector3 swirlDir  = Vector3.Cross(centerDir, Vector3.up).normalized;

            float pushForce, swirlForce;
            if (localDensity > 0.65f)
            {
                // Pink-purple zone: moderate push, noticeable but beatable with WASD
                float t = (localDensity - 0.65f) / 0.35f;
                pushForce  = 4f + t * 8f;   // max 12 units/s
                swirlForce = 5f + t * 5f;   // max 10 units/s
            }
            else
            {
                // Open space inside nebula: barely felt, purely visual
                pushForce  = localDensity * 1.5f;
                swirlForce = localDensity * 2f;
            }
            playerProbe.transform.position += (centerDir * pushForce + swirlDir * swirlForce) * Time.deltaTime;
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.7f, 0.3f, 1f, 0.12f);
        Gizmos.DrawSphere(transform.position, radius);
    }
}