using System.Collections.Generic;
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

        TargetIndicator.Instance?.SetTarget(probe.Target);

        Vector3 targetPos = probe.Target.transform.position;
        Vector3 currentPos = probe.transform.position;
        Vector3 dirToTarget = (targetPos - currentPos).normalized;
        if (dirToTarget.sqrMagnitude < 0.1f) dirToTarget = Vector3.forward;

        // Ensure approach point is always at a safe distance from the planet's surface
        float safeDistance = probe.Target.radius + 15f;
        Vector3 approach = targetPos - dirToTarget * safeDistance;

        // If the probe is already inside the safe distance, just use current position as start
        // and approach as target, but AStarPathfinder.EnsureSafePoint will handle this too.
        
        probe.Path = AStarPathfinder.FindPath(currentPos, approach, planets);

        if (probe.Path == null || probe.Path.Count < 2)
        {
            Debug.LogWarning($"[FSM] A* failed for {probe.Target.data.planetName} — using sun-aware direct path");
            probe.Path = BuildSunAwarePath(currentPos, approach);
        }

        probe.WaypointIndex = 1;
        probe.FSM.ChangeState(new TravelState(probe));
    }

    void AutoSelectTarget(List<Planet> planets)
    {
        Planet best = null;
        float bestScore = float.MinValue;

        foreach (var p in planets)
        {
            if (p == null || p.data.explored || !p.gameObject.activeSelf) continue;
            if (p == probe.LastFailedTarget) continue; // skip recently-unreachable planet

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
        // Clear the failed-target skip once we've picked something else
        if (best != null && best != probe.LastFailedTarget)
            probe.LastFailedTarget = null;
        probe.TargetReason = best != null
            ? $"Next target: {best.data.planetName} — Reason: closest unexplored"
            : "";

        if (probe.TargetReason != "")
            Debug.Log($"[FSM] {probe.TargetReason}");
    }

    // Builds a 2-or-3-waypoint path that arcs around the sun if the straight line hits it
    static List<Vector3> BuildSunAwarePath(Vector3 from, Vector3 to)
    {
        var sunGO  = GameObject.Find("Sun");
        if (sunGO != null)
        {
            Vector3 sunPos  = sunGO.transform.position;
            float   sunR    = sunGO.transform.localScale.x * 0.5f * 1.6f; // 1.6× safety margin

            float closest = DistancePointToSegment(sunPos, from, to);
            if (closest < sunR)
            {
                // The straight path passes through the sun zone — add a detour waypoint
                Vector3 lineDir  = (to - from).normalized;
                Vector3 perpA    = Vector3.Cross(lineDir, Vector3.up).normalized;
                Vector3 perpB    = -perpA;

                // Pick the side that the probe is already on
                float sideA = Vector3.Dot(from - sunPos, perpA);
                Vector3 perp = sideA >= 0f ? perpA : perpB;

                // Detour point: beside the sun at a safe radius, at the closest approach latitude
                float   t       = Mathf.Clamp01(Vector3.Dot(sunPos - from, lineDir) / Vector3.Distance(from, to));
                Vector3 closest3D = Vector3.Lerp(from, to, t);
                Vector3 detour  = sunPos + perp * (sunR * 2.5f) + (closest3D - sunPos) * 0.1f;

                return new List<Vector3> { from, detour, to };
            }
        }
        return new List<Vector3> { from, to };
    }

    static float DistancePointToSegment(Vector3 p, Vector3 a, Vector3 b)
    {
        Vector3 ab = b - a;
        float len2 = ab.sqrMagnitude;
        if (len2 < 0.001f) return Vector3.Distance(p, a);
        float t = Mathf.Clamp01(Vector3.Dot(p - a, ab) / len2);
        return Vector3.Distance(p, a + t * ab);
    }

    public override void OnUpdate() { }
    public override void OnExit() { }
}
