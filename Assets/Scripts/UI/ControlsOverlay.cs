using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ControlsOverlay : MonoBehaviour
{
    public static ControlsOverlay Instance { get; private set; }

    CanvasGroup group;
    bool visible;

    public void Setup()
    {
        Instance = this;

        var canvas = GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;

        var scaler = GetComponent<CanvasScaler>();
        if (scaler != null)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
        }

        group = gameObject.GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();

        var root = GetComponent<RectTransform>();

        // Full-screen backdrop — clicking it closes the overlay
        var backdrop = R("Backdrop", root);
        backdrop.anchorMin = Vector2.zero; backdrop.anchorMax = Vector2.one;
        backdrop.offsetMin = backdrop.offsetMax = Vector2.zero;
        var bdImg = I(backdrop, new Color(0f, 0f, 0f, 0.75f));
        var bdBtn = backdrop.gameObject.AddComponent<Button>();
        bdBtn.targetGraphic = bdImg;
        var bdBc = bdBtn.colors;
        bdBc.normalColor     = new Color(0f, 0f, 0f, 0.75f);
        bdBc.highlightedColor = new Color(0f, 0f, 0f, 0.80f);
        bdBc.pressedColor    = new Color(0f, 0f, 0f, 0.85f);
        bdBtn.colors = bdBc;
        bdBtn.onClick.AddListener(Hide);

        // Center panel 600x480
        var panel = R("Panel", root);
        panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(0.5f, 0.5f);
        panel.anchoredPosition = Vector2.zero;
        panel.sizeDelta = new Vector2(600, 480);
        I(panel, new Color(0.04f, 0.07f, 0.14f, 0.97f));

        // Top accent
        var accent = R("Accent", panel);
        accent.anchorMin = new Vector2(0, 1); accent.anchorMax = new Vector2(1, 1);
        accent.pivot = new Vector2(0.5f, 1); accent.anchoredPosition = Vector2.zero;
        accent.sizeDelta = new Vector2(0, 3);
        I(accent, new Color(0.2f, 0.55f, 1f, 0.9f));

        // Title
        var titleR = R("Title", panel);
        titleR.anchorMin = new Vector2(0, 0.85f); titleR.anchorMax = new Vector2(1, 1f);
        titleR.offsetMin = titleR.offsetMax = Vector2.zero;
        var t = T(titleR, "CONTROLS", 32, new Color(0.25f, 0.70f, 1f));
        t.fontStyle = FontStyles.Bold; t.characterSpacing = 6f;

        // Divider
        var div = R("Div", panel);
        div.anchorMin = new Vector2(0.05f, 0.845f); div.anchorMax = new Vector2(0.95f, 0.845f);
        div.sizeDelta = new Vector2(0, 1);
        I(div, new Color(0.25f, 0.5f, 0.85f, 0.45f));

        // Controls text
        var ctrlR = R("Controls", panel);
        ctrlR.anchorMin = new Vector2(0.06f, 0.10f); ctrlR.anchorMax = new Vector2(0.94f, 0.845f);
        ctrlR.offsetMin = ctrlR.offsetMax = Vector2.zero;
        var ctrl = T(ctrlR, ControlsText(), 16, new Color(0.70f, 0.85f, 1f, 0.9f));
        ctrl.alignment = TextAlignmentOptions.TopLeft;
        ctrl.textWrappingMode = TextWrappingModes.Normal;
        ctrl.lineSpacing = 6f;
        ctrl.richText = true;

        // Close hint at bottom
        var hintR = R("Hint", panel);
        hintR.anchorMin = new Vector2(0, 0); hintR.anchorMax = new Vector2(1, 0);
        hintR.pivot = new Vector2(0.5f, 0); hintR.anchoredPosition = new Vector2(0, 8);
        hintR.sizeDelta = new Vector2(0, 22);
        T(hintR, "Press  H  or  ESC  to close  ·  or click outside", 11,
            new Color(0.4f, 0.55f, 0.7f, 0.8f)).characterSpacing = 1f;

        // Start hidden
        Hide();
    }

    static string ControlsText() =>
        "<b>CAMERA</b>\n" +
        "    RMB + drag          Look around\n" +
        "    W / A / S / D       Fly forward / left / back / right\n" +
        "    Q / E               Fly down / up\n" +
        "    Left Shift          Move faster\n\n" +
        "<b>PROBE</b>\n" +
        "    Dropdown + SEND PROBE   Dispatch probe to selected planet\n" +
        "    Click on planet     Select target\n\n" +
        "<b>SIMULATION</b>\n" +
        "    Time slider         Control speed  (1x – 100x)\n" +
        "    PAUSE button        Pause / resume\n\n" +
        "<b>UI</b>\n" +
        "    H  or  ?            Toggle this overlay\n" +
        "    ESC                 Close overlay";

    public void Toggle()
    {
        if (visible) Hide(); else Show();
    }

    public void Show()
    {
        visible = true;
        group.alpha = 1f;
        group.blocksRaycasts = true;
        group.interactable = true;
    }

    public void Hide()
    {
        visible = false;
        group.alpha = 0f;
        group.blocksRaycasts = false;
        group.interactable = false;
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        if (kb.hKey.wasPressedThisFrame)
            Toggle();
        else if (visible && kb.escapeKey.wasPressedThisFrame)
            Hide();
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
