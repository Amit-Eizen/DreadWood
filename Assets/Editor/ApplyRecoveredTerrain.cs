using UnityEngine;
using UnityEditor;
using System.IO;

// Rebuilds your ORIGINAL terrain from the heightmap recovered out of the
// corrupted asset (extracted to Assets/DreadWood/terrain_heights.bin).
// Creates a fresh TerrainData with the exact original heights, reuses the
// surviving green ground layer, assigns it to the "Terrain" object, and
// snaps the existing Forest trees back onto it.
//
// Open via:  Tools > DreadWood > Apply Recovered Terrain
public class ApplyRecoveredTerrain : EditorWindow
{
    const string BinPath = "Assets/DreadWood/terrain_heights.bin";
    const string DataPath = "Assets/DreadWood/RecoveredTerrain.asset";
    const string TerrainGOName = "Terrain";
    const string ForestName = "Forest";

    public float sizeXZ = 200f;       // original terrain footprint was 200 x 200
    public float terrainHeight = 1200f; // vertical scale; raise this if hills look too flat

    [MenuItem("Tools/DreadWood/Apply Recovered Terrain")]
    static void Open() => GetWindow<ApplyRecoveredTerrain>("Recover Terrain");

    // Fixes "Terrain has zero detail resolution" so grass details can be painted,
    // WITHOUT rebuilding the terrain (keeps your green ground + heights + trees).
    [MenuItem("Tools/DreadWood/Fix Detail Resolution")]
    static void FixDetailResolution()
    {
        GameObject go = GameObject.Find(TerrainGOName);
        Terrain terrain = go != null ? go.GetComponent<Terrain>() : null;
        if (terrain == null || terrain.terrainData == null)
        {
            EditorUtility.DisplayDialog("Fix Detail Resolution", "No 'Terrain' with TerrainData found in the scene.", "OK");
            return;
        }
        terrain.terrainData.SetDetailResolution(512, 16);
        EditorUtility.SetDirty(terrain.terrainData);
        AssetDatabase.SaveAssets();
        Debug.Log("[DreadWood] Detail resolution set to 512 (per-patch 16). You can paint grass now.");
        EditorUtility.DisplayDialog("Fix Detail Resolution", "Done! Now use Paint Details to paint grass.", "OK");
    }

    void OnGUI()
    {
        EditorGUILayout.LabelField("Restore your ORIGINAL terrain shape", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        sizeXZ = EditorGUILayout.FloatField("Terrain Size (X/Z)", sizeXZ);
        terrainHeight = EditorGUILayout.Slider("Terrain Height (raise if flat)", terrainHeight, 200f, 4000f);
        bool exists = File.Exists(BinPath);
        EditorGUILayout.HelpBox(exists
            ? "Recovered heightmap found. Click to rebuild your exact terrain."
            : "terrain_heights.bin not found in Assets/DreadWood!", exists ? MessageType.Info : MessageType.Error);
        EditorGUILayout.Space();
        using (new EditorGUI.DisabledScope(!exists))
            if (GUILayout.Button("Apply Recovered Terrain", GUILayout.Height(34))) Apply();
    }

    void Apply()
    {
        byte[] data = File.ReadAllBytes(BinPath);
        int w = System.BitConverter.ToInt32(data, 0);
        int h = System.BitConverter.ToInt32(data, 4);
        int n = System.BitConverter.ToInt32(data, 8);
        float scaleY = System.BitConverter.ToSingle(data, 12);
        if (n != w * h) { Debug.LogError("[Recover] header/count mismatch"); return; }

        // raw uint16 (0..65535) -> normalized 0..1, as heights[y, x]
        float[,] heights = new float[h, w];
        int baseOff = 16;
        for (int y = 0; y < h; y++)
        {
            int row = baseOff + y * w * 4;
            for (int x = 0; x < w; x++)
                heights[y, x] = System.BitConverter.ToInt32(data, row + x * 4) / 65535f;
        }

        float sizeY = terrainHeight;   // vertical scale (tune in the window if too flat/tall)
        _ = scaleY;                    // (header value kept for reference)

        TerrainData td = new TerrainData();
        td.heightmapResolution = w;                 // 513
        td.size = new Vector3(sizeXZ, sizeY, sizeXZ);
        td.SetHeights(0, 0, heights);
        td.SetDetailResolution(512, 16);   // so grass details can be painted later
        td.terrainLayers = new TerrainLayer[] { FindGroundLayer() };

        if (AssetDatabase.LoadAssetAtPath<TerrainData>(DataPath) != null)
            AssetDatabase.DeleteAsset(DataPath);
        AssetDatabase.CreateAsset(td, DataPath);
        AssetDatabase.SaveAssets();

        GameObject go = GameObject.Find(TerrainGOName);
        if (go == null)
        {
            go = Terrain.CreateTerrainGameObject(td);
            go.name = TerrainGOName;
            Undo.RegisterCreatedObjectUndo(go, "Recover Terrain");
        }
        else
        {
            Terrain t = go.GetComponent<Terrain>() ?? go.AddComponent<Terrain>();
            t.terrainData = td;
            TerrainCollider col = go.GetComponent<TerrainCollider>() ?? go.AddComponent<TerrainCollider>();
            col.terrainData = td;
        }
        go.transform.position = new Vector3(-100f, 0f, -100f); // original position

        SnapForest(go.GetComponent<Terrain>());

        Selection.activeGameObject = go;
        Debug.Log($"[Recover] Original terrain restored ({w}x{h}, height {sizeY:0}). Trees re-seated.");
        EditorUtility.DisplayDialog("Recover Terrain", "Your original terrain is back! Check the scene and press Play.", "Great");
    }

    TerrainLayer FindGroundLayer()
    {
        foreach (string guid in AssetDatabase.FindAssets("t:TerrainLayer"))
        {
            string p = AssetDatabase.GUIDToAssetPath(guid);
            if (p.Contains("_TerrainAutoUpgrade") || p.Contains("DreadWoodGround"))
            {
                TerrainLayer l = AssetDatabase.LoadAssetAtPath<TerrainLayer>(p);
                if (l != null) return l;
            }
        }
        string[] any = AssetDatabase.FindAssets("t:TerrainLayer");
        if (any.Length > 0) return AssetDatabase.LoadAssetAtPath<TerrainLayer>(AssetDatabase.GUIDToAssetPath(any[0]));
        TerrainLayer nl = new TerrainLayer();
        AssetDatabase.CreateAsset(nl, "Assets/DreadWood/DreadWoodGround.terrainlayer");
        return nl;
    }

    void SnapForest(Terrain terrain)
    {
        GameObject forest = GameObject.Find(ForestName);
        if (forest == null || terrain == null) return;
        int c = 0;
        foreach (Transform tree in forest.transform)
        {
            Vector3 p = tree.position;
            p.y = terrain.SampleHeight(p) + terrain.transform.position.y;
            Undo.RecordObject(tree, "Snap Tree");
            tree.position = p;
            c++;
        }
        Debug.Log($"[Recover] Snapped {c} trees onto the recovered terrain.");
    }
}
