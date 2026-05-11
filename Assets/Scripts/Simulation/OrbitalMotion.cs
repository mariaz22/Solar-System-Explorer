using UnityEngine;

public class OrbitalMotion : MonoBehaviour
{
    public Transform center;
    public float angularSpeedDeg = 10f;
    public Vector3 axis = Vector3.up;  // orbit plane normal

    float   _angle;
    float   _radius;
    Vector3 _right;   // two basis vectors spanning the orbital plane
    Vector3 _fwd;

    void Start()
    {
        Vector3 c      = center != null ? center.position : Vector3.zero;
        Vector3 offset = transform.position - c;
        _radius = offset.magnitude;

        Vector3 n = axis.sqrMagnitude > 0.001f ? axis.normalized : Vector3.up;
        // Build an orthonormal basis in the orbital plane
        _right = Vector3.Cross(n, Vector3.forward);
        if (_right.sqrMagnitude < 0.001f) _right = Vector3.Cross(n, Vector3.right);
        _right.Normalize();
        _fwd = Vector3.Cross(_right, n).normalized;

        // Initial angle from offset projected onto the plane
        float projX = Vector3.Dot(offset, _right);
        float projZ = Vector3.Dot(offset, _fwd);
        _angle = Mathf.Atan2(projZ, projX) * Mathf.Rad2Deg;
    }

    void Update()
    {
        if (_radius <= 0.0001f) return;
        _angle += angularSpeedDeg * Time.deltaTime;
        float r = _angle * Mathf.Deg2Rad;
        Vector3 c = center != null ? center.position : Vector3.zero;
        transform.position = c + (_right * Mathf.Cos(r) + _fwd * Mathf.Sin(r)) * _radius;
    }
}
