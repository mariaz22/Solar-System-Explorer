using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class SunDistortionSetup : MonoBehaviour
{
    [Range(1.1f, 2.0f)] public float zoneScale = 1.4f;

    Material _mat;
    GameObject _zoneSphere;

    static readonly int DistortStrength = Shader.PropertyToID("_DistortStrength");
    static readonly int HeatTintStr     = Shader.PropertyToID("_HeatTintStr");

    void Start()
    {
        EnableOpaqueTexture();

        var shader = Shader.Find("Solar/SunHeatDistortion");
        if (shader == null)
        {
            Debug.LogWarning("[SunDistortion] Solar/SunHeatDistortion shader not found — distortion disabled.");
            enabled = false;
            return;
        }

        _mat = new Material(shader);
        ApplyStageDefaults();

        _zoneSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        _zoneSphere.name = "SunHeatZone";
        _zoneSphere.transform.SetParent(transform, false);
        _zoneSphere.transform.localScale = Vector3.one * zoneScale;

        Destroy(_zoneSphere.GetComponent<SphereCollider>());

        var mr = _zoneSphere.GetComponent<MeshRenderer>();
        mr.material          = _mat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows    = false;
    }

    public void SetEvolutionIntensity(float t)
    {
        if (_mat == null) return;
        _mat.SetFloat(DistortStrength, Mathf.Lerp(0.010f, 0.032f, t));
        _mat.SetFloat(HeatTintStr,     Mathf.Lerp(0.045f, 0.160f, t));
    }

    void ApplyStageDefaults()
    {
        _mat.SetFloat("_DistortStrength", 0.007f);
        _mat.SetFloat("_DistortFreq",     5.0f);
        _mat.SetFloat("_DistortSpeed",    0.8f);
        _mat.SetFloat("_FalloffPow",      3.5f);
        _mat.SetColor("_HeatTint",        new Color(1.0f, 0.78f, 0.40f, 0f));
        _mat.SetFloat("_HeatTintStr",     0.03f);
    }

    static void EnableOpaqueTexture()
    {
        if (GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset urp
            && !urp.supportsCameraOpaqueTexture)
        {
            urp.supportsCameraOpaqueTexture = true;
        }
    }
}
