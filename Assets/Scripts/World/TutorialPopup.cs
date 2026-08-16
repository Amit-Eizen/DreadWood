using UnityEngine;
using StarterAssets;

// A one-time tutorial card. Walk into the trigger — put it just outside the cabin door —
// and the game holds while a panel explains what to do and which keys to use. The player
// reads at their own pace and presses CONTINUE to carry on.
[RequireComponent(typeof(Collider))]
public class TutorialPopup : MonoBehaviour
{
    [Header("What it says")]
    public string title = "THE WOODS";
    [TextArea(2, 6)]
    public string message = "Zombies roam these trees. Beat three of them and the portal will open.";

    [Tooltip("Left column of the key list")]
    public string[] keys = { "WASD", "SHIFT", "CTRL", "L-Click", "R-Click tap", "R-Click hold", "L-Click while aiming", "Y" };
    [Tooltip("Right column — must line up with the keys above")]
    public string[] actions = { "Move", "Run", "Sneak", "Attack", "Dodge", "Aim (first person)", "Throw a rock", "Open door" };

    public string continueLabel = "CONTINUE";

    private bool alreadyShown = false;
    private bool showing = false;
    private MonoBehaviour[] playerScripts;

    // Makes the collider a trigger the moment the component is added, so it can't end up
    // in the scene as a solid wall by mistake.
    void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (alreadyShown || showing) return;
        if (!other.CompareTag("Player") && other.GetComponentInParent<ThirdPersonController>() == null) return;

        Show();
    }

    void Show()
    {
        showing = true;
        alreadyShown = true;

        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Freezing time stops movement, but the player could still swing or aim, and the
        // Starter Assets controller re-locks the cursor on focus — so switch them off.
        playerScripts = FindPlayerScripts();
        foreach (MonoBehaviour script in playerScripts)
            if (script != null) script.enabled = false;
    }

    void Continue()
    {
        showing = false;

        foreach (MonoBehaviour script in playerScripts)
            if (script != null) script.enabled = true;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        Time.timeScale = 1f;

        gameObject.SetActive(false);   // it has done its job
    }

    static MonoBehaviour[] FindPlayerScripts()
    {
        ThirdPersonController controller = Object.FindFirstObjectByType<ThirdPersonController>();
        if (controller == null) return new MonoBehaviour[0];

        GameObject p = controller.gameObject;
        return new MonoBehaviour[]
        {
            p.GetComponent<ThirdPersonController>(),
            p.GetComponent<StarterAssetsInputs>(),
            p.GetComponent<PlayerCombat>(),
            p.GetComponent<PlayerDodge>(),
            p.GetComponent<PlayerStealth>(),
            p.GetComponent<PlayerAiming>(),
            p.GetComponent<RockThrow>(),
        };
    }

    void OnGUI()
    {
        if (!showing) return;

        int lines = Mathf.Min(keys.Length, actions.Length);
        float lineHeight = 28f;
        float panelW = 520f;
        float panelH = 150f + lines * lineHeight + 70f;
        float x = Screen.width / 2f - panelW / 2f;
        float y = Screen.height / 2f - panelH / 2f;

        GUI.color = new Color(0f, 0f, 0f, 0.85f);
        GUI.DrawTexture(new Rect(x, y, panelW, panelH), Texture2D.whiteTexture);
        GUI.color = Color.white;

        GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
        { fontSize = 30, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        GUI.Label(new Rect(x, y + 18, panelW, 40), title, titleStyle);

        GUIStyle bodyStyle = new GUIStyle(GUI.skin.label)
        { fontSize = 17, alignment = TextAnchor.UpperCenter, wordWrap = true };
        GUI.Label(new Rect(x + 30, y + 64, panelW - 60, 70), message, bodyStyle);

        GUIStyle keyStyle = new GUIStyle(GUI.skin.label)
        { fontSize = 16, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft };
        keyStyle.normal.textColor = new Color(0.7f, 0.9f, 1f);
        GUIStyle actionStyle = new GUIStyle(GUI.skin.label)
        { fontSize = 16, alignment = TextAnchor.MiddleLeft };

        for (int i = 0; i < lines; i++)
        {
            float lineY = y + 140 + i * lineHeight;
            GUI.Label(new Rect(x + 40, lineY, 200, lineHeight), keys[i], keyStyle);
            GUI.Label(new Rect(x + 250, lineY, 240, lineHeight), actions[i], actionStyle);
        }

        GUIStyle buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 20, fontStyle = FontStyle.Bold };
        float buttonW = 180f, buttonH = 42f;
        if (GUI.Button(new Rect(x + panelW / 2f - buttonW / 2f, y + panelH - 56, buttonW, buttonH), continueLabel, buttonStyle))
            Continue();
    }
}
