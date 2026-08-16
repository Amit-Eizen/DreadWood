using UnityEngine;
using UnityEngine.SceneManagement;

// Central game state: health, the objective hint, and win/lose screens.
// Draws a simple HUD with OnGUI (HP top-left, objective top-right, and a
// win/lose overlay with a Restart button). Other scripts call:
//   GameManager.Instance.TakeDamage(x) / Heal(x) / Win() / Lose() / SetObjective("...")
public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Health")]
    public int maxHP = 100;
    public int currentHP = 100;

    [Header("Armour")]
    [Tooltip("Armour soaks damage before health does. Picked up in the world.")]
    public int maxArmour = 50;
    public int currentArmour = 0;

    [Header("Objective")]
    [TextArea] public string objectiveText = "Find the portal";

    private bool isWin = false;
    private bool isLose = false;
    private float damageFlash = 0f;

    // read by the menu (GameMenu draws the end screens; GameManager only tracks state)
    public bool IsWin => isWin;
    public bool IsLose => isLose;
    public bool HasEnded => isWin || isLose;

    void Awake()
    {
        Instance = this;
        currentHP = maxHP;

        // Unity REMEMBERS Time.timeScale across play sessions in the editor. If a
        // previous run ended frozen (win/lose) or mid hit-stop, it would stay
        // frozen forever. Force normal time at the start of every run.
        Time.timeScale = 1f;
        isWin = false;
        isLose = false;
    }

    // On scene start, load the health saved in PlayerProgress (it carries over from
    // the previous scene). Start runs after all Awakes, so PlayerProgress exists here.
    void Start()
    {
        if (PlayerProgressBetweenScenes.Instance != null)
        {
            maxHP = PlayerProgressBetweenScenes.Instance.maxHealth;
            currentHP = PlayerProgressBetweenScenes.Instance.currentHealth;
            maxArmour = PlayerProgressBetweenScenes.Instance.maxArmour;
            currentArmour = PlayerProgressBetweenScenes.Instance.currentArmour;
        }
    }

    void Update()
    {
        if (damageFlash > 0f) damageFlash -= Time.unscaledDeltaTime;
    }

    // True while the armour bar still has something in it. Enemies read this and hit
    // harder, so wearing armour shortens the fight instead of just stretching it out.
    public bool HasArmour => currentArmour > 0;

    public void TakeDamage(int amount)
    {
        if (isWin || isLose) return;
        if (PlayerDodge.IsDodging) return;   // i-frames: no damage mid-dodge

        // Armour soaks damage first — only what is left over reaches health.
        if (currentArmour > 0)
        {
            int absorbed = Mathf.Min(currentArmour, amount);
            currentArmour -= absorbed;
            amount -= absorbed;
            SaveArmour();
        }

        if (amount > 0) currentHP = Mathf.Max(0, currentHP - amount);

        damageFlash = 0.4f;
        SaveHealth();
        if (currentHP <= 0) Lose();
    }

    public void Heal(int amount)
    {
        if (isWin || isLose) return;
        currentHP = Mathf.Min(maxHP, currentHP + amount);
        SaveHealth();
    }

    public void AddArmour(int amount)
    {
        if (isWin || isLose) return;
        currentArmour = Mathf.Min(maxArmour, currentArmour + amount);
        SaveArmour();
    }

    // Save the current health into PlayerProgress so it survives the next scene load.
    void SaveHealth()
    {
        if (PlayerProgressBetweenScenes.Instance != null)
            PlayerProgressBetweenScenes.Instance.SetHealth(currentHP);
    }

    void SaveArmour()
    {
        if (PlayerProgressBetweenScenes.Instance != null)
            PlayerProgressBetweenScenes.Instance.SetArmour(currentArmour);
    }

    public void SetObjective(string text) { objectiveText = text; }

    public void Win()
    {
        if (isWin || isLose) return;
        isWin = true;
        FreezeGame();
    }

    public void Lose()
    {
        if (isWin || isLose) return;
        isLose = true;
        FreezeGame();
    }

    void FreezeGame()
    {
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Disable the player's controller/input so it can't re-lock the cursor
        // (Starter Assets re-locks the cursor on focus, which blocks the button).
        GameObject p = GameObject.FindWithTag("Player");
        if (p == null) p = GameObject.Find("PlayerArmature");
        if (p != null)
            foreach (MonoBehaviour mb in p.GetComponents<MonoBehaviour>())
            {
                string n = mb.GetType().Name;
                if (n == "ThirdPersonController" || n == "StarterAssetsInputs")
                    mb.enabled = false;
            }
    }

    public void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // One thin stat bar with its name and numbers beside it.
    void StatBar(float y, string label, int value, int max, Color fill)
    {
        const float barWidth = 150f, barHeight = 10f, x = 20f;
        float filled = max > 0 ? Mathf.Clamp01((float)value / max) : 0f;

        Color previous = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.55f);
        GUI.DrawTexture(new Rect(x, y, barWidth, barHeight), Texture2D.whiteTexture);
        GUI.color = fill;
        GUI.DrawTexture(new Rect(x, y, barWidth * filled, barHeight), Texture2D.whiteTexture);
        GUI.color = previous;

        GUIStyle style = new GUIStyle(GUI.skin.label) { fontSize = 14, fontStyle = FontStyle.Bold };
        Shadowed(new Rect(x + barWidth + 10f, y - 5f, 200f, 20f), label + "  " + value + " / " + max,
                 style, TextAnchor.MiddleLeft, Color.white);
    }

    // ---------- HUD ----------
    void OnGUI()
    {
        if (damageFlash > 0f)
        {
            Color prev = GUI.color;
            GUI.color = new Color(1f, 0f, 0f, Mathf.Clamp01(damageFlash) * 0.4f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = prev;
        }

        // Health and armour both read as bars so they compare at a glance. Armour is drawn
        // even when empty, so the player can see the stat exists before finding any.
        StatBar(16f, "HP", currentHP, maxHP, new Color(0.85f, 0.25f, 0.25f));
        StatBar(34f, "ARMOUR", currentArmour, maxArmour, new Color(0.45f, 0.75f, 1f));

        // Rocks left to throw. Turns red at zero so the player knows why nothing happens.
        PlayerProgressBetweenScenes progress = PlayerProgressBetweenScenes.Instance;
        if (progress != null)
        {
            GUIStyle rockStyle = new GUIStyle(GUI.skin.label) { fontSize = 14, fontStyle = FontStyle.Bold };
            Color rockColour = progress.rockAmmo > 0 ? new Color(0.85f, 0.85f, 0.8f) : new Color(1f, 0.5f, 0.5f);
            Shadowed(new Rect(20, 52f, 220, 20), "ROCKS  " + progress.rockAmmo + " / " + progress.maxRockAmmo,
                     rockStyle, TextAnchor.MiddleLeft, rockColour);
        }

        if (!isWin && !isLose && !string.IsNullOrEmpty(objectiveText))
        {
            GUIStyle ob = new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold, wordWrap = true };
            float w = 320f;
            Shadowed(new Rect(Screen.width - w - 20f, 15f, w, 90f), "» " + objectiveText, ob, TextAnchor.UpperRight, new Color(0.8f, 0.95f, 1f));
        }

        // The win/lose SCREEN is drawn by GameMenu (RESTART/QUIT). GameManager only
        // tracks the state + freezes the game here.
    }

    void Shadowed(Rect r, string text, GUIStyle style, TextAnchor anchor, Color color)
    {
        style.alignment = anchor;
        style.normal.textColor = Color.black;
        GUI.Label(new Rect(r.x + 1.5f, r.y + 1.5f, r.width, r.height), text, style);
        style.normal.textColor = color;
        GUI.Label(r, text, style);
    }
}
