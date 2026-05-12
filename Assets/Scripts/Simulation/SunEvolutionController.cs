using UnityEngine;

public class SunEvolutionController : MonoBehaviour
{
    [System.Serializable]
    public struct StageVisuals
    {
        public float  scale;
        public float  lightIntensity;
        public Color  lightColor;
        public Color  glowColor;
    }

    [Header("Stage Visuals (index matches SolarStage enum)")]
    public StageVisuals[] stages = new StageVisuals[]
    {
        new StageVisuals { scale = 1.00f, lightIntensity =  65f, lightColor = new Color(1.00f, 0.96f, 0.82f), glowColor = new Color(1.00f, 0.95f, 0.60f, 0.70f) }, // MainSequence
        new StageVisuals { scale = 1.60f, lightIntensity =  80f, lightColor = new Color(1.00f, 0.85f, 0.60f), glowColor = new Color(1.00f, 0.75f, 0.30f, 0.75f) }, // SubGiant
        new StageVisuals { scale = 2.80f, lightIntensity = 110f, lightColor = new Color(1.00f, 0.28f, 0.08f), glowColor = new Color(1.00f, 0.18f, 0.04f, 0.80f) }, // RedGiant
        new StageVisuals { scale = 1.50f, lightIntensity =  35f, lightColor = new Color(0.55f, 0.35f, 1.00f), glowColor = new Color(0.80f, 0.50f, 1.00f, 0.90f) }, // PlanetaryNebula — contracting purple core
        new StageVisuals { scale = 0.04f, lightIntensity = 200f, lightColor = new Color(0.80f, 0.92f, 1.00f), glowColor = new Color(0.90f, 0.96f, 1.00f, 0.85f) }, // WhiteDwarf — tiny cyan point
    };

    [Header("Transition Curves (x=stage progress 0-1, y=lerp factor 0-1)")]
    public AnimationCurve scaleCurve     = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    public AnimationCurve intensityCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    public AnimationCurve colorCurve     = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    Light      sunLight;
    Material   glowMat;
    Material   coronaMat;
    Vector3    baseScale;

    void Start()
    {
        sunLight  = GetComponent<Light>();
        baseScale = transform.localScale;

        var glowChild = transform.Find("SunGlow");
        if (glowChild != null)
        {
            var mr = glowChild.GetComponent<MeshRenderer>();
            if (mr != null) glowMat = mr.material;
        }

        var coronaChild = transform.Find("SunCorona");
        if (coronaChild != null)
        {
            var mr = coronaChild.GetComponent<MeshRenderer>();
            if (mr != null) coronaMat = mr.material;
        }

        if (CosmicTimelineManager.Instance != null)
        {
            CosmicTimelineManager.Instance.OnCosmicTimeChanged += OnCosmicTimeChanged;
            ApplyVisuals(CosmicTimelineManager.Instance.GetCurrentStage(),
                         CosmicTimelineManager.Instance.GetStageProgress());
        }
    }

    void OnDestroy()
    {
        if (CosmicTimelineManager.Instance != null)
            CosmicTimelineManager.Instance.OnCosmicTimeChanged -= OnCosmicTimeChanged;
    }

    void OnCosmicTimeChanged(float gyr, SolarStage stage)
    {
        ApplyVisuals(stage, CosmicTimelineManager.Instance.GetStageProgress());
    }

    void ApplyVisuals(SolarStage stage, float progress)
    {
        int cur  = (int)stage;
        int next = Mathf.Min(cur + 1, stages.Length - 1);

        StageVisuals a = stages[cur];
        StageVisuals b = stages[next];

        float st = scaleCurve.Evaluate(progress);
        float it = intensityCurve.Evaluate(progress);
        float ct = colorCurve.Evaluate(progress);

        // Scale — relative to the Sun's original scale at Start
        transform.localScale = baseScale * Mathf.Lerp(a.scale, b.scale, st);

        // Light
        if (sunLight != null)
        {
            sunLight.intensity = Mathf.Lerp(a.lightIntensity, b.lightIntensity, it);
            sunLight.color     = Color.Lerp(a.lightColor, b.lightColor, ct);
        }

        // Glow materials (_BaseColor carries alpha for additive opacity)
        Color gc = Color.Lerp(a.glowColor, b.glowColor, ct);
        if (glowMat != null && glowMat.HasProperty("_BaseColor"))
            glowMat.SetColor("_BaseColor", gc);
        if (coronaMat != null && coronaMat.HasProperty("_BaseColor"))
            coronaMat.SetColor("_BaseColor", new Color(gc.r, gc.g, gc.b, gc.a * 0.65f));
    }
}
