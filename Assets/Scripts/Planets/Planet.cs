using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
public class Planet : MonoBehaviour
{
    public PlanetData data = new PlanetData();

    [Tooltip("Effective radius used by pathfinding. Auto-set from scale at Start.")]
    public float radius = 1f;

    [Tooltip("Glow color used when this planet is selected.")]
    public Color highlightColor = new Color(1f, 0.85f, 0.3f);

    Material runtimeMaterial;
    Color baseEmission;

    void Start()
    {
        radius = transform.lossyScale.x * 0.5f;

        var renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            runtimeMaterial = renderer.material;
            if (runtimeMaterial.HasProperty("_EmissionColor"))
                baseEmission = runtimeMaterial.GetColor("_EmissionColor");
        }
    }

    public void SetSelected(bool on)
    {
        if (runtimeMaterial == null || !runtimeMaterial.HasProperty("_EmissionColor")) return;

        if (on)
        {
            runtimeMaterial.EnableKeyword("_EMISSION");
            runtimeMaterial.SetColor("_EmissionColor", highlightColor * 1.5f);
        }
        else
        {
            runtimeMaterial.SetColor("_EmissionColor", baseEmission);
        }
    }
}
