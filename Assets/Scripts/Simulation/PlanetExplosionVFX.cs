using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlanetExplosionVFX : MonoBehaviour
{
    private Vector3 _explosionPos;
    private float _planetRadius;
    private Vector3 _sunPos = Vector3.zero;

    public void PlayExplosion(Vector3 position, float planetRadius)
    {
        _explosionPos = position;
        _planetRadius = planetRadius;

        // Try to find the sun position
        var sun = Object.FindAnyObjectByType<SunEvolutionController>();
        if (sun != null) _sunPos = sun.transform.position;

        StartCoroutine(ExplosionSequence());
    }

    private IEnumerator ExplosionSequence()
    {
        // Phase 1 — Initial Contact (0–0.3s)
        StartCoroutine(Phase1_InitialContact());
        yield return new WaitForSeconds(0.3f);

        // Phase 2 — Fragmentation (0.3–1.2s)
        StartCoroutine(Phase2_Fragmentation());

        // Phase 3 — Shockwave + Fireball (0.3–2s)
        StartCoroutine(Phase3_ShockwaveAndFireball());
        yield return new WaitForSeconds(0.7f);

        // Phase 4 — Aftermath (1–6s)
        StartCoroutine(Phase4_Aftermath());

        yield return new WaitForSeconds(10f);
        Destroy(gameObject);
    }

    private IEnumerator Phase1_InitialContact()
    {
        // Intense white-hot point flash at planet surface facing the sun
        Vector3 toSun = (_sunPos - _explosionPos).normalized;
        Vector3 flashPos = _explosionPos + toSun * _planetRadius;

        GameObject flash = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        flash.name = "InitialFlash";
        flash.transform.position = flashPos;
        flash.transform.localScale = Vector3.zero;
        Destroy(flash.GetComponent<Collider>());

        Material flashMat = CreateMaterial(new Color(1f, 1f, 1f, 1f), true);
        flash.GetComponent<Renderer>().material = flashMat;

        // Glowing planet proxy
        GameObject glowPlanet = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        glowPlanet.name = "GlowPlanet";
        glowPlanet.transform.position = _explosionPos;
        glowPlanet.transform.localScale = Vector3.one * (_planetRadius * 2f);
        Destroy(glowPlanet.GetComponent<Collider>());
        Material glowMat = CreateMaterial(new Color(1f, 0.2f, 0f, 0f), false);
        glowMat.EnableKeyword("_EMISSION");
        glowPlanet.GetComponent<Renderer>().material = glowMat;

        // Screen flash
        HUDController.Instance?.FlashScreen(new Color(1f, 0.5f, 0.1f, 0.6f));

        // Camera shake
        StartCoroutine(CameraShake(0.8f, 0.4f));

        float elapsed = 0f;
        while (elapsed < 0.3f)
        {
            elapsed += Time.deltaTime;
            
            // Flash expansion
            if (elapsed < 0.1f)
            {
                flash.transform.localScale = Vector3.one * Mathf.Lerp(0f, _planetRadius * 2f, elapsed / 0.1f);
            }
            else
            {
                float fade = 1f - (elapsed - 0.1f) / 0.2f;
                flashMat.SetColor("_BaseColor", new Color(1f, 1f, 1f, fade));
            }

            // Planet glow lerp
            float glowT = elapsed / 0.3f;
            Color glowCol = Color.Lerp(new Color(1f, 0.2f, 0f, 0f), new Color(1f, 0.3f, 0.1f, 1f), glowT);
            glowMat.SetColor("_BaseColor", glowCol);
            glowMat.SetColor("_EmissionColor", glowCol * 5f);

            yield return null;
        }
        Destroy(flash);
        Destroy(glowPlanet, 0.9f); // Destroy when fragmentation starts
    }

    private Texture2D _particleTex;

    private Texture2D GetParticleTex()
    {
        if (_particleTex != null) return _particleTex;
        _particleTex = new Texture2D(64, 64);
        for (int y = 0; y < 64; y++)
        {
            for (int x = 0; x < 64; x++)
            {
                float dx = (x / 32f) - 1f;
                float dy = (y / 32f) - 1f;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Clamp01(1f - d);
                a = Mathf.Pow(a, 2f);
                _particleTex.SetPixel(x, y, new Color(1, 1, 1, a));
            }
        }
        _particleTex.Apply();
        return _particleTex;
    }

    private IEnumerator Phase2_Fragmentation()
    {
        int chunkCount = Random.Range(15, 26);
        Vector3 awayFromSun = (_explosionPos - _sunPos).normalized;

        for (int i = 0; i < chunkCount; i++)
        {
            GameObject chunk = GameObject.CreatePrimitive(Random.value > 0.5f ? PrimitiveType.Capsule : PrimitiveType.Sphere);
            chunk.name = "DebrisChunk";
            chunk.transform.position = _explosionPos + Random.insideUnitSphere * (_planetRadius * 0.5f);
            float size = Random.Range(0.15f, 0.45f) * _planetRadius;
            chunk.transform.localScale = Vector3.one * size;
            Destroy(chunk.GetComponent<Collider>());

            Material chunkMat = CreateMaterial(new Color(1f, 0.3f, 0f), false);
            chunkMat.EnableKeyword("_EMISSION");
            chunkMat.SetColor("_EmissionColor", new Color(1f, 0.3f, 0f) * 4f);
            chunk.GetComponent<Renderer>().material = chunkMat;

            // Trail
            TrailRenderer trail = chunk.AddComponent<TrailRenderer>();
            trail.time = 0.4f;
            trail.startWidth = size * 0.6f;
            trail.endWidth = 0f;
            trail.material = CreateMaterial(new Color(1f, 0.6f, 0.2f), true);

            Vector3 randomDir = (Random.insideUnitSphere + awayFromSun * 0.8f).normalized;
            float speed = Random.Range(40f, 120f);
            float life = Random.Range(3f, 5f);

            chunk.AddComponent<ChunkMover>().Init(randomDir, speed, life, chunkMat);
        }

        // Particle burst
        GameObject burstGO = new GameObject("MoltenBurst");
        burstGO.transform.position = _explosionPos;
        ParticleSystem ps = burstGO.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1f, 4f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(20f, 180f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.3f, 3f);
        main.maxParticles = 400;

        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, Random.Range(200, 401)) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = _planetRadius * 0.5f;

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.orange, 0.4f), new GradientColorKey(Color.red, 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.8f, 0.5f), new GradientAlphaKey(0f, 1f) }
        );
        colorOverLifetime.color = grad;

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.material = CreateMaterial(Color.white, true);
        renderer.material.mainTexture = GetParticleTex();

        ps.Play();
        Destroy(burstGO, 5f);
        yield break;
    }

    private IEnumerator Phase3_ShockwaveAndFireball()
    {
        // Shockwave 1
        SpawnShockwave(_planetRadius, _planetRadius * 6f, 1.2f, new Color(1f, 0.7f, 0.2f, 0.9f));
        // Shockwave 2
        SpawnShockwave(_planetRadius, _planetRadius * 10f, 2.5f, new Color(0.8f, 0.1f, 0.05f, 0.3f));

        // Central fireball
        GameObject fireball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        fireball.name = "Fireball";
        fireball.transform.position = _explosionPos;
        Destroy(fireball.GetComponent<Collider>());
        Material fireMat = CreateMaterial(new Color(1f, 0.4f, 0f, 1f), true);
        fireMat.EnableKeyword("_EMISSION");
        fireMat.SetColor("_EmissionColor", new Color(1f, 0.4f, 0f) * 8f);
        fireball.GetComponent<Renderer>().material = fireMat;

        float elapsed = 0f;
        while (elapsed < 1.2f)
        {
            elapsed += Time.deltaTime;
            if (elapsed < 0.4f)
            {
                fireball.transform.localScale = Vector3.one * Mathf.Lerp(0f, _planetRadius * 4f, elapsed / 0.4f);
            }
            else
            {
                float t = (elapsed - 0.4f) / 0.8f;
                fireball.transform.localScale = Vector3.one * Mathf.Lerp(_planetRadius * 4f, 0f, t);
                fireMat.SetColor("_BaseColor", new Color(1f, 0.4f, 0f, 1f - t));
            }
            yield return null;
        }
        Destroy(fireball);
    }

    private void SpawnShockwave(float startR, float endR, float duration, Color color)
    {
        GameObject sw = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sw.name = "Shockwave";
        sw.transform.position = _explosionPos;
        Destroy(sw.GetComponent<Collider>());
        Material mat = CreateMaterial(color, true);
        sw.GetComponent<Renderer>().material = mat;
        StartCoroutine(AnimateShockwave(sw, startR, endR, duration, mat));
    }

    private IEnumerator AnimateShockwave(GameObject sw, float startR, float endR, float duration, Material mat)
    {
        float elapsed = 0f;
        Color baseCol = mat.GetColor("_BaseColor");
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            sw.transform.localScale = Vector3.one * Mathf.Lerp(startR * 2f, endR * 2f, t);
            mat.SetColor("_BaseColor", new Color(baseCol.r, baseCol.g, baseCol.b, baseCol.a * (1f - t)));
            yield return null;
        }
        Destroy(sw);
    }

    private IEnumerator Phase4_Aftermath()
    {
        // Volumetric smoke
        GameObject smokeGO = new GameObject("AftermathSmoke");
        smokeGO.transform.position = _explosionPos;
        ParticleSystem ps = smokeGO.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = new ParticleSystem.MinMaxCurve(4f, 8f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 10f);
        main.startSize = new ParticleSystem.MinMaxCurve(30f, 80f);
        main.maxParticles = 100;
        main.startColor = new Color(0.15f, 0.12f, 0.1f, 0.6f);

        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 80) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = _planetRadius * 2f;

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(new Color(0.2f, 0.15f, 0.1f), 0f), new GradientColorKey(Color.black, 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.6f, 0.2f), new GradientAlphaKey(0f, 1f) }
        );
        colorOverLifetime.color = grad;

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.material = CreateMaterial(Color.white, false); // Standard alpha
        renderer.material.SetInt("_Blend", 0); // Alpha blend
        renderer.material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        renderer.material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        renderer.material.mainTexture = GetParticleTex();

        ps.Play();

        // Embers
        GameObject embersGO = new GameObject("Embers");
        embersGO.transform.position = _explosionPos;
        ParticleSystem eps = embersGO.AddComponent<ParticleSystem>();
        var emain = eps.main;
        emain.simulationSpace = ParticleSystemSimulationSpace.World;
        emain.startLifetime = new ParticleSystem.MinMaxCurve(3f, 6f);
        emain.startSpeed = new ParticleSystem.MinMaxCurve(5f, 15f);
        emain.startSize = new ParticleSystem.MinMaxCurve(1f, 4f);
        emain.maxParticles = 80;
        emain.startColor = new Color(1f, 0.3f, 0.1f, 1f);

        var eemission = eps.emission;
        eemission.rateOverTime = 0;
        eemission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0.1f, 80) });

        var ecolorOverLifetime = eps.colorOverLifetime;
        ecolorOverLifetime.enabled = true;
        ecolorOverLifetime.color = new ParticleSystem.MinMaxGradient(new Color(1f, 0.3f, 0.1f, 1f), new Color(1f, 0.1f, 0f, 0f));

        var esizeOverLifetime = eps.sizeOverLifetime;
        esizeOverLifetime.enabled = true;
        esizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, 0f);

        var erenderer = eps.GetComponent<ParticleSystemRenderer>();
        erenderer.material = CreateMaterial(Color.white, true);
        erenderer.material.mainTexture = GetParticleTex();

        eps.Play();

        // Point Light
        GameObject lightGO = new GameObject("ExplosionLight");
        lightGO.transform.position = _explosionPos;
        Light light = lightGO.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(1f, 0.5f, 0.2f);
        light.intensity = 8f;
        light.range = _planetRadius * 4f;

        StartCoroutine(FadeLight(light, 3f));

        Destroy(smokeGO, 10f);
        Destroy(embersGO, 10f);
        Destroy(lightGO, 10f);
        yield break;
    }

    private IEnumerator FadeLight(Light light, float duration)
    {
        float elapsed = 0f;
        float startInt = light.intensity;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            light.intensity = Mathf.Lerp(startInt, 0f, elapsed / duration);
            yield return null;
        }
    }

    private IEnumerator CameraShake(float intensity, float duration)
    {
        Camera mainCam = Camera.main;
        if (mainCam == null) yield break;
        Transform camTransform = mainCam.transform;
        Vector3 originalPos = camTransform.localPosition;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * intensity;
            float y = Random.Range(-1f, 1f) * intensity;
            camTransform.localPosition = originalPos + new Vector3(x, y, 0);
            elapsed += Time.deltaTime;
            yield return null;
        }
        camTransform.localPosition = originalPos;
    }

    private Material CreateMaterial(Color color, bool additive)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        Material mat = new Material(shader);
        mat.SetColor("_BaseColor", color);
        mat.SetFloat("_Surface", 1f); // Transparent

        if (additive)
        {
            mat.SetInt("_Blend", 1); // Additive
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
        }
        else
        {
            mat.SetInt("_Blend", 0); // Alpha
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        }
        mat.SetInt("_ZWrite", 0);
        return mat;
    }
}

internal class ChunkMover : MonoBehaviour
{
    private Vector3 _velocity;
    private Vector3 _rotationAxis;
    private float _rotationSpeed;
    private float _deceleration;
    private Material _mat;
    private float _lifetime;
    private float _elapsed;

    public void Init(Vector3 direction, float speed, float lifetime, Material mat)
    {
        _velocity = direction * speed;
        _rotationAxis = Random.onUnitSphere;
        _rotationSpeed = Random.Range(150f, 400f);
        _deceleration = speed / lifetime;
        _mat = mat;
        _lifetime = lifetime;
        Destroy(gameObject, lifetime + 1f);
    }

    void Update()
    {
        _elapsed += Time.deltaTime;
        transform.position += _velocity * Time.deltaTime;
        transform.Rotate(_rotationAxis, _rotationSpeed * Time.deltaTime);

        float speed = _velocity.magnitude;
        if (speed > 0)
        {
            _velocity = _velocity.normalized * Mathf.Max(0, speed - _deceleration * Time.deltaTime);
        }

        // Material lerp
        float t = Mathf.Clamp01(_elapsed / 4f);
        Color startCol = new Color(1f, 0.3f, 0f);
        Color endCol = new Color(0.2f, 0.18f, 0.15f);
        Color current = Color.Lerp(startCol, endCol, t);
        _mat.SetColor("_BaseColor", current);
        if (_mat.HasProperty("_EmissionColor"))
        {
            _mat.SetColor("_EmissionColor", current * Mathf.Lerp(4f, 0f, t));
        }
    }
}
