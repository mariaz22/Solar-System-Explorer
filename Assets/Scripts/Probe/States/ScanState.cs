using UnityEngine;

public class ScanState : State
{
    readonly ProbeController probe;
    float timer;
    const float ScanDuration = 2f;

    public ScanState(ProbeController probe) { this.probe = probe; }

    public override void OnEnter()
    {
        timer = 0f;
        if (probe.Target != null)
        {
            Debug.Log($"[FSM] Scanning {probe.Target.data.planetName}...");
            probe.Target.GetComponent<ScanEffect>()?.Play(probe.Target.radius);
            CameraController.Instance?.MoveTo(probe.Target);
        }
    }

    public override void OnUpdate()
    {
        timer += Time.deltaTime;
        if (timer >= ScanDuration)
        {
            if (probe.Target != null)
            {
                string name = probe.Target.data.planetName;
                probe.Target.data.explored = true;
                probe.Target.SetExplored();

                string summary = MissionLog.GetScanSummary(name);
                MissionLog.Instance?.AddEntry(
                    $"Scan complete: <b>{name}</b>. {summary}",
                    new UnityEngine.Color(0.3f, 1f, 0.55f));
            }

            TargetIndicator.Instance?.SetTarget(null);

            if (probe.AllPlanetsExplored())
            {
                MissionLog.Instance?.AddEntry(
                    "All planets explored. Returning to base.",
                    new UnityEngine.Color(1f, 0.85f, 0.2f));
                probe.FSM.ChangeState(new ReturnState(probe));
            }
            else
            {
                probe.Target = null;
                probe.FSM.ChangeState(new ChooseTargetState(probe));
            }
        }
    }

    public override void OnExit() { }
}
