using UnityEngine;
using UnityEngine.InputSystem;

// A small controls legend in the corner of the screen, e.g.:
//   WASD  Move
//   SHIFT Run
//   CTRL  Sneak
//   LMB   Attack
//   RMB   Dodge
// Fades out after a while so it doesn't clutter the screen (press H to toggle).
public class ControlsHUD : MonoBehaviour
{
    [System.Serializable]
    public struct Control { public string key; public string action; }

    [Tooltip("The legend lines, key + action")]
    public Control[] controls = new Control[]
    {
        new Control { key = "WASD",  action = "Move" },
        new Control { key = "SHIFT", action = "Run" },
        new Control { key = "CTRL",  action = "Sneak" },
        new Control { key = "LMB",   action = "Attack" },
        new Control { key = "RMB",   action = "Dodge" },
        new Control { key = "SPACE", action = "Jump" },
    };

    [Header("Placement")]
    public bool bottomRight = true;
    public int fontSize = 15;

    [Header("Auto-hide")]
    [Tooltip("Seconds the legend stays fully visible at the start (0 = never auto-hide)")]
    public float visibleSeconds = 12f;
    [Tooltip("How long it takes to fade out")]
    public float fadeSeconds = 1.5f;

    private float shownAt;
    private bool forcedVisible = true;

    void Start() { shownAt = Time.unscaledTime; }

    void Update()
    {
        // press H to toggle the legend back on/off
        if (Keyboard.current != null && Keyboard.current.hKey.wasPressedThisFrame)
        {
            forcedVisible = !forcedVisible;
            shownAt = Time.unscaledTime;
        }
    }

    float CurrentAlpha()
    {
        if (visibleSeconds <= 0f) return 1f;                 // always on
        float t = Time.unscaledTime - shownAt;
        if (!forcedVisible) return 0f;
        if (t <= visibleSeconds) return 1f;
        float f = 1f - (t - visibleSeconds) / Mathf.Max(0.01f, fadeSeconds);
        return Mathf.Clamp01(f);
    }

    void OnGUI()
    {
        float alpha = CurrentAlpha();
        if (alpha <= 0.001f)
        {
            // tiny hint that it can be toggled back
            DrawHint();
            return;
        }

        var keyStyle = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, fontSize = fontSize, alignment = TextAnchor.MiddleLeft };
        var actStyle = new GUIStyle(GUI.skin.label) { fontSize = fontSize, alignment = TextAnchor.MiddleLeft };

        float lineH = fontSize + 8f;
        float keyW = 70f, actW = 110f, pad = 12f;
        float boxW = keyW + actW + pad * 2f;
        float boxH = lineH * controls.Length + pad * 2f;

        float x = bottomRight ? Screen.width - boxW - 16f : 16f;
        float y = Screen.height - boxH - 16f;

        // background panel
        GUI.color = new Color(0f, 0f, 0f, 0.45f * alpha);
        GUI.DrawTexture(new Rect(x, y, boxW, boxH), Texture2D.whiteTexture);
        GUI.color = Color.white;

        for (int i = 0; i < controls.Length; i++)
        {
            float ly = y + pad + i * lineH;
            keyStyle.normal.textColor = new Color(0.7f, 0.9f, 1f, alpha);   // cold blue key
            actStyle.normal.textColor = new Color(0.92f, 0.92f, 0.92f, alpha);
            GUI.Label(new Rect(x + pad, ly, keyW, lineH), controls[i].key, keyStyle);
            GUI.Label(new Rect(x + pad + keyW, ly, actW, lineH), controls[i].action, actStyle);
        }
    }

    void DrawHint()
    {
        var s = new GUIStyle(GUI.skin.label) { fontSize = 12, alignment = TextAnchor.MiddleRight };
        s.normal.textColor = new Color(1f, 1f, 1f, 0.4f);
        GUI.Label(new Rect(Screen.width - 130, Screen.height - 24, 120, 18), "[H] Controls", s);
    }
}