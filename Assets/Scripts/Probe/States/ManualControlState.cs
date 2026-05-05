using UnityEngine;
using UnityEngine.InputSystem;

public class ManualControlState : State
{
    readonly ProbeController probe;
    readonly State returnState;

    Planet scanTarget;
    float scanProgress;
    bool enteredScanRange;

    const float MoveSpeed  = 50f;
    const float BoostSpeed = 220f;
    const float ScanTime   = 2.5f;
    const float ScanMargin = 8f;

    public ManualControlState(ProbeController probe, State returnState)
    {
        this.probe = probe;
        this.returnState = returnState;
    }

    public override void OnEnter()
    {
        scanTarget      = null;
        scanProgress    = 0f;
        enteredScanRange = false;
        CameraController.Instance?.StartFollowing(probe.transform);
        HUDController.Instance?.ShowNotification(
            "[MANUAL] WASD/QE: fly  |  SHIFT: boost  |  apropiati-va de o planeta pentru scan  |  TAB: standby",
            new Color(1f, 0.9f, 0.15f), 5f);
        if (returnState != null)
            MissionLog.Instance?.AddEntry("Manual control activated.", new Color(1f, 0.85f, 0.2f));
    }

    public override void OnUpdate()
    {
        var kb = Keyboard.current;
        if (kb.tabKey.wasPressedThisFrame)
        {
            probe.FSM.ChangeState(returnState ?? new IdleState(probe));
            return;
        }

        HandleMovement(kb);
        HandleScan();
    }

    void HandleMovement(Keyboard kb)
    {
        Camera cam = Camera.main;
        Vector3 forward = cam != null ? cam.transform.forward : Vector3.forward;
        Vector3 right   = cam != null ? cam.transform.right   : Vector3.right;
        forward.y = 0f;
        if (forward.sqrMagnitude > 0f) forward.Normalize();
        right.y = 0f;
        if (right.sqrMagnitude > 0f) right.Normalize();

        Vector3 move = Vector3.zero;
        if (kb.wKey.isPressed || kb.upArrowKey.isPressed)    move += forward;
        if (kb.sKey.isPressed || kb.downArrowKey.isPressed)  move -= forward;
        if (kb.aKey.isPressed || kb.leftArrowKey.isPressed)  move -= right;
        if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) move += right;
        if (kb.qKey.isPressed) move += Vector3.up;
        if (kb.eKey.isPressed) move -= Vector3.up;

        if (move.sqrMagnitude > 0.01f)
        {
            bool boost = kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed;
            float speed = boost ? BoostSpeed : MoveSpeed;
            probe.transform.position += move.normalized * speed * Time.deltaTime;

            // Rotate probe only on horizontal plane so camera stays behind, not below
            Vector3 horizontal = new Vector3(move.x, 0f, move.z);
            if (horizontal.sqrMagnitude > 0.01f)
                probe.transform.forward = Vector3.Slerp(
                    probe.transform.forward, horizontal.normalized, Time.deltaTime * 8f);
        }
    }

    void HandleScan()
    {
        if (PlanetManager.Instance == null) return;

        Planet nearest = null;
        float nearestDist = float.MaxValue;
        foreach (var planet in PlanetManager.Instance.planets)
        {
            if (planet == null || planet.data.explored) continue;
            float dist = Vector3.Distance(probe.transform.position, planet.transform.position);
            if (dist < planet.radius + ScanMargin && dist < nearestDist)
            {
                nearest = planet;
                nearestDist = dist;
            }
        }

        if (nearest == null)
        {
            if (scanTarget != null)
            {
                // Left scan range before completing
                HUDController.Instance?.ShowNotification(
                    $"[SCAN ABORTED] Prea departe de {scanTarget.data.planetName}",
                    new Color(1f, 0.4f, 0.2f), 2f);
                probe.Target = null;
                scanTarget      = null;
                scanProgress    = 0f;
                enteredScanRange = false;
            }
            return;
        }

        if (nearest != scanTarget)
        {
            scanTarget      = nearest;
            scanProgress    = 0f;
            enteredScanRange = false;
        }

        // First frame entering range — announce it
        if (!enteredScanRange)
        {
            enteredScanRange = true;
            probe.Target = scanTarget;
            HUDController.Instance?.ShowNotification(
                $"[IN RANGE] {scanTarget.data.planetName} detectata — mentineti pozitia pentru scan!",
                new Color(0.4f, 1f, 0.7f), 3f);
            MissionLog.Instance?.AddEntry(
                $"In orbit de {scanTarget.data.planetName}. Scanare initiata...",
                new Color(0.4f, 1f, 0.7f));
        }

        scanProgress += Time.deltaTime;
        float pct = Mathf.Clamp01(scanProgress / ScanTime);
        int pctInt = (int)(pct * 100f);

        // Progress bar using ASCII blocks
        int filled = (int)(pct * 20f);
        string bar = "[" + new string('#', filled) + new string('-', 20 - filled) + "]";
        HUDController.Instance?.ShowNotification(
            $"[SCANNING] {scanTarget.data.planetName}  {bar}  {pctInt}%",
            new Color(0.2f, 1f, 0.6f), 0.2f);

        if (scanProgress >= ScanTime)
            CompleteScan();
    }

    void CompleteScan()
    {
        string name = scanTarget.data.planetName;
        scanTarget.data.explored = true;
        scanTarget.SetExplored();
        scanTarget.GetComponent<ScanEffect>()?.Play(scanTarget.radius);

        string summary = MissionLog.GetScanSummary(name);
        MissionLog.Instance?.AddEntry(
            $"Scan complet: <b>{name}</b>. {summary}",
            new Color(0.3f, 1f, 0.55f));
        HUDController.Instance?.ShowNotification(
            $"[SCAN COMPLET] {name} explorata cu succes!",
            new Color(0.3f, 1f, 0.55f), 5f);
        HUDController.Instance?.FlashScreen(new Color(0.1f, 0.8f, 0.4f));

        probe.Target = null;
        scanTarget      = null;
        scanProgress    = 0f;
        enteredScanRange = false;

        if (probe.AllPlanetsExplored())
        {
            HUDController.Instance?.ShowNotification(
                "[MISIUNE COMPLETA] Toate planetele au fost explorate!",
                new Color(1f, 0.9f, 0.1f), 8f);
            MissionLog.Instance?.AddEntry(
                "Toate planetele explorate. Misiune incheiata cu succes!",
                new Color(1f, 0.85f, 0.2f));
        }
    }

    public override void OnExit()
    {
        probe.Target = null;
        CameraController.Instance?.StopFollowing();
        if (returnState != null)
            MissionLog.Instance?.AddEntry("Auto-pilot re-engaged.", new Color(0.4f, 0.8f, 1f));
    }
}
