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
    public bool paintGrass = true;      // auto-scatter grass details after rebuilding
    [Range(0f, 1f)] public float grassDensity = 0.5f;
    public float grassMaxSteepness = 30f;   // no grass on cliffs steeper than this

    [MenuItem("Tools/DreadWood/Apply Recovered Terrain")]
    static void Open() => GetWindow<ApplyRecoveredTerrain>("Recover Terrain");

    // Quick cycle through the NSK2 ground textures on the existing terrain, so you
    // can SEE which one is the green grass you want — without rebuilding anything.
    [MenuItem("Tools/DreadWood/Cycle Ground Texture")]
    static void CycleGroundTexture()
    {
        GameObject go = GameObject.Find(TerrainGOName);
        Terrain terrain = go != null ? go.GetComponent<Terrain>() : null;
        if (terrain == null || terrain.terrainData == null)
        {
            EditorUtility.DisplayDialog("Cycle Ground Texture", "No 'Terrain' with TerrainData found.", "OK");
            return;
        }

        string[] options = {
            "Assets/NatureStarterKit2/Textures/ground01.tga",
            "Assets/NatureStarterKit2/Textures/ground02.tga",
            "Assets/NatureStarterKit2/Textures/ground03.tga",
        };

        TerrainData td = terrain.terrainData;
        TerrainLayer layer = (td.terrainLayers != null && td.terrainLayers.Length > 0) ? td.terrainLayers[0] : null;
        if (layer == null)
        {
            layer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(GroundLayerPath);
            if (layer == null) { layer = new TerrainLayer(); AssetDatabase.CreateAsset(layer, GroundLayerPath); }
            td.terrainLayers = new TerrainLayer[] { layer };
        }

        // figure out current texture, advance to the next
        int cur = 0;
        for (int i = 0; i < options.Length; i++)
            if (layer.diffuseTexture == AssetDatabase.LoadAssetAtPath<Texture2D>(options[i])) { cur = i; break; }
        int next = (cur + 1) % options.Length;

        layer.diffuseTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(options[next]);
        layer.tileSize = new Vector2(15f, 15f);
        EditorUtility.SetDirty(layer);
        AssetDatabase.SaveAssets();
        Debug.Log($"[DreadWood] Ground texture -> {System.IO.Path.GetFileName(options[next])}. Run again to cycle.");
    }

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
        EditorGUILayout.Space();
        paintGrass = EditorGUILayout.Toggle("Auto-paint grass", paintGrass);
        using (new EditorGUI.DisabledScope(!paintGrass))
        {
            grassDensity = EditorGUILayout.Slider("Grass density", grassDensity, 0f, 1f);
            grassMaxSteepness = EditorGUILayout.Slider("Grass max steepness", grassMaxSteepness, 5f, 60f);
        }
        EditorGUILayout.Space();
        bool exists = File.Exists(BinPath);
        EditorGUILayout.HelpBox(exists
            ? "Recovered heightmap found. Click to rebuild your exact terrain — heights, ground, trees, and grass all at once."
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

        if (paintGrass) PaintGrass(td);

        EditorUtility.SetDirty(td);
        AssetDatabase.SaveAssets();

        Selection.activeGameObject = go;
        Debug.Log($"[Recover] Original terrain restored ({w}x{h}, height {sizeY:0}). Trees re-seated{(paintGrass ? ", grass painted" : "")}.");
        EditorUtility.DisplayDialog("Recover Terrain", "Your terrain is back — heights, ground, trees" + (paintGrass ? ", and grass" : "") + "!\n\nNow: save the scene, then close & reopen Unity to confirm it loads clean, then commit.", "Great");
    }

    // Scatters grass detail over the terrain automatically, skipping steep cliffs,
    // so you don't have to hand-paint it after every recovery.
    void PaintGrass(TerrainData td)
    {
        TerrainLayer ground = td.terrainLayers != null && td.terrainLayers.Length > 0 ? td.terrainLayers[0] : null;

        // build a grass detail prototype from the NSK2 grass texture
        Texture2D grassTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/NatureStarterKit2/Textures/grass01.tga");
        if (grassTex == null) { Debug.LogWarning("[Recover] grass01.tga not found — skipping grass."); return; }

        DetailPrototype grass = new DetailPrototype
        {
            prototypeTexture = grassTex,
            renderMode = DetailRenderMode.GrassBillboard,
            healthyColor = new Color(0.5f, 0.65f, 0.35f),
            dryColor = new Color(0.45f, 0.55f, 0.3f),
            minWidth = 0.6f, maxWidth = 1.4f,
            minHeight = 0.6f, maxHeight = 1.4f,
            noiseSpread = 0.3f,
            usePrototypeMesh = false
        };
        td.detailPrototypes = new DetailPrototype[] { grass };
        td.SetDetailResolution(512, 16);
        td.wavingGrassStrength = 0.25f;
        td.wavingGrassAmount = 0.15f;
        td.wavingGrassSpeed = 0.3f;
        td.wavingGrassTint = new Color(0.7f, 0.8f, 0.6f);

        int res = td.detailResolution;
        int[,] map = new int[res, res];
        int maxDensity = Mathf.RoundToInt(Mathf.Lerp(0f, 12f, grassDensity));

        for (int y = 0; y < res; y++)
        {
            for (int x = 0; x < res; x++)
            {
                // normalized terrain coords (note detail map is [y,x] but steepness wants normalized x,z)
                float nx = x / (float)res;
                float nz = y / (float)res;
                float steep = td.GetSteepness(nx, nz);
                map[y, x] = steep <= grassMaxSteepness ? maxDensity : 0;
            }
        }
        td.SetDetailLayer(0, 0, 0, map);
        _ = ground;
        Debug.Log($"[Recover] Grass painted (density {grassDensity:0.0}, skipping slopes > {grassMaxSteepness:0}°).");
    }

    const string GroundLayerPath = "Assets/DreadWood/DreadWoodGround.terrainlayer";

    // Build a fresh ground layer from the GREEN opaque ground texture (ground01),
    // which is what the original terrain used. (grass01/02 are alpha grass-blade
    // textures meant for Paint Details — they render white as a ground layer, so
    // we do NOT use them here.)
    TerrainLayer FindGroundLayer()
    {
        Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/NatureStarterKit2/Textures/ground01.tga");

        TerrainLayer layer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(GroundLayerPath);
        if (layer == null)
        {
            layer = new TerrainLayer();
            AssetDatabase.CreateAsset(layer, GroundLayerPath);
        }
        if (tex != null) layer.diffuseTexture = tex;
        layer.tileSize = new Vector2(15f, 15f);   // texture repeat across the terrain
        layer.specular = Color.black;
        layer.metallic = 0f;
        layer.smoothness = 0f;
        EditorUtility.SetDirty(layer);
        return layer;
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
