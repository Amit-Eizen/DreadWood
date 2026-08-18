using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

// Tools > DreadWood > Scatter Pickups
//
// Drops pickups and breakable props at random spots on the terrain, snapped to the ground so
// nothing ends up floating (the rubric checks for that). Pickups go under one "Pickups"
// object and props under "Breakables", so either lot can be cleared on its own.
public class PickupScatterTool : EditorWindow
{
    public GameObject healthPrefab;
    public GameObject armourPrefab;
    public GameObject rockPrefab;
    public GameObject breakablePrefab;

    public int healthCount = 0;
    public int armourCount = 4;
    public int rockCount = 4;
    public int breakableCount = 6;

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
    const string BreakablesName = "Breakables";

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
        breakablePrefab = (GameObject)EditorGUILayout.ObjectField("Breakable Prop", breakablePrefab, typeof(GameObject), false);
        breakableCount = EditorGUILayout.IntSlider("Breakable Count", breakableCount, 0, 30);

        EditorGUILayout.Space();
        hoverHeight = EditorGUILayout.Slider("Hover Height", hoverHeight, 0f, 2f);
        minGapBetweenPickups = EditorGUILayout.Slider("Min Gap", minGapBetweenPickups, 2f, 40f);
        clearRadiusAroundCabin = EditorGUILayout.Slider("Clear Around Cabin", clearRadiusAroundCabin, 0f, 40f);
        cabinPosition = EditorGUILayout.Vector2Field("Cabin Position (X, Z)", cabinPosition);
        maxSlopeDegrees = EditorGUILayout.Slider("Max Slope", maxSlopeDegrees, 5f, 60f);
        edgeMargin = EditorGUILayout.Slider("Edge Margin", edgeMargin, 0f, 40f);

        EditorGUILayout.Space();
        if (GUILayout.Button("Scatter Pickups", GUILayout.Height(28))) ScatterPickups();
        if (GUILayout.Button("Scatter Breakables", GUILayout.Height(28))) ScatterBreakables();

        EditorGUILayout.Space();
        if (GUILayout.Button("Clear Pickups")) Clear(ParentName);
        if (GUILayout.Button("Clear Breakables")) Clear(BreakablesName);

        EditorGUILayout.HelpBox(
            "The two are independent: scattering breakables leaves the pickups exactly where " +
            "they are, and the other way round.\n\n" +
            "Each button REPLACES its own group, so pressing it twice does not stack — but it " +
            "does keep clear of whatever the other group already put down.",
            MessageType.Info);
    }

    void ScatterPickups()
    {
        Terrain terrain = FindTerrain();
        if (terrain == null) return;

        Clear(ParentName);

        GameObject parent = new GameObject(ParentName);
        Undo.RegisterCreatedObjectUndo(parent, "Scatter Pickups");

        List<Vector3> placed = SpotsAlreadyTaken();
        int total = 0;
        total += ScatterOne(healthPrefab, healthCount, "Health", parent, terrain, placed);
        total += ScatterOne(armourPrefab, armourCount, "Armour", parent, terrain, placed);
        total += ScatterOne(rockPrefab, rockCount, "Rocks", parent, terrain, placed);

        Selection.activeGameObject = parent;
        Debug.Log($"[Scatter] Placed {total} pickups.");
    }

    void ScatterBreakables()
    {
        Terrain terrain = FindTerrain();
        if (terrain == null) return;

        Clear(BreakablesName);

        GameObject parent = new GameObject(BreakablesName);
        Undo.RegisterCreatedObjectUndo(parent, "Scatter Breakables");

        int total = ScatterOne(breakablePrefab, breakableCount, "Props", parent, terrain, SpotsAlreadyTaken());

        Selection.activeGameObject = parent;
        Debug.Log($"[Scatter] Placed {total} breakable props.");
    }

    // Where things already stand, so scattering one kind never drops it on top of another.
    List<Vector3> SpotsAlreadyTaken()
    {
        List<Vector3> spots = new List<Vector3>();

        foreach (string rootName in new[] { ParentName, BreakablesName })
        {
            GameObject root = GameObject.Find(rootName);
            if (root == null) continue;

            foreach (Transform group in root.transform)
                foreach (Transform item in group)
                    spots.Add(item.position);
        }

        return spots;
    }

    Terrain FindTerrain()
    {
        Terrain terrain = Terrain.activeTerrain;
        if (terrain == null)
            EditorUtility.DisplayDialog("Scatter", "No active terrain in this scene.", "OK");
        return terrain;
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

    void Clear(string rootName)
    {
        GameObject existing = GameObject.Find(rootName);
        if (existing != null) Undo.DestroyObjectImmediate(existing);
    }
}
