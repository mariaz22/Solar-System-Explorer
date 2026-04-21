using UnityEngine;

public class TargetIndicator : MonoBehaviour
{
    public static TargetIndicator Instance { get; private set; }

    Planet target;
    LineRenderer ringA, ringB;
    float spin;
    const int Segs = 48;

    void Awake()
    {
        Instance = this;
        ringA = MakeRing("RingA", 0.10f);
        ringB = MakeRing("RingB", 0.05f);
        gameObject.SetActive(false);
    }

    LineRenderer MakeRing(string name, float width)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = false;
        lr.loop = true;
        lr.positionCount = Segs;
        lr.widthMultiplier = width;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;
        lr.material = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color"));
        return lr;
    }

    public void SetTarget(Planet planet)
    {
        target = planet;
        gameObject.SetActive(planet != null);
        spin = 0f;
    }

    void Update()
    {
        if (target == null) { gameObject.SetActive(false); return; }

        transform.position = target.transform.position;
        spin += Time.deltaTime;

        float r = target.radius * 1.8f + Mathf.Sin(spin * 2f) * target.radius * 0.06f;
        float alpha = 0.5f + Mathf.Sin(spin * 4f) * 0.35f;

        DrawRing(ringA, r, Quaternion.Euler(12f, spin * 30f, 0f),
            new Color(1f, 0.78f, 0f, alpha));
        DrawRing(ringB, r * 1.22f, Quaternion.Euler(-8f, -spin * 24f, 5f),
            new Color(1f, 0.4f, 0.1f, alpha * 0.55f));
    }

    void DrawRing(LineRenderer lr, float r, Quaternion rot, Color col)
    {
        lr.startColor = lr.endColor = col;
        for (int i = 0; i < Segs; i++)
        {
            float a = (float)i / Segs * Mathf.PI * 2f;
            lr.SetPosition(i, rot * new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r));
        }
    }
}
