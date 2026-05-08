using System.Collections.Generic;
using UnityEngine;

public static class AStarPathfinder
{
    [System.Serializable]
    public class Settings
    {
        public int waypointsPerPlanet = 12; // Increased from 6
        public float waypointDistanceMultiplier = 3.0f; // Increased from 2.5
        public float massPenaltyRadiusMultiplier = 6f; // Increased from 5
        public float massPenaltyStrength = 3f; // Increased from 2
    }

    static readonly Vector3[] WaypointDirs =
    {
        Vector3.up, Vector3.down,
        Vector3.left, Vector3.right,
        Vector3.forward, Vector3.back,
        (Vector3.up + Vector3.right).normalized,
        (Vector3.up + Vector3.left).normalized,
        (Vector3.down + Vector3.right).normalized,
        (Vector3.down + Vector3.left).normalized,
        (Vector3.forward + Vector3.up).normalized,
        (Vector3.back + Vector3.up).normalized
    };

    public static List<Vector3> FindPath(Vector3 start, Vector3 end, IList<Planet> planets, Settings s = null)
    {
        s ??= new Settings();
        var nodes = BuildNodes(start, end, planets, s);
        var edges = BuildEdges(nodes, planets, s);
        return AStar(nodes, edges, 0, 1);
    }

    static List<Vector3> BuildNodes(Vector3 start, Vector3 end, IList<Planet> planets, Settings s)
    {
        var nodes = new List<Vector3> { start, end };
        if (planets == null) return nodes;

        int count = Mathf.Min(s.waypointsPerPlanet, WaypointDirs.Length);
        foreach (var p in planets)
        {
            if (p == null) continue;
            float d = p.radius * s.waypointDistanceMultiplier;
            for (int i = 0; i < count; i++)
                nodes.Add(p.transform.position + WaypointDirs[i] * d);
        }
        return nodes;
    }

    static Dictionary<int, List<(int target, float cost)>> BuildEdges(
        List<Vector3> nodes, IList<Planet> planets, Settings s)
    {
        var graph = new Dictionary<int, List<(int, float)>>();
        for (int i = 0; i < nodes.Count; i++) graph[i] = new List<(int, float)>();

        for (int i = 0; i < nodes.Count; i++)
        {
            for (int j = i + 1; j < nodes.Count; j++)
            {
                if (!SegmentClear(nodes[i], nodes[j], planets, s, out float penalty)) continue;
                float cost = Vector3.Distance(nodes[i], nodes[j]) + penalty;
                graph[i].Add((j, cost));
                graph[j].Add((i, cost));
            }
        }
        return graph;
    }

    static bool SegmentClear(Vector3 a, Vector3 b, IList<Planet> planets, Settings s, out float penalty)
    {
        penalty = 0f;
        
        // 1. Check Nebulae Hazards with dynamic cost sampling
        var nebulae = Object.FindObjectsByType<ReactiveNebula>(FindObjectsInactive.Exclude);
        foreach (var n in nebulae)
        {
            // Sample density at 5 points along the segment to find tunnels
            for (int step = 0; step <= 4; step++)
            {
                float t = step / 4f;
Vector3 samplePos = Vector3.Lerp(a, b, t);
                float density = n.GetDensityAt(samplePos);
                
                if (density > 0.05f)
                {
                    penalty += 300f * Mathf.Pow(density, 2f);
                    // Danger zones (lilac, density > 0.65) get heavy extra penalty
                    // so autopilot strongly prefers tunnels and open corridors
                    if (density > 0.65f)
                        penalty += 900f * Mathf.Pow(density - 0.65f, 1.5f);
                }
            }
        }

        if (planets == null) return true;

        foreach (var p in planets)
        {
            if (p == null) continue;
            Vector3 c = p.transform.position;
            float surface = p.radius * 1.25f; // Increased safety margin from 1.05
            bool ownsEndpoint = Vector3.Distance(c, a) < surface || Vector3.Distance(c, b) < surface;

            float d = DistancePointToSegment(c, a, b);
            if (!ownsEndpoint && d < p.radius * 1.2f) return false; // Increased clearance
if (ownsEndpoint) continue;

            float zone = p.radius * s.massPenaltyRadiusMultiplier;
            if (d < zone)
            {
                float t = 1f - (d / zone);
                penalty += t * Mathf.Max(0.1f, p.data.relativeMass) * s.massPenaltyStrength;
            }
        }
        return true;
    }

    static float DistancePointToSegment(Vector3 p, Vector3 a, Vector3 b)
    {
        Vector3 ab = b - a;
        float len2 = ab.sqrMagnitude;
        if (len2 < 1e-6f) return Vector3.Distance(p, a);
        float t = Mathf.Clamp01(Vector3.Dot(p - a, ab) / len2);
        return Vector3.Distance(p, a + ab * t);
    }

    static List<Vector3> AStar(List<Vector3> nodes,
        Dictionary<int, List<(int, float)>> edges, int start, int goal)
    {
        var open = new SortedSet<(float f, int idx)>();
        var gScore = new Dictionary<int, float> { [start] = 0f };
        var came = new Dictionary<int, int>();

        float H(int i) => Vector3.Distance(nodes[i], nodes[goal]);

        open.Add((H(start), start));

        while (open.Count > 0)
        {
            var current = open.Min;
            open.Remove(current);

            if (current.idx == goal)
                return Reconstruct(came, current.idx, nodes);

            float bestF = gScore[current.idx] + H(current.idx);
            if (current.f > bestF + 1e-4f) continue;

            foreach (var (n, c) in edges[current.idx])
            {
                float tentative = gScore[current.idx] + c;
                if (!gScore.TryGetValue(n, out float g) || tentative < g)
                {
                    came[n] = current.idx;
                    gScore[n] = tentative;
                    open.Add((tentative + H(n), n));
                }
            }
        }
        return new List<Vector3>();
    }

    static List<Vector3> Reconstruct(Dictionary<int, int> came, int current, List<Vector3> nodes)
    {
        var path = new List<Vector3> { nodes[current] };
        while (came.TryGetValue(current, out int prev))
        {
            current = prev;
            path.Insert(0, nodes[current]);
        }
        return path;
    }
}
