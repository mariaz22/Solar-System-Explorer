using UnityEngine;

public class OrbitalMotion : MonoBehaviour
{
    public Transform center;
    public float angularSpeedDeg = 10f;
    public Vector3 axis = Vector3.up;

    float angle;
    float radius;
    float height;

    void Start()
    {
        Vector3 c = center != null ? center.position : Vector3.zero;
        Vector3 offset = transform.position - c;
        radius = new Vector2(offset.x, offset.z).magnitude;
        height = offset.y;
        angle = Mathf.Atan2(offset.z, offset.x) * Mathf.Rad2Deg;
    }

    void Update()
    {
        if (radius <= 0.0001f) return;
        angle += angularSpeedDeg * Time.deltaTime;
        float r = angle * Mathf.Deg2Rad;
        Vector3 c = center != null ? center.position : Vector3.zero;
        transform.position = c + new Vector3(Mathf.Cos(r) * radius, height, Mathf.Sin(r) * radius);
    }
}
