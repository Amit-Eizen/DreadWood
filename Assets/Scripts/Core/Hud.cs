using UnityEngine;

// Bits of on-screen drawing that more than one script needs.
public static class Hud
{
    // The HUD is hidden while a menu or the tutorial card is on screen.
    public static bool Hidden => MainMenu.IsOpen || TutorialPopup.IsShowing;

    // A small bar floating above a point in the world — an enemy's head, a pillar's top.
    public static void BarAbove(Camera view, Vector3 worldPoint, float fraction, Color fill,
                                float width = 64f, float height = 8f)
    {
        if (view == null) return;

        Vector3 screen = view.WorldToScreenPoint(worldPoint);
        if (screen.z <= 0f) return;   // behind the camera

        // OnGUI counts y from the top, the camera counts it from the bottom.
        Rect area = new Rect(screen.x - width / 2f, Screen.height - screen.y - height, width, height);
        Bar(area, fraction, fill);
    }

    public static void Bar(Rect area, float fraction, Color fill)
    {
        Color previous = GUI.color;

        GUI.color = new Color(0f, 0f, 0f, 0.7f);
        GUI.DrawTexture(new Rect(area.x - 1, area.y - 1, area.width + 2, area.height + 2), Texture2D.whiteTexture);
        GUI.color = new Color(fill.r * 0.25f, fill.g * 0.25f, fill.b * 0.25f, 1f);
        GUI.DrawTexture(area, Texture2D.whiteTexture);
        GUI.color = fill;
        GUI.DrawTexture(new Rect(area.x, area.y, area.width * Mathf.Clamp01(fraction), area.height), Texture2D.whiteTexture);

        GUI.color = previous;
    }
}
