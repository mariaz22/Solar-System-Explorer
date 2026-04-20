using UnityEngine;

public class ReturnState : State
{
    readonly ProbeController probe;

    public ReturnState(ProbeController probe) { this.probe = probe; }

    public override void OnEnter()
    {
        Debug.Log("[FSM] All planets explored. Returning to origin.");
    }

    public override void OnUpdate()
    {
        probe.transform.position = Vector3.MoveTowards(
            probe.transform.position, probe.Origin, probe.speed * Time.deltaTime);

        if (Vector3.Distance(probe.transform.position, probe.Origin) < probe.arrivalThreshold)
            probe.FSM.ChangeState(new IdleState(probe));
    }

    public override void OnExit() { }
}
