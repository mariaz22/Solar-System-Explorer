using UnityEngine;

public class SelfRotation : MonoBehaviour
{
    public Vector3 axis = Vector3.up;
    public float degreesPerSecond = 30f;

    void Update()
    {
        transform.Rotate(axis.normalized, degreesPerSecond * Time.deltaTime, Space.Self);
    }
}
