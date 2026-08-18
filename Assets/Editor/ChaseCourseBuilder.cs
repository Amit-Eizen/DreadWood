using UnityEngine;
using UnityEditor;

// One-click builder for the chase corridor. It lays out the shell only — floor, rock walls,
// a slalom of blocks, gaps to jump, and the empty space below them. Checkpoints, teleports,
// boulders and barriers go in by hand afterwards, wherever they feel right.
//
// Open via:  Tools > DreadWood > Build Chase Course
public class ChaseCourseBuilder : EditorWindow
{
    public float courseLength = 80f;     // how far the player has to run
    public float corridorWidth = 8f;
    public float tileLength = 4f;        // one floor slab
    public float wallHeight = 6f;

    public float gapWidth = 2.2f;        // the player clears ~3m at a sprint
    public int gapEveryTiles = 7;
    public int blockEveryTiles = 3;      // the slalom

    const string CourseName = "ChaseCourse";
    const int RunUpTiles = 2;            // floor behind the start, for the pursuer to charge from

    [MenuItem("Tools/DreadWood/Build Chase Course")]
    static void Open() => GetWindow<ChaseCourseBuilder>("Chase Course");

    void OnGUI()
    {
        EditorGUILayout.LabelField("Build the chase corridor (runs along +Z)", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        courseLength = EditorGUILayout.Slider("Course Length", courseLength, 30f, 200f);
        corridorWidth = EditorGUILayout.Slider("Corridor Width", corridorWidth, 4f, 20f);
        tileLength = EditorGUILayout.Slider("Tile Length", tileLength, 2f, 8f);
        wallHeight = EditorGUILayout.Slider("Wall Height", wallHeight, 3f, 15f);

        EditorGUILayout.Space();
        gapWidth = EditorGUILayout.Slider("Gap Width", gapWidth, 1f, 3.5f);
        gapEveryTiles = EditorGUILayout.IntSlider("Gap Every N Tiles", gapEveryTiles, 3, 15);
        blockEveryTiles = EditorGUILayout.IntSlider("Block Every N Tiles", blockEveryTiles, 2, 10);

        EditorGUILayout.Space();
        if (GUILayout.Button("Build Chase Course", GUILayout.Height(30))) Build();
        if (GUILayout.Button("Clear Chase Course")) Clear();

        EditorGUILayout.HelpBox(
            "Build REPLACES the existing course.\n\n" +
            "The floor goes on the Ground layer and the walls and blocks on Obstacle, so the " +
            "player and the pursuer both know what they are standing on.\n\n" +
            "Gaps wider than about 3m cannot be jumped at a sprint.",
            MessageType.Info);
    }

    void Build()
    {
        Clear();

        GameObject course = new GameObject(CourseName);
        Undo.RegisterCreatedObjectUndo(course, "Build Chase Course");

        Material floorMaterial = BuildingBlocks.GetMaterial("M_ChaseFloor", new Color(0.24f, 0.23f, 0.21f));
        Material rockMaterial = BuildingBlocks.GetMaterial("M_ChaseRock", new Color(0.31f, 0.30f, 0.28f));
        Material blockMaterial = BuildingBlocks.GetMaterial("M_ChaseBlock", new Color(0.40f, 0.34f, 0.26f));

        GameObject floors = Group(course, "Floor");
        GameObject walls = Group(course, "Walls");
        GameObject obstacles = Group(course, "Obstacles");
        GameObject markers = Group(course, "Markers");

        int tiles = Mathf.Max(6, Mathf.RoundToInt(courseLength / tileLength));
        float courseEnd = tiles * tileLength;
        float runUpStart = -RunUpTiles * tileLength;
        int blockSide = 1;

        for (int i = -RunUpTiles; i < tiles; i++)
        {
            float tileStart = i * tileLength;

            // No gaps or blocks in the run-up or the first few tiles — the player needs a
            // moment to realise they are being chased before the course starts testing them.
            bool safeStretch = i < 3;
            bool isGap = !safeStretch && i % gapEveryTiles == 0;

            // A gap tile keeps only its far end, so the hole is exactly gapWidth across and
            // the player always lands back on solid floor.
            float slabLength = isGap ? tileLength - gapWidth : tileLength;
            float slabCenter = tileStart + (isGap ? gapWidth : 0f) + slabLength / 2f;

            BuildingBlocks.Box(floors, "Floor_" + i,
                new Vector3(0f, -0.25f, slabCenter),
                new Vector3(corridorWidth, 0.5f, slabLength), floorMaterial);

            float wallCenter = tileStart + tileLength / 2f;
            float wallOffset = corridorWidth / 2f + 0.25f;
            BuildingBlocks.Box(walls, "Wall_L_" + i,
                new Vector3(-wallOffset, wallHeight / 2f, wallCenter),
                new Vector3(0.5f, wallHeight, tileLength), rockMaterial);
            BuildingBlocks.Box(walls, "Wall_R_" + i,
                new Vector3(wallOffset, wallHeight / 2f, wallCenter),
                new Vector3(0.5f, wallHeight, tileLength), rockMaterial);

            if (!safeStretch && !isGap && i % blockEveryTiles == 0)
            {
                float blockWidth = corridorWidth * 0.45f;
                BuildingBlocks.Box(obstacles, "Block_" + i,
                    new Vector3(blockSide * (corridorWidth - blockWidth) / 2f, 1.25f, wallCenter),
                    new Vector3(blockWidth, 2.5f, 1.5f), blockMaterial);
                blockSide = -blockSide;   // next one hugs the other wall, so the run weaves
            }
        }

        // Caps at both ends: nothing to run back to, and a wall to stop at.
        BuildingBlocks.Box(walls, "Wall_Behind",
            new Vector3(0f, wallHeight / 2f, runUpStart - 0.25f),
            new Vector3(corridorWidth + 1f, wallHeight, 0.5f), rockMaterial);
        BuildingBlocks.Box(walls, "Wall_End",
            new Vector3(0f, wallHeight / 2f, courseEnd + 0.25f),
            new Vector3(corridorWidth + 1f, wallHeight, 0.5f), rockMaterial);

        BuildingBlocks.SetLayer(floors, "Ground");
        BuildingBlocks.SetLayer(walls, "Obstacle");
        BuildingBlocks.SetLayer(obstacles, "Obstacle");

        BuildDeathZone(course, runUpStart, courseEnd);

        BuildingBlocks.Marker(markers, "PlayerStart", new Vector3(0f, 0.1f, 2f), Quaternion.identity);
        BuildingBlocks.Marker(markers, "PursuerSpawn", new Vector3(0f, 0.1f, runUpStart + 2f), Quaternion.identity);
        BuildingBlocks.Marker(markers, "Exit", new Vector3(0f, 0.1f, courseEnd - 2f), Quaternion.identity);

        Selection.activeGameObject = course;
        Debug.Log("[Chase] Built a " + courseEnd + "m course, " + tiles + " tiles.");
    }

    // One big trigger slung under the whole course, so any missed jump lands in it.
    void BuildDeathZone(GameObject course, float runUpStart, float courseEnd)
    {
        GameObject zone = new GameObject("DeathZone");
        zone.transform.SetParent(course.transform, false);
        zone.transform.localPosition = new Vector3(0f, -12f, (runUpStart + courseEnd) / 2f);

        BoxCollider trigger = zone.AddComponent<BoxCollider>();
        trigger.isTrigger = true;
        trigger.size = new Vector3(corridorWidth + 10f, 4f, courseEnd - runUpStart + 20f);

        zone.AddComponent<DeathZone>();
    }

    void Clear()
    {
        GameObject existing = GameObject.Find(CourseName);
        if (existing != null) Undo.DestroyObjectImmediate(existing);
    }

    static GameObject Group(GameObject parent, string name)
    {
        return BuildingBlocks.Marker(parent, name, Vector3.zero, Quaternion.identity);
    }
}
