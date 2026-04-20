using UnityEngine;

public class TravelState : State
{
    readonly ProbeController probe;
    public TravelState(ProbeController probe) { this.probe = probe; }

    public override void OnEnter() { }

    public override void OnUpdate()
    {
        if (probe.Path == null) return;

        Vector3 next = (probe.WaypointIndex == probe.Path.Count - 1 && probe.Target != null)
            ? probe.Target.transform.position - (probe.Target.transform.position - probe.transform.position).normalized * (probe.Target.radius + 1f)
            : probe.Path[probe.WaypointIndex];

        probe.transform.position = Vector3.MoveTowards(probe.transform.position, next, probe.speed * Time.deltaTime);

        if ((next - probe.transform.position).sqrMagnitude > 1e-4f)
            probe.transform.forward = (next - probe.transform.position).normalized;

        if (Vector3.Distance(probe.transform.position, next) < probe.arrivalThreshold)
        {
            probe.WaypointIndex++;
            if (probe.WaypointIndex >= probe.Path.Count)
            {
                probe.PathVis.Hide();
                probe.FSM.ChangeState(new ScanState(probe));
            }
        }
    }

    public override void OnExit() { }
}
