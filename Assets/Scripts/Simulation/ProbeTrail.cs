using UnityEngine;

[RequireComponent(typeof(ProbeController))]
public class ProbeTrail : MonoBehaviour
{
    public float time = 1.5f;
    public float startWidth = 0.4f;
    public float endWidth = 0.05f;
    public Color color = new Color(1f, 0.4f, 1f, 1f);

    ProbeController probe;
    TrailRenderer trail;

    void Awake()
    {
        probe = GetComponent<ProbeController>();

        trail = GetComponent<TrailRenderer>();
        if (trail == null) trail = gameObject.AddComponent<TrailRenderer>();

        trail.time = time;
        trail.startWidth = startWidth;
        trail.endWidth = endWidth;

        var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
        if (shader != null)
        {
            trail.material = new Material(shader);
            trail.material.color = color;
        }
        trail.startColor = color;
        trail.endColor = new Color(color.r, color.g, color.b, 0f);
        trail.emitting = false;
    }

    void Update()
    {
        trail.emitting = probe.CurrentState == ProbeController.State.Traveling;
    }
}
