using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomEventManager : MonoBehaviour
{
    [Header("Timing")]
    public float minInterval = 25f;
    public float maxInterval = 50f;

    [Header("Solar Storm")]
    public float stormPushForce = 8f;
    public float stormDuration = 3f;

    [Header("Rogue Asteroid")]
    public float asteroidSpeed = 55f;
    public float asteroidProximityRadius = 12f;
    public float asteroidPushForce = 4f;

    [Header("Meteor Shower")]
    public int meteorCount = 20;
    public float meteorSpeed = 130f;
    public float meteorProximityRadius = 10f;
    public float meteorPushForce = 3f;
    public float showerDuration = 10f;

    ProbeController probe;
    bool eventRunning;

    void Start()
    {
        probe = FindAnyObjectByType<ProbeController>();
        StartCoroutine(EventLoop());
    }

    IEnumerator EventLoop()
    {
        yield return new WaitForSeconds(8f);

        while (true)
        {
            if (!eventRunning)
            {
                int roll = Random.Range(0, 3);
                if (roll == 0) StartCoroutine(SolarStorm());
                else if (roll == 1) StartCoroutine(RogueAsteroid());
                else StartCoroutine(MeteorShower());
            }
            yield return new WaitForSeconds(Random.Range(minInterval, maxInterval));
        }
    }

    // ── Solar Storm ──────────────────────────────────────────────

    IEnumerator SolarStorm()
    {
        eventRunning = true;

        HUDController.Instance?.ShowNotification(
            "[SOLAR STORM] Coronal mass ejection detected!",
            new Color(1f, 0.55f, 0.1f));
        HUDController.Instance?.FlashScreen(new Color(1f, 0.45f, 0.05f));
        MissionLog.Instance?.AddEntry("Coronal mass ejection detected. Plasma wave incoming.", new Color(1f, 0.55f, 0.1f));

        // Visual effects: CME wave + solar particles
        StartCoroutine(CMEWave());
        StartCoroutine(SolarParticles());
        StartCoroutine(SunPulse());

        // Push probe
        if (probe != null)
        {
            Vector3 pushDir = probe.transform.position.normalized + Random.insideUnitSphere * 0.3f;
            pushDir.Normalize();
            float elapsed = 0f;
            while (elapsed < stormDuration)
            {
                if (probe != null)
                    probe.transform.position += pushDir * stormPushForce * Time.deltaTime;
                elapsed += Time.deltaTime;

                if (elapsed > stormDuration * 0.5f && elapsed - Time.deltaTime <= stormDuration * 0.5f)
                    HUDController.Instance?.FlashScreen(new Color(1f, 0.6f, 0.1f));

                yield return null;
            }
        }

        yield return new WaitForSeconds(1f);
        eventRunning = false;
    }

    // ── Rogue Asteroid ───────────────────────────────────────────

    IEnumerator RogueAsteroid()
    {
        eventRunning = true;

        Vector3 spawnDir = Random.insideUnitSphere.normalized;
        spawnDir.y *= 0.3f;
        Vector3 spawnPos = spawnDir * 250f;
        Vector3 travelDir = (-spawnDir + Random.insideUnitSphere * 0.15f).normalized;

        var asteroid = BuildRock(spawnPos, Random.Range(1.8f, 3.2f), name: "RogueAsteroid");

        HUDController.Instance?.ShowNotification(
            "[ASTEROID] Rogue asteroid detected — Brace for impact!",
            new Color(0.9f, 0.85f, 0.3f));
        MissionLog.Instance?.AddEntry("Rogue asteroid detected on intercept trajectory.", new Color(0.9f, 0.85f, 0.3f));

        bool hitProbe = false;
        float t = 0f;

        while (t < 12f)
        {
            if (asteroid == null) break;
            asteroid.transform.position += travelDir * asteroidSpeed * Time.deltaTime;
            asteroid.transform.Rotate(Vector3.one, 45f * Time.deltaTime);

            if (!hitProbe && probe != null)
            {
                float dist = Vector3.Distance(asteroid.transform.position, probe.transform.position);
                if (dist < asteroidProximityRadius)
                {
                    hitProbe = true;
                    Vector3 pushDir = (probe.transform.position - asteroid.transform.position).normalized;
                    StartCoroutine(PushProbe(pushDir, asteroidPushForce));
                    HUDController.Instance?.ShowNotification(
                        "[ASTEROID] Shockwave — Probe knocked off course!",
                        new Color(1f, 0.4f, 0.1f));
                    HUDController.Instance?.FlashScreen(new Color(0.8f, 0.7f, 0.1f));
                }
            }

            t += Time.deltaTime;
            yield return null;
        }

        if (asteroid != null) Destroy(asteroid);
        yield return new WaitForSeconds(1f);
        eventRunning = false;
    }

    // ── Meteor Shower ────────────────────────────────────────────

    IEnumerator MeteorShower()
    {
        eventRunning = true;

        HUDController.Instance?.ShowNotification(
            "[METEORS] Meteor shower — Evasive maneuvers!",
            new Color(0.6f, 0.85f, 1f));
        HUDController.Instance?.FlashScreen(new Color(0.4f, 0.7f, 1f));
        MissionLog.Instance?.AddEntry($"Meteor shower incoming — {meteorCount} objects detected.", new Color(0.6f, 0.85f, 1f));

        // All meteors come from the same general direction with slight spread
        Vector3 showerDir = Random.insideUnitSphere.normalized;
        showerDir.y *= 0.2f;
        showerDir.Normalize();

        var meteors = new List<GameObject>();
        bool probeHit = false;

        // Spawn meteors staggered over time
        StartCoroutine(SpawnMeteors(showerDir, meteors));

        float elapsed = 0f;
        while (elapsed < showerDuration)
        {
            // Check each meteor for proximity to probe
            if (!probeHit && probe != null)
            {
                foreach (var m in meteors)
                {
                    if (m == null) continue;
                    float dist = Vector3.Distance(m.transform.position, probe.transform.position);
                    if (dist < meteorProximityRadius)
                    {
                        probeHit = true;
                        Vector3 pushDir = (probe.transform.position - m.transform.position).normalized;
                        StartCoroutine(PushProbe(pushDir, meteorPushForce));
                        HUDController.Instance?.ShowNotification(
                            "[METEORS] Impact — Probe destabilized!",
                            new Color(0.5f, 0.8f, 1f));
                        HUDController.Instance?.FlashScreen(new Color(0.3f, 0.6f, 1f));
                        break;
                    }
                }
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Clean up surviving meteors
        foreach (var m in meteors)
            if (m != null) Destroy(m);

        yield return new WaitForSeconds(1f);
        eventRunning = false;
    }

    IEnumerator SpawnMeteors(Vector3 direction, List<GameObject> meteors)
    {
        for (int i = 0; i < meteorCount; i++)
        {
            // Spread spawn points perpendicular to travel direction
            Vector3 right = Vector3.Cross(direction, Vector3.up).normalized;
            Vector3 up = Vector3.Cross(right, direction).normalized;
            Vector3 spread = right * Random.Range(-60f, 60f) + up * Random.Range(-30f, 30f);
            Vector3 spawnPos = -direction * 220f + spread;

            var meteor = BuildMeteor(spawnPos, direction);
            var col = meteor.GetComponent<Collider>();
            if (col != null) col.isTrigger = true;
            meteors.Add(meteor);

            yield return new WaitForSeconds(Random.Range(0.1f, 0.5f));
        }
    }

    GameObject BuildMeteor(Vector3 position, Vector3 direction)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "Meteor";
        go.transform.position = position;
        float size = Random.Range(0.3f, 0.9f);
        go.transform.localScale = new Vector3(
            size * Random.Range(0.8f, 1.3f),
            size * Random.Range(0.7f, 1.1f),
            size * Random.Range(0.9f, 1.4f));
        var col = go.GetComponent<Collider>();
        if (col != null) col.isTrigger = true;

        // Rocky material
        var r = go.GetComponent<Renderer>();
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? r.sharedMaterial.shader);
        Color c = new Color(Random.Range(0.55f, 0.75f), Random.Range(0.45f, 0.60f), Random.Range(0.35f, 0.50f));
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.05f);
        r.material = mat;

        // Glowing trail
        var trail = go.AddComponent<TrailRenderer>();
        trail.time = 0.4f;
        trail.startWidth = size * 0.6f;
        trail.endWidth = 0f;
        trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        var trailMat = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color"));
        trail.material = trailMat;
        trail.startColor = new Color(0.9f, 0.85f, 0.6f, 1f);
        trail.endColor = new Color(0.6f, 0.5f, 0.3f, 0f);

        // Move independently
        go.AddComponent<MeteorMover>().Init(direction, meteorSpeed);
        return go;
    }

    // ── Solar Storm Visuals ──────────────────────────────────────

    IEnumerator CMEWave()
    {
        // Expanding transparent plasma sphere from the sun
        var wave = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        wave.name = "CMEWave";
        wave.transform.position = Vector3.zero;
        Destroy(wave.GetComponent<Collider>());

        var r = wave.GetComponent<Renderer>();
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? r.sharedMaterial.shader);
        if (mat.HasProperty("_BaseColor"))    mat.SetColor("_BaseColor", new Color(1f, 0.5f, 0.1f, 0.15f));
        if (mat.HasProperty("_Surface"))      mat.SetFloat("_Surface", 1f);
        if (mat.HasProperty("_Blend"))        mat.SetFloat("_Blend", 0f);
        if (mat.HasProperty("_SrcBlend"))     mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        if (mat.HasProperty("_DstBlend"))     mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        if (mat.HasProperty("_ZWrite"))       mat.SetFloat("_ZWrite", 0f);
        mat.EnableKeyword("_EMISSION");
        if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", new Color(1f, 0.4f, 0.05f) * 1.5f);
        mat.renderQueue = 3000;
        r.material = mat;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        float t = 0f;
        float maxRadius = 350f;
        float duration = 4f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float progress = t / duration;
            float radius = Mathf.Lerp(8f, maxRadius, progress);
            wave.transform.localScale = Vector3.one * radius;

            // Fade out as it expands
            float alpha = Mathf.Lerp(0.18f, 0f, progress);
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", new Color(1f, 0.5f, 0.1f, alpha));
            if (mat.HasProperty("_EmissionColor"))
                mat.SetColor("_EmissionColor", new Color(1f, 0.4f, 0.05f) * (1.5f * (1f - progress)));

            yield return null;
        }

        Destroy(wave);
    }

    IEnumerator SolarParticles()
    {
        // Stream of plasma particles shooting from sun toward probe direction
        Vector3 baseDir = probe != null
            ? (probe.transform.position).normalized
            : Random.insideUnitSphere.normalized;

        int count = 35;
        for (int i = 0; i < count; i++)
        {
            // Slight spread around the main direction
            Vector3 dir = (baseDir + Random.insideUnitSphere * 0.4f).normalized;
            SpawnSolarParticle(dir);
            yield return new WaitForSeconds(Random.Range(0.04f, 0.14f));
        }
    }

    void SpawnSolarParticle(Vector3 direction)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "SolarParticle";
        go.transform.position = Vector3.zero + Random.insideUnitSphere * 6f;
        float size = Random.Range(0.4f, 1.2f);
        go.transform.localScale = Vector3.one * size;
        Destroy(go.GetComponent<Collider>());

        var r = go.GetComponent<Renderer>();
        var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? r.sharedMaterial.shader);
        Color c = Color.Lerp(new Color(1f, 0.8f, 0.1f), new Color(1f, 0.3f, 0f), Random.value);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
        r.material = mat;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        // Trail
        var trail = go.AddComponent<TrailRenderer>();
        trail.time = 0.5f;
        trail.startWidth = size * 0.8f;
        trail.endWidth = 0f;
        trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        var trailMat = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color"));
        trail.material = trailMat;
        trail.startColor = new Color(1f, 0.75f, 0.2f, 1f);
        trail.endColor   = new Color(1f, 0.3f, 0f, 0f);

        float speed = Random.Range(80f, 160f);
        go.AddComponent<MeteorMover>().Init(direction, speed);
        Destroy(go, 5f);
    }

    IEnumerator SunPulse()
    {
        // Briefly boost sun light intensity
        var sun = GameObject.Find("Sun");
        if (sun == null) yield break;

        var light = sun.GetComponent<Light>();
        if (light == null) yield break;

        float originalIntensity = light.intensity;
        float originalRange = light.range;

        float t = 0f, duration = 1.5f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float pulse = 1f + Mathf.Sin(t * Mathf.PI / duration) * 2.5f;
            light.intensity = originalIntensity * pulse;
            light.range = originalRange * (1f + Mathf.Sin(t * Mathf.PI / duration) * 0.3f);
            yield return null;
        }

        light.intensity = originalIntensity;
        light.range = originalRange;
    }

    // ── Shared helpers ───────────────────────────────────────────

    IEnumerator PushProbe(Vector3 direction, float force)
    {
        float elapsed = 0f, duration = 1.2f;
        while (elapsed < duration && probe != null)
        {
            float strength = Mathf.Lerp(force, 0f, elapsed / duration);
            probe.transform.position += direction * strength * Time.deltaTime;
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    GameObject BuildRock(Vector3 position, float size, string name = "Rock")
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = name;
        go.transform.position = position;
        go.transform.localScale = new Vector3(
            size * Random.Range(0.8f, 1.0f),
            size * Random.Range(0.6f, 0.9f),
            size * Random.Range(0.8f, 1.0f));
        var col = go.GetComponent<Collider>();
        if (col != null) col.isTrigger = true;

        var r = go.GetComponent<Renderer>();
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? r.sharedMaterial.shader);
        Color rockColor = new Color(Random.Range(0.28f, 0.38f), Random.Range(0.24f, 0.32f), Random.Range(0.20f, 0.28f));
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", rockColor);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.05f);
        r.material = mat;
        return go;
    }
}
