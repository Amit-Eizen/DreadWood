using System.Collections.Generic;
using UnityEngine;

// Holds the player's progress (health, score, collected items) and KEEPS it alive when a new scene loads
public class PlayerProgressBetweenScenes : MonoBehaviour
{
    // The one shared instance. Everything reads/writes progress through this.
    public static PlayerProgressBetweenScenes Instance { get; private set; }

    [Header("Health (carried between scenes)")]
    public int maxHealth = 100;
    public int currentHealth = 100;

    [Header("Armour (carried between scenes)")]
    // Armour soaks up damage before health does. It lives here rather than on GameManager
    // because GameManager is rebuilt in every scene, so armour would reset on each load.
    public int maxArmour = 50;
    public int currentArmour = 0;

    [Header("Rocks to throw (carried between scenes)")]
    public int maxRockAmmo = 10;
    public int rockAmmo = 5;

    [Header("Score (carried between scenes)")]
    public int score = 0;

    // Items the player has picked up (e.g. "Battery", "Key"). Kept between scenes
    private readonly HashSet<string> collectedItems = new HashSet<string>();

    // Ids of forest zombies the player has already beaten (in the arena). Kept between scenes.
    // How many are in this set = how many zombies are done (see ZombiesDefeated below).
    private readonly HashSet<string> defeatedZombieIds = new HashSet<string>();

    // Which forest zombies the player went to the arena to fight. A whole group can be pulled
    // in at once, so the arena spawns one zombie per id and all of them count when beaten.
    public readonly List<string> zombiesBeingFought = new List<string>();

    // Where the player stood in the forest before leaving, so we can put them back on return.
    public Vector3 forestReturnPosition;
    public bool hasForestReturnPosition = false;

    // Runs once, when Unity creates this object - doesn't run again on later scene loads, because this object is not deleted
    void Awake()
    {
        // Check if an instance of this class already exists (object that stores the player's progress) 
        // If one already exists (it survived from a previous scene), then I'm a copy - delete me and stop, so the original stays
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);   // <-- this is what makes it survive scene loads
        currentHealth = maxHealth;
    }

    public void SetHealth(int value)
    {
        currentHealth = Mathf.Clamp(value, 0, maxHealth);
    }

    public void SetArmour(int value)
    {
        currentArmour = Mathf.Clamp(value, 0, maxArmour);
    }

    public void AddArmour(int amount)
    {
        SetArmour(currentArmour + amount);
    }

    public void AddScore(int amount)
    {
        score = Mathf.Max(0, score + amount);
    }

    public void AddRocks(int amount)
    {
        rockAmmo = Mathf.Clamp(rockAmmo + amount, 0, maxRockAmmo);
    }

    // Returns false when there is nothing left to throw.
    public bool TryUseRock()
    {
        if (rockAmmo <= 0) return false;
        rockAmmo--;
        return true;
    }

    public void CollectItem(string itemName)
    {
        collectedItems.Add(itemName);
    }

    public bool HasItem(string itemName)
    {
        return collectedItems.Contains(itemName);
    }

    // How many distinct items the player is carrying.
    public int ItemCount => collectedItems.Count;

    // ---------- Zombies beaten (beat 3 to open the way to the boss) ----------

    // How many forest zombies the player has beaten so far.
    public int ZombiesDefeated => defeatedZombieIds.Count;

    // Remember this forest zombie was beaten, so it stays gone when we return to the forest.
    public void MarkZombieDefeated(string zombieId)
    {
        defeatedZombieIds.Add(zombieId);
    }

    // True if this forest zombie was already beaten.
    public bool IsZombieDefeated(string zombieId)
    {
        return defeatedZombieIds.Contains(zombieId);
    }

    // Remember where the player was in the forest, so returning from the arena puts them back.
    public void SetForestReturnPosition(Vector3 position)
    {
        forestReturnPosition = position;
        hasForestReturnPosition = true;
    }

    public void ResetForNewGame()
    {
        currentHealth = maxHealth;
        currentArmour = 0;
        rockAmmo = 5;
        score = 0;
        collectedItems.Clear();
        defeatedZombieIds.Clear();
        zombiesBeingFought.Clear();
        hasForestReturnPosition = false;
    }
}
