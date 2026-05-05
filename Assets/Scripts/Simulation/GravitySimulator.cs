using UnityEngine;

public class GravitySimulator : MonoBehaviour
{
    public static readonly float G = 1f;

    CelestialBody[] bodies;

    void Start()
    {
        bodies = FindObjectsByType<CelestialBody>(FindObjectsInactive.Exclude);
    }

    void FixedUpdate()
    {
        for (int i = 0; i < bodies.Length; i++)
        {
            if (bodies[i] == null || bodies[i].isStatic) continue;

            Vector3 acceleration = Vector3.zero;
            for (int j = 0; j < bodies.Length; j++)
            {
                if (i == j || bodies[j] == null) continue;
                Vector3 dir = bodies[j].transform.position - bodies[i].transform.position;
                float distSq = dir.sqrMagnitude;
                if (distSq < 1f) continue;
                // F = G*m1*m2/r^2, a = F/m1 = G*m2/r^2
                acceleration += dir.normalized * (G * bodies[j].mass / distSq);
            }

            bodies[i].velocity += acceleration * Time.fixedDeltaTime;
        }

        foreach (var b in bodies)
        {
            if (b != null && !b.isStatic)
                b.transform.position += b.velocity * Time.fixedDeltaTime;
        }
    }
}
