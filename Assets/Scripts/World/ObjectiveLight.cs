using UnityEngine;

// Trigger zone at the end point (the light / beacon).
// When the player arrives, the hidden BOSS is revealed and the final fight begins
// — the guiding beacon switches OFF so only the world's dim atmosphere remains,
// making the boss fight darker and tenser. The player must defeat the boss to win.
// (If no boss is assigned it just wins immediately, useful for testing the loop.)
[RequireComponent(typeof(Collider))]
public class ObjectiveLight : MonoBehaviour
{
    [Tooltip("The boss mutant to reveal (kept disabled until the player arrives)")]
    public GameObject boss;

    [Tooltip("Objective text shown once the boss appears")]
    public string bossObjective = "It found you — DEFEAT IT!";

    [Header("When the fight begins")]
    [Tooltip("Turn these OFF when the boss appears — e.g. the glowing Beacon + its light")]
    public GameObject[] turnOffOnBoss;

    [Tooltip("Optional: turn these ON for the fight — e.g. a dim arena light")]
    public GameObject[] turnOnOnBoss;

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

            // the guiding light dies — only the world's atmosphere remains
            foreach (GameObject go in turnOffOnBoss) if (go != null) go.SetActive(false);
            foreach (GameObject go in turnOnOnBoss) if (go != null) go.SetActive(true);

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
