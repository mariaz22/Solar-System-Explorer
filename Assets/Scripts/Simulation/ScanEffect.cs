using UnityEngine;

public class ScanEffect : MonoBehaviour
{
    LineRenderer ring;
    GameObject ringGO;
    bool active;
    float t;
    float planetR;
    Vector3 originPos;

    const int Segs = 64;
    const float Duration = 2f;

    void Awake()
    {
        ringGO = new GameObject("ScanRing");
        ring = ringGO.AddComponent<LineRenderer>();
        ring.useWorldSpace = true;
        ring.loop = true;
        ring.positionCount = Segs;
        ring.widthMultiplier = 0.18f;
        ring.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        ring.receiveShadows = false;
        ring.material = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color"));
        ring.enabled = false;
    }

    public void Play(float radius)
    {
        planetR = radius;
        originPos = transform.position;
        t = 0f;
        active = true;
        ring.enabled = true;
    }

    void Update()
    {
        if (!active) return;
        t += Time.deltaTime / Duration;
        if (t >= 1f) { ring.enabled = false; active = false; return; }

        float r = planetR * (1f + t * 4f);
        float alpha = Mathf.Sin(t * Mathf.PI);

        float intensity = 2.5f;
        Color baseC = Color.Lerp(new Color(0f, 1f, 0.5f), new Color(0.2f, 0.5f, 1f), t);
        Color c = baseC * intensity;
        c.a = alpha * 0.9f;
        ring.startColor = ring.endColor = c;
        ring.widthMultiplier = Mathf.Lerp(0.28f, 0.04f, t);

        Vector3 center = transform.position;
        for (int i = 0; i < Segs; i++)
        {
            float a = (float)i / Segs * Mathf.PI * 2f;
            ring.SetPosition(i, center + new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r));
        }
    }

    void OnDestroy()
    {
        if (ringGO != null) Destroy(ringGO);
    }
}
