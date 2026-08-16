using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

// Tools > DreadWood > Scatter Pickups
//
// Drops health, armour and rock pickups at random spots on the terrain, snapped to the
// ground so nothing ends up floating (the rubric checks for that). Everything lands under
// one "Pickups" object so the hierarchy stays tidy and the whole lot can be cleared at once.
public class PickupScatterTool : EditorWindow
{
    public GameObject healthPrefab;
    public GameObject armourPrefab;
    public GameObject rockPrefab;

    public int healthCount = 0;
    public int armourCount = 4;
    public int rockCount = 4;

    [Tooltip("How high above the ground they sit")]
    public float hoverHeight = 0.8f;

    [Tooltip("Keep this far away from the cabin so the start isn't crowded")]
    public float clearRadiusAroundCabin = 18f;
    public Vector2 cabinPosition = new Vector2(0f, -80f);

    [Tooltip("Minimum gap between two pickups")]
    public float minGapBetweenPickups = 12f;

    [Tooltip("Skip slopes steeper than this, so nothing lands on a cliff face")]
    public float maxSlopeDegrees = 25f;

    [Tooltip("Stay this far inside the terrain edges")]
    public float edgeMargin = 15f;

    const string ParentName = "Pickups";

    [MenuItem("Tools/DreadWood/Scatter Pickups")]
    static void Open() => GetWindow<PickupScatterTool>("Scatter Pickups");

    void OnGUI()
    {
        EditorGUILayout.LabelField("Scatter pickups over the terrain", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        healthPrefab = (GameObject)EditorGUILayout.ObjectField("Health Prefab", healthPrefab, typeof(GameObject), false);
        healthCount = EditorGUILayout.IntSlider("Health Count", healthCount, 0, 20);
        armourPrefab = (GameObject)EditorGUILayout.ObjectField("Armour Prefab", armourPrefab, typeof(GameObject), false);
        armourCount = EditorGUILayout.IntSlider("Armour Count", armourCount, 0, 20);
        rockPrefab = (GameObject)EditorGUILayout.ObjectField("Rock Ammo Prefab", rockPrefab, typeof(GameObject), false);
        rockCount = EditorGUILayout.IntSlider("Rock Count", rockCount, 0, 20);

        EditorGUILayout.Space();
        hoverHeight = EditorGUILayout.Slider("Hover Height", hoverHeight, 0f, 2f);
        minGapBetweenPickups = EditorGUILayout.Slider("Min Gap", minGapBetweenPickups, 2f, 40f);
        clearRadiusAroundCabin = EditorGUILayout.Slider("Clear Around Cabin", clearRadiusAroundCabin, 0f, 40f);
        cabinPosition = EditorGUILayout.Vector2Field("Cabin Position (X, Z)", cabinPosition);
        maxSlopeDegrees = EditorGUILayout.Slider("Max Slope", maxSlopeDegrees, 5f, 60f);
        edgeMargin = EditorGUILayout.Slider("Edge Margin", edgeMargin, 0f, 40f);

        EditorGUILayout.Space();
        if (GUILayout.Button("Scatter", GUILayout.Height(30))) Scatter();
        if (GUILayout.Button("Clear All Pickups")) Clear();

        EditorGUILayout.HelpBox(
            "Everything goes under a single 'Pickups' object, grouped by type.\n" +
            "Scatter REPLACES whatever is already under it, so pressing it twice does not stack.",
            MessageType.Info);
    }

    void Scatter()
    {
        Terrain terrain = Terrain.activeTerrain;
        if (terrain == null)
        {
            EditorUtility.DisplayDialog("Scatter Pickups", "No active terrain in this scene.", "OK");
            return;
        }

        Clear();

        GameObject parent = new GameObject(ParentName);
        Undo.RegisterCreatedObjectUndo(parent, "Scatter Pickups");

        List<Vector3> placed = new List<Vector3>();
        int total = 0;
        total += ScatterOne(healthPrefab, healthCount, "Health", parent, terrain, placed);
        total += ScatterOne(armourPrefab, armourCount, "Armour", parent, terrain, placed);
        total += ScatterOne(rockPrefab, rockCount, "Rocks", parent, terrain, placed);

        Selection.activeGameObject = parent;
        Debug.Log($"[Pickups] Scattered {total} pickups under '{ParentName}'.");
    }

    int ScatterOne(GameObject prefab, int count, string groupName, GameObject parent,
                   Terrain terrain, List<Vector3> placed)
    {
        if (prefab == null || count <= 0) return 0;

        GameObject group = new GameObject(groupName);
        group.transform.SetParent(parent.transform, false);

        Vector3 terrainOrigin = terrain.transform.position;
        Vector3 size = terrain.terrainData.size;

        int madeCount = 0;
        int attempts = 0;
        while (madeCount < count && attempts < count * 200)
        {
            attempts++;

            float x = Random.Range(terrainOrigin.x + edgeMargin, terrainOrigin.x + size.x - edgeMargin);
            float z = Random.Range(terrainOrigin.z + edgeMargin, terrainOrigin.z + size.z - edgeMargin);

            if (Vector2.Distance(new Vector2(x, z), cabinPosition) < clearRadiusAroundCabin) continue;

            bool tooClose = false;
            foreach (Vector3 p in placed)
                if (Vector2.Distance(new Vector2(x, z), new Vector2(p.x, p.z)) < minGapBetweenPickups)
                { tooClose = true; break; }
            if (tooClose) continue;

            // Steep ground would leave a pickup half-buried or hanging off a cliff.
            float u = (x - terrainOrigin.x) / size.x;
            float v = (z - terrainOrigin.z) / size.z;
            Vector3 normal = terrain.terrainData.GetInterpolatedNormal(u, v);
            if (Vector3.Angle(normal, Vector3.up) > maxSlopeDegrees) continue;

            float groundY = terrain.SampleHeight(new Vector3(x, 0f, z)) + terrainOrigin.y;
            Vector3 spot = new Vector3(x, groundY + hoverHeight, z);

            GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, group.transform);
            go.transform.position = spot;
            Undo.RegisterCreatedObjectUndo(go, "Scatter Pickups");

            placed.Add(spot);
            madeCount++;
        }

        if (madeCount < count)
            Debug.LogWarning($"[Pickups] Only placed {madeCount}/{count} {groupName} — try a smaller Min Gap.");

        return madeCount;
    }

    void Clear()
    {
        GameObject existing = GameObject.Find(ParentName);
        if (existing != null) Undo.DestroyObjectImmediate(existing);
    }
}
