using UnityEngine;

public class TravelState : State
{
    readonly ProbeController probe;
    Vector3 currentVelocity;

    static float GetNebulaSlowdown(Vector3 pos)
    {
        float maxDensity = 0f;
        var nebulae = Object.FindObjectsByType<ReactiveNebula>(FindObjectsInactive.Exclude);
        foreach (var n in nebulae)
            maxDensity = Mathf.Max(maxDensity, n.GetDensityAt(pos));
        return Mathf.Lerp(1f, 0.65f, maxDensity);
    }

    public TravelState(ProbeController probe) { this.probe = probe; }

    public override void OnEnter()
    {
        currentVelocity = probe.transform.forward * probe.speed * 0.5f;
        CameraController.Instance?.StartFollowing(probe.transform);
        if (probe.Target != null)
            MissionLog.Instance?.AddEntry(
                $"En route to {probe.Target.data.planetName}.",
                new UnityEngine.Color(0.4f, 0.8f, 1f));
    }

    public override void OnUpdate()
    {
        if (probe.Path == null || probe.WaypointIndex >= probe.Path.Count)
        {
            probe.FSM.ChangeState(new ScanState(probe));
            return;
        }

        Vector3 currentPos = probe.transform.position;
        Vector3 next = (probe.WaypointIndex == probe.Path.Count - 1 && probe.Target != null)
            ? probe.Target.transform.position
              - (probe.Target.transform.position - currentPos).normalized
              * (probe.Target.radius + 12f)
            : probe.Path[probe.WaypointIndex];

        // 1. Desired velocity toward next waypoint
        Vector3 desiredDir = (next - currentPos).normalized;
        float slowdown = GetNebulaSlowdown(currentPos);
        Vector3 targetVelocity = desiredDir * (probe.speed * slowdown);

        // 2. Obstacle avoidance via SphereCast (ignore triggers so meteors/asteroids don't deflect autopilot)
        if (Physics.SphereCast(currentPos, probe.avoidanceRadius, probe.transform.forward,
            out RaycastHit hit, probe.lookAheadDistance,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            bool isTarget      = probe.Target      != null && hit.collider.gameObject == probe.Target.gameObject;
            bool isLastScanned = probe.LastScanned != null && hit.collider.gameObject == probe.LastScanned.gameObject;

            if (!isTarget && !isLastScanned)
            {
                Vector3 avoidDir = Vector3.Reflect(probe.transform.forward, hit.normal);
                Vector3 pushAway = (currentPos - hit.point).normalized;
                targetVelocity  += (avoidDir + pushAway * 2f).normalized * probe.avoidanceStrength;

                if (hit.distance < probe.avoidanceRadius * 0.6f)
                {
                    probe.FSM.ChangeState(new AvoidCollisionState(probe));
                    return;
                }
            }
        }

        // 3. Smooth velocity and move
        currentVelocity = Vector3.Lerp(currentVelocity, targetVelocity, Time.deltaTime * 4f);
        probe.transform.position += currentVelocity * Time.deltaTime;

        // 4. Smooth rotation
        if (currentVelocity.sqrMagnitude > 0.01f)
        {
            Quaternion targetRot = Quaternion.LookRotation(currentVelocity.normalized);
            probe.transform.rotation = Quaternion.Slerp(probe.transform.rotation, targetRot, Time.deltaTime * 5f);
        }

        // 5. Hard push-out: never let probe enter a planet or sun
        PushOutOfObstacles();

        // 6. Waypoint advance
        if (Vector3.Distance(currentPos, next) < probe.arrivalThreshold)
        {
            probe.WaypointIndex++;
            if (probe.WaypointIndex >= probe.Path.Count)
                probe.FSM.ChangeState(new ScanState(probe));
        }
    }

    void PushOutOfObstacles()
    {
        if (PlanetManager.Instance != null)
            foreach (var planet in PlanetManager.Instance.planets)
            {
                if (planet == null) continue;
                float minDist = planet.radius + 3f;
                Vector3 diff = probe.transform.position - planet.transform.position;
                if (diff.sqrMagnitude < minDist * minDist)
                    probe.transform.position = planet.transform.position + diff.normalized * minDist;
            }

        var sun = GameObject.Find("Sun");
        if (sun != null)
        {
            float sunR = sun.transform.localScale.x * 0.5f + 10f;
            Vector3 diff = probe.transform.position - sun.transform.position;
            if (diff.sqrMagnitude < sunR * sunR)
                probe.transform.position = sun.transform.position + diff.normalized * sunR;
        }
    }

    public override void OnExit()
    {
        CameraController.Instance?.StopFollowing();
    }
}
