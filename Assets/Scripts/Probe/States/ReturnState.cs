using UnityEngine;

public class ReturnState : State
{
    readonly ProbeController probe;

    public ReturnState(ProbeController probe) { this.probe = probe; }

    public override void OnEnter()
    {
        Debug.Log("[FSM] All planets explored. Returning to origin.");
    }

    Vector3 currentVelocity;

    public override void OnUpdate()
    {
        Vector3 currentPos = probe.transform.position;
        Vector3 targetVelocity = (probe.Origin - currentPos).normalized * probe.speed;

        // Obstacle Avoidance
        if (Physics.SphereCast(currentPos, probe.avoidanceRadius, probe.transform.forward, out RaycastHit hit, probe.lookAheadDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide))
        {
            // Ignore origin if it has a collider (unlikely, but safe)
            if (hit.distance < 1.5f)
            {
                Vector3 avoidDir = Vector3.Reflect(probe.transform.forward, hit.normal);
                Vector3 pushAway = (currentPos - hit.point).normalized;
                targetVelocity += (avoidDir + pushAway).normalized * probe.avoidanceStrength;
            }
        }

        currentVelocity = Vector3.Lerp(currentVelocity, targetVelocity, Time.deltaTime * 3f);
        probe.transform.position += currentVelocity * Time.deltaTime;

        if (currentVelocity.sqrMagnitude > 0.01f)
            probe.transform.rotation = Quaternion.Slerp(probe.transform.rotation, Quaternion.LookRotation(currentVelocity.normalized), Time.deltaTime * 4f);

        if (Vector3.Distance(currentPos, probe.Origin) < probe.arrivalThreshold + 1f)
            probe.FSM.ChangeState(new IdleState(probe));
    }

    public override void OnExit() { }
}
