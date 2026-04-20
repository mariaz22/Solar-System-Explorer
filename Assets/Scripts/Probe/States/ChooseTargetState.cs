using UnityEngine;

public class ChooseTargetState : State
{
    readonly ProbeController probe;
    public ChooseTargetState(ProbeController probe) { this.probe = probe; }

    public override void OnEnter()
    {
        var planets = PlanetManager.Instance?.planets;
        if (planets == null)
        {
            probe.FSM.ChangeState(new IdleState(probe));
            return;
        }

        if (probe.Target == null)
            AutoSelectTarget(planets);

        if (probe.Target == null)
        {
            probe.FSM.ChangeState(new ReturnState(probe));
            return;
        }

        Vector3 dir = (probe.Target.transform.position - probe.transform.position).normalized;
        Vector3 approach = probe.Target.transform.position - dir * (probe.Target.radius + 1f);

        probe.Path = AStarPathfinder.FindPath(probe.transform.position, approach, planets);

        if (probe.Path != null && probe.Path.Count >= 2)
        {
            probe.PathVis.Show(probe.Path);
            probe.WaypointIndex = 1;
            probe.FSM.ChangeState(new TravelState(probe));
        }
        else
        {
            Debug.LogWarning($"[FSM] No path found to {probe.Target.data.planetName}");
            probe.FSM.ChangeState(new IdleState(probe));
        }
    }

    void AutoSelectTarget(System.Collections.Generic.List<Planet> planets)
    {
        Planet best = null;
        float bestScore = float.MinValue;

        foreach (var p in planets)
        {
            if (p == null || p.data.explored) continue;

            float distance = Vector3.Distance(probe.transform.position, p.transform.position);
            if (distance < 0.01f) distance = 0.01f;

            float score = 1f / distance;
            p.data.explorationScore = score;

            if (score > bestScore)
            {
                bestScore = score;
                best = p;
            }
        }

        probe.Target = best;
        probe.TargetReason = best != null ? $"Next target: {best.data.planetName} — Reason: closest unexplored" : "";

        if (probe.TargetReason != "")
            Debug.Log($"[FSM] {probe.TargetReason}");
    }

    public override void OnUpdate() { }
    public override void OnExit() { }
}
