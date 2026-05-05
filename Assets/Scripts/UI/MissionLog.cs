using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MissionLog : MonoBehaviour
{
    public static MissionLog Instance { get; private set; }

    static readonly Dictionary<string, (string temp, string atmo, string fact)> PlanetFacts = new()
    {
        { "Mercury", ("-180°C / +430°C", "None",            "Smallest planet in the solar system") },
        { "Venus",   ("+465°C",           "CO2 96%",         "Hottest planet despite not being closest to Sun") },
        { "Earth",   ("+15°C avg",        "N2 78%, O2 21%",  "Only known planet with life") },
        { "Mars",    ("-60°C avg",         "CO2 95%",         "Home to Olympus Mons, tallest volcano") },
        { "Jupiter", ("-110°C clouds",    "H2 90%, He 10%",  "1300 Earths could fit inside") },
        { "Saturn",  ("-140°C",           "H2 96%",          "Least dense planet — could float on water") },
        { "Uranus",  ("-195°C",           "H2, He, CH4",     "Rotates on its side, 98 degree axial tilt") },
        { "Neptune", ("-200°C",           "H2, He, CH4",     "Strongest winds in solar system: 2100 km/h") },
    };

    readonly List<(string time, string msg, Color col)> entries = new();
    const int MaxEntries = 8;

    float elapsed;
    TextMeshProUGUI logText;

    // ── Public API ───────────────────────────────────────────────

    public void AddEntry(string message, Color color)
    {
        int m = (int)(elapsed / 60), s = (int)(elapsed % 60);
        entries.Insert(0, ($"T+{m:00}:{s:00}", message, color));
        if (entries.Count > MaxEntries) entries.RemoveAt(entries.Count - 1);
        RefreshText();
    }

    public static string GetScanSummary(string planetName)
    {
        if (PlanetFacts.TryGetValue(planetName, out var f))
            return $"Temp: {f.temp}  |  Atmo: {f.atmo}\n          {f.fact}.";
        return "Data unavailable.";
    }

    // ── Lifecycle ────────────────────────────────────────────────

    void Awake()
    {
        Instance = this;
        BuildUI();
    }

    void Update()
    {
        elapsed += Time.deltaTime;
    }

    // ── UI Construction ──────────────────────────────────────────

    void BuildUI()
    {
        var canvasGO = new GameObject("MissionLogCanvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 45;
        canvasGO.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasGO.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
        canvasGO.AddComponent<GraphicRaycaster>();

        var root = canvasGO.GetComponent<RectTransform>();

        // Outer panel — bottom left
        var panel = MakeRect("LogPanel", root);
        panel.anchorMin = new Vector2(0f, 0f);
        panel.anchorMax = new Vector2(0f, 0f);
        panel.pivot     = new Vector2(0f, 0f);
        panel.anchoredPosition = new Vector2(12f, 80f);
        panel.sizeDelta = new Vector2(370f, 280f);

        var bg = panel.gameObject.AddComponent<Image>();
        bg.color = new Color(0.03f, 0.06f, 0.12f, 0.88f);

        // Top border
        var border = MakeRect("Border", panel);
        border.anchorMin = new Vector2(0, 1); border.anchorMax = new Vector2(1, 1);
        border.pivot = new Vector2(0.5f, 1);
        border.anchoredPosition = Vector2.zero; border.sizeDelta = new Vector2(0, 2);
        border.gameObject.AddComponent<Image>().color = new Color(0.2f, 0.5f, 1f, 0.5f);

        // Title
        var titleRT = MakeRect("Title", panel);
        titleRT.anchorMin = new Vector2(0, 1); titleRT.anchorMax = new Vector2(1, 1);
        titleRT.pivot = new Vector2(0.5f, 1);
        titleRT.anchoredPosition = new Vector2(0, -2); titleRT.sizeDelta = new Vector2(0, 24);
        var title = titleRT.gameObject.AddComponent<TextMeshProUGUI>();
        title.text = "//  MISSION LOG";
        title.fontSize = 11; title.color = new Color(0.3f, 0.65f, 1f, 0.9f);
        title.alignment = TextAlignmentOptions.MidlineLeft;
        title.fontStyle = FontStyles.Bold; title.characterSpacing = 2f;
        title.margin = new Vector4(8, 0, 0, 0);

        // Divider under title
        var div = MakeRect("Div", panel);
        div.anchorMin = new Vector2(0, 1); div.anchorMax = new Vector2(1, 1);
        div.pivot = new Vector2(0.5f, 1);
        div.anchoredPosition = new Vector2(0, -26); div.sizeDelta = new Vector2(-16, 1);
        div.gameObject.AddComponent<Image>().color = new Color(0.2f, 0.45f, 0.8f, 0.3f);

        // Log text area
        var textRT = MakeRect("LogText", panel);
        textRT.anchorMin = Vector2.zero; textRT.anchorMax = Vector2.one;
        textRT.offsetMin = new Vector2(8, 6); textRT.offsetMax = new Vector2(-8, -30);
        logText = textRT.gameObject.AddComponent<TextMeshProUGUI>();
        logText.fontSize = 10.5f;
        logText.color = Color.white;
        logText.alignment = TextAlignmentOptions.TopLeft;
        logText.textWrappingMode = TextWrappingModes.Normal;
        logText.richText = true;
        logText.lineSpacing = 8f;

        AddEntry("Mission started. Probe standing by.", new Color(0.5f, 0.8f, 1f));
    }

    void RefreshText()
    {
        if (logText == null) return;

        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < entries.Count; i++)
        {
            var (time, msg, col) = entries[i];
            string hex = ColorUtility.ToHtmlStringRGB(col);
            float alpha = 1f - i * 0.1f; // older entries slightly faded
            string alphaHex = ((int)(alpha * 255)).ToString("X2");
            sb.Append($"<color=#{hex}{alphaHex}><size=9><color=#4d8fcc>{time}</color>  {msg}</size></color>");
            if (i < entries.Count - 1) sb.Append("\n\n");
        }
        logText.text = sb.ToString();
    }

    static RectTransform MakeRect(string name, RectTransform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        go.GetComponent<RectTransform>().localScale = Vector3.one;
        return go.GetComponent<RectTransform>();
    }
}
