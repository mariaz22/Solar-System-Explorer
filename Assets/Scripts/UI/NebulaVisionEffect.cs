using UnityEngine;
using UnityEngine.UI;

public class NebulaVisionEffect : MonoBehaviour
{
    private Image overlay;
    private float targetAlpha = 0f;
    private Color targetColor = Color.clear;
    private float currentAlpha = 0f;

    void Start()
    {
        var canvasGo = new GameObject("NebulaVisionCanvas", typeof(Canvas), typeof(CanvasScaler));
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999; // Above everything

        var overlayGo = new GameObject("NebulaOverlay", typeof(Image));
        overlayGo.transform.SetParent(canvasGo.transform, false);
        overlay = overlayGo.GetComponent<Image>();
        overlay.color = Color.clear;
        
        RectTransform rt = overlay.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;
    }

    void Update()
    {
        CheckNebulaeProximity();
        
        currentAlpha = Mathf.MoveTowards(currentAlpha, targetAlpha, Time.deltaTime * 0.5f);
        overlay.color = new Color(targetColor.r, targetColor.g, targetColor.b, currentAlpha);
    }

    void CheckNebulaeProximity()
    {
        var nebulae = Object.FindObjectsByType<ReactiveNebula>(FindObjectsInactive.Exclude);
        float maxAgitation = 0f;
        Color bestColor = Color.clear;
        bool inside = false;

        Vector3 camPos = Camera.main.transform.position;

        foreach (var n in nebulae)
        {
            float dist = Vector3.Distance(camPos, n.transform.position);
            if (dist < n.radius * 1.2f)
            {
                float t = Mathf.Clamp01(1f - (dist / (n.radius * 1.2f)));
                if (t > maxAgitation)
                {
                    maxAgitation = t;
                    bestColor = n.currentState == ReactiveNebula.NebulaState.Danger ? n.dangerColor : 
                               (n.currentState == ReactiveNebula.NebulaState.Warning ? n.warningColor : n.calmColor);
                }
                inside = true;
            }
        }

        if (inside)
        {
            targetAlpha = maxAgitation * 0.45f;
            targetColor = bestColor;
        }
        else
        {
            targetAlpha = 0f;
        }
    }
}
