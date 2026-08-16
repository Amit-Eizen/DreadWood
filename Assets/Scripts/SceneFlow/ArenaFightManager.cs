using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Put this on the GameManager in the arena scene.
// It spawns one zombie for each forest zombie that was dragged into this fight, then waits.
// When they are all dead it marks them beaten (raising the counter) and returns to the forest.
public class ArenaFightManager : MonoBehaviour
{
    [Tooltip("The zombie prefab to spawn for the fight")]
    public GameObject zombiePrefab;

    [Tooltip("Where the zombies appear - the middle of the arena")]
    public Transform spawnCenter;

    [Tooltip("How far apart the spawned zombies stand")]
    public float spawnSpread = 3f;

    [Tooltip("Name of the forest scene to return to after the win")]
    public string forestSceneName = "Scene1_Forest";

    [Tooltip("Seconds to wait after the last zombie dies before returning")]
    public float returnDelay = 1.5f;

    // Forest zombies we are fighting here. They are marked beaten once the fight is won.
    private List<string> zombieIdsInThisFight = new List<string>();
    private int zombiesStillAlive = 0;
    private bool fightIsOver = false;
    private bool thisIsATestFight = false;

    void Start()
    {
        if (zombiePrefab == null) return;

        // No progress object means the arena was opened on its own, which is a normal way
        // to test it. Carry on instead of leaving the scene with nothing to fight.
        PlayerProgressBetweenScenes progress = PlayerProgressBetweenScenes.Instance;
        if (progress != null) zombieIdsInThisFight = new List<string>(progress.zombiesBeingFought);

        // Nothing was passed in - spawn one to fight, but keep it out of the real progress.
        if (zombieIdsInThisFight.Count == 0)
        {
            thisIsATestFight = true;
            zombieIdsInThisFight.Add("test");
        }

        for (int i = 0; i < zombieIdsInThisFight.Count; i++)
        {
            SpawnZombie(i, zombieIdsInThisFight.Count);
        }
    }

    void SpawnZombie(int index, int total)
    {
        // Spread the zombies out in a line so they don't stand inside each other.
        Vector3 center = Vector3.zero;
        if (spawnCenter != null) center = spawnCenter.position;

        float offset = (index - (total - 1) / 2f) * spawnSpread;
        Vector3 spawnPosition = center + new Vector3(offset, 0f, 0f);

        GameObject zombie = Instantiate(zombiePrefab, spawnPosition, Quaternion.identity);
        zombiesStillAlive++;

        // In the arena the zombies attack straight away, instead of wandering like in the forest.
        MutantAI ai = zombie.GetComponent<MutantAI>();
        if (ai != null) ai.alwaysAggressive = true;

        // Count it down when it dies.
        EnemyHealth health = zombie.GetComponent<EnemyHealth>();
        if (health != null) health.OnDeath += OnOneZombieDied;
    }

    void OnOneZombieDied()
    {
        zombiesStillAlive--;
        if (zombiesStillAlive > 0 || fightIsOver) return;

        fightIsOver = true;
        WinTheFight();
    }

    void WinTheFight()
    {
        // All of the forest zombies in this fight are now beaten, so they stay gone
        // and the counter goes up by the size of the group.
        // A test fight has no real forest zombies in it, so it must not raise the counter.
        PlayerProgressBetweenScenes progress = PlayerProgressBetweenScenes.Instance;
        if (progress != null && !thisIsATestFight)
        {
            foreach (string zombieId in zombieIdsInThisFight)
            {
                progress.MarkZombieDefeated(zombieId);
            }
        }

        Invoke(nameof(ReturnToForest), returnDelay);
    }

    void ReturnToForest()
    {
        SceneManager.LoadScene(forestSceneName);
    }
}
