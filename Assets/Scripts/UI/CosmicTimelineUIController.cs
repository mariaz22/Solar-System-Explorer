using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CosmicTimelineUIController : MonoBehaviour
{
    public Slider           slider;
    public TextMeshProUGUI  timeLabel;
    public TextMeshProUGUI  stageLabel;

    void Start()
    {
        slider.onValueChanged.AddListener(OnSliderChanged);

        if (CosmicTimelineManager.Instance != null)
        {
            CosmicTimelineManager.Instance.OnCosmicTimeChanged += OnCosmicTimeChanged;
            CosmicTimelineManager.Instance.SetCosmicTime(slider.value);
        }
    }

    void OnDestroy()
    {
        if (CosmicTimelineManager.Instance != null)
            CosmicTimelineManager.Instance.OnCosmicTimeChanged -= OnCosmicTimeChanged;
    }

    void OnSliderChanged(float value)
    {
        CosmicTimelineManager.Instance?.SetCosmicTime(value);
    }

    void OnCosmicTimeChanged(float gyr, SolarStage stage)
    {
        if (timeLabel  != null) timeLabel.text  = FormatTime(gyr, stage);
        if (stageLabel != null)
        {
            stageLabel.text  = FormatStage(stage);
            stageLabel.color = StageColor(stage);
        }
    }

    // ── Formatters ──────────────────────────────────────────────────

    static string FormatTime(float gyr, SolarStage stage)
    {
        string tag = (stage == SolarStage.MainSequence && Mathf.Abs(gyr - 4.5f) < 0.15f)
            ? "  PRESENT DAY" : "";
        return $"{gyr:F1} GYR{tag}";
    }

    static string FormatStage(SolarStage stage) => stage switch
    {
        SolarStage.MainSequence    => "●  MAIN SEQUENCE",
        SolarStage.SubGiant        => "●  SUB-GIANT",
        SolarStage.RedGiant        => "●  RED GIANT",
        SolarStage.PlanetaryNebula => "●  PLANETARY NEBULA",
        SolarStage.WhiteDwarf      => "●  WHITE DWARF",
        _                          => "",
    };

    public static Color StageColor(SolarStage stage) => stage switch
    {
        SolarStage.MainSequence    => new Color(0.30f, 1.00f, 0.40f),
        SolarStage.SubGiant        => new Color(1.00f, 0.85f, 0.20f),
        SolarStage.RedGiant        => new Color(1.00f, 0.25f, 0.10f),
        SolarStage.PlanetaryNebula => new Color(0.70f, 0.30f, 1.00f),
        SolarStage.WhiteDwarf      => new Color(0.40f, 0.85f, 1.00f),
        _                          => Color.white,
    };
}
