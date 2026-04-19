using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class PathVisualizer : MonoBehaviour
{
    public float width = 0.2f;
    public Color color = Color.cyan;

    LineRenderer lr;

    void Awake()
    {
        lr = GetComponent<LineRenderer>();
        lr.widthMultiplier = width;
        lr.useWorldSpace = true;
        lr.positionCount = 0;

        var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
        if (shader != null)
        {
            lr.material = new Material(shader);
            lr.material.color = color;
        }
        lr.startColor = color;
        lr.endColor = color;
    }

    public void Show(List<Vector3> path)
    {
        if (path == null || path.Count < 2) { Hide(); return; }
        lr.positionCount = path.Count;
        lr.SetPositions(path.ToArray());
        lr.enabled = true;
    }

    public void Hide()
    {
        lr.positionCount = 0;
        lr.enabled = false;
    }
}
