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
        if (Physics.Raycast(probe.transform.position, probe.transform.forward, out RaycastHit hit, 200f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            avoidDir = Vector3.Cross(hit.normal, Vector3.up).normalized;
            if (avoidDir.sqrMagnitude < 0.1f) avoidDir = Vector3.Cross(hit.normal, Vector3.right).normalized;
            if (avoidDir.sqrMagnitude < 0.1f) avoidDir = probe.transform.right;
        }
        else
        {
            avoidDir = probe.transform.right;
        }
        Debug.Log("[FSM] Avoiding collision...");
    }

    public override void OnUpdate()
    {
        probe.transform.position += (avoidDir + probe.transform.forward * 0.3f).normalized * probe.speed * 0.9f * Time.deltaTime;
        probe.transform.forward = Vector3.Slerp(probe.transform.forward, avoidDir, Time.deltaTime * 8f);

        timer += Time.deltaTime;

        if (timer >= AvoidDuration)
        {
            // Replan from current position so we don't loop back into the same obstacle
            probe.Path = null;
            probe.FSM.ChangeState(new ChooseTargetState(probe));
        }
    }

    public override void OnExit() { }
}
