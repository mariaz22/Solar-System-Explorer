using UnityEngine;

public class RocketExhaust : MonoBehaviour
{
    private ParticleSystem firePS;
    private ParticleSystem smokePS;
    private Light thrustLight;
    private ProbeController probe;
    
    void Start()
    {
        probe = GetComponent<ProbeController>();
        
        // 1. Setup Fire System (Hot Core)
        GameObject fireGo = new GameObject("FireCore");
        fireGo.transform.SetParent(this.transform, false);
        fireGo.transform.localPosition = new Vector3(0f, 0f, -0.85f);
        fireGo.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        firePS = SetupFireSystem(fireGo);

        // 2. Setup Smoke System (Volumetric Trails)
        GameObject smokeGo = new GameObject("SmokeTrail");
        smokeGo.transform.SetParent(this.transform, false);
        smokeGo.transform.localPosition = new Vector3(0f, 0f, -1.0f);
        smokeGo.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        smokePS = SetupSmokeSystem(smokeGo);

        // 3. Add dynamic thrust light
        GameObject lightGo = new GameObject("ThrustLight");
        lightGo.transform.SetParent(this.transform, false);
        lightGo.transform.localPosition = new Vector3(0f, 0f, -1.2f);
        thrustLight = lightGo.AddComponent<Light>();
        thrustLight.type = LightType.Point;
        thrustLight.color = new Color(1f, 0.4f, 0.1f);
        thrustLight.intensity = 15f;
        thrustLight.range = 10f;
    }

    ParticleSystem SetupFireSystem(GameObject go)
    {
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(); // Ensure it's not playing before setting duration
        var main = ps.main;
        main.playOnAwake = false;
        main.duration = 1f;
        main.startLifetime = 0.4f;
        main.startSpeed = 15f;
        main.startSize = 0.6f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 200;
        main.loop = true;

        var emission = ps.emission;
        emission.rateOverTime = 80f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 5f;
        shape.radius = 0.1f;

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(1f, 0.8f, 0.2f), 0.2f), new GradientColorKey(new Color(1f, 0.2f, 0f), 0.7f) },
            new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.5f), new GradientAlphaKey(0f, 1f) }
        );
        colorOverLifetime.color = grad;

        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.8f), new Keyframe(0.2f, 1.2f), new Keyframe(1f, 0.2f)));

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
        
        ps.Play();
        return ps;
    }

    ParticleSystem SetupSmokeSystem(GameObject go)
    {
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(); // Ensure it's not playing before setting duration
        var main = ps.main;
        main.playOnAwake = false;
        main.duration = 1f;
        main.startLifetime = 3.5f;
        main.startSpeed = new ParticleSystem.MinMaxCurve(3f, 7f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.5f, 1.2f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, 360f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 800;
        main.loop = true;

        var emission = ps.emission;
        emission.rateOverTime = 50f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 15f;
        shape.radius = 0.25f;

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(new Color(0.4f, 0.4f, 0.4f), 0f), new GradientColorKey(new Color(0.2f, 0.2f, 0.2f), 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.4f, 0.1f), new GradientAlphaKey(0.2f, 0.5f), new GradientAlphaKey(0f, 1f) }
        );
        colorOverLifetime.color = grad;

        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.4f), new Keyframe(1f, 5.0f)));

        var rotationOverLifetime = ps.rotationOverLifetime;
        rotationOverLifetime.enabled = true;
        rotationOverLifetime.z = new ParticleSystem.MinMaxCurve(-45f, 45f);

        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.8f;
        noise.frequency = 0.5f;
        noise.scrollSpeed = 1.0f;
        noise.damping = true;

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
        renderer.sortingOrder = -1;

        ps.Play();
        return ps;
    }

    void Update()
    {
        if (probe == null) return;
        
        bool isMoving = probe.FSM.CurrentState is TravelState || 
                        probe.FSM.CurrentState is ManualControlState ||
                        probe.FSM.CurrentState is ReturnState ||
                        probe.FSM.CurrentState is AvoidCollisionState;
        
        var fireEmission = firePS.emission;
        fireEmission.enabled = isMoving;
        
        var smokeEmission = smokePS.emission;
        smokeEmission.enabled = isMoving;

        if (isMoving)
        {
            thrustLight.enabled = true;
            thrustLight.intensity = 15f + Mathf.PingPong(Time.time * 20f, 10f);
            thrustLight.range = 8f + Random.Range(-0.5f, 0.5f);
        }
        else
        {
            thrustLight.enabled = false;
        }
    }
}


