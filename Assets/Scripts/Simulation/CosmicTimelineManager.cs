using System;
using System.Collections.Generic;
using UnityEngine;

public enum SolarStage
{
    MainSequence,    // 0 - 5 Gyr
    SubGiant,        // 5 - 6 Gyr
    RedGiant,        // 6 - 8.5 Gyr
    PlanetaryNebula, // 8.5 - 9.5 Gyr
    WhiteDwarf       // 9.5 - 13 Gyr
}

[ExecuteAlways]
public class CosmicTimelineManager : MonoBehaviour
{
    public static CosmicTimelineManager Instance { get; private set; }

    [SerializeField, Range(0f, 13f)] private float _editorPreviewGyr = 4.5f;

    public float cosmicTimeGyr { get; private set; } = 4.5f;

    public event Action<float, SolarStage> OnCosmicTimeChanged;

    SolarStage previousStage = SolarStage.MainSequence;
    readonly HashSet<float> triggeredMilestones = new();

    static readonly (float start, float end, SolarStage stage)[] Stages =
    {
        (0f,   5f,   SolarStage.MainSequence),
        (5f,   6f,   SolarStage.SubGiant),
        (6f,   8.5f, SolarStage.RedGiant),
        (8.5f, 9.5f, SolarStage.PlanetaryNebula),
        (9.5f, 13f,  SolarStage.WhiteDwarf),
    };

    static readonly Dictionary<SolarStage, (string message, Color color)> StageMessages = new()
    {
        [SolarStage.SubGiant]        = ("T+5.2 Gyr: Sun expanding — hydrogen reserves depleting.",       new Color(1.00f, 0.85f, 0.20f)),
        [SolarStage.RedGiant]        = ("T+6.1 Gyr: RED GIANT PHASE. Mercury and Venus consumed.",       new Color(1.00f, 0.25f, 0.10f)),
        [SolarStage.PlanetaryNebula] = ("T+8.7 Gyr: Solar ejection detected. Planetary Nebula forming.", new Color(0.70f, 0.30f, 1.00f)),
        [SolarStage.WhiteDwarf]      = ("T+9.6 Gyr: White Dwarf confirmed. System stabilized.",          new Color(0.40f, 0.85f, 1.00f)),
    };

    static readonly (float gyr, string message, Color color)[] Milestones =
    {
        (4.5f, "T+4.5 Gyr: Present day. Solar System at peak stability.",          new Color(0.30f, 1.00f, 0.40f)),
        (7.0f, "T+7.0 Gyr: Earth's oceans evaporated. Surface temperature 1200°C.", new Color(1.00f, 0.55f, 0.00f)),
        (8.0f, "T+8.0 Gyr: Earth consumed by expanding Sun.",                       new Color(1.00f, 0.25f, 0.10f)),
    };

    void Awake()
    {
        Instance = this;
    }

    void OnEnable()
    {
        Instance = this;
    }

    void Update()
    {
        if (!Application.isPlaying)
        {
            if (Mathf.Abs(cosmicTimeGyr - _editorPreviewGyr) > 0.001f)
            {
                SetCosmicTime(_editorPreviewGyr);
            }
        }
    }

    public SolarStage GetCurrentStage()
{
        foreach (var (start, end, stage) in Stages)
            if (cosmicTimeGyr < end) return stage;
        return SolarStage.WhiteDwarf;
    }

    public float GetStageProgress()
    {
        foreach (var (start, end, stage) in Stages)
        {
            if (cosmicTimeGyr < end)
                return Mathf.Clamp01((cosmicTimeGyr - start) / (end - start));
        }
        return 1f;
    }

    public void SetCosmicTime(float gyr)
    {
        float prev = cosmicTimeGyr;
        cosmicTimeGyr = Mathf.Clamp(gyr, 0f, 13f);

        SolarStage newStage = GetCurrentStage();

        // Stage transition — fire only when advancing forward into a new stage
        if (newStage != previousStage)
        {
            if (newStage > previousStage && StageMessages.TryGetValue(newStage, out var sm))
            {
                MissionLog.Instance?.AddEntry(sm.message, sm.color);
                HUDController.Instance?.FlashScreen(sm.color);
            }
            previousStage = newStage;
        }

        // Milestone crossings — fire going forward, reset going backward
        foreach (var (milestone, message, color) in Milestones)
        {
            if (prev < milestone && cosmicTimeGyr >= milestone)
            {
                triggeredMilestones.Add(milestone);
                MissionLog.Instance?.AddEntry(message, color);
            }
            else if (prev >= milestone && cosmicTimeGyr < milestone)
            {
                triggeredMilestones.Remove(milestone);
            }
        }

        OnCosmicTimeChanged?.Invoke(cosmicTimeGyr, newStage);
    }
}
