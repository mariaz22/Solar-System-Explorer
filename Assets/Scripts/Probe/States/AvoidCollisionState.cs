using UnityEngine;

public class AvoidCollisionState : State
{
    readonly ProbeController probe;
    float   timer;
    Vector3 avoidDir;
    const float AvoidDuration = 3.0f;

    public AvoidCollisionState(ProbeController probe) { this.probe = probe; }

    public override void OnEnter()
    {
        timer = 0f;

        if (Physics.Raycast(probe.transform.position, probe.transform.forward,
            out RaycastHit hit, 200f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            Vector3 toObstacle    = (hit.point - probe.transform.position).normalized;
            Vector3 awayFromCenter = -toObstacle;
            Vector3 perpA = Vector3.Cross(toObstacle, Vector3.up).normalized;
            Vector3 perpB = Vector3.Cross(toObstacle, Vector3.right).normalized;
            Vector3 perp  = Mathf.Abs(Vector3.Dot(perpA, Vector3.up)) > 0.1f ? perpA : perpB;

            // Alternate side each retry to break symmetry
            if (probe.AvoidRetryCount % 2 == 1) perp = -perp;

            avoidDir = (perp * 0.7f + awayFromCenter * 0.3f).normalized;
        }
        else
        {
            // Alternate left/right AND up/down to prevent drift accumulating in one direction
            float ySign = (probe.AvoidRetryCount % 4 < 2) ? 0.2f : -0.2f;
            avoidDir = (probe.AvoidRetryCount % 2 == 0)
                ? ( probe.transform.right + Vector3.up * ySign).normalized
                : (-probe.transform.right + Vector3.up * ySign).normalized;
        }

        Debug.Log($"[FSM] Avoiding collision... (attempt {probe.AvoidRetryCount})");
    }

    public override void OnUpdate()
    {
        probe.transform.position += avoidDir * probe.speed * 0.85f * Time.deltaTime;
        probe.transform.forward   = Vector3.Slerp(probe.transform.forward, avoidDir, Time.deltaTime * 6f);

        timer += Time.deltaTime;
        if (timer < AvoidDuration) return;

        probe.Path = null;

        if (probe.AvoidRetryCount >= 3)
        {
            Debug.LogWarning($"[FSM] Giving up on {probe.Target?.data.planetName} after {probe.AvoidRetryCount} retries — skipping.");
            probe.LastFailedTarget  = probe.Target;
            probe.Target            = null;
            probe.AvoidRetryCount   = 0;
        }

        probe.FSM.ChangeState(new ChooseTargetState(probe));
    }

    public override void OnExit() { }
}
