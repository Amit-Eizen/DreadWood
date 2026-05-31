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

    void Awake()
    {
        Instance = this;
        currentHP = maxHP;
    }

    void Update()
    {
        if (damageFlash > 0f) damageFlash -= Time.unscaledDeltaTime;
    }

    public void TakeDamage(int amount)
    {
        if (isWin || isLose) return;
        currentHP = Mathf.Max(0, currentHP - amount);
        damageFlash = 0.4f;
        if (currentHP <= 0) Lose();
    }

    public void Heal(int amount)
    {
        if (isWin || isLose) return;
        currentHP = Mathf.Min(maxHP, currentHP + amount);
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

        if (isWin || isLose)
        {
            GUI.color = new Color(0f, 0f, 0f, 0.6f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            GUIStyle big = new GUIStyle(GUI.skin.label) { fontSize = 46, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            big.normal.textColor = isWin ? new Color(0.6f, 1f, 0.6f) : new Color(1f, 0.5f, 0.5f);
            GUI.Label(new Rect(0, Screen.height / 2f - 90f, Screen.width, 60f), isWin ? "YOU ESCAPED" : "YOU DIED", big);

            GUIStyle btn = new GUIStyle(GUI.skin.button) { fontSize = 22, fontStyle = FontStyle.Bold };
            if (GUI.Button(new Rect(Screen.width / 2f - 90f, Screen.height / 2f + 10f, 180f, 50f), "Restart", btn))
                Restart();
        }
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
