using System.Collections.Generic;
using UnityEngine;

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
        FSM.ChangeState(new IdleState(this));
    }

    void Update()
    {
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
