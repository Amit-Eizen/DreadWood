using UnityEngine;
using UnityEditor;
using System.IO;

// One-click terrain restore for DreadWood.
// Builds a FRESH TerrainData (gentle hills in the middle, mountains around the
// edges), reuses the surviving green ground layer if found, assigns it to the
// scene's "Terrain" object (or creates one), and SNAPS the existing Forest
// trees onto the new ground so the winding-trail layout is preserved.
//
// Open via:  Tools > DreadWood > Build Terrain
public class TerrainBuilder : EditorWindow
{
    public float width = 200f;
    public float length = 200f;
    public float height = 60f;
    public int resolution = 513;                 // must be 2^n + 1
    public Vector2 positionXZ = new Vector2(-100f, -100f);
    public float hillStrength = 0.12f;           // gentle rolling hills in the middle
    public float hillFrequency = 6f;
    public float edgeMountains = 0.5f;           // how tall the surrounding mountains are
    public string fallbackGroundTexture = "Assets/NatureStarterKit2/Textures/ground01.tga";
    public bool snapForest = true;

    const string TerrainGOName = "Terrain";
    const string ForestName = "Forest";
    const string DataPath = "Assets/DreadWood/DreadWoodTerrain.asset";
    const string LayerPath = "Assets/DreadWood/DreadWoodGround.terrainlayer";

    [MenuItem("Tools/DreadWood/Build Terrain")]
    static void Open() => GetWindow<TerrainBuilder>("Terrain Builder");

    void OnGUI()
    {
        EditorGUILayout.LabelField("Restore the terrain surface (keeps your tree layout)", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        width = EditorGUILayout.FloatField("Width", width);
        length = EditorGUILayout.FloatField("Length", length);
        height = EditorGUILayout.FloatField("Max Height", height);
        positionXZ = EditorGUILayout.Vector2Field("Position (X, Z)", positionXZ);
        hillStrength = EditorGUILayout.Slider("Hill Strength", hillStrength, 0f, 0.4f);
        hillFrequency = EditorGUILayout.Slider("Hill Frequency", hillFrequency, 1f, 15f);
        edgeMountains = EditorGUILayout.Slider("Edge Mountains", edgeMountains, 0f, 1f);
        snapForest = EditorGUILayout.Toggle("Snap Forest to Ground", snapForest);

        EditorGUILayout.Space();
        if (GUILayout.Button("Build Terrain", GUILayout.Height(30))) Build();
        EditorGUILayout.HelpBox(
            "Builds a fresh terrain, reuses the surviving green ground layer, and drops\n" +
            "your existing trees onto it. After this, re-run Build Cabin to re-seat the cabin.",
            MessageType.Info);
    }

    void Build()
    {
        if (!Directory.Exists("Assets/DreadWood")) { Directory.CreateDirectory("Assets/DreadWood"); AssetDatabase.Refresh(); }

        // Fresh TerrainData
        TerrainData td = new TerrainData();
        td.heightmapResolution = resolution;
        td.size = new Vector3(width, height, length);

        int res = td.heightmapResolution;
        float[,] h = new float[res, res];
        for (int y = 0; y < res; y++)
        {
            for (int x = 0; x < res; x++)
            {
                float nx = (float)x / (res - 1);
                float ny = (float)y / (res - 1);
                float hills = Mathf.PerlinNoise(nx * hillFrequency, ny * hillFrequency) * hillStrength;
                float d = Mathf.Max(Mathf.Abs(nx - 0.5f), Mathf.Abs(ny - 0.5f)) * 2f;
                float edge = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.55f, 1f, d)) * edgeMountains;
                h[y, x] = Mathf.Clamp01(hills + edge);
            }
        }
        td.SetHeights(0, 0, h);
        td.terrainLayers = new TerrainLayer[] { GetGroundLayer() };

        // Save the TerrainData asset (replace if it exists)
        if (AssetDatabase.LoadAssetAtPath<TerrainData>(DataPath) != null)
            AssetDatabase.DeleteAsset(DataPath);
        AssetDatabase.CreateAsset(td, DataPath);
        AssetDatabase.SaveAssets();

        // Assign to the scene's Terrain object (or create one)
        GameObject go = GameObject.Find(TerrainGOName);
        if (go == null)
        {
            go = Terrain.CreateTerrainGameObject(td);
            go.name = TerrainGOName;
            Undo.RegisterCreatedObjectUndo(go, "Build Terrain");
        }
        else
        {
            Terrain t = go.GetComponent<Terrain>() ?? go.AddComponent<Terrain>();
            t.terrainData = td;
            TerrainCollider col = go.GetComponent<TerrainCollider>() ?? go.AddComponent<TerrainCollider>();
            col.terrainData = td;
        }
        go.transform.position = new Vector3(positionXZ.x, 0f, positionXZ.y);

        Terrain terrain = go.GetComponent<Terrain>();
        if (snapForest) SnapForest(terrain);

        Selection.activeGameObject = go;
        Debug.Log("[TerrainBuilder] Terrain restored + trees snapped: " + DataPath);
    }

    // Reuse a surviving TerrainLayer (the green ground Unity moved to _TerrainAutoUpgrade), else make one
    TerrainLayer GetGroundLayer()
    {
        string[] found = AssetDatabase.FindAssets("t:TerrainLayer");
        foreach (string guid in found)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.Contains("_TerrainAutoUpgrade") || path.Contains("DreadWoodGround"))
                return AssetDatabase.LoadAssetAtPath<TerrainLayer>(path);
        }

        TerrainLayer layer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(LayerPath);
        if (layer == null) { layer = new TerrainLayer(); AssetDatabase.CreateAsset(layer, LayerPath); }
        Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(fallbackGroundTexture);
        if (tex != null) layer.diffuseTexture = tex;
        layer.tileSize = new Vector2(15f, 15f);
        EditorUtility.SetDirty(layer);
        return layer;
    }

    // Drop every tree under "Forest" onto the new ground (keeps X/Z = the trail layout)
    void SnapForest(Terrain terrain)
    {
        GameObject forest = GameObject.Find(ForestName);
        if (forest == null || terrain == null) return;

        int snapped = 0;
        foreach (Transform tree in forest.transform)
        {
            Vector3 p = tree.position;
            p.y = terrain.SampleHeight(p) + terrain.transform.position.y;
            Undo.RecordObject(tree, "Snap Tree");
            tree.position = p;
            snapped++;
        }
        Debug.Log($"[TerrainBuilder] Snapped {snapped} trees to the new ground.");
    }
}
