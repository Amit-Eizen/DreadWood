using UnityEngine;
using UnityEditor;

// Scatters zombie prefabs across the active Terrain as normal scene objects,
// dropped onto the ground. Keeps a clear radius around the start (cabin) and
// the end (light/boss) so the player isn't swarmed at spawn or the finale.
//
// Open via:  Tools > DreadWood > Zombie Scatter
public class ZombieScatterTool : EditorWindow
{
    public GameObject zombiePrefab;
    public int count = 15;
    public float avoidStartRadius = 28f;        // clear area around the cabin
    public float avoidEndRadius = 28f;          // clear area around the light/boss
    public Vector2 startXZ = new Vector2(0f, -80f);
    public Vector2 endXZ = new Vector2(0f, 85f);
    public float maxSteepness = 32f;            // don't drop zombies on cliffs

    const string Root = "Zombies";

    [MenuItem("Tools/DreadWood/Zombie Scatter")]
    static void Open() => GetWindow<ZombieScatterTool>("Zombie Scatter");

    void OnGUI()
    {
        EditorGUILayout.LabelField("Scatter zombies on the Terrain", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        zombiePrefab = (GameObject)EditorGUILayout.ObjectField("Zombie Prefab", zombiePrefab, typeof(GameObject), false);
        count = EditorGUILayout.IntSlider("Count", count, 1, 60);
        avoidStartRadius = EditorGUILayout.Slider("Clear around start", avoidStartRadius, 0f, 80f);
        avoidEndRadius = EditorGUILayout.Slider("Clear around light", avoidEndRadius, 0f, 80f);
        startXZ = EditorGUILayout.Vector2Field("Start (X,Z)", startXZ);
        endXZ = EditorGUILayout.Vector2Field("Light (X,Z)", endXZ);

        EditorGUILayout.Space();
        if (GUILayout.Button("Scatter Zombies", GUILayout.Height(30))) Scatter();
        if (GUILayout.Button("Clear Zombies")) Clear();
        EditorGUILayout.HelpBox("Drag your Zombie prefab in, set the count, and Scatter. Run again to add more; Clear Zombies to start over.", MessageType.Info);
    }

    void Scatter()
    {
        if (zombiePrefab == null) { EditorUtility.DisplayDialog("Zombie Scatter", "Assign the Zombie prefab first.", "OK"); return; }
        Terrain terrain = Terrain.activeTerrain;
        if (terrain == null) { EditorUtility.DisplayDialog("Zombie Scatter", "No active Terrain in the scene.", "OK"); return; }

        TerrainData td = terrain.terrainData;
        Vector3 tPos = terrain.transform.position;

        GameObject root = GameObject.Find(Root);
        if (root == null) { root = new GameObject(Root); Undo.RegisterCreatedObjectUndo(root, "Create Zombies"); }

        Vector3 start = new Vector3(startXZ.x, 0f, startXZ.y);
        Vector3 end = new Vector3(endXZ.x, 0f, endXZ.y);

        int placed = 0, attempts = 0, maxAttempts = count * 40;
        while (placed < count && attempts < maxAttempts)
        {
            attempts++;
            float x = Random.Range(0f, td.size.x);
            float z = Random.Range(0f, td.size.z);
            Vector3 wp = tPos + new Vector3(x, 0f, z);
            wp.y = terrain.SampleHeight(wp) + tPos.y;

            Vector3 flat = new Vector3(wp.x, 0f, wp.z);
            if (Vector3.Distance(flat, start) < avoidStartRadius) continue;
            if (Vector3.Distance(flat, end) < avoidEndRadius) continue;
            if (td.GetSteepness(x / td.size.x, z / td.size.z) > maxSteepness) continue;

            GameObject zo = (GameObject)PrefabUtility.InstantiatePrefab(zombiePrefab, root.transform);
            zo.transform.position = wp;
            zo.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            Undo.RegisterCreatedObjectUndo(zo, "Scatter Zombie");
            placed++;
        }

        Debug.Log($"[ZombieScatter] Placed {placed} zombies under '{Root}'.");
    }

    void Clear()
    {
        GameObject root = GameObject.Find(Root);
        if (root != null) { Undo.DestroyObjectImmediate(root); Debug.Log("[ZombieScatter] Cleared."); }
    }
}
