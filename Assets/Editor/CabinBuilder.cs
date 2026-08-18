using UnityEngine;
using UnityEditor;

// One-click cabin builder for DreadWood.
// Builds a simple wooden cabin from primitives: floor, 4 walls with a doorway
// (facing +Z / north, toward the trail), a gable roof, and a bed inside.
// All pieces are cube primitives, so they come with box colliders for free:
// the walls block the player, the doorway is a real opening to walk through.
//
// Open via:  Tools > DreadWood > Build Cabin
public class CabinBuilder : EditorWindow
{
    public Vector2 groundPosition = new Vector2(0f, -80f); // world X,Z (south start)
    public float interiorWidth = 6f;    // along X
    public float interiorDepth = 7f;    // along Z
    public float floorHeight = 3f;       // height of ONE floor
    public float wallThickness = 0.2f;
    public float doorWidth = 1.6f;
    public float doorHeight = 2.3f;
    public float roofRise = 1.8f;
    public bool placePlayerInside = true;

    [Header("Upper floor")]
    public int floors = 2;
    public float stairWidth = 1.2f;
    // The player climbs steps by stepping over them, so a step must never be taller than the
    // CharacterController's Step Offset (0.3 on the Starter Assets player) or he gets stuck.
    public float maxStepHeight = 0.22f;
    public float stepDepth = 0.3f;

    const string CabinName = "Cabin";

    [MenuItem("Tools/DreadWood/Build Cabin")]
    static void Open() => GetWindow<CabinBuilder>("Cabin Builder");

    void OnGUI()
    {
        EditorGUILayout.LabelField("Build a wooden cabin (door faces +Z / north)", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        groundPosition = EditorGUILayout.Vector2Field("Ground Position (X, Z)", groundPosition);
        interiorWidth = EditorGUILayout.Slider("Interior Width", interiorWidth, 3f, 12f);
        interiorDepth = EditorGUILayout.Slider("Interior Depth", interiorDepth, 3f, 14f);
        floorHeight = EditorGUILayout.Slider("Floor Height", floorHeight, 2.2f, 5f);
        doorWidth = EditorGUILayout.Slider("Door Width", doorWidth, 1f, 3f);
        doorHeight = EditorGUILayout.Slider("Door Height", doorHeight, 1.8f, floorHeight);
        roofRise = EditorGUILayout.Slider("Roof Rise (peak)", roofRise, 0.5f, 4f);
        placePlayerInside = EditorGUILayout.Toggle("Place Player Inside", placePlayerInside);

        EditorGUILayout.Space();
        floors = EditorGUILayout.IntSlider("Floors", floors, 1, 3);
        stairWidth = EditorGUILayout.Slider("Stair Width", stairWidth, 0.9f, 2.5f);
        maxStepHeight = EditorGUILayout.Slider("Max Step Height", maxStepHeight, 0.12f, 0.28f);
        stepDepth = EditorGUILayout.Slider("Step Depth", stepDepth, 0.22f, 0.45f);

        EditorGUILayout.Space();
        if (GUILayout.Button("Build Cabin", GUILayout.Height(30))) Build();
        if (GUILayout.Button("Clear Cabin")) Clear();

        EditorGUILayout.HelpBox(
            "Builds a 'Cabin' object at the ground position (snapped to terrain height).\n" +
            "The doorway faces north (+Z) toward the trail and light.\n\n" +
            "With Floors = 2 it also builds a stairwell against the right wall and a floor " +
            "above with a hole to climb through.\n\n" +
            "Build REPLACES the existing cabin, so set the sliders to match what is in the " +
            "scene first (it was built with Floor Height 4 and Door Height 3).\n" +
            "If the player cannot climb the stairs, lower Max Step Height — a step must be " +
            "shorter than the player's CharacterController Step Offset.",
            MessageType.Info);
    }

    void Build()
    {
        Clear(); // start fresh so re-builds don't stack

        // Find ground height from the active terrain (fallback to y=0)
        float groundY = 0f;
        Terrain terrain = Terrain.activeTerrain;
        Vector3 worldPos = new Vector3(groundPosition.x, 0f, groundPosition.y);
        if (terrain != null)
            groundY = terrain.SampleHeight(worldPos) + terrain.transform.position.y;
        worldPos.y = groundY;

        GameObject cabin = new GameObject(CabinName);
        Undo.RegisterCreatedObjectUndo(cabin, "Build Cabin");
        cabin.transform.position = worldPos;

        // Materials
        Material wood = BuildingBlocks.GetTexturedMaterial("M_CabinWood", "Assets/NatureStarterKit2/Textures/bark02.tga", new Vector2(4f, 4f), new Color(0.45f, 0.30f, 0.17f));
        Material roofMat = BuildingBlocks.GetMaterial("M_CabinRoof", new Color(0.25f, 0.15f, 0.10f));
        Material bedFrame = BuildingBlocks.GetMaterial("M_BedFrame", new Color(0.20f, 0.12f, 0.07f));
        Material mattress = BuildingBlocks.GetMaterial("M_Mattress", new Color(0.78f, 0.74f, 0.66f));
        Material pillow = BuildingBlocks.GetMaterial("M_Pillow", new Color(0.90f, 0.90f, 0.88f));

        float W = interiorWidth, D = interiorDepth, H = floorHeight, T = wallThickness;
        float Wt = W + 2f * T;   // outer width
        float Dt = D + 2f * T;   // outer depth
        float totalH = H * Mathf.Max(1, floors);   // outer walls run the full height of the house

        // Floor (top surface at local y = 0)
        BuildingBlocks.Box(cabin, "Floor", new Vector3(0f, -0.1f, 0f), new Vector3(Wt, 0.2f, Dt), Quaternion.identity, wood);

        // Back wall (-Z)
        BuildingBlocks.Box(cabin, "Wall_Back", new Vector3(0f, totalH / 2f, -(D / 2f + T / 2f)), new Vector3(Wt, totalH, T), Quaternion.identity, wood);
        // Left wall (-X)
        BuildingBlocks.Box(cabin, "Wall_Left", new Vector3(-(W / 2f + T / 2f), totalH / 2f, 0f), new Vector3(T, totalH, D), Quaternion.identity, wood);
        // Right wall (+X)
        BuildingBlocks.Box(cabin, "Wall_Right", new Vector3(W / 2f + T / 2f, totalH / 2f, 0f), new Vector3(T, totalH, D), Quaternion.identity, wood);

        // Front wall (+Z) WITH a doorway: two side segments + a lintel above the door
        float frontZ = D / 2f + T / 2f;
        float sideSegW = (Wt - doorWidth) / 2f;
        BuildingBlocks.Box(cabin, "Wall_Front_L", new Vector3(-(doorWidth / 2f + sideSegW / 2f), totalH / 2f, frontZ), new Vector3(sideSegW, totalH, T), Quaternion.identity, wood);
        BuildingBlocks.Box(cabin, "Wall_Front_R", new Vector3(doorWidth / 2f + sideSegW / 2f, totalH / 2f, frontZ), new Vector3(sideSegW, totalH, T), Quaternion.identity, wood);
        float lintelH = totalH - doorHeight;
        if (lintelH > 0.01f)
            BuildingBlocks.Box(cabin, "Wall_Front_Lintel", new Vector3(0f, doorHeight + lintelH / 2f, frontZ), new Vector3(doorWidth, lintelH, T), Quaternion.identity, wood);

        // A wooden door, hinged on the left of the opening and left ajar.
        // Decorative only (collider removed) so it never blocks the walk-through.
        Material doorMat = BuildingBlocks.GetMaterial("M_CabinDoor", new Color(0.30f, 0.19f, 0.10f));
        GameObject hinge = new GameObject("Door");
        hinge.transform.SetParent(cabin.transform, false);
        hinge.transform.localPosition = new Vector3(-doorWidth / 2f, doorHeight / 2f, frontZ);
        hinge.transform.localRotation = Quaternion.identity; // starts shut; DoorController opens it on approach
        GameObject panel = GameObject.CreatePrimitive(PrimitiveType.Cube);
        panel.name = "DoorPanel";
        panel.transform.SetParent(hinge.transform, false);
        // Clear of the wall's own thickness — a panel sunk inside the frame is jammed and
        // no amount of motor force will swing it.
        panel.transform.localPosition = new Vector3(doorWidth / 2f, 0f, T / 2f + 0.06f);
        panel.transform.localScale = new Vector3(doorWidth, doorHeight, 0.08f);
        panel.GetComponent<MeshRenderer>().sharedMaterial = doorMat;
        panel.isStatic = false;   // it swings, so it must not be batched into the walls

        // A real hinge rather than a scripted rotation: the panel gets a body, the joint pins
        // its left edge, and the limits stop it at shut and at fully open.
        Rigidbody panelBody = panel.AddComponent<Rigidbody>();
        panelBody.mass = 20f;

        HingeJoint door = panel.AddComponent<HingeJoint>();
        door.anchor = new Vector3(-0.5f, 0f, 0f);   // the cube's own left edge, before scaling
        door.axis = new Vector3(0f, -1f, 0f);   // swings outward, away from the wall behind it
        door.useLimits = true;
        door.limits = new JointLimits { min = 0f, max = 95f };

        panel.AddComponent<DoorController>();   // press Y nearby and the motor swings it open

        // ---- Upper floor ----
        // The stairs hug the right (+X) wall and climb from the back of the room forwards.
        // The floor above is built as slabs AROUND the stairwell, the same way the front
        // wall is built around the doorway, so there is a real hole to walk up through.
        if (floors > 1)
        {
            int stepCount = Mathf.Max(3, Mathf.CeilToInt(H / maxStepHeight));
            float stepH = H / stepCount;                    // exact, so the last step meets the floor
            float stepD = Mathf.Min(stepDepth, (D - 0.4f) / stepCount);   // keep the run inside the room

            float stairMinX = W / 2f - stairWidth;
            float stairStartZ = -D / 2f + 0.2f;
            float stairEndZ = stairStartZ + stepCount * stepD;

            for (int i = 0; i < stepCount; i++)
            {
                float z0 = stairStartZ + i * stepD;
                BuildingBlocks.BoxBetween(cabin, "Step_" + (i + 1),
                    new Vector3(stairMinX, 0f, z0),
                    new Vector3(W / 2f, (i + 1) * stepH, z0 + stepD), wood);
            }

            // Floor of the level above — its top surface sits exactly at y = H.
            float slabBottom = H - 0.2f;
            BuildingBlocks.BoxBetween(cabin, "Floor2_Left",
                new Vector3(-Wt / 2f, slabBottom, -Dt / 2f), new Vector3(stairMinX, H, Dt / 2f), wood);
            BuildingBlocks.BoxBetween(cabin, "Floor2_Back",
                new Vector3(stairMinX, slabBottom, -Dt / 2f), new Vector3(Wt / 2f, H, stairStartZ), wood);
            if (Dt / 2f - stairEndZ > 0.05f)
                BuildingBlocks.BoxBetween(cabin, "Floor2_Front",
                    new Vector3(stairMinX, slabBottom, stairEndZ), new Vector3(Wt / 2f, H, Dt / 2f), wood);
        }

        // Gable roof: two sloped boxes meeting at a ridge running along Z
        float halfW = Wt / 2f;
        float angleDeg = Mathf.Atan2(roofRise, halfW) * Mathf.Rad2Deg;
        float slopeLen = Mathf.Sqrt(halfW * halfW + roofRise * roofRise) + 0.5f; // +overlap at ridge/eave
        float overhang = 0.5f;
        Vector3 roofSize = new Vector3(slopeLen, 0.15f, Dt + 2f * overhang);
        BuildingBlocks.Box(cabin, "Roof_L", new Vector3(-halfW / 2f, totalH + roofRise / 2f, 0f), roofSize, Quaternion.Euler(0f, 0f, angleDeg), roofMat);
        BuildingBlocks.Box(cabin, "Roof_R", new Vector3(halfW / 2f, totalH + roofRise / 2f, 0f), roofSize, Quaternion.Euler(0f, 0f, -angleDeg), roofMat);

        // Bed in the back-left corner, on the TOP floor — the player wakes up there and has
        // to come down the stairs. The stairs are on the +X side, so they never clash.
        float bedFloorY = (floors > 1) ? H : 0f;
        float bedX = -(W / 2f - 0.7f);
        float bedZ = -(D / 2f - 1.2f);
        BuildingBlocks.Box(cabin, "Bed_Frame", new Vector3(bedX, bedFloorY + 0.25f, bedZ), new Vector3(1.3f, 0.5f, 2.2f), Quaternion.identity, bedFrame);
        BuildingBlocks.Box(cabin, "Bed_Mattress", new Vector3(bedX, bedFloorY + 0.62f, bedZ), new Vector3(1.2f, 0.25f, 2.0f), Quaternion.identity, mattress);
        BuildingBlocks.Box(cabin, "Bed_Pillow", new Vector3(bedX, bedFloorY + 0.78f, bedZ - 0.7f), new Vector3(1.0f, 0.18f, 0.5f), Quaternion.identity, pillow);

        // Optionally move the player next to the bed, facing the door (+Z / north).
        // With two floors that is upstairs, so the game opens by walking down.
        if (placePlayerInside)
        {
            GameObject player = GameObject.Find("PlayerArmature");
            if (player == null)
            {
                GameObject tagged = GameObject.FindWithTag("Player");
                if (tagged != null) player = tagged;
            }
            if (player != null)
            {
                Undo.RecordObject(player.transform, "Place Player In Cabin");
                player.transform.position = worldPos + new Vector3(0.6f, bedFloorY + 0.2f, bedZ + 0.2f);
                player.transform.rotation = Quaternion.identity; // face +Z toward the doorway
            }
            else
            {
                Debug.LogWarning("[Cabin] PlayerArmature not found — couldn't place the player inside.");
            }
        }

        Selection.activeGameObject = cabin;
        Debug.Log("[Cabin] Built cabin at " + worldPos);
    }

    void Clear()
    {
        GameObject cabin = GameObject.Find(CabinName);
        if (cabin != null) Undo.DestroyObjectImmediate(cabin);
    }

}
