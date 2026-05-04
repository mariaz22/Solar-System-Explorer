using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DefaultExecutionOrder(-100)]
public class MainMenuBootstrap : MonoBehaviour
{
    const string SimulationScene = "SampleScene";

    float cameraYaw;

    void Awake()
    {
        SetupCamera();
        SetupStarfield();
        SetupEventSystem();
        SetupUI();
    }

    void Update()
    {
        cameraYaw += 1.5f * Time.deltaTime;
        if (Camera.main != null)
            Camera.main.transform.rotation = Quaternion.Euler(8f, cameraYaw, 0f);
    }

    void SetupCamera()
    {
        var cam = Camera.main;
        if (cam == null)
        {
            var go = new GameObject("Main Camera") { tag = "MainCamera" };
            cam = go.AddComponent<Camera>();
            go.AddComponent<AudioListener>();
        }
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        cam.farClipPlane = 10000f;
        cam.transform.position = Vector3.zero;
        cam.transform.rotation = Quaternion.Euler(8f, 0f, 0f);
    }

    void SetupStarfield()
    {
        var sfGO = new GameObject("StarfieldHost");
        sfGO.AddComponent<Starfield>();
    }

    void SetupEventSystem()
    {
        if (Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }
    }

    void SetupUI()
    {
        var canvasGO = new GameObject("MainMenuCanvas",
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        var root = canvasGO.GetComponent<RectTransform>();
        BuildCard(root);
    }

    void BuildCard(RectTransform root)
    {
        var card = R("Card", root);
        card.anchorMin = card.anchorMax = card.pivot = new Vector2(0.5f, 0.5f);
        card.anchoredPosition = Vector2.zero;
        card.sizeDelta = new Vector2(640, 520);
        I(card, new Color(0.04f, 0.08f, 0.16f, 0.92f));

        var bT = R("BT", card);
        bT.anchorMin = new Vector2(0, 1); bT.anchorMax = new Vector2(1, 1);
        bT.pivot = new Vector2(0.5f, 1); bT.anchoredPosition = Vector2.zero; bT.sizeDelta = new Vector2(0, 2);
        I(bT, new Color(0.2f, 0.55f, 1f, 0.8f));

        var bB = R("BB", card);
        bB.anchorMin = new Vector2(0, 0); bB.anchorMax = new Vector2(1, 0);
        bB.pivot = new Vector2(0.5f, 0); bB.anchoredPosition = Vector2.zero; bB.sizeDelta = new Vector2(0, 2);
        I(bB, new Color(0.2f, 0.55f, 1f, 0.8f));

        var titleR = R("Title", card);
        titleR.anchorMin = new Vector2(0, 0.75f); titleR.anchorMax = new Vector2(1, 0.97f);
        titleR.offsetMin = titleR.offsetMax = Vector2.zero;
        var titleT = T(titleR, "SOLAR SYSTEM EXPLORER", 52, new Color(0.25f, 0.70f, 1f));
        titleT.fontStyle = FontStyles.Bold; titleT.characterSpacing = 4f;

        var subR = R("Sub", card);
        subR.anchorMin = new Vector2(0, 0.63f); subR.anchorMax = new Vector2(1, 0.76f);
        subR.offsetMin = subR.offsetMax = Vector2.zero;
        var sub = T(subR, "———  AUTONOMOUS PROBE MISSION SYSTEM  ———",
            12, new Color(0.40f, 0.60f, 0.88f, 0.75f));
        sub.characterSpacing = 4f;

        var div = R("Div", card);
        div.anchorMin = new Vector2(0.08f, 0.625f); div.anchorMax = new Vector2(0.92f, 0.625f);
        div.sizeDelta = new Vector2(0, 1);
        I(div, new Color(0.25f, 0.5f, 0.85f, 0.45f));

        // Start Simulation button
        var startR = R("StartBtn", card);
        startR.anchorMin = new Vector2(0.10f, 0.38f); startR.anchorMax = new Vector2(0.90f, 0.54f);
        startR.offsetMin = startR.offsetMax = Vector2.zero;
        var startImg = I(startR, new Color(0.08f, 0.22f, 0.50f, 0.95f));
        var startBtn = startR.gameObject.AddComponent<Button>();
        var startBc = startBtn.colors;
        startBc.normalColor    = new Color(0.08f, 0.22f, 0.50f, 0.95f);
        startBc.highlightedColor = new Color(0.14f, 0.36f, 0.72f);
        startBc.pressedColor   = new Color(0.04f, 0.12f, 0.30f);
        startBtn.colors = startBc; startBtn.targetGraphic = startImg;
        startBtn.onClick.AddListener(() => SceneManager.LoadScene(SimulationScene));
        var sTR = R("Txt", startR);
        sTR.anchorMin = Vector2.zero; sTR.anchorMax = Vector2.one;
        sTR.offsetMin = sTR.offsetMax = Vector2.zero;
        var sT = T(sTR, ">>  START SIMULATION", 24, new Color(0.35f, 0.85f, 1f));
        sT.fontStyle = FontStyles.Bold; sT.characterSpacing = 3f;

        // Quit button
        var qR = R("QuitBtn", card);
        qR.anchorMin = new Vector2(0.25f, 0.22f); qR.anchorMax = new Vector2(0.75f, 0.36f);
        qR.offsetMin = qR.offsetMax = Vector2.zero;
        var qImg = I(qR, new Color(0.10f, 0.06f, 0.06f, 0.85f));
        var qBtn = qR.gameObject.AddComponent<Button>();
        var qBc = qBtn.colors;
        qBc.normalColor      = new Color(0.10f, 0.06f, 0.06f, 0.85f);
        qBc.highlightedColor = new Color(0.24f, 0.10f, 0.10f);
        qBc.pressedColor     = new Color(0.04f, 0.03f, 0.03f);
        qBtn.colors = qBc; qBtn.targetGraphic = qImg;
        qBtn.onClick.AddListener(() => Application.Quit());
        var qTR = R("Txt", qR);
        qTR.anchorMin = Vector2.zero; qTR.anchorMax = Vector2.one;
        qTR.offsetMin = qTR.offsetMax = Vector2.zero;
        var qT = T(qTR, "QUIT", 18, new Color(0.90f, 0.50f, 0.50f));
        qT.fontStyle = FontStyles.Bold; qT.characterSpacing = 3f;

        var verR = R("Ver", card);
        verR.anchorMin = new Vector2(0, 0); verR.anchorMax = new Vector2(1, 0);
        verR.pivot = new Vector2(0.5f, 0); verR.anchoredPosition = new Vector2(0, 5);
        verR.sizeDelta = new Vector2(0, 18);
        T(verR, "v1.0  ·  2026  ·  University Project", 10, new Color(0.3f, 0.4f, 0.55f, 0.7f));
    }

    // ── helpers ──────────────────────────────────────────────────

    static RectTransform R(string name, RectTransform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        go.GetComponent<RectTransform>().localScale = Vector3.one;
        return go.GetComponent<RectTransform>();
    }

    static Image I(RectTransform rt, Color col)
    {
        var img = rt.gameObject.AddComponent<Image>(); img.color = col; return img;
    }

    static TextMeshProUGUI T(RectTransform rt, string text, float size, Color col)
    {
        var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
        tmp.text = text; tmp.fontSize = size; tmp.color = col;
        tmp.alignment = TextAlignmentOptions.Center;
        return tmp;
    }
}
