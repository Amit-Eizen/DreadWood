using UnityEngine;
using UnityEditor;

// Scatters health pickups along the trail between the cabin (start) and the
// light (end), dropped onto the ground at a hover height. Keeps them roughly in
// the play corridor (not too far sideways) and always drops ONE in the boss
// arena at the light, so there's a heal right before the final fight.
//
// Open via:  Tools > DreadWood > Health Pickup Scatter
public class HealthPickupScatterTool : EditorWindow
{
    public GameObject pickupPrefab;
    public int count = 6;                          // random ones along the trail
    public float hoverHeight = 1f;                 // float above the ground
    public Vector2 startXZ = new Vector2(0f, -80f);// cabin
    public Vector2 endXZ = new Vector2(0f, 85f);   // light / boss arena
    public float avoidStartRadius = 18f;           // don't drop right on the cabin
    public float avoidEndRadius = 14f;             // (the guaranteed one covers the arena)
    public float corridorHalfWidth = 22f;          // keep them near the trail (X spread)
    public float maxSteepness = 30f;               // not on cliffs

    public bool placeInBossArena = true;
    public float bossArenaOffset = 4f;             // how far from the light the arena pickup sits

    const string Root = "HealthPickups";

    [MenuItem("Tools/DreadWood/Health Pickup Scatter")]
    static void Open() => GetWindow<HealthPickupScatterTool>("Health Pickup Scatter");

    void OnGUI()
    {
        EditorGUILayout.LabelField("Scatter health pickups along the trail", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        pickupPrefab = (GameObject)EditorGUILayout.ObjectField("Pickup Prefab", pickupPrefab, typeof(GameObject), false);
        count = EditorGUILayout.IntSlider("Random count", count, 1, 20);
        hoverHeight = EditorGUILayout.Slider("Hover height", hoverHeight, 0f, 3f);
        startXZ = EditorGUILayout.Vector2Field("Start / cabin (X,Z)", startXZ);
        endXZ = EditorGUILayout.Vector2Field("Light / boss (X,Z)", endXZ);
        avoidStartRadius = EditorGUILayout.Slider("Clear around cabin", avoidStartRadius, 0f, 60f);
        avoidEndRadius = EditorGUILayout.Slider("Clear around light", avoidEndRadius, 0f, 60f);
        corridorHalfWidth = EditorGUILayout.Slider("Trail half-width", corridorHalfWidth, 5f, 60f);
        placeInBossArena = EditorGUILayout.Toggle("One in boss arena", placeInBossArena);

        EditorGUILayout.Space();
        if (GUILayout.Button("Scatter Pickups", GUILayout.Height(30))) Scatter();
        if (GUILayout.Button("Clear Pickups")) Clear();
        EditorGUILayout.HelpBox("Drag your HealthPickup prefab in, then Scatter. Always drops one near the light for the boss fight. Run again to add more; Clear to start over.", MessageType.Info);
    }

    void Scatter()
    {
        if (pickupPrefab == null) { EditorUtility.DisplayDialog("Health Pickup Scatter", "Assign the HealthPickup prefab first.", "OK"); return; }
        Terrain terrain = Terrain.activeTerrain;
        if (terrain == null) { EditorUtility.DisplayDialog("Health Pickup Scatter", "No active Terrain in the scene.", "OK"); return; }

        TerrainData td = terrain.terrainData;
        Vector3 tPos = terrain.transform.position;

        GameObject root = GameObject.Find(Root);
        if (root == null) { root = new GameObject(Root); Undo.RegisterCreatedObjectUndo(root, "Create HealthPickups"); }

        Vector3 start = new Vector3(startXZ.x, 0f, startXZ.y);
        Vector3 end = new Vector3(endXZ.x, 0f, endXZ.y);

        int placed = 0, attempts = 0, maxAttempts = count * 40;
        while (placed < count && attempts < maxAttempts)
        {
            attempts++;

            // walk down the trail (Z from start to end) with some sideways spread (X)
            float t = Random.value;
            float z = Mathf.Lerp(start.z, end.z, t);
            float x = Mathf.Lerp(start.x, end.x, t) + Random.Range(-corridorHalfWidth, corridorHalfWidth);

            Vector3 wp = new Vector3(x, 0f, z);
            wp.y = terrain.SampleHeight(wp) + tPos.y + hoverHeight;

            Vector3 flat = new Vector3(x, 0f, z);
            if (Vector3.Distance(flat, start) < avoidStartRadius) continue;
            if (Vector3.Distance(flat, end) < avoidEndRadius) continue;

            // steepness check (needs normalized terrain coords)
            float nx = (x - tPos.x) / td.size.x;
            float nz = (z - tPos.z) / td.size.z;
            if (nx < 0f || nx > 1f || nz < 0f || nz > 1f) continue;
            if (td.GetSteepness(nx, nz) > maxSteepness) continue;

            Place(root, wp);
            placed++;
        }

        // guaranteed one in the boss arena, just shy of the light
        if (placeInBossArena)
        {
            Vector3 arena = new Vector3(end.x + bossArenaOffset, 0f, end.z - bossArenaOffset);
            arena.y = terrain.SampleHeight(arena) + tPos.y + hoverHeight;
            Place(root, arena);
        }

        Debug.Log($"[HealthPickupScatter] Placed {placed} random pickups{(placeInBossArena ? " + 1 in the boss arena" : "")} under '{Root}'.");
    }

    void Place(GameObject root, Vector3 pos)
    {
        GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(pickupPrefab, root.transform);
        go.transform.position = pos;
        Undo.RegisterCreatedObjectUndo(go, "Scatter HealthPickup");
    }

    void Clear()
    {
        GameObject root = GameObject.Find(Root);
        if (root != null) { Undo.DestroyObjectImmediate(root); Debug.Log("[HealthPickupScatter] Cleared."); }
    }
}
