using UnityEngine;
using UnityEditor;

// Simple editor tool to scatter tree prefabs across the active Terrain as
// normal scene objects. We use this instead of the Terrain "Paint Trees"
// system because the Nature Starter Kit 2 tree prefabs keep their
// MeshRenderer on a child object, which the Terrain tree instancer rejects
// ("no valid mesh renderer"). As regular GameObjects they render fine.
//
// Open via:  Tools > DreadWood > Tree Scatter
public class TreeScatterTool : EditorWindow
{
    public GameObject[] treePrefabs;
    public int treeCount = 60;
    public float avoidCenterRadius = 14f;   // keep a clearing/trail in the middle
    public float maxSteepness = 35f;        // don't put trees on steep cliffs
    public Vector2 scaleRange = new Vector2(0.8f, 1.4f);
    public bool addColliders = true;        // so the player can't walk through trunks

    const string ForestRootName = "Forest";

    [MenuItem("Tools/DreadWood/Tree Scatter")]
    static void Open()
    {
        GetWindow<TreeScatterTool>("Tree Scatter");
    }

    void OnEnable()
    {
        // Auto-load the Nature Starter Kit 2 trees if nothing is assigned yet
        if (treePrefabs == null || treePrefabs.Length == 0)
        {
            treePrefabs = new GameObject[]
            {
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/NatureStarterKit2/Nature/tree01.prefab"),
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/NatureStarterKit2/Nature/tree02.prefab"),
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/NatureStarterKit2/Nature/tree04.prefab"),
            };
        }
    }

    void OnGUI()
    {
        EditorGUILayout.LabelField("Scatter trees on the active Terrain", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        SerializedObject so = new SerializedObject(this);
        EditorGUILayout.PropertyField(so.FindProperty("treePrefabs"), true);
        treeCount = EditorGUILayout.IntSlider("Tree Count", treeCount, 1, 400);
        avoidCenterRadius = EditorGUILayout.Slider("Clearing Radius (trail)", avoidCenterRadius, 0f, 60f);
        maxSteepness = EditorGUILayout.Slider("Max Slope (deg)", maxSteepness, 0f, 90f);
        scaleRange = EditorGUILayout.Vector2Field("Scale Range (min/max)", scaleRange);
        addColliders = EditorGUILayout.Toggle("Add Trunk Colliders", addColliders);
        so.ApplyModifiedProperties();

        EditorGUILayout.Space();
        if (GUILayout.Button("Scatter Trees", GUILayout.Height(30))) Scatter();
        if (GUILayout.Button("Clear Forest")) Clear();

        EditorGUILayout.HelpBox(
            "Scatter adds trees under a 'Forest' object, leaving a clearing in the middle for the trail.\n" +
            "Run it again to add more; use Clear Forest to start over.", MessageType.Info);
    }

    void Scatter()
    {
        Terrain terrain = Terrain.activeTerrain;
        if (terrain == null)
        {
            EditorUtility.DisplayDialog("Tree Scatter", "No active Terrain found in the scene.", "OK");
            return;
        }

        // Validate prefabs
        bool anyPrefab = false;
        if (treePrefabs != null)
            foreach (var p in treePrefabs) if (p != null) anyPrefab = true;
        if (!anyPrefab)
        {
            EditorUtility.DisplayDialog("Tree Scatter", "Assign at least one tree prefab.", "OK");
            return;
        }

        TerrainData td = terrain.terrainData;
        Vector3 tPos = terrain.transform.position;
        Vector3 center = tPos + new Vector3(td.size.x / 2f, 0f, td.size.z / 2f);

        GameObject forest = GameObject.Find(ForestRootName);
        if (forest == null)
        {
            forest = new GameObject(ForestRootName);
            Undo.RegisterCreatedObjectUndo(forest, "Create Forest");
        }

        int placed = 0;
        int attempts = 0;
        int maxAttempts = treeCount * 30;

        while (placed < treeCount && attempts < maxAttempts)
        {
            attempts++;

            float x = Random.Range(0f, td.size.x);
            float z = Random.Range(0f, td.size.z);
            Vector3 worldPos = tPos + new Vector3(x, 0f, z);
            worldPos.y = terrain.SampleHeight(worldPos) + tPos.y;

            // Keep the middle clear for the trail toward the light
            Vector3 flat = new Vector3(worldPos.x, center.y, worldPos.z);
            if (Vector3.Distance(flat, center) < avoidCenterRadius) continue;

            // Skip steep slopes (GetSteepness wants normalized 0..1 coords)
            float steep = td.GetSteepness(x / td.size.x, z / td.size.z);
            if (steep > maxSteepness) continue;

            GameObject prefab = treePrefabs[Random.Range(0, treePrefabs.Length)];
            if (prefab == null) continue;

            GameObject tree = (GameObject)PrefabUtility.InstantiatePrefab(prefab, forest.transform);
            tree.transform.position = worldPos;
            tree.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            float s = Random.Range(scaleRange.x, scaleRange.y);
            tree.transform.localScale = new Vector3(s, s, s);

            if (addColliders && tree.GetComponentInChildren<Collider>() == null)
            {
                CapsuleCollider cc = tree.AddComponent<CapsuleCollider>();
                cc.radius = 0.4f;
                cc.height = 6f;
                cc.center = new Vector3(0f, 3f, 0f);
            }

            Undo.RegisterCreatedObjectUndo(tree, "Scatter Tree");
            placed++;
        }

        Debug.Log($"[Tree Scatter] Placed {placed} trees under '{ForestRootName}'.");
    }

    void Clear()
    {
        GameObject forest = GameObject.Find(ForestRootName);
        if (forest != null)
        {
            Undo.DestroyObjectImmediate(forest);
            Debug.Log("[Tree Scatter] Cleared the Forest.");
        }
    }
}
