using UnityEngine;

public class RocketExhaust : MonoBehaviour
{
    private ParticleSystem firePS;
    private ParticleSystem smokePS;
    private Light thrustLight;
    private ProbeController probe;

    void Start()
    {
        // BeautifulRocketExhaust is the replacement — don't run both
        if (GetComponent<BeautifulRocketExhaust>() != null)
        {
            enabled = false;
            return;
        }

        probe = GetComponent<ProbeController>();

        GameObject fireGo = new GameObject("FireCore");
        fireGo.transform.SetParent(this.transform, false);
        fireGo.transform.localPosition = new Vector3(0f, 0f, -0.85f);
        fireGo.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        firePS = SetupFireSystem(fireGo);

        GameObject smokeGo = new GameObject("SmokeTrail");
        smokeGo.transform.SetParent(this.transform, false);
        smokeGo.transform.localPosition = new Vector3(0f, 0f, -1.0f);
        smokeGo.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        smokePS = SetupSmokeSystem(smokeGo);

        GameObject lightGo = new GameObject("ThrustLight");
        lightGo.transform.SetParent(this.transform, false);
        lightGo.transform.localPosition = new Vector3(0f, 0f, -1.2f);
        thrustLight = lightGo.AddComponent<Light>();
        thrustLight.type = LightType.Point;
        thrustLight.color = new Color(1f, 0.4f, 0.1f);
        thrustLight.intensity = 15f;
        thrustLight.range = 10f;
        thrustLight.enabled = false;
    }

    ParticleSystem SetupFireSystem(GameObject go)
    {
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop();
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
        emission.enabled = false;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 5f;
        shape.radius = 0.1f;

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        var grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(new Color(1f, 0.8f, 0.2f), 0.2f),
                new GradientColorKey(new Color(1f, 0.2f, 0f), 0.7f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 0.5f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = grad;

        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f,
            new AnimationCurve(new Keyframe(0f, 0.8f), new Keyframe(0.2f, 1.2f), new Keyframe(1f, 0.2f)));

        go.GetComponent<ParticleSystemRenderer>().material =
            new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));

        return ps;
    }

    ParticleSystem SetupSmokeSystem(GameObject go)
    {
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop();
        var main = ps.main;
        main.playOnAwake = false;
        main.duration = 1f;
        main.startLifetime = 0.6f;
        main.startSpeed = new ParticleSystem.MinMaxCurve(3f, 7f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.15f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, 360f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 60;
        main.loop = true;

        var emission = ps.emission;
        emission.rateOverTime = 20f;
        emission.enabled = false;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 8f;
        shape.radius = 0.08f;

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        var grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(0.9f, 0.6f, 0.2f), 0f),
                new GradientColorKey(new Color(0.5f, 0.5f, 0.5f), 0.4f),
                new GradientColorKey(new Color(0.2f, 0.2f, 0.2f), 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0.6f, 0f),
                new GradientAlphaKey(0.3f, 0.4f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = grad;

        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f,
            new AnimationCurve(new Keyframe(0f, 0.5f), new Keyframe(1f, 1.5f)));

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
        renderer.sortingOrder = -1;

        return ps;
    }

    void Update()
    {
        if (probe == null) return;

        bool isMoving;
        if (probe.FSM?.CurrentState is ManualControlState)
            isMoving = probe.IsThrusting;
        else
            isMoving = probe.FSM?.CurrentState is TravelState       ||
                       probe.FSM?.CurrentState is ReturnState        ||
                       probe.FSM?.CurrentState is AvoidCollisionState;

        var fireEmission = firePS.emission;
        if (fireEmission.enabled != isMoving)
        {
            fireEmission.enabled = isMoving;
            if (!isMoving) firePS.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            else           firePS.Play();
        }

        var smokeEmission = smokePS.emission;
        if (smokeEmission.enabled != isMoving)
        {
            smokeEmission.enabled = isMoving;
            if (!isMoving) smokePS.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            else           smokePS.Play();
        }

        thrustLight.enabled = isMoving;
        if (isMoving)
        {
            thrustLight.intensity = 15f + Mathf.PingPong(Time.time * 20f, 10f);
            thrustLight.range     = 8f  + Random.Range(-0.5f, 0.5f);
        }
    }
}
