using UnityEngine;
using UnityEditor;

public static class PlanetMaterialCreator
{
    const string OutputFolder = "Assets/Resources/Materials/Planets";

    static readonly (string name, float smoothness, bool emissive)[] PlanetDefs =
    {
        ("Mercury", 0.05f, false),
        ("Venus",   0.12f, false),
        ("Earth",   0.18f, false),
        ("Mars",    0.05f, false),
        ("Jupiter", 0.25f, false),
        ("Saturn",  0.22f, false),
        ("Uranus",  0.30f, false),
        ("Neptune", 0.28f, false),
        ("Sun",     0.00f, true),
    };

    [MenuItem("Tools/Solar System/Create Planet Materials")]
    static void CreateAll()
    {
        EnsureFolders();

        var litShader = Shader.Find("Universal Render Pipeline/Lit");
        if (litShader == null) { Debug.LogError("[PlanetMaterialCreator] URP Lit shader not found."); return; }

        foreach (var (name, smoothness, emissive) in PlanetDefs)
            CreateOrUpdate(name, smoothness, emissive, litShader);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[PlanetMaterialCreator] Done — materials saved to {OutputFolder}");
    }

    static void CreateOrUpdate(string planetName, float smoothness, bool emissive, Shader shader)
    {
        string path = $"{OutputFolder}/Planet_{planetName}.mat";

        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, path);
        }
        else
        {
            mat.shader = shader;
        }

        var tex = Resources.Load<Texture2D>($"PlanetTextures/{planetName}");
        if (tex != null)
        {
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
            if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", tex);
        }
        else
        {
            Debug.LogWarning($"[PlanetMaterialCreator] No texture found for {planetName} — material created without texture.");
        }

        if (emissive)
        {
            mat.EnableKeyword("_EMISSION");
            if (tex != null && mat.HasProperty("_EmissionMap")) mat.SetTexture("_EmissionMap", tex);
            if (mat.HasProperty("_EmissionColor"))
                mat.SetColor("_EmissionColor", new Color(1f, 0.75f, 0.3f) * 4.5f);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);
        }
        else
        {
            if (mat.HasProperty("_BaseColor"))   mat.SetColor("_BaseColor",  Color.white);
            if (mat.HasProperty("_Metallic"))    mat.SetFloat("_Metallic",   0f);
            if (mat.HasProperty("_Smoothness"))  mat.SetFloat("_Smoothness", smoothness);
        }

        EditorUtility.SetDirty(mat);
        Debug.Log($"[PlanetMaterialCreator] {(tex != null ? "✓" : "⚠")} Planet_{planetName}.mat");
    }

    static void EnsureFolders()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder("Assets/Resources/Materials"))
            AssetDatabase.CreateFolder("Assets/Resources", "Materials");
        if (!AssetDatabase.IsValidFolder(OutputFolder))
            AssetDatabase.CreateFolder("Assets/Resources/Materials", "Planets");
    }
}
