using UnityEngine;

public class TargetIndicator : MonoBehaviour
{
    public static TargetIndicator Instance
    {
        get
        {
            if (_instance == null) _instance = Object.FindAnyObjectByType<TargetIndicator>(FindObjectsInactive.Include);
            return _instance;
        }
    }
    static TargetIndicator _instance;

    Planet target;
    LineRenderer ringA, ringB, beam;
    GameObject arrowVisual;
    float spin;
    const int Segs = 48;

    void Awake()
    {
        _instance = this;
        ringA = MakeRing("RingA", 0.08f);
        ringB = MakeRing("RingB", 0.04f);
        beam = MakeBeam("PointerBeam");
        BuildArrow();
        SetVisualsActive(false);
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

    LineRenderer MakeBeam(string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = false;
        lr.positionCount = 2;
        lr.widthMultiplier = 0.04f;
        lr.material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        lr.material.SetColor("_BaseColor", new Color(1, 0.85f, 0, 0.4f));
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        return lr;
    }

    void BuildArrow()
    {
        arrowVisual = new GameObject("SelectionArrow");
        arrowVisual.transform.SetParent(transform, false);
        
        var body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Destroy(body.GetComponent<Collider>());
        body.transform.SetParent(arrowVisual.transform, false);
        body.transform.localScale = new Vector3(0.06f, 0.7f, 0.06f);
        body.transform.localPosition = new Vector3(0, 0.7f, 0);
        
        var head = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Destroy(head.GetComponent<Collider>());
        head.transform.SetParent(arrowVisual.transform, false);
        head.transform.localPosition = Vector3.zero;
        head.transform.localScale = new Vector3(0.6f, 0.6f, 0.6f);
        head.transform.localRotation = Quaternion.Euler(45, 45, 45);

        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.SetColor("_BaseColor", new Color(1f, 0.9f, 0.2f, 0.7f));
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", new Color(1f, 0.85f, 0f) * 8.0f);
        
        mat.SetFloat("_Surface", 1f); 
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One); 
        mat.SetInt("_ZWrite", 0);
        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent + 100;

        body.GetComponent<MeshRenderer>().material = mat;
        head.GetComponent<MeshRenderer>().material = mat;
    }

    public void SetTarget(Planet planet)
    {
        target = planet;
        SetVisualsActive(planet != null);
        spin = 0f;
        if (target != null)
        {
            transform.position = target.transform.position;
            UpdateArrow();
        }
    }

    void SetVisualsActive(bool on)
    {
        if (ringA != null) ringA.enabled = on;
        if (ringB != null) ringB.enabled = on;
        if (beam != null) beam.enabled = on;
        if (arrowVisual != null) arrowVisual.SetActive(on);
    }

    void Update()
    {
        if (target == null) return;

        transform.position = target.transform.position;
        spin += Time.deltaTime;

        UpdateArrow();

        float r = target.radius * 2.0f + Mathf.Sin(spin * 2f) * target.radius * 0.08f;
        float alpha = 0.4f + Mathf.Sin(spin * 4f) * 0.2f;

        DrawRing(ringA, r, Quaternion.Euler(12f, spin * 40f, 0f),
            new Color(1f, 0.85f, 0.2f, alpha * 0.2f));
        DrawRing(ringB, r * 1.25f, Quaternion.Euler(-8f, -spin * 30f, 5f),
            new Color(1f, 0.45f, 0.1f, alpha * 0.1f));
            
        if (beam != null)
        {
            beam.SetPosition(0, Vector3.zero);
            beam.SetPosition(1, arrowVisual.transform.localPosition);
            beam.startColor = beam.endColor = new Color(1, 0.85f, 0.1f, alpha * 0.25f);
        }
    }

    void UpdateArrow()
    {
        float bob = Mathf.Sin(spin * 3.5f) * 0.8f;
        float height = target.radius + 4.2f + bob;
        arrowVisual.transform.localPosition = new Vector3(0, height, 0);
        arrowVisual.transform.Rotate(Vector3.up, 75f * Time.deltaTime, Space.Self);
        
        float p = 6.0f + Mathf.PingPong(spin * 2.5f, 4.0f);
        Color glowColor = new Color(1f, 0.85f, 0.1f) * p;

        foreach (var mr in arrowVisual.GetComponentsInChildren<MeshRenderer>())
        {
            mr.material.SetColor("_EmissionColor", glowColor);
        }
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
