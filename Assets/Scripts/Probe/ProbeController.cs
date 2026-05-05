using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public enum ProbeState { Idle, ChooseTarget, Travel, Scan, AvoidCollision, Return }

[RequireComponent(typeof(PathVisualizer))]
public class ProbeController : MonoBehaviour
{
    public float speed = 10f;
    public float arrivalThreshold = 0.5f;

    public StateMachine FSM { get; private set; } = new StateMachine();
    public Planet Target { get; set; }
    public string TargetReason { get; set; } = "";
    public List<Vector3> Path { get; set; }
    public int WaypointIndex { get; set; }
    public PathVisualizer PathVis { get; private set; }
    public Vector3 Origin { get; private set; }

    void Awake()
    {
        PathVis = GetComponent<PathVisualizer>();
        Origin = transform.position;
    }

    void Start()
    {
        FSM.ChangeState(new ManualControlState(this, null));
    }

    void Update()
    {
        var kb = Keyboard.current;

        if (kb.tabKey.wasPressedThisFrame)
        {
            if (FSM.CurrentState is ManualControlState)
                FSM.ChangeState(new IdleState(this));
            else
                FSM.ChangeState(new ManualControlState(this, FSM.CurrentState));
        }

        // Orice tastă de mișcare din Standby intră automat în manual
        if (FSM.CurrentState is IdleState)
        {
            bool anyMove = kb.wKey.isPressed || kb.sKey.isPressed ||
                           kb.aKey.isPressed || kb.dKey.isPressed ||
                           kb.upArrowKey.isPressed || kb.downArrowKey.isPressed ||
                           kb.leftArrowKey.isPressed || kb.rightArrowKey.isPressed ||
                           kb.qKey.isPressed || kb.eKey.isPressed;
            if (anyMove)
                FSM.ChangeState(new ManualControlState(this, null));
        }

        FSM.Update();
    }

    public void SetTarget(Planet planet)
    {
        if (planet == null) return;
        Target = planet;
        FSM.ChangeState(new ChooseTargetState(this));
    }

    public bool AllPlanetsExplored()
    {
        if (PlanetManager.Instance == null) return false;
        foreach (var p in PlanetManager.Instance.planets)
            if (p != null && !p.data.explored) return false;
        return true;
    }
}
