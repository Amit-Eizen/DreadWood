using UnityEngine;
using UnityEngine.SceneManagement;

// Put this on EVERY forest zombie in Scene 1 (just multi-select them and Add Component).
// - Each zombie gets a unique id automatically from its start position, so no numbering by hand.
// - If this zombie was already beaten (in the arena), it removes itself on load, so it stays gone.
// - When the player gets close, it remembers which zombie this is + where the player is,
//   then loads the Zombie Arena scene to fight it there.
// - Once enough zombies are beaten, zombies stop pulling the player in, so they can reach the light.
public class ForestZombie : MonoBehaviour
{
    [Tooltip("How close the player must get to start the fight")]
    public float engageDistance = 2.5f;

    [Tooltip("Name of the arena scene to load for the fight")]
    public string arenaSceneName = "Scene2_ZombieArena";

    [Tooltip("Once this many zombies are beaten, zombies stop starting fights (so you can reach the portal)")]
    public int stopEngagingAfterDefeated = 3;

    [Tooltip("Nearby zombies within this range get dragged into the same arena fight")]
    public float groupRange = 10f;

    // This zombie's id - unique + stable, made from its start position. Other scripts read it
    // so a whole group can be sent to the arena together.
    public string ZombieId { get; private set; }

    private string zombieId;      // unique + stable, made from this zombie's start position
    private Transform player;
    private bool engaged = false;

    void Awake()
    {
        // The id is the zombie's starting spot turned into text (e.g. "123_-456").
        // It's the same every time the scene loads (before the zombie wanders), so a beaten
        // zombie is recognised and stays gone. Automatic - no need to number zombies by hand.
        Vector3 p = transform.position;
        zombieId = Mathf.RoundToInt(p.x * 10f) + "_" + Mathf.RoundToInt(p.z * 10f);
        ZombieId = zombieId;
    }

    void Start()
    {
        // Already beaten? Then stay gone.
        if (PlayerProgressBetweenScenes.Instance != null &&
            PlayerProgressBetweenScenes.Instance.IsZombieDefeated(zombieId))
        {
            Destroy(gameObject);
            return;
        }

        GameObject p = GameObject.FindWithTag("Player");
        if (p == null) p = GameObject.Find("PlayerArmature");
        if (p != null) player = p.transform;

        // Killing this zombie out here — with a thrown rock, say — counts towards the
        // objective too, not only beating it in the arena.
        EnemyHealth health = GetComponent<EnemyHealth>();
        if (health != null) health.OnDeath += CountAsDefeated;
    }

    void CountAsDefeated()
    {
        if (PlayerProgressBetweenScenes.Instance != null)
            PlayerProgressBetweenScenes.Instance.MarkZombieDefeated(zombieId);
    }

    void Update()
    {
        if (engaged || player == null) return;

        // Once enough zombies are beaten, stop starting fights so the player can walk to the light.
        if (PlayerProgressBetweenScenes.Instance != null &&
            PlayerProgressBetweenScenes.Instance.ZombiesDefeated >= stopEngagingAfterDefeated)
            return;

        if (Vector3.Distance(transform.position, player.position) <= engageDistance)
            Engage();
    }

    void Engage()
    {
        engaged = true;   // only once, so we don't load the scene many times in one frame

        PlayerProgressBetweenScenes progress = PlayerProgressBetweenScenes.Instance;
        if (progress != null)
        {
            // Take this zombie AND any of its friends standing nearby into the same fight.
            progress.zombiesBeingFought.Clear();
            progress.zombiesBeingFought.Add(zombieId);

            foreach (ForestZombie other in Object.FindObjectsByType<ForestZombie>(FindObjectsSortMode.None))
            {
                if (other == this) continue;
                if (Vector3.Distance(transform.position, other.transform.position) <= groupRange)
                    progress.zombiesBeingFought.Add(other.ZombieId);
            }

            progress.SetForestReturnPosition(player.position);
        }

        SceneManager.LoadScene(arenaSceneName);
    }
}
