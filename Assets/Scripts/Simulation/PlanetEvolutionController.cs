using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;
using Object = UnityEngine.Object;

[ExecuteAlways]
public class PlanetEvolutionController : MonoBehaviour
{
    [Serializable]
public class PlanetStageData
    {
        public SolarStage stage;
        public bool       isDestroyed;
        public float      scaleMultiplier;
        public Color      atmosphereTint;
    }

    public List<PlanetStageData> stageData = new();
    public AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    static readonly Dictionary<string, PlanetStageData[]> PlanetStages = new()
    {
        ["Mercury"] = new[] {
            S(SolarStage.MainSequence,    false, 1.0f, Color.white),
            S(SolarStage.SubGiant,        false, 1.0f, new Color(1.00f, 0.85f, 0.70f)),
            S(SolarStage.RedGiant,        true,  0.0f, Color.black),
            S(SolarStage.PlanetaryNebula, true,  0.0f, Color.black),
            S(SolarStage.WhiteDwarf,      true,  0.0f, Color.black),
        },
        ["Venus"] = new[] {
            S(SolarStage.MainSequence,    false, 1.0f, Color.white),
            S(SolarStage.SubGiant,        false, 1.0f, new Color(1.00f, 0.75f, 0.40f)),
            S(SolarStage.RedGiant,        true,  0.0f, Color.black),
            S(SolarStage.PlanetaryNebula, true,  0.0f, Color.black),
            S(SolarStage.WhiteDwarf,      true,  0.0f, Color.black),
        },
        ["Earth"] = new[] {
            S(SolarStage.MainSequence,    false, 1.0f, Color.white),
            S(SolarStage.SubGiant,        false, 1.0f, new Color(1.00f, 0.90f, 0.70f)),
            S(SolarStage.RedGiant,        false, 1.0f, new Color(1.00f, 0.25f, 0.05f)),
            S(SolarStage.PlanetaryNebula, true,  0.0f, Color.black),
            S(SolarStage.WhiteDwarf,      true,  0.0f, Color.black),
        },
        ["Mars"] = new[] {
            S(SolarStage.MainSequence,    false, 1.0f, Color.white),
            S(SolarStage.SubGiant,        false, 1.0f, new Color(0.85f, 0.70f, 0.60f)),
            S(SolarStage.RedGiant,        false, 1.0f, new Color(0.55f, 0.30f, 0.20f)),
            S(SolarStage.PlanetaryNebula, false, 1.0f, new Color(0.25f, 0.15f, 0.10f)),
            S(SolarStage.WhiteDwarf,      false, 1.0f, new Color(0.10f, 0.08f, 0.08f)),
        },
        ["Jupiter"] = new[] {
            S(SolarStage.MainSequence,    false, 1.0f, Color.white),
            S(SolarStage.SubGiant,        false, 1.0f, new Color(0.85f, 0.75f, 0.65f)),
            S(SolarStage.RedGiant,        false, 1.0f, new Color(0.60f, 0.50f, 0.40f)),
            S(SolarStage.PlanetaryNebula, false, 1.0f, new Color(0.35f, 0.28f, 0.22f)),
            S(SolarStage.WhiteDwarf,      false, 1.0f, new Color(0.12f, 0.10f, 0.10f)),
        },
        ["Saturn"] = new[] {
            S(SolarStage.MainSequence,    false, 1.0f, Color.white),
            S(SolarStage.SubGiant,        false, 1.0f, new Color(0.88f, 0.78f, 0.60f)),
            S(SolarStage.RedGiant,        false, 1.0f, new Color(0.62f, 0.50f, 0.38f)),
            S(SolarStage.PlanetaryNebula, false, 1.0f, new Color(0.32f, 0.25f, 0.18f)),
            S(SolarStage.WhiteDwarf,      false, 1.0f, new Color(0.11f, 0.09f, 0.09f)),
        },
        ["Uranus"] = new[] {
            S(SolarStage.MainSequence,    false, 1.0f, Color.white),
            S(SolarStage.SubGiant,        false, 1.0f, new Color(0.80f, 0.90f, 0.90f)),
            S(SolarStage.RedGiant,        false, 1.0f, new Color(0.55f, 0.65f, 0.65f)),
            S(SolarStage.PlanetaryNebula, false, 1.0f, new Color(0.28f, 0.35f, 0.35f)),
            S(SolarStage.WhiteDwarf,      false, 1.0f, new Color(0.10f, 0.12f, 0.12f)),
        },
        ["Neptune"] = new[] {
            S(SolarStage.MainSequence,    false, 1.0f, Color.white),
            S(SolarStage.SubGiant,        false, 1.0f, new Color(0.75f, 0.80f, 1.00f)),
            S(SolarStage.RedGiant,        false, 1.0f, new Color(0.45f, 0.50f, 0.70f)),
            S(SolarStage.PlanetaryNebula, false, 1.0f, new Color(0.22f, 0.25f, 0.38f)),
            S(SolarStage.WhiteDwarf,      false, 1.0f, new Color(0.08f, 0.09f, 0.13f)),
        },
    };

    static PlanetStageData S(SolarStage stage, bool destroyed, float scale, Color tint) =>
        new PlanetStageData { stage = stage, isDestroyed = destroyed, scaleMultiplier = scale, atmosphereTint = tint };

    Renderer rend;
    Vector3  baseScale;
    Color    originalColor;
    bool     _isConsumed;

    void Start()
    {
        rend      = GetComponent<Renderer>();
        baseScale = transform.localScale;
        if (rend != null) originalColor = GetMatColor(rend.material);

        var planet = GetComponent<Planet>();
        if (planet != null && PlanetStages.TryGetValue(planet.data.planetName, out var table))
        {
            stageData.Clear();
            stageData.AddRange(table);
        }

        if (CosmicTimelineManager.Instance != null)
        {
            CosmicTimelineManager.Instance.OnCosmicTimeChanged += OnCosmicTimeChanged;
            Apply(CosmicTimelineManager.Instance.GetCurrentStage(),
                  CosmicTimelineManager.Instance.GetStageProgress());
        }
        }

        void OnEnable()
        {
        if (!Application.isPlaying && CosmicTimelineManager.Instance != null)
        {
            CosmicTimelineManager.Instance.OnCosmicTimeChanged -= OnCosmicTimeChanged;
            CosmicTimelineManager.Instance.OnCosmicTimeChanged += OnCosmicTimeChanged;
            Apply(CosmicTimelineManager.Instance.GetCurrentStage(),
                  CosmicTimelineManager.Instance.GetStageProgress());
        }
        }

        void OnDestroy()
        {
        if (CosmicTimelineManager.Instance != null)
            CosmicTimelineManager.Instance.OnCosmicTimeChanged -= OnCosmicTimeChanged;
        }

        void Update()
        {
        if (!Application.isPlaying && CosmicTimelineManager.Instance != null)
        {
            Apply(CosmicTimelineManager.Instance.GetCurrentStage(),
                  CosmicTimelineManager.Instance.GetStageProgress());
        }
        }

        void OnCosmicTimeChanged(float gyr, SolarStage stage)
        {
        Apply(stage, CosmicTimelineManager.Instance.GetStageProgress());
        }

        void Apply(SolarStage stage, float progress)
        {
        if (stageData == null || stageData.Count == 0) return;

        int cur  = Mathf.Clamp((int)stage, 0, stageData.Count - 1);
        int next = Mathf.Min(cur + 1, stageData.Count - 1);

        PlanetStageData a = stageData[cur];
        PlanetStageData b = stageData[next];

        // Trigger consumed explosion once when first entering a destroyed stage
        if (Application.isPlaying && a.isDestroyed && !_isConsumed)
        {
            _isConsumed = true;
            StartCoroutine(ConsumedEffect());
        }
        else if (!a.isDestroyed)
        {
            _isConsumed = false;
        }

        if (a.isDestroyed)
        {
            if (Application.isPlaying) gameObject.SetActive(false);
            return;
        }

        if (Application.isPlaying) gameObject.SetActive(true);

        float t           = transitionCurve.Evaluate(progress);
        float targetScale = b.isDestroyed ? 0f : b.scaleMultiplier;
        Color targetTint  = b.isDestroyed ? Color.black : b.atmosphereTint;

        transform.localScale = baseScale * Mathf.Lerp(a.scaleMultiplier, targetScale, t);

        if (rend != null && rend.material != null)
            SetMatColor(rend.material, originalColor * Color.Lerp(a.atmosphereTint, targetTint, t));
    }

    IEnumerator ConsumedEffect()
    {
        var vfxGO = new GameObject("PlanetExplosion_VFX");
        var vfx = vfxGO.AddComponent<PlanetExplosionVFX>();
        vfx.PlayExplosion(transform.position, Mathf.Max(transform.localScale.x * 0.5f, 5f));
        yield break;
    }

    static Color GetMatColor(Material mat)
    {
        if (mat == null) return Color.white;
        if (mat.HasProperty("_BaseColor")) return mat.GetColor("_BaseColor");
        if (mat.HasProperty("_Color"))     return mat.GetColor("_Color");
        return Color.white;
    }

    static void SetMatColor(Material mat, Color color)
    {
        if (mat == null) return;
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        else if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
    }
    }
