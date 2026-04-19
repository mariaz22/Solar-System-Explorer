using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class OrbitPathRenderer : MonoBehaviour
{
    public Transform center;
    public int segments = 128;
    public float width = 0.1f;
    public Color color = new Color(1f, 1f, 1f, 0.15f);

    LineRenderer lr;

    void Awake()
    {
        lr = GetComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.loop = true;
        lr.widthMultiplier = width;
        var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
        if (shader != null)
        {
            lr.material = new Material(shader);
            lr.material.color = color;
        }
        lr.startColor = color;
        lr.endColor = color;
    }

    void Start()
    {
        Rebuild();
    }

    public void Rebuild()
    {
        Vector3 c = center != null ? center.position : Vector3.zero;
        Vector3 offset = transform.position - c;
        float radius = new Vector2(offset.x, offset.z).magnitude;
        float y = offset.y;

        lr.positionCount = segments;
        for (int i = 0; i < segments; i++)
        {
            float t = i / (float)segments * Mathf.PI * 2f;
            lr.SetPosition(i, c + new Vector3(Mathf.Cos(t) * radius, y, Mathf.Sin(t) * radius));
        }
    }
}
