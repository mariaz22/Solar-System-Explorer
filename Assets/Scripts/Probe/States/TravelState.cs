using UnityEngine;

public class TravelState : State
{
    readonly ProbeController probe;
    Vector3 currentVelocity;

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
        if (probe.Path == null || probe.WaypointIndex >= probe.Path.Count) return;

        Vector3 currentPos = probe.transform.position;
        Vector3 next = (probe.WaypointIndex == probe.Path.Count - 1 && probe.Target != null)
            ? probe.Target.transform.position - (probe.Target.transform.position - currentPos).normalized * (probe.Target.radius + 1.5f)
            : probe.Path[probe.WaypointIndex];

        // 1. Calculate desired velocity
        Vector3 desiredDir = (next - currentPos).normalized;
        Vector3 targetVelocity = desiredDir * probe.speed;

        // 2. Obstacle Avoidance (Raycast/SphereCast)
        if (Physics.SphereCast(currentPos, probe.avoidanceRadius, probe.transform.forward, out RaycastHit hit, probe.lookAheadDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide))
        {
            // If we hit something that is NOT our target AND NOT the planet we just left
            bool isTarget = probe.Target != null && hit.collider.gameObject == probe.Target.gameObject;
            bool isLastScanned = probe.LastScanned != null && hit.collider.gameObject == probe.LastScanned.gameObject;
            
            if (!isTarget && !isLastScanned)
            {
                // Calculate avoidance force
                Vector3 avoidDir = Vector3.Reflect(probe.transform.forward, hit.normal);
                Vector3 pushAway = (currentPos - hit.point).normalized;
                Vector3 combinedAvoid = (avoidDir + pushAway).normalized;
                
                targetVelocity += combinedAvoid * probe.avoidanceStrength;
                
                // Emergency transition if extremely close
                if (hit.distance < 1.5f)
                {
                    probe.FSM.ChangeState(new AvoidCollisionState(probe));
                    return;
                }
            }
        }

        // 3. Smooth velocity and apply movement
        currentVelocity = Vector3.Lerp(currentVelocity, targetVelocity, Time.deltaTime * 3f);
        probe.transform.position += currentVelocity * Time.deltaTime;

        // 4. Smooth rotation
        if (currentVelocity.sqrMagnitude > 0.01f)
        {
            Quaternion targetRot = Quaternion.LookRotation(currentVelocity.normalized);
            probe.transform.rotation = Quaternion.Slerp(probe.transform.rotation, targetRot, Time.deltaTime * 4f);
        }

        // 5. Waypoint logic
        if (Vector3.Distance(currentPos, next) < probe.arrivalThreshold + 0.5f)
        {
            probe.WaypointIndex++;
            if (probe.WaypointIndex >= probe.Path.Count)
            {
                probe.FSM.ChangeState(new ScanState(probe));
            }
        }
    }

    public override void OnExit()
    {
        CameraController.Instance?.StopFollowing();
    }
}
