using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// All of the game's menus in one overlay (no extra scene needed):
//   * MAIN     — at launch: START / CONTROLS / QUIT  (game frozen behind it)
//   * PAUSE    — press ESC while playing: RESUME / RESTART / CONTROLS / QUIT
//   * END      — when GameManager reports win/lose: "YOU ESCAPED" / "YOU DIED"
//                with RESTART / QUIT
// While any menu is up the game is frozen and the player's control scripts are
// disabled so the cursor is free for the buttons.
//
// Put this on an empty "MainMenu" GameObject. It finds the player automatically.
public class MainMenu : MonoBehaviour
{
    enum State { Main, Playing, Paused }

    // The same menu code serves three jobs, so each scene says which one it is here.
    public enum Kind { InGame, TitleScene, VictoryScene }

    [Header("What this menu is")]
    [Tooltip("Title = the opening scene. Victory = the ending scene. In Game = pause and death only.")]
    public Kind kind = Kind.InGame;

    [Tooltip("Scene that START and PLAY AGAIN load")]
    public string firstGameScene = "Scene1_Forest";

    [Header("Text")]
    public string title = "DREADWOOD";
    public string subtitle = "Escape the woods. Find the portal.";

    [Header("Victory scene")]
    public string victoryTitle = "YOU ESCAPED";
    public string victorySubtitle = "The woods are behind you.";

    [Header("Background (main menu)")]
    [Tooltip("Optional image behind the MAIN menu. If empty, the live game view shows through.")]
    public Texture2D backgroundImage;

    [Header("Controls legend")]
    public string[] controlsKeys = { "WASD", "SHIFT", "CTRL", "L-Click", "R-Click tap", "R-Click hold", "L-Click while aiming", "SPACE", "Y" };
    public string[] controlsActions = { "Move", "Run", "Sneak", "Attack", "Dodge", "Aim (first person)", "Throw a rock", "Jump", "Open door" };

    private State state = State.Main;
    private bool showControls = false;
    private MonoBehaviour[] playerControlScripts;

    // True while any menu covers the screen. GameManager checks it before drawing the HUD —
    // health bars behind a main menu look like a mistake.
    public static bool IsOpen { get; private set; }

    void OnDisable() => IsOpen = false;

    void Start()
    {
        playerControlScripts = PlayerControls.FindAll();

        if (kind == Kind.InGame) { BeginPlay(); return; }

        // A menu scene has no game behind it to freeze.
        IsOpen = true;
        Time.timeScale = 1f;
        FreeCursor(true);
    }

    void Update()
    {
        // once the game has ended, the END screen takes over (no ESC toggling)
        if (GameManager.Instance != null && GameManager.Instance.HasEnded)
        {
            FreeCursor(true);
            return;
        }

        // ESC toggles pause during play
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (state == State.Playing) Pause();
            else if (state == State.Paused) Resume();
        }
    }

    // ---- state transitions ----
    void BeginPlay()
    {
        state = State.Playing;
        IsOpen = false;
        Time.timeScale = 1f;
        FreeCursor(false);
        SetPlayerControl(true);
    }

    void Pause()
    {
        state = State.Paused;
        IsOpen = true;
        showControls = false;
        Time.timeScale = 0f;
        FreeCursor(true);
        SetPlayerControl(false);
    }

    void Resume() => BeginPlay();

    // START and PLAY AGAIN both mean a brand-new run: wipe the carried-over progress first,
    // or the second run starts with the first one's wounds.
    void StartNewGame()
    {
        if (PlayerProgressBetweenScenes.Instance != null)
            PlayerProgressBetweenScenes.Instance.ResetForNewGame();

        Time.timeScale = 1f;
        SceneManager.LoadScene(firstGameScene);
    }

    // Restart (pause menu and the death screen both use this) = a fresh run.
    void Restart()
    {
        if (PlayerProgressBetweenScenes.Instance != null)
            PlayerProgressBetweenScenes.Instance.ResetForNewGame();

        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    void Quit()
    {
        Time.timeScale = 1f;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void FreeCursor(bool free)
    {
        Cursor.lockState = free ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = free;
    }

    void SetPlayerControl(bool on)
    {
        PlayerControls.SetEnabled(playerControlScripts, on);
    }

    // ---- drawing ----
    void OnGUI()
    {
        if (kind == Kind.TitleScene) { DrawMainMenu(); return; }
        if (kind == Kind.VictoryScene) { DrawVictoryScreen(); return; }

        bool ended = GameManager.Instance != null && GameManager.Instance.HasEnded;

        // nothing to draw while actively playing
        if (!ended && state == State.Playing) return;

        if (ended) { DrawEndScreen(); return; }
        if (state == State.Paused) DrawPauseMenu();
    }

    void DrawVictoryScreen()
    {
        if (backgroundImage != null)
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), backgroundImage, ScaleMode.ScaleAndCrop);
        Dim(0.55f);

        var big = new GUIStyle(GUI.skin.label) { fontSize = 60, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        big.normal.textColor = new Color(0.6f, 1f, 0.65f);
        ShadowLabel(new Rect(0, Screen.height * 0.2f, Screen.width, 90), victoryTitle, big);

        var sub = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Italic, fontSize = 20 };
        sub.normal.textColor = new Color(0.75f, 0.8f, 0.85f);
        GUI.Label(new Rect(0, Screen.height * 0.2f + 92, Screen.width, 30), victorySubtitle, sub);

        float bw = 260, bh = 54, cx = Screen.width / 2f - bw / 2f, by = Screen.height * 0.55f;
        var s = BtnStyle();
        if (GUI.Button(new Rect(cx, by, bw, bh), "PLAY AGAIN", s)) StartNewGame();
        if (GUI.Button(new Rect(cx, by + bh + 14, bw, bh), "QUIT", s)) Quit();
    }

    void DrawMainMenu()
    {
        if (backgroundImage != null)
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), backgroundImage, ScaleMode.ScaleAndCrop);
        Dim(0.45f);

        if (showControls) { DrawControlsPanel(() => showControls = false); return; }

        Title(title, subtitle);

        float bw = 260, bh = 54, cx = Screen.width / 2f - bw / 2f, by = Screen.height * 0.52f;
        var s = BtnStyle();
        if (GUI.Button(new Rect(cx, by, bw, bh), "START", s)) StartNewGame();
        if (GUI.Button(new Rect(cx, by + (bh + 14), bw, bh), "CONTROLS", s)) showControls = true;
        if (GUI.Button(new Rect(cx, by + (bh + 14) * 2, bw, bh), "QUIT", s)) Quit();
    }

    void DrawPauseMenu()
    {
        Dim(0.6f);

        if (showControls) { DrawControlsPanel(() => showControls = false); return; }

        Title("PAUSED", "");

        float bw = 260, bh = 50, cx = Screen.width / 2f - bw / 2f, by = Screen.height * 0.42f;
        var s = BtnStyle();
        if (GUI.Button(new Rect(cx, by, bw, bh), "RESUME", s)) Resume();
        if (GUI.Button(new Rect(cx, by + (bh + 12), bw, bh), "RESTART", s)) Restart();
        if (GUI.Button(new Rect(cx, by + (bh + 12) * 2, bw, bh), "CONTROLS", s)) showControls = true;
        if (GUI.Button(new Rect(cx, by + (bh + 12) * 3, bw, bh), "QUIT", s)) Quit();
    }

    void DrawEndScreen()
    {
        bool win = GameManager.Instance.IsWin;
        Dim(0.65f);

        var big = new GUIStyle(GUI.skin.label) { fontSize = 56, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        big.normal.textColor = win ? new Color(0.6f, 1f, 0.65f) : new Color(1f, 0.45f, 0.45f);
        ShadowLabel(new Rect(0, Screen.height * 0.28f, Screen.width, 80), win ? "YOU ESCAPED" : "YOU DIED", big);

        float bw = 260, bh = 54, cx = Screen.width / 2f - bw / 2f, by = Screen.height * 0.5f;
        var s = BtnStyle();
        if (GUI.Button(new Rect(cx, by, bw, bh), "RESTART", s)) Restart();
        if (GUI.Button(new Rect(cx, by + bh + 14, bw, bh), "QUIT", s)) Quit();
    }

    // ---- helpers ----
    void Dim(float a)
    {
        GUI.color = new Color(0f, 0f, 0f, a);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;
    }

    void Title(string main, string sub)
    {
        var t = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, fontSize = 60 };
        t.normal.textColor = new Color(0.85f, 0.9f, 1f);
        ShadowLabel(new Rect(0, Screen.height * 0.2f, Screen.width, 90), main, t);

        if (!string.IsNullOrEmpty(sub))
        {
            var st = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Italic, fontSize = 20 };
            st.normal.textColor = new Color(0.75f, 0.8f, 0.85f);
            GUI.Label(new Rect(0, Screen.height * 0.2f + 92, Screen.width, 30), sub, st);
        }
    }

    void DrawControlsPanel(System.Action onBack)
    {
        int n = Mathf.Min(controlsKeys.Length, controlsActions.Length);
        float panelW = 460f;

        // Nine lines is taller than a short game window, so the rows tighten until it fits
        // and the whole panel sits in the middle rather than running off the bottom.
        float lineH = Mathf.Clamp((Screen.height - 130f) / Mathf.Max(1, n), 18f, 30f);
        float panelH = lineH * n + 90f;
        float x = Screen.width / 2f - panelW / 2f;
        float y = Mathf.Max(10f, Screen.height / 2f - panelH / 2f);

        GUI.color = new Color(0f, 0f, 0f, 0.6f);
        GUI.DrawTexture(new Rect(x, y, panelW, panelH), Texture2D.whiteTexture);
        GUI.color = Color.white;

        var keyS = new GUIStyle(GUI.skin.label) { fontSize = 17, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft };
        var actS = new GUIStyle(GUI.skin.label) { fontSize = 17, alignment = TextAnchor.MiddleLeft };
        keyS.normal.textColor = new Color(0.7f, 0.9f, 1f);
        actS.normal.textColor = Color.white;

        for (int i = 0; i < n; i++)
        {
            float ly = y + 16 + i * lineH;
            GUI.Label(new Rect(x + 24, ly, 190, lineH), controlsKeys[i], keyS);
            GUI.Label(new Rect(x + 220, ly, 220, lineH), controlsActions[i], actS);
        }

        float bw = 160, bh = 40;
        if (GUI.Button(new Rect(x + panelW / 2f - bw / 2f, y + panelH - 52, bw, bh), "BACK", BtnStyle()))
            onBack();
    }

    GUIStyle BtnStyle() => new GUIStyle(GUI.skin.button) { fontSize = 22, fontStyle = FontStyle.Bold };

    void ShadowLabel(Rect r, string text, GUIStyle style)
    {
        Color c = style.normal.textColor;
        style.normal.textColor = Color.black;
        GUI.Label(new Rect(r.x + 2, r.y + 2, r.width, r.height), text, style);
        style.normal.textColor = c;
        GUI.Label(r, text, style);
    }
}