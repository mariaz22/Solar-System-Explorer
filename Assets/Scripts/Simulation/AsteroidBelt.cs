using UnityEngine;

public class AsteroidBelt : MonoBehaviour
{
    public int count = 400;
    public float minRadius = 45f;
    public float maxRadius = 65f;
    public float height = 4f;
    public float minSize = 0.2f;
    public float maxSize = 0.8f;

    void Start()
    {
        for (int i = 0; i < count; i++)
        {
            SpawnAsteroid();
        }
    }

    void SpawnAsteroid()
    {
        float angle = Random.Range(0f, Mathf.PI * 2f);
        float radius = Random.Range(minRadius, maxRadius);
        Vector3 pos = new Vector3(Mathf.Cos(angle) * radius, Random.Range(-height, height), Mathf.Sin(angle) * radius);
        
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "Asteroid";
        go.transform.SetParent(transform);
        go.transform.position = pos;
        
        float size = Random.Range(minSize, maxSize);
        go.transform.localScale = new Vector3(
            size * Random.Range(0.8f, 1.2f),
            size * Random.Range(0.8f, 1.2f),
            size * Random.Range(0.8f, 1.2f)
        );
        go.transform.rotation = Random.rotation;

        Destroy(go.GetComponent<Collider>());

        var mr = go.GetComponent<MeshRenderer>();
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        Color rockColor = new Color(Random.Range(0.2f, 0.4f), Random.Range(0.2f, 0.35f), Random.Range(0.15f, 0.3f));
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", rockColor);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.1f);
        mr.material = mat;

        // Add some rotation
        var spin = go.AddComponent<SelfRotation>();
        spin.degreesPerSecond = Random.Range(5f, 25f);
        }
        }
