public class IdleState : State
{
    readonly ProbeController probe;
    public IdleState(ProbeController probe) { this.probe = probe; }

    public override void OnEnter() { }
    public override void OnUpdate() { }
    public override void OnExit() { }
}
