using UnityEngine;

public class StateMachine
{
    public State CurrentState { get; private set; }

    public void ChangeState(State newState)
    {
        string from = CurrentState?.GetType().Name ?? "None";
        CurrentState?.OnExit();
        CurrentState = newState;
        Debug.Log($"[FSM] {from} -> {CurrentState.GetType().Name}");
        CurrentState.OnEnter();
    }

    public void Update()
    {
        CurrentState?.OnUpdate();
    }
}
