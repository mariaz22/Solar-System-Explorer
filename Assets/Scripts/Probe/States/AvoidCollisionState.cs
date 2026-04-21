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
        avoidDir = Vector3.Cross(probe.transform.forward, Vector3.up).normalized;
        Debug.Log("[FSM] Avoiding collision...");
    }

    public override void OnUpdate()
    {
        probe.transform.position += avoidDir * probe.speed * Time.deltaTime;
        timer += Time.deltaTime;

        if (timer >= AvoidDuration)
            probe.FSM.ChangeState(new ChooseTargetState(probe));
    }

    public override void OnExit() { }
}
