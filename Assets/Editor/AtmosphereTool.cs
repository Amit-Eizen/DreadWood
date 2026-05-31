using UnityEngine;
using UnityEditor;

// One-click eerie "dusk" atmosphere for the whole scene: cold blue fog, dim low
// sun, dark ambient. Tunable, reversible (Restore Bright Day), and it leaves the
// objects alone — it only touches RenderSettings + the Directional Light.
//
// Open via:  Tools > DreadWood > Atmosphere
public class AtmosphereTool : EditorWindow
{
    // --- Dusk look (deep teal/blue, NOT flat grey — colour + contrast = mood) ---
    [Header("Fog")]
    public Color fogColor = new Color(0.16f, 0.26f, 0.30f);   // deep cold teal
    public float fogDensity = 0.020f;                         // exp2 density

    [Header("Sun (Directional Light)")]
    public Color sunColor = new Color(0.45f, 0.58f, 0.85f);   // cold moonlight blue
    public float sunIntensity = 0.7f;                         // dim but gives shape
    public Vector3 sunAngles = new Vector3(16f, 35f, 0f);     // low dusk angle

    [Header("Ambient")]
    public Color ambientSky = new Color(0.16f, 0.22f, 0.30f);     // cool blue top light
    public Color ambientEquator = new Color(0.10f, 0.12f, 0.14f);
    public Color ambientGround = new Color(0.03f, 0.04f, 0.04f);  // near-black ground = contrast

    [MenuItem("Tools/DreadWood/Atmosphere")]
    static void Open() => GetWindow<AtmosphereTool>("Atmosphere");

    void OnGUI()
    {
        EditorGUILayout.LabelField("Eerie dusk atmosphere", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Fog", EditorStyles.boldLabel);
        fogColor = EditorGUILayout.ColorField("Fog Color", fogColor);
        fogDensity = EditorGUILayout.Slider("Fog Density", fogDensity, 0f, 0.05f);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Sun", EditorStyles.boldLabel);
        sunColor = EditorGUILayout.ColorField("Sun Color", sunColor);
        sunIntensity = EditorGUILayout.Slider("Sun Intensity", sunIntensity, 0f, 2f);
        sunAngles = EditorGUILayout.Vector3Field("Sun Angles", sunAngles);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Ambient", EditorStyles.boldLabel);
        ambientSky = EditorGUILayout.ColorField("Ambient Sky", ambientSky);
        ambientEquator = EditorGUILayout.ColorField("Ambient Equator", ambientEquator);
        ambientGround = EditorGUILayout.ColorField("Ambient Ground", ambientGround);

        EditorGUILayout.Space();
        if (GUILayout.Button("Apply Dusk Atmosphere", GUILayout.Height(34))) ApplyDusk();
        EditorGUILayout.Space();
        if (GUILayout.Button("Restore Bright Day")) RestoreDay();

        EditorGUILayout.HelpBox("Apply for the eerie look. Tweak the values and Apply again to taste. 'Restore Bright Day' undoes it. Only touches lighting/fog — your objects are untouched.", MessageType.Info);
    }

    void ApplyDusk()
    {
        Undo.RegisterCompleteObjectUndo(RenderSettings.sun, "Apply Dusk");

        // Fog (exponential squared = soft distance fade)
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = fogColor;
        RenderSettings.fogDensity = fogDensity;

        // Ambient = trilight (gradient), dark
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = ambientSky;
        RenderSettings.ambientEquatorColor = ambientEquator;
        RenderSettings.ambientGroundColor = ambientGround;

        // Sun: dim, cold, low angle
        Light sun = RenderSettings.sun;
        if (sun == null) sun = FindDirectionalLight();
        if (sun != null)
        {
            Undo.RegisterCompleteObjectUndo(sun.transform, "Apply Dusk");
            sun.color = sunColor;
            sun.intensity = sunIntensity;
            sun.transform.rotation = Quaternion.Euler(sunAngles);
        }

        MarkDirty();
        Debug.Log("[Atmosphere] Applied eerie dusk.");
    }

    void RestoreDay()
    {
        RenderSettings.fog = false;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Skybox;
        RenderSettings.ambientIntensity = 1f;

        Light sun = RenderSettings.sun;
        if (sun == null) sun = FindDirectionalLight();
        if (sun != null)
        {
            sun.color = new Color(1f, 0.957f, 0.839f);
            sun.intensity = 1f;
            sun.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }

        MarkDirty();
        Debug.Log("[Atmosphere] Restored bright day.");
    }

    static Light FindDirectionalLight()
    {
        foreach (Light l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            if (l.type == LightType.Directional) return l;
        return null;
    }

    static void MarkDirty()
    {
        if (!Application.isPlaying)
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
    }
}
