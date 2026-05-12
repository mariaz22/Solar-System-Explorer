using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PlanetSelectionUI : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Dropdown dropdown;
    public TextMeshProUGUI infoText;
    public Button sendProbeButton;

    [Header("Scene References")]
    public Camera mainCamera;
    public ProbeController probe;

    Planet selected;
    CanvasGroup canvasGroup;

    void Start()
    {
        canvasGroup = GetComponentInParent<Canvas>()?.gameObject.GetComponent<CanvasGroup>()
            ?? GetComponentInParent<Canvas>()?.gameObject.AddComponent<CanvasGroup>();
        if (mainCamera == null) mainCamera = Camera.main;

        PopulateDropdown();

        if (dropdown != null) dropdown.onValueChanged.AddListener(OnDropdownChanged);
        if (sendProbeButton != null) sendProbeButton.onClick.AddListener(OnSendProbe);

        var list = PlanetManager.Instance != null ? PlanetManager.Instance.planets : null;
        if (list != null && list.Count > 0) Select(list[0]);
    }

    void Update()
    {
        bool screenActive = StartScreen.IsActive;
        if (canvasGroup != null)
        {
            canvasGroup.alpha = screenActive ? 0f : 1f;
            canvasGroup.blocksRaycasts = !screenActive;
        }
        if (screenActive) return;
        if (mainCamera == null || Mouse.current == null) return;
        if (!Mouse.current.leftButton.wasPressedThisFrame) return;
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            var p = hit.collider.GetComponent<Planet>();
            if (p != null) Select(p);
        }
    }

    void PopulateDropdown()
    {
        if (dropdown == null || PlanetManager.Instance == null) return;

        dropdown.ClearOptions();
        var names = new List<string>();
        foreach (var p in PlanetManager.Instance.planets)
            if (p != null) names.Add(p.data.planetName);
        dropdown.AddOptions(names);
    }

    void OnDropdownChanged(int idx)
    {
        if (PlanetManager.Instance == null) return;
        var planets = PlanetManager.Instance.planets;
        if (idx >= 0 && idx < planets.Count) Select(planets[idx]);
    }

    public void Select(Planet p)
    {
        if (p == null) return;

        if (selected != null) selected.SetSelected(false);
        selected = p;
        selected.SetSelected(true);

        UpdateInfo();
        if (TargetIndicator.Instance != null)
        {
            TargetIndicator.Instance.SetTarget(p);
        }
        else
        {
            Debug.LogWarning("TargetIndicator.Instance is null during Select!");
        }

        if (CameraController.Instance != null) CameraController.Instance.MoveTo(p);

        if (dropdown != null && PlanetManager.Instance != null)
        {
            int i = PlanetManager.Instance.planets.IndexOf(p);
            if (i >= 0 && dropdown.value != i) dropdown.SetValueWithoutNotify(i);
        }
    }

    void UpdateInfo()
    {
        if (infoText == null || selected == null) return;

        var d = selected.data;
        string probeStatus = probe != null && probe.TargetReason != ""
            ? $"\n<i>{probe.TargetReason}</i>"
            : "";

        bool destroyed = !selected.gameObject.activeSelf;
        string status  = destroyed ? "<color=#ff4444>Destroyed</color>"
                       : d.explored ? "Explored"
                       : "Unexplored";

        infoText.text =
            $"<b>{d.planetName}</b>\n" +
            $"Mass (Earth=1): {d.relativeMass:0.##}\n" +
            $"Distance from Sun (AU): {d.distanceFromSun:0.##}\n" +
            $"Status: {status}" +
            probeStatus;
    }

    void OnSendProbe()
    {
        if (probe == null || selected == null) return;
        if (!selected.gameObject.activeSelf)
        {
            HUDController.Instance?.ShowNotification(
                $"[ERROR] {selected.data.planetName} was destroyed — cannot scan.",
                new Color(1f, 0.3f, 0.3f));
            return;
        }
        probe.SetTarget(selected);
    }
}
