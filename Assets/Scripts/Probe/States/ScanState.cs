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
                probe.Target.data.explored = true;
                probe.Target.SetExplored();
            }

            TargetIndicator.Instance?.SetTarget(null);

            if (probe.AllPlanetsExplored())
            {
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
