using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PathVisualizer))]
public class ProbeController : MonoBehaviour
{
    public enum State { Idle, ChooseTarget, Traveling, Arrived }

    public float speed = 10f;
    public float arrivalThreshold = 0.5f;

    public State CurrentState { get; private set; } = State.Idle;
    public Planet Target { get; private set; }

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
        CurrentState = State.ChooseTarget;

        var planets = PlanetManager.Instance != null ? PlanetManager.Instance.planets : null;

        Vector3 dir = (planet.transform.position - transform.position).normalized;
        Vector3 approach = planet.transform.position - dir * (planet.radius + 1f);

        path = AStarPathfinder.FindPath(transform.position, approach, planets);

        if (path != null && path.Count >= 2)
        {
            vis.Show(path);
            waypointIndex = 1;
            CurrentState = State.Traveling;
        }
        else
        {
            Debug.LogWarning($"Probe: no path found to {planet.data.planetName}");
            CurrentState = State.Idle;
        }
    }

    void Update()
    {
        if (CurrentState != State.Traveling || path == null) return;

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
                CurrentState = State.Arrived;
                if (Target != null) Target.data.explored = true;
                vis.Hide();
            }
        }
    }
}
