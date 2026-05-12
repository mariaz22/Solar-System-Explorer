using System.Collections.Generic;
using UnityEngine;

public static class AStarPathfinder
{
    [System.Serializable]
    public class Settings
    {
        public int waypointsPerPlanet = 24; 
        public float[] shellMultipliers = { 2.2f, 4.0f, 6.5f };
        public float massPenaltyRadiusMultiplier = 8f;
        public float massPenaltyStrength = 5f;
    }

    static Vector3[] _waypointDirs;
    static Vector3[] WaypointDirs
    {
        get
        {
            if (_waypointDirs == null) _waypointDirs = GenerateDirections(32);
            return _waypointDirs;
        }
    }

    static Vector3[] GenerateDirections(int count)
    {
        var dirs = new List<Vector3>();
        float goldenRatio = (1 + Mathf.Sqrt(5)) / 2;
        for (int i = 0; i < count; i++)
        {
            float theta = 2 * Mathf.PI * i / goldenRatio;
            float phi = Mathf.Acos(1 - 2 * (i + 0.5f) / count);
            dirs.Add(new Vector3(Mathf.Cos(theta) * Mathf.Sin(phi), Mathf.Sin(theta) * Mathf.Sin(phi), Mathf.Cos(phi)));
        }
        return dirs.ToArray();
    }

    public static List<Vector3> FindPath(Vector3 start, Vector3 end, IList<Planet> planets, Settings s = null)
    {
        s ??= new Settings();

        // Cache sun once — avoid 57k+ GameObject.Find calls inside SegmentClear
        var sunGO = GameObject.Find("Sun");

        Vector3 safeStart = EnsureSafePoint(start, planets, s, sunGO);
        Vector3 safeEnd   = EnsureSafePoint(end,   planets, s, sunGO);

        var nodes = BuildNodes(safeStart, safeEnd, planets, s, sunGO);

        var edges = BuildEdges(nodes, planets, s, false, sunGO);
        var path  = AStar(nodes, edges, 0, 1);

        if (path.Count == 0)
        {
            edges = BuildEdges(nodes, planets, s, true, sunGO);
            path  = AStar(nodes, edges, 0, 1);
        }

        return path;
    }

    static Vector3 EnsureSafePoint(Vector3 p, IList<Planet> planets, Settings s, GameObject sunGO)
    {
        if (sunGO != null)
        {
            float sunRadius = sunGO.transform.localScale.x * 0.5f;
            float killZone = sunRadius * 1.15f;
            Vector3 diff = p - sunGO.transform.position;
            if (diff.magnitude < killZone) return sunGO.transform.position + diff.normalized * (killZone + 5f);
        }

        if (planets != null)
        {
            foreach (var planet in planets)
            {
                if (planet == null) continue;
                float killZone = planet.radius * 1.05f;
                Vector3 diff = p - planet.transform.position;
                if (diff.magnitude < killZone) return planet.transform.position + diff.normalized * (killZone + 2f);
            }
        }
        return p;
    }

    static List<Vector3> BuildNodes(Vector3 start, Vector3 end, IList<Planet> planets, Settings s, GameObject sunGO)
    {
        var nodes = new List<Vector3> { start, end };
        if (planets == null) return nodes;

        foreach (var p in planets)
        {
            if (p == null) continue;
            foreach (float mult in s.shellMultipliers)
            {
                float d = p.radius * mult;
                int count = 12;
                for (int i = 0; i < count; i++)
                {
                    int dirIdx = (i * 3) % WaypointDirs.Length;
                    nodes.Add(p.transform.position + WaypointDirs[dirIdx] * d);
                }
            }
        }

        if (sunGO != null)
        {
            float sunRadius = sunGO.transform.localScale.x * 0.5f;
            foreach (float mult in new float[] { 2.2f, 3.5f, 5.5f })
            {
                float d = sunRadius * mult;
                for (int i = 0; i < 16; i++)
                {
                    int dirIdx = (i * 2) % WaypointDirs.Length;
                    nodes.Add(sunGO.transform.position + WaypointDirs[dirIdx] * d);
                }
            }
        }

        return nodes;
    }

    static Dictionary<int, List<(int target, float cost)>> BuildEdges(
        List<Vector3> nodes, IList<Planet> planets, Settings s, bool permissive, GameObject sunGO)
    {
        var graph = new Dictionary<int, List<(int, float)>>();
        for (int i = 0; i < nodes.Count; i++) graph[i] = new List<(int, float)>();

        for (int i = 0; i < nodes.Count; i++)
        {
            for (int j = i + 1; j < nodes.Count; j++)
            {
                if (!SegmentClear(nodes[i], nodes[j], planets, s, out float penalty, permissive, sunGO)) continue;
                float cost = Vector3.Distance(nodes[i], nodes[j]) + penalty;
                graph[i].Add((j, cost));
                graph[j].Add((i, cost));
            }
        }
        return graph;
    }

    static bool SegmentClear(Vector3 a, Vector3 b, IList<Planet> planets, Settings s, out float penalty, bool permissive, GameObject sunGO)
    {
        penalty = 0f;
        float killMult = permissive ? 1.02f : 1.15f;
        float planetKillMult = permissive ? 1.01f : 1.06f;

        if (sunGO != null)
        {
            float sunRadius = sunGO.transform.localScale.x * 0.5f;
            float d = DistancePointToSegment(sunGO.transform.position, a, b);
            
            float killZone = sunRadius * killMult; 
            float bufferZone = sunRadius * (permissive ? 1.1f : 1.5f);

            bool aInside = Vector3.Distance(sunGO.transform.position, a) < bufferZone;
            bool bInside = Vector3.Distance(sunGO.transform.position, b) < bufferZone;
            
            if (!(aInside || bInside) && d < killZone) return false;
            
            float penaltyZone = sunRadius * 4f;
            if (d < penaltyZone)
            {
                float t = 1f - (d / penaltyZone);
                penalty += t * t * 5000f;
            }
        }

        if (!permissive)
        {
            foreach (var moon in MoonTag.All)
            {
                float moonRadius = moon.transform.localScale.x * 0.5f;
                float d = DistancePointToSegment(moon.transform.position, a, b);
                float avoidRadius = moonRadius * 2.5f;
                if (d < avoidRadius)
                {
                    float t = 1f - (d / avoidRadius);
                    penalty += t * 1500f; 
                }
            }
        }

        if (planets == null) return true;

        foreach (var p in planets)
        {
            if (p == null) continue;
            Vector3 c = p.transform.position;
            float killZone = p.radius * planetKillMult;
            float bufferZone = p.radius * (permissive ? 1.05f : 1.35f);

            bool aInside = Vector3.Distance(c, a) < bufferZone;
            bool bInside = Vector3.Distance(c, b) < bufferZone;

            float d = DistancePointToSegment(c, a, b);
            
            if (!(aInside || bInside) && d < killZone) return false;

            float zone = p.radius * s.massPenaltyRadiusMultiplier;
            if (d < zone)
            {
                float t = 1f - (d / zone);
                float massImpact = Mathf.Max(0.1f, p.data.relativeMass);
                penalty += t * massImpact * s.massPenaltyStrength * (permissive ? 2f : 15f);
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
