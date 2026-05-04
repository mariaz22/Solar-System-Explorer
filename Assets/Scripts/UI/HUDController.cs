using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HUDController : MonoBehaviour
{
    public static HUDController Instance { get; private set; }

    ProbeController probe;
    Image stateBadgeBg;
    TextMeshProUGUI stateLabel, targetLabel, progressLabel, reasonLabel, timerLabel;
    RectTransform progressFillRT;

    float elapsed;
    string prevState;
    float pulseT;

    static readonly (System.Type t, string text, Color col)[] StateMap =
    {
        (typeof(IdleState),           "*   STANDBY",    new Color(0.55f, 0.55f, 0.65f)),
        (typeof(ChooseTargetState),   "o   COMPUTING",  new Color(1.00f, 0.80f, 0.00f)),
        (typeof(TravelState),         ">>  IN TRANSIT", new Color(0.20f, 0.80f, 1.00f)),
        (typeof(ScanState),           "+   SCANNING",   new Color(0.00f, 1.00f, 0.50f)),
        (typeof(AvoidCollisionState), "!   AVOIDING",   new Color(1.00f, 0.30f, 0.20f)),
        (typeof(ReturnState),         "<<  RETURNING",  new Color(0.80f, 0.40f, 1.00f)),
    };

    public void Setup(ProbeController probeController)
    {
        Instance = this;
        probe = probeController;

        var canvas = GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;

        var scaler = GetComponent<CanvasScaler>();
        if (scaler != null)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
        }

        var root = GetComponent<RectTransform>();
        BuildBottomBar(root);
        BuildTopBanner(root);
        BuildTimer(root);
        BuildControlsHint(root);
        BuildHelpButton(root);
    }

    void BuildBottomBar(RectTransform root)
    {
        var bar = Panel("BottomBar", root, new Color(0.03f, 0.06f, 0.12f, 0.92f));
        bar.anchorMin = new Vector2(0, 0); bar.anchorMax = new Vector2(1, 0);
        bar.pivot = new Vector2(0.5f, 0);
        bar.anchoredPosition = Vector2.zero; bar.sizeDelta = new Vector2(0, 72);

        var topLine = Panel("TopLine", bar, new Color(0.2f, 0.5f, 1f, 0.45f));
        topLine.anchorMin = new Vector2(0, 1); topLine.anchorMax = new Vector2(1, 1);
        topLine.pivot = new Vector2(0.5f, 1); topLine.anchoredPosition = Vector2.zero; topLine.sizeDelta = new Vector2(0, 1);

        // State badge
        var badge = Panel("Badge", bar, new Color(0.10f, 0.15f, 0.30f, 0.70f));
        badge.anchorMin = new Vector2(0, 0); badge.anchorMax = new Vector2(0, 1);
        badge.pivot = new Vector2(0, 0.5f); badge.anchoredPosition = new Vector2(12, 0); badge.sizeDelta = new Vector2(218, -14);
        stateBadgeBg = badge.GetComponent<Image>();
        stateLabel = Lbl("StateLbl", badge, "*   STANDBY", 17, new Color(0.55f, 0.55f, 0.65f));
        stateLabel.fontStyle = FontStyles.Bold; stateLabel.characterSpacing = 1f;

        Sep(bar, 238);

        // Target
        var tgt = Panel("Target", bar, Color.clear);
        tgt.anchorMin = new Vector2(0, 0); tgt.anchorMax = new Vector2(0, 1);
        tgt.pivot = new Vector2(0, 0.5f); tgt.anchoredPosition = new Vector2(250, 0); tgt.sizeDelta = new Vector2(185, -10);

        var tCap = Lbl("TgtCap", tgt, "TARGET", 9, new Color(0.35f, 0.60f, 0.90f));
        tCap.rectTransform.anchorMin = new Vector2(0, 0.55f); tCap.rectTransform.anchorMax = Vector2.one;
        tCap.characterSpacing = 3f; tCap.alignment = TextAlignmentOptions.BottomLeft;

        targetLabel = Lbl("TgtVal", tgt, "—", 20, Color.white);
        targetLabel.fontStyle = FontStyles.Bold; targetLabel.alignment = TextAlignmentOptions.TopLeft;
        targetLabel.rectTransform.anchorMin = Vector2.zero; targetLabel.rectTransform.anchorMax = new Vector2(1, 0.55f);

        Sep(bar, 443);

        // Progress
        var prog = Panel("Progress", bar, Color.clear);
        prog.anchorMin = new Vector2(0, 0); prog.anchorMax = new Vector2(0, 1);
        prog.pivot = new Vector2(0, 0.5f); prog.anchoredPosition = new Vector2(455, 0); prog.sizeDelta = new Vector2(200, -10);

        var pCap = Lbl("ProgCap", prog, "MISSION PROGRESS", 9, new Color(0.35f, 0.60f, 0.90f));
        pCap.rectTransform.anchorMin = new Vector2(0, 0.62f); pCap.rectTransform.anchorMax = Vector2.one;
        pCap.characterSpacing = 3f; pCap.alignment = TextAlignmentOptions.BottomLeft;

        progressLabel = Lbl("ProgVal", prog, "0 / 8  EXPLORED", 14, Color.white);
        progressLabel.fontStyle = FontStyles.Bold; progressLabel.alignment = TextAlignmentOptions.TopLeft;
        progressLabel.rectTransform.anchorMin = new Vector2(0, 0.3f); progressLabel.rectTransform.anchorMax = new Vector2(1, 0.65f);

        var barBG = Panel("BarBG", prog, new Color(0.08f, 0.14f, 0.22f, 0.9f));
        barBG.anchorMin = new Vector2(0, 0); barBG.anchorMax = new Vector2(1, 0.27f);
        barBG.offsetMin = barBG.offsetMax = Vector2.zero;

        var fill = Panel("Fill", barBG, new Color(0.15f, 0.75f, 1.0f, 0.95f));
        fill.anchorMin = Vector2.zero; fill.anchorMax = new Vector2(0, 1);
        fill.offsetMin = fill.offsetMax = Vector2.zero;
        progressFillRT = fill;

        Sep(bar, 663);

        // Reason
        reasonLabel = Lbl("Reason", bar, "", 11, new Color(0.5f, 0.82f, 0.55f, 0.9f));
        reasonLabel.fontStyle = FontStyles.Italic; reasonLabel.alignment = TextAlignmentOptions.MidlineLeft;
        reasonLabel.rectTransform.anchorMin = new Vector2(0, 0); reasonLabel.rectTransform.anchorMax = new Vector2(1, 1);
        reasonLabel.rectTransform.offsetMin = new Vector2(675, 6); reasonLabel.rectTransform.offsetMax = new Vector2(-10, -6);
    }

    void BuildTopBanner(RectTransform root)
    {
        var banner = Panel("Banner", root, new Color(0.03f, 0.06f, 0.12f, 0.88f));
        banner.anchorMin = new Vector2(0.5f, 1); banner.anchorMax = new Vector2(0.5f, 1);
        banner.pivot = new Vector2(0.5f, 1); banner.anchoredPosition = Vector2.zero; banner.sizeDelta = new Vector2(460, 36);

        var bLine = Panel("BLine", banner, new Color(0.2f, 0.5f, 1f, 0.35f));
        bLine.anchorMin = new Vector2(0, 0); bLine.anchorMax = new Vector2(1, 0);
        bLine.pivot = new Vector2(0.5f, 0); bLine.anchoredPosition = Vector2.zero; bLine.sizeDelta = new Vector2(0, 1);

        var t = Lbl("Txt", banner, "//  SOLAR SYSTEM PROBE MISSION  //", 13, new Color(0.3f, 0.65f, 1f, 0.9f));
        t.characterSpacing = 2f;
    }

    void BuildTimer(RectTransform root)
    {
        var timer = Panel("Timer", root, new Color(0.03f, 0.06f, 0.12f, 0.88f));
        timer.anchorMin = new Vector2(1, 1); timer.anchorMax = new Vector2(1, 1);
        timer.pivot = new Vector2(1, 1); timer.anchoredPosition = Vector2.zero; timer.sizeDelta = new Vector2(162, 36);

        var line = Panel("Line", timer, new Color(0.2f, 0.5f, 1f, 0.35f));
        line.anchorMin = new Vector2(0, 0); line.anchorMax = new Vector2(0, 1);
        line.pivot = new Vector2(0, 0.5f); line.anchoredPosition = Vector2.zero; line.sizeDelta = new Vector2(1, 0);

        timerLabel = Lbl("Txt", timer, "T+  00:00:00", 13, new Color(0.35f, 0.9f, 0.4f));
        timerLabel.characterSpacing = 1f;
    }

    void BuildControlsHint(RectTransform root)
    {
        var hint = Panel("Hint", root, new Color(0.03f, 0.06f, 0.12f, 0.75f));
        hint.anchorMin = new Vector2(1, 0); hint.anchorMax = new Vector2(1, 0);
        hint.pivot = new Vector2(1, 0); hint.anchoredPosition = new Vector2(0, 72); hint.sizeDelta = new Vector2(330, 26);

        var line = Panel("Line", hint, new Color(0.2f, 0.5f, 1f, 0.25f));
        line.anchorMin = new Vector2(0, 1); line.anchorMax = new Vector2(1, 1);
        line.pivot = new Vector2(0.5f, 1); line.anchoredPosition = Vector2.zero; line.sizeDelta = new Vector2(0, 1);

        var t = Lbl("Txt", hint, "RMB: Look  ·  WASD/QE: Fly  ·  Click: Select Planet", 10, new Color(0.4f, 0.5f, 0.65f, 0.85f));
        t.characterSpacing = 0.3f;
    }

    void BuildHelpButton(RectTransform root)
    {
        var btnR = Panel("HelpBtn", root, new Color(0.04f, 0.08f, 0.18f, 0.90f));
        btnR.anchorMin = new Vector2(1, 1); btnR.anchorMax = new Vector2(1, 1);
        btnR.pivot = new Vector2(1, 1); btnR.anchoredPosition = new Vector2(-166, 0);
        btnR.sizeDelta = new Vector2(36, 36);

        var img = btnR.GetComponent<Image>();
        var btn = btnR.gameObject.AddComponent<UnityEngine.UI.Button>();
        btn.targetGraphic = img;
        var bc = btn.colors;
        bc.normalColor      = new Color(0.04f, 0.08f, 0.18f, 0.90f);
        bc.highlightedColor = new Color(0.10f, 0.22f, 0.48f);
        bc.pressedColor     = new Color(0.02f, 0.05f, 0.12f);
        btn.colors = bc;
        btn.onClick.AddListener(() => ControlsOverlay.Instance?.Toggle());

        var lbl = Lbl("?", btnR, "?", 20, new Color(0.25f, 0.70f, 1f));
        lbl.fontStyle = FontStyles.Bold;
    }

    void Update()
    {
        if (probe?.FSM == null) return;
        elapsed += Time.deltaTime;
        UpdateState();
        UpdateMission();
        UpdateTimer();
    }

    void UpdateState()
    {
        var cur = probe.FSM.CurrentState;
        if (cur == null) return;

        string curName = cur.GetType().Name;
        var col = new Color(0.55f, 0.55f, 0.65f);
        string text = "◉  STANDBY";
        foreach (var e in StateMap)
            if (cur.GetType() == e.t) { text = e.text; col = e.col; break; }

        if (curName != prevState) { pulseT = 1f; prevState = curName; }
        pulseT = Mathf.Max(0f, pulseT - Time.deltaTime * 2.5f);

        float b = 1f + pulseT * 0.45f;
        if (stateLabel != null) { stateLabel.text = text; stateLabel.color = col * b; }
        if (stateBadgeBg != null)
            stateBadgeBg.color = new Color(col.r * 0.15f, col.g * 0.15f, col.b * 0.15f, 0.75f + pulseT * 0.2f);
        if (targetLabel != null)
            targetLabel.text = probe.Target != null ? probe.Target.data.planetName.ToUpper() : "—";
        if (reasonLabel != null)
            reasonLabel.text = probe.TargetReason;
    }

    void UpdateMission()
    {
        if (PlanetManager.Instance == null) return;
        int total = PlanetManager.Instance.planets.Count, done = 0;
        foreach (var p in PlanetManager.Instance.planets)
            if (p != null && p.data.explored) done++;
        if (progressLabel != null) progressLabel.text = $"{done} / {total}  EXPLORED";
        if (progressFillRT != null && total > 0)
        {
            float tgt = (float)done / total;
            var cur = progressFillRT.anchorMax;
            progressFillRT.anchorMax = new Vector2(Mathf.Lerp(cur.x, tgt, Time.deltaTime * 2f), 1);
        }
    }

    void UpdateTimer()
    {
        if (timerLabel == null) return;
        int h = (int)(elapsed / 3600), m = (int)(elapsed % 3600 / 60), s = (int)(elapsed % 60);
        timerLabel.text = $"T+  {h:00}:{m:00}:{s:00}";
    }

    // ── UI helpers ──
    static RectTransform Panel(string name, RectTransform parent, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        go.GetComponent<RectTransform>().localScale = Vector3.one;
        if (color.a > 0f) go.AddComponent<Image>().color = color;
        return go.GetComponent<RectTransform>();
    }

    static TextMeshProUGUI Lbl(string name, RectTransform parent, string text, float size, Color col)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(6, 0); rt.offsetMax = new Vector2(-6, 0);
        rt.localScale = Vector3.one;
        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = text; tmp.fontSize = size; tmp.color = col;
        tmp.alignment = TextAlignmentOptions.Center;
        return tmp;
    }

    static void Sep(RectTransform bar, float x)
    {
        var sep = Panel("Sep", bar, new Color(0.25f, 0.45f, 0.70f, 0.40f));
        sep.anchorMin = new Vector2(0, 0); sep.anchorMax = new Vector2(0, 1);
        sep.pivot = new Vector2(0, 0.5f); sep.anchoredPosition = new Vector2(x, 0); sep.sizeDelta = new Vector2(1, -18);
    }
}
