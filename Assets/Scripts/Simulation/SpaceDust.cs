using UnityEngine;

public class SpaceDust : MonoBehaviour
{
    public int particleCount = 200;
    public float areaSize = 50f;
    public float minSize = 0.05f;
    public float maxSize = 0.15f;
    
    GameObject[] particles;
    Vector3[] offsets;

    void Start()
    {
        particles = new GameObject[particleCount];
        offsets = new Vector3[particleCount];
        
        var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        mat.SetFloat("_Surface", 1f);
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
        mat.SetInt("_ZWrite", 0);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", new Color(0.5f, 0.7f, 1f, 0.2f));

        for (int i = 0; i < particleCount; i++)
        {
            var p = GameObject.CreatePrimitive(PrimitiveType.Quad);
            p.name = "Dust";
            Destroy(p.GetComponent<Collider>());
            p.transform.SetParent(transform);
            
            offsets[i] = new Vector3(
                Random.Range(-areaSize, areaSize),
                Random.Range(-areaSize, areaSize),
                Random.Range(-areaSize, areaSize)
            );
            
            p.transform.localScale = Vector3.one * Random.Range(minSize, maxSize);
            p.GetComponent<MeshRenderer>().material = mat;
            p.AddComponent<Billboard>();
            particles[i] = p;
        }
    }

    void Update()
    {
        Vector3 camPos = Camera.main.transform.position;
        for (int i = 0; i < particleCount; i++)
        {
            Vector3 pos = camPos + offsets[i];
            
            // Wrap around
            if (pos.x - camPos.x > areaSize) offsets[i].x -= areaSize * 2;
            if (pos.x - camPos.x < -areaSize) offsets[i].x += areaSize * 2;
            if (pos.y - camPos.y > areaSize) offsets[i].y -= areaSize * 2;
            if (pos.y - camPos.y < -areaSize) offsets[i].y += areaSize * 2;
            if (pos.z - camPos.z > areaSize) offsets[i].z -= areaSize * 2;
            if (pos.z - camPos.z < -areaSize) offsets[i].z += areaSize * 2;
            
            particles[i].transform.position = camPos + offsets[i];
            
            // Fade based on distance to center of area
            float dist = Vector3.Distance(particles[i].transform.position, camPos);
            float alpha = Mathf.Clamp01(1f - (dist / areaSize));
            particles[i].GetComponent<MeshRenderer>().material.SetColor("_BaseColor", new Color(0.5f, 0.7f, 1f, alpha * 0.3f));
        }
    }
}
