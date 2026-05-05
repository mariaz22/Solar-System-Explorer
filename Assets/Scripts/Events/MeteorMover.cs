using UnityEngine;

public class MeteorMover : MonoBehaviour
{
    Vector3 direction;
    float speed;

    public void Init(Vector3 dir, float spd)
    {
        direction = dir;
        speed = spd + Random.Range(-20f, 20f);
    }

    void Update()
    {
        transform.position += direction * speed * Time.deltaTime;
        transform.Rotate(direction, 120f * Time.deltaTime);
    }
}
