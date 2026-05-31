using UnityEngine;

// Trigger zone at the end point (the light / portal).
// When the player arrives, the hidden BOSS is revealed and the final fight
// begins — the player must defeat it to win. (If no boss is assigned it just
// wins immediately, useful for testing the loop.)
[RequireComponent(typeof(Collider))]
public class ObjectiveLight : MonoBehaviour
{
    [Tooltip("The boss mutant to reveal (kept disabled until the player arrives)")]
    public GameObject boss;

    [Tooltip("Objective text shown once the boss appears")]
    public string bossObjective = "It found you — DEFEAT IT!";

    private bool triggered = false;

    void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (triggered || !IsPlayer(other)) return;
        triggered = true;

        if (boss != null)
        {
            boss.SetActive(true);                              // the creature appears
            if (GameManager.Instance != null) GameManager.Instance.SetObjective(bossObjective);
        }
        else if (GameManager.Instance != null)
        {
            GameManager.Instance.Win();                        // no boss -> just win (test mode)
        }
    }

    static bool IsPlayer(Collider c)
    {
        return c.CompareTag("Player") || c.gameObject.name == "PlayerArmature";
    }
}
