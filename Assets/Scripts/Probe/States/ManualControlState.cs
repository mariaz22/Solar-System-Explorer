using UnityEngine;
using UnityEngine.InputSystem;

public class ManualControlState : State
{
    readonly ProbeController probe;
    readonly State returnState;

    Planet scanTarget;
    float scanProgress;
    bool enteredScanRange;

    Vector3 _driftVelocity; // accumulated from gravity + solar wind

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
        scanTarget       = null;
        scanProgress     = 0f;
        enteredScanRange = false;
        _driftVelocity   = Vector3.zero;
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

        probe.IsThrusting = move.sqrMagnitude > 0.01f;

        if (probe.IsThrusting)
        {
            bool boost = kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed;
            float speed = boost ? BoostSpeed : MoveSpeed;
            speed *= GetNebulaSpeedMultiplier(probe.transform.position);
            probe.transform.position += move.normalized * speed * Time.deltaTime;

            Vector3 horizontal = new Vector3(move.x, 0f, move.z);
            if (horizontal.sqrMagnitude > 0.01f)
                probe.transform.forward = Vector3.Slerp(
                    probe.transform.forward, horizontal.normalized, Time.deltaTime * 8f);
        }

        // ── Environmental forces (gravity + solar wind) ──────────────
        ApplyEnvironmentalDrift();
    }

    void ApplyEnvironmentalDrift()
    {
        Vector3 force = Vector3.zero;

        // Planetary gravity: clamp mass to 0.5–12 so all planets feel noticeable
        const float G = 500f;
        if (PlanetManager.Instance != null)
        {
            foreach (var planet in PlanetManager.Instance.planets)
            {
                if (planet == null) continue;
                Vector3 dir = planet.transform.position - probe.transform.position;
                float distSq = dir.sqrMagnitude;
                if (distSq > 500f * 500f) continue;
                float mass = Mathf.Clamp(planet.data.relativeMass, 0.5f, 12f);
                float f = G * mass / distSq;
                force += dir.normalized * Mathf.Clamp(f, 0f, 40f);
            }
        }

        // Solar wind: pushes away from sun
        var wind = Object.FindAnyObjectByType<SolarWind>();
        if (wind != null) force += wind.GetWindForceAt(probe.transform.position);

        _driftVelocity += force * Time.deltaTime;

        // Thrusting bleeds off drift faster (engines fight gravity)
        float damping = probe.IsThrusting ? 0.90f : 0.988f;
        _driftVelocity *= Mathf.Pow(damping, Time.deltaTime * 60f);
        _driftVelocity  = Vector3.ClampMagnitude(_driftVelocity, 60f);

        probe.transform.position += _driftVelocity * Time.deltaTime;

        // Hard pushout: never let probe enter a planet or sun
        PushOutOfObstacles();
    }

    void PushOutOfObstacles()
    {
        if (PlanetManager.Instance != null)
            foreach (var planet in PlanetManager.Instance.planets)
            {
                if (planet == null) continue;
                float minDist = planet.radius + 2f;
                Vector3 diff = probe.transform.position - planet.transform.position;
                if (diff.sqrMagnitude < minDist * minDist)
                {
                    probe.transform.position = planet.transform.position + diff.normalized * minDist;
                    _driftVelocity = Vector3.Reflect(_driftVelocity, diff.normalized) * 0.3f;
                }
            }

        foreach (var moon in MoonTag.All)
        {
            float radius = moon.transform.localScale.x * 0.5f;
            float minDist = radius + 1.2f;
            Vector3 diff = probe.transform.position - moon.transform.position;
            if (diff.sqrMagnitude < minDist * minDist)
            {
                probe.transform.position = moon.transform.position + diff.normalized * minDist;
                _driftVelocity = Vector3.Reflect(_driftVelocity, diff.normalized) * 0.3f;
            }
        }

        var sun = GameObject.Find("Sun");
        if (sun != null)
        {
            float sunR = sun.transform.localScale.x * 0.5f + 10f;
            Vector3 diff = probe.transform.position - sun.transform.position;
            if (diff.sqrMagnitude < sunR * sunR)
            {
                probe.transform.position = sun.transform.position + diff.normalized * sunR;
                _driftVelocity = Vector3.Reflect(_driftVelocity, diff.normalized) * 0.3f;
            }
        }
    }

    static float GetNebulaSpeedMultiplier(Vector3 pos)
    {
        float maxDensity = 0f;
        var nebulae = Object.FindObjectsByType<ReactiveNebula>(FindObjectsInactive.Exclude);
        foreach (var n in nebulae)
            maxDensity = Mathf.Max(maxDensity, n.GetDensityAt(pos));
        return Mathf.Lerp(1f, 0.5f, maxDensity);
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
        probe.LastScanned = scanTarget;

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
        probe.IsThrusting = false;
        probe.Target = null;
        CameraController.Instance?.StopFollowing();
        if (returnState != null)
            MissionLog.Instance?.AddEntry("Auto-pilot re-engaged.", new Color(0.4f, 0.8f, 1f));
    }
}
