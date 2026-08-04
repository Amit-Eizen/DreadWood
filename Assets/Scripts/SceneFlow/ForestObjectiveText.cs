using UnityEngine;

// Shows the player's current objective in the forest (Scene 1), using the HUD that
// GameManager already draws. While zombies remain it shows "Defeat zombies: X / 3";
// once enough are beaten it switches to "Go to the light".
// This is the game's in-game tutorial text, and it updates by itself (dynamic).
public class ForestObjectiveText : MonoBehaviour
{
    [Tooltip("How many zombies must be beaten before the way to the boss opens")]
    public int zombiesToDefeat = 3;

    [Tooltip("Text shown once enough zombies are beaten")]
    public string reachLightText = "Go to the light";

    // The last count we showed, so we only update the text when it actually changes.
    private int lastShownCount = -1;

    void Update()
    {
        if (GameManager.Instance == null || PlayerProgressBetweenScenes.Instance == null)
            return;

        int defeated = PlayerProgressBetweenScenes.Instance.ZombiesDefeated;
        if (defeated == lastShownCount)
            return;                       // nothing changed - don't rebuild the text every frame
        lastShownCount = defeated;

        if (defeated >= zombiesToDefeat)
            GameManager.Instance.SetObjective(reachLightText);
        else
            GameManager.Instance.SetObjective("Defeat zombies: " + defeated + " / " + zombiesToDefeat);
    }
}
