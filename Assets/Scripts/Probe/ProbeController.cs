using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PathVisualizer))]
public class ProbeController : MonoBehaviour
{
    public enum State { Idle, ChooseTarget, Travel, AvoidCollision, Scan, Arrived }

    public float speed = 10f;
    public float arrivalThreshold = 0.5f;

    public State CurrentState { get; private set; } = State.Idle;
    public Planet Target { get; private set; }
    public event Action<State> StateChanged;

    PathVisualizer vis;
    List<Vector3> path;
    int waypointIndex;

    void Awake()
    {
        vis = GetComponent<PathVisualizer>();
    }

    public void ChooseTarget(Planet planet)
    {
        if (planet == null) return;

        Target = planet;
        SetState(State.ChooseTarget);

        var planets = PlanetManager.Instance != null ? PlanetManager.Instance.planets : null;

        Vector3 dir = (planet.transform.position - transform.position).normalized;
        Vector3 approach = planet.transform.position - dir * (planet.radius + 1f);

        path = AStarPathfinder.FindPath(transform.position, approach, planets);

        if (path != null && path.Count >= 2)
        {
            vis.Show(path);
            waypointIndex = 1;
            SetState(State.Travel);
        }
        else
        {
            Debug.LogWarning($"Probe: no path found to {planet.data.planetName}");
            SetState(State.Idle);
        }
    }

    void Update()
    {
        if ((CurrentState != State.Travel && CurrentState != State.AvoidCollision) || path == null) return;

        Vector3 next = (waypointIndex == path.Count - 1 && Target != null)
            ? Target.transform.position - (Target.transform.position - transform.position).normalized * (Target.radius + 1f)
            : path[waypointIndex];

        transform.position = Vector3.MoveTowards(transform.position, next, speed * Time.deltaTime);
        transform.forward = (next - transform.position).sqrMagnitude > 1e-4f
            ? (next - transform.position).normalized
            : transform.forward;

        if (Vector3.Distance(transform.position, next) < arrivalThreshold)
        {
            waypointIndex++;
            if (waypointIndex >= path.Count)
            {
                SetState(State.Scan);
                if (Target != null) Target.data.explored = true;
                vis.Hide();
            }
        }
    }

    void SetState(State nextState)
    {
        if (CurrentState == nextState) return;

        CurrentState = nextState;
        StateChanged?.Invoke(CurrentState);
    }
}
