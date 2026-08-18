using UnityEngine;

// Trigger zone at the end point (the light / beacon).
// When the player arrives, the hidden BOSS is revealed and the final fight begins
// — the guiding beacon switches OFF so only the world's dim atmosphere remains,
// making the boss fight darker and tenser. The player must defeat the boss to win.
// (If no boss is assigned it just wins immediately, useful for testing the loop.)
[RequireComponent(typeof(Collider))]
public class ObjectiveLight : MonoBehaviour
{
    [Header("Entry condition")]
    [Tooltip("The portal stays shut until this many zombies are beaten")]
    public int zombiesRequired = 3;

    [Tooltip("Shown when the player arrives too early. {0} is how many are still missing.")]
    public string notReadyMessage = "The portal is sealed — {0} zombies left";

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
        if (triggered || !PlayerTeleport.IsPlayer(other)) return;

        // The portal will not open until the objective is done, so the player cannot
        // simply walk past every zombie and skip straight to the end.
        int stillNeeded = ZombiesStillNeeded();
        if (stillNeeded > 0)
        {
            if (GameManager.Instance != null)
                GameManager.Instance.SetObjective(string.Format(notReadyMessage, stillNeeded));
            return;   // no `triggered` flag — walking back in later must work
        }

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

    int ZombiesStillNeeded()
    {
        PlayerProgressBetweenScenes progress = PlayerProgressBetweenScenes.Instance;
        if (progress == null) return 0;   // no progress object (testing this scene alone) — let it through
        return Mathf.Max(0, zombiesRequired - progress.ZombiesDefeated);
    }
}
