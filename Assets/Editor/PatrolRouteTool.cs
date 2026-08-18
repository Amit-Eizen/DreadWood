using UnityEngine;
using UnityEditor;

// Gives every wandering enemy a patrol route: a ring of empty markers around wherever it
// already stands, wired into its MutantAI. Placing those by hand is the same handful of
// clicks per zombie, and there are a lot of zombies.
//
// Open via:  Tools > DreadWood > Build Patrol Routes
public class PatrolRouteTool : EditorWindow
{
    public int pointsPerEnemy = 3;
    public float routeRadius = 8f;
    public float waitSecondsAtEachPoint = 2.5f;
    public bool onlySelectedEnemies = false;
    public bool replaceExistingRoutes = false;

    const string RoutesName = "PatrolRoutes";

    [MenuItem("Tools/DreadWood/Build Patrol Routes")]
    static void Open() => GetWindow<PatrolRouteTool>("Patrol Routes");

    void OnGUI()
    {
        EditorGUILayout.LabelField("Give the enemies something to walk around", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        pointsPerEnemy = EditorGUILayout.IntSlider("Points Per Enemy", pointsPerEnemy, 2, 6);
        routeRadius = EditorGUILayout.Slider("Route Radius", routeRadius, 3f, 25f);
        waitSecondsAtEachPoint = EditorGUILayout.Slider("Wait At Each Point", waitSecondsAtEachPoint, 0f, 8f);

        EditorGUILayout.Space();
        onlySelectedEnemies = EditorGUILayout.Toggle("Only Selected Enemies", onlySelectedEnemies);
        replaceExistingRoutes = EditorGUILayout.Toggle("Replace Existing Routes", replaceExistingRoutes);

        EditorGUILayout.Space();
        if (GUILayout.Button("Build Patrol Routes", GUILayout.Height(30))) Build();
        if (GUILayout.Button("Clear All Routes")) Clear();

        EditorGUILayout.HelpBox(
            "Skips anything marked Always Aggressive — the boss and the pursuer never patrol.\n\n" +
            "Markers are parented to a 'PatrolRoutes' object, never to the enemy itself: a route " +
            "that moves with the enemy is one it can never arrive at.\n\n" +
            "Points snap to the terrain where there is one.",
            MessageType.Info);
    }

    void Build()
    {
        MutantAI[] enemies = onlySelectedEnemies
            ? Selection.GetFiltered<MutantAI>(SelectionMode.Editable | SelectionMode.Deep)
            : Object.FindObjectsByType<MutantAI>(FindObjectsSortMode.None);

        if (enemies.Length == 0)
        {
            Debug.LogWarning("[Patrol] No enemies found. With 'Only Selected' on, select some first.");
            return;
        }

        GameObject routes = GameObject.Find(RoutesName);
        if (routes == null)
        {
            routes = new GameObject(RoutesName);
            Undo.RegisterCreatedObjectUndo(routes, "Build Patrol Routes");
        }

        Terrain terrain = Terrain.activeTerrain;
        int built = 0;

        foreach (MutantAI enemy in enemies)
        {
            if (enemy.alwaysAggressive) continue;
            if (!replaceExistingRoutes && enemy.patrolPoints != null && enemy.patrolPoints.Length > 0) continue;

            enemy.patrolPoints = MakeRoute(routes, enemy, terrain);

            Undo.RecordObject(enemy, "Build Patrol Routes");
            enemy.waitSecondsAtEachPoint = waitSecondsAtEachPoint;
            EditorUtility.SetDirty(enemy);
            built++;
        }

        Debug.Log("[Patrol] Gave " + built + " of " + enemies.Length + " enemies a route.");
    }

    Transform[] MakeRoute(GameObject routes, MutantAI enemy, Terrain terrain)
    {
        GameObject group = new GameObject(enemy.name + "_Route");
        group.transform.SetParent(routes.transform, false);
        Undo.RegisterCreatedObjectUndo(group, "Build Patrol Routes");

        // A random starting angle so neighbouring zombies don't all walk the same triangle.
        float turn = Random.value * Mathf.PI * 2f;
        Transform[] points = new Transform[pointsPerEnemy];

        for (int i = 0; i < pointsPerEnemy; i++)
        {
            float angle = turn + i * Mathf.PI * 2f / pointsPerEnemy;
            Vector3 spot = enemy.transform.position
                         + new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle)) * routeRadius;

            if (terrain != null) spot.y = terrain.SampleHeight(spot) + terrain.transform.position.y;

            GameObject marker = new GameObject(enemy.name + "_Point_" + (i + 1));
            marker.transform.SetParent(group.transform, true);
            marker.transform.position = spot;
            points[i] = marker.transform;
        }

        return points;
    }

    void Clear()
    {
        foreach (MutantAI enemy in Object.FindObjectsByType<MutantAI>(FindObjectsSortMode.None))
        {
            Undo.RecordObject(enemy, "Clear Patrol Routes");
            enemy.patrolPoints = new Transform[0];
            EditorUtility.SetDirty(enemy);
        }

        GameObject routes = GameObject.Find(RoutesName);
        if (routes != null) Undo.DestroyObjectImmediate(routes);
    }
}
