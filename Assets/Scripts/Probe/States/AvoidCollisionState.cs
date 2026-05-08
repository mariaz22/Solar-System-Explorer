using UnityEngine;

public class AvoidCollisionState : State
{
    readonly ProbeController probe;
    float timer;
    Vector3 avoidDir;
    const float AvoidDuration = 1.5f;

    public AvoidCollisionState(ProbeController probe) { this.probe = probe; }

    public override void OnEnter()
    {
        timer = 0f;
        // Find a direction perpendicular to the collision
        if (Physics.Raycast(probe.transform.position, probe.transform.forward, out RaycastHit hit, 10f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide))
        {
            avoidDir = Vector3.Cross(hit.normal, Vector3.up).normalized;
            if (avoidDir.sqrMagnitude < 0.1f) avoidDir = Vector3.up;
        }
        else
        {
            avoidDir = probe.transform.right;
        }
        
        MissionLog.Instance?.AddEntry("CRITICAL: Obstacle detected. Executing avoidance maneuver.", Color.red);
        Debug.Log("[FSM] Avoiding collision...");
    }

    public override void OnUpdate()
    {
        // Move in avoidance direction with extra speed
        probe.transform.position += (avoidDir + probe.transform.forward * 0.5f).normalized * probe.speed * 1.2f * Time.deltaTime;
        probe.transform.forward = Vector3.Slerp(probe.transform.forward, avoidDir, Time.deltaTime * 10f);
        
        timer += Time.deltaTime;

        if (timer >= AvoidDuration)
        {
            // Instead of ChooseTargetState, go back to TravelState to continue current path
            probe.FSM.ChangeState(new TravelState(probe));
        }
    }

    public override void OnExit() { }
}
