using UnityEngine;

// Put this on the GameManager in the arena scene. It sets the objective text for the fight,
// so the HUD doesn't keep showing the forest's objective while you're in the arena.
public class ArenaObjectiveText : MonoBehaviour
{
    [Tooltip("Objective text shown during the arena fight")]
    public string fightText = "Kill the zombie!";

    void Start()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.SetObjective(fightText);
    }
}
