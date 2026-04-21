using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StartScreen : MonoBehaviour
{
    public static bool IsActive => instance != null && instance.gameObject.activeSelf;
    static StartScreen instance;

    CanvasGroup group;
    bool launching;
    float fadeT;
    TextMeshProUGUI titleText;
    float pulse;

    public void Setup()
    {
        instance = this;
        group = gameObject.AddComponent<CanvasGroup>();

        var canvas = GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        var scaler = GetComponent<CanvasScaler>();
        if (scaler != null)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
        }

        var root = GetComponent<RectTransform>();

        // Full-screen dark bg
        var bg = MakeRect("BG", root);
        bg.anchorMin = Vector2.zero; bg.anchorMax = Vector2.one;
        bg.offsetMin = bg.offsetMax = Vector2.zero;
        bg.gameObject.AddComponent<Image>().color = new Color(0.02f, 0.04f, 0.09f, 0.97f);

        // Center card
        var card = MakeRect("Card", root);
        card.anchorMin = new Vector2(0.5f, 0.5f); card.anchorMax = new Vector2(0.5f, 0.5f);
        card.pivot = new Vector2(0.5f, 0.5f); card.anchoredPosition = Vector2.zero;
        card.sizeDelta = new Vector2(640, 520);
        var cardImg = card.gameObject.AddComponent<Image>();
        cardImg.color = new Color(0.04f, 0.08f, 0.16f, 0.92f);

        // Card border top
        var borderT = MakeRect("BT", card);
        borderT.anchorMin = new Vector2(0, 1); borderT.anchorMax = new Vector2(1, 1);
        borderT.pivot = new Vector2(0.5f, 1); borderT.anchoredPosition = Vector2.zero; borderT.sizeDelta = new Vector2(0, 2);
        borderT.gameObject.AddComponent<Image>().color = new Color(0.2f, 0.55f, 1f, 0.8f);

        // Card border bottom
        var borderB = MakeRect("BB", card);
        borderB.anchorMin = new Vector2(0, 0); borderB.anchorMax = new Vector2(1, 0);
        borderB.pivot = new Vector2(0.5f, 0); borderB.anchoredPosition = Vector2.zero; borderB.sizeDelta = new Vector2(0, 2);
        borderB.gameObject.AddComponent<Image>().color = new Color(0.2f, 0.55f, 1f, 0.8f);

        // Title
        var titleGO = MakeRect("Title", card);
        titleGO.anchorMin = new Vector2(0, 0.72f); titleGO.anchorMax = new Vector2(1, 0.97f);
        titleGO.offsetMin = titleGO.offsetMax = Vector2.zero;
        titleText = titleGO.gameObject.AddComponent<TextMeshProUGUI>();
        titleText.text = "SOLAR SYSTEM EXPLORER";
        titleText.fontSize = 52; titleText.color = new Color(0.25f, 0.70f, 1f);
        titleText.alignment = TextAlignmentOptions.Center; titleText.fontStyle = FontStyles.Bold;
        titleText.characterSpacing = 4f;

        // Subtitle
        var subGO = MakeRect("Sub", card);
        subGO.anchorMin = new Vector2(0, 0.60f); subGO.anchorMax = new Vector2(1, 0.73f);
        subGO.offsetMin = subGO.offsetMax = Vector2.zero;
        var sub = subGO.gameObject.AddComponent<TextMeshProUGUI>();
        sub.text = "\u2014\u2014\u2014  AUTONOMOUS PROBE MISSION SYSTEM  \u2014\u2014\u2014";
        sub.fontSize = 12; sub.color = new Color(0.40f, 0.60f, 0.88f, 0.75f);
        sub.alignment = TextAlignmentOptions.Center; sub.characterSpacing = 4f;

        // Divider
        var div = MakeRect("Div", card);
        div.anchorMin = new Vector2(0.08f, 0.585f); div.anchorMax = new Vector2(0.92f, 0.585f);
        div.sizeDelta = new Vector2(0, 1);
        div.gameObject.AddComponent<Image>().color = new Color(0.25f, 0.5f, 0.85f, 0.45f);

        // Instructions
        var instrGO = MakeRect("Instr", card);
        instrGO.anchorMin = new Vector2(0.06f, 0.20f); instrGO.anchorMax = new Vector2(0.94f, 0.58f);
        instrGO.offsetMin = instrGO.offsetMax = Vector2.zero;
        var instr = instrGO.gameObject.AddComponent<TextMeshProUGUI>();
        instr.text =
            "\u00BB   Select a planet from the dropdown \u2014 or click on it\n" +
            "\u00BB   Press  SEND PROBE HERE  to dispatch the probe\n" +
            "\u00BB   Probe auto-scans each planet autonomously\n" +
            "\u00AB   Returns to base after full system exploration\n" +
            "\u00B7   RMB + WASD / QE  \u2014  free camera flight";
        instr.enableAutoSizing = true;
        instr.fontSizeMin = 8f; instr.fontSizeMax = 16f;
        instr.color = new Color(0.6f, 0.75f, 0.92f, 0.88f);
        instr.alignment = TextAlignmentOptions.Left;
        instr.textWrappingMode = TextWrappingModes.Normal;
        instr.overflowMode = TextOverflowModes.Truncate;

        // Launch button bg
        var btnBG = MakeRect("BtnBG", card);
        btnBG.anchorMin = new Vector2(0.15f, 0.05f); btnBG.anchorMax = new Vector2(0.85f, 0.19f);
        btnBG.offsetMin = btnBG.offsetMax = Vector2.zero;
        btnBG.gameObject.AddComponent<Image>().color = new Color(0.08f, 0.22f, 0.50f, 0.95f);

        // Launch button
        btnBG.gameObject.AddComponent<Button>().onClick.AddListener(Launch);

        var btnTxtGO = MakeRect("BtnTxt", btnBG);
        btnTxtGO.anchorMin = Vector2.zero; btnTxtGO.anchorMax = Vector2.one;
        btnTxtGO.offsetMin = btnTxtGO.offsetMax = Vector2.zero;
        var btnTxt = btnTxtGO.gameObject.AddComponent<TextMeshProUGUI>();
        btnTxt.text = ">>  LAUNCH MISSION";
        btnTxt.fontSize = 22; btnTxt.color = new Color(0.35f, 0.85f, 1f);
        btnTxt.alignment = TextAlignmentOptions.Center; btnTxt.fontStyle = FontStyles.Bold;
        btnTxt.characterSpacing = 3f;

        // Version tag
        var verGO = MakeRect("Ver", card);
        verGO.anchorMin = new Vector2(0, 0); verGO.anchorMax = new Vector2(1, 0);
        verGO.pivot = new Vector2(0.5f, 0); verGO.anchoredPosition = new Vector2(0, 5); verGO.sizeDelta = new Vector2(0, 18);
        var ver = verGO.gameObject.AddComponent<TextMeshProUGUI>();
        ver.text = "v1.0  ·  2026  ·  University Project";
        ver.fontSize = 10; ver.color = new Color(0.3f, 0.4f, 0.55f, 0.7f);
        ver.alignment = TextAlignmentOptions.Center;
    }

    void Launch() => launching = true;

    void Update()
    {
        pulse += Time.deltaTime;
        if (titleText != null)
        {
            float b = 0.85f + Mathf.Sin(pulse * 1.4f) * 0.15f;
            titleText.color = new Color(0.25f * b, 0.70f * b, 1f * b);
        }

        if (launching)
        {
            fadeT += Time.deltaTime * 1.8f;
            if (group != null) group.alpha = Mathf.Lerp(1f, 0f, fadeT);
            if (fadeT >= 1f) { instance = null; Destroy(gameObject); }
        }
    }

    static RectTransform MakeRect(string name, RectTransform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        go.GetComponent<RectTransform>().localScale = Vector3.one;
        return go.GetComponent<RectTransform>();
    }
}
