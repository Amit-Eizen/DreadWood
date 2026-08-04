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

    [Header("Objective")]
    [TextArea] public string objectiveText = "You need to reach the light";

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
        }
    }

    void Update()
    {
        if (damageFlash > 0f) damageFlash -= Time.unscaledDeltaTime;
    }

    public void TakeDamage(int amount)
    {
        if (isWin || isLose) return;
        if (PlayerDodge.IsDodging) return;   // i-frames: no damage mid-dodge
        currentHP = Mathf.Max(0, currentHP - amount);
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

    // Save the current health into PlayerProgress so it survives the next scene load.
    void SaveHealth()
    {
        if (PlayerProgressBetweenScenes.Instance != null)
            PlayerProgressBetweenScenes.Instance.SetHealth(currentHP);
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

        GUIStyle hp = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold };
        Shadowed(new Rect(20, 15, 320, 30), "HP: " + currentHP + " / " + maxHP, hp, TextAnchor.MiddleLeft, Color.white);

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
