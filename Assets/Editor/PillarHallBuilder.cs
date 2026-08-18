using UnityEngine;
using UnityEditor;

// One-click builder for the pillar hall — the room the boss fight happens in. It makes the
// shell: floor, walls, the gate you came through, a ring of pillars and the low warm lights
// between them. The fight itself is wired by hand afterwards.
//
// Open via:  Tools > DreadWood > Build Pillar Hall
public class PillarHallBuilder : EditorWindow
{
    public float hallSize = 40f;
    public float wallHeight = 10f;
    public float wallThickness = 0.5f;

    public int pillarCount = 6;
    public float pillarRingRadius = 14f;
    public float pillarHeight = 12f;
    public float pillarRadius = 1.5f;

    public int lightCount = 3;
    public float lightHeight = 8f;

    const string HallName = "PillarHall";

    [MenuItem("Tools/DreadWood/Build Pillar Hall")]
    static void Open() => GetWindow<PillarHallBuilder>("Pillar Hall");

    void OnGUI()
    {
        EditorGUILayout.LabelField("Build the boss arena (you enter from -Z)", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        hallSize = EditorGUILayout.Slider("Hall Size", hallSize, 20f, 80f);
        wallHeight = EditorGUILayout.Slider("Wall Height", wallHeight, 5f, 25f);

        EditorGUILayout.Space();
        pillarCount = EditorGUILayout.IntSlider("Pillars", pillarCount, 3, 12);
        pillarRingRadius = EditorGUILayout.Slider("Pillar Ring Radius", pillarRingRadius, 5f, hallSize / 2f - 2f);
        pillarHeight = EditorGUILayout.Slider("Pillar Height", pillarHeight, 4f, 25f);
        pillarRadius = EditorGUILayout.Slider("Pillar Radius", pillarRadius, 0.5f, 4f);

        EditorGUILayout.Space();
        lightCount = EditorGUILayout.IntSlider("Lights", lightCount, 1, 8);
        lightHeight = EditorGUILayout.Slider("Light Height", lightHeight, 3f, wallHeight);

        EditorGUILayout.Space();
        if (GUILayout.Button("Build Pillar Hall", GUILayout.Height(30))) Build();
        if (GUILayout.Button("Clear Pillar Hall")) Clear();

        EditorGUILayout.HelpBox(
            "Build REPLACES the existing hall.\n\n" +
            "Pillars are named Pillar_1..N — PillarTopple goes on them one by one.\n\n" +
            "Keep the ring radius well inside the walls, or there is no room to run around " +
            "the outside of a pillar while the boss is down.",
            MessageType.Info);
    }

    void Build()
    {
        Clear();

        GameObject hall = new GameObject(HallName);
        Undo.RegisterCreatedObjectUndo(hall, "Build Pillar Hall");

        Material floorMaterial = BuildingBlocks.GetMaterial("M_HallFloor", new Color(0.20f, 0.19f, 0.18f));
        Material wallMaterial = BuildingBlocks.GetMaterial("M_HallWall", new Color(0.28f, 0.26f, 0.24f));
        Material pillarMaterial = BuildingBlocks.GetMaterial("M_HallPillar", new Color(0.42f, 0.39f, 0.34f));
        Material gateMaterial = BuildingBlocks.GetMaterial("M_HallGate", new Color(0.12f, 0.11f, 0.10f));

        GameObject walls = Group(hall, "Walls");
        GameObject pillars = Group(hall, "Pillars");
        GameObject lights = Group(hall, "Lights");
        GameObject markers = Group(hall, "Markers");

        float half = hallSize / 2f;
        float wallCentreY = wallHeight / 2f;
        float wallOffset = half + wallThickness / 2f;

        GameObject floor = BuildingBlocks.Box(hall, "Floor",
            new Vector3(0f, -0.25f, 0f), new Vector3(hallSize, 0.5f, hallSize), floorMaterial);

        BuildingBlocks.Box(walls, "Wall_North", new Vector3(0f, wallCentreY, wallOffset),
            new Vector3(hallSize + wallThickness * 2f, wallHeight, wallThickness), wallMaterial);
        BuildingBlocks.Box(walls, "Wall_South", new Vector3(0f, wallCentreY, -wallOffset),
            new Vector3(hallSize + wallThickness * 2f, wallHeight, wallThickness), wallMaterial);
        BuildingBlocks.Box(walls, "Wall_West", new Vector3(-wallOffset, wallCentreY, 0f),
            new Vector3(wallThickness, wallHeight, hallSize), wallMaterial);
        BuildingBlocks.Box(walls, "Wall_East", new Vector3(wallOffset, wallCentreY, 0f),
            new Vector3(wallThickness, wallHeight, hallSize), wallMaterial);

        // The way in, already shut. You arrive on the inside of it — there is no going back.
        BuildingBlocks.Box(walls, "Gate", new Vector3(0f, 3f, -half + 0.4f),
            new Vector3(6f, 6f, 0.6f), gateMaterial);

        for (int i = 0; i < pillarCount; i++)
        {
            float angle = i * Mathf.PI * 2f / pillarCount;
            Vector3 spot = new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle)) * pillarRingRadius;

            // A Unity cylinder is 2 units tall at scale 1, so the y scale is half the height.
            GameObject pillar = BuildingBlocks.Shape(pillars, "Pillar_" + (i + 1), PrimitiveType.Cylinder,
                new Vector3(spot.x, pillarHeight / 2f, spot.z),
                new Vector3(pillarRadius * 2f, pillarHeight / 2f, pillarRadius * 2f),
                Quaternion.identity, pillarMaterial);

            // Everything else here is static so Unity can batch it. A pillar has to topple,
            // and a batched mesh cannot move — the transform turns but nothing on screen does.
            pillar.isStatic = false;
        }

        for (int i = 0; i < lightCount; i++)
        {
            float angle = i * Mathf.PI * 2f / lightCount;
            Vector3 spot = new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle)) * (pillarRingRadius * 0.6f);
            AddTorchLight(lights, "Light_" + (i + 1), new Vector3(spot.x, lightHeight, spot.z));
        }

        BuildingBlocks.SetLayer(floor, "Ground");
        BuildingBlocks.SetLayer(walls, "Obstacle");
        BuildingBlocks.SetLayer(pillars, "Obstacle");

        BuildingBlocks.Marker(markers, "PlayerStart", new Vector3(0f, 0.1f, -half + 4f), Quaternion.identity);
        BuildingBlocks.Marker(markers, "BossSpawn", new Vector3(0f, 0.1f, half - 6f), Quaternion.Euler(0f, 180f, 0f));

        Selection.activeGameObject = hall;
        Debug.Log("[Hall] Built a " + hallSize + "m hall with " + pillarCount + " pillars.");
    }

    // Warm and short-ranged, so the middle of the room stays lit and the walls stay dark.
    static void AddTorchLight(GameObject parent, string name, Vector3 localPosition)
    {
        GameObject go = BuildingBlocks.Marker(parent, name, localPosition, Quaternion.identity);

        Light torch = go.AddComponent<Light>();
        torch.type = LightType.Point;
        torch.color = new Color(1f, 0.78f, 0.5f);
        torch.range = 25f;
        torch.intensity = 3f;
        torch.shadows = LightShadows.Soft;
    }

    void Clear()
    {
        GameObject existing = GameObject.Find(HallName);
        if (existing != null) Undo.DestroyObjectImmediate(existing);
    }

    static GameObject Group(GameObject parent, string name)
    {
        return BuildingBlocks.Marker(parent, name, Vector3.zero, Quaternion.identity);
    }
}
