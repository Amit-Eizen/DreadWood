using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// The trigger at the portal. Reach it with enough zombies beaten and the portal dies and the
// next scene loads. Arrive early and it says how many are still missing and lets you walk away.
[RequireComponent(typeof(Collider))]
public class ObjectiveLight : MonoBehaviour
{
    [Header("Entry condition")]
    [Tooltip("The portal stays shut until this many zombies are beaten")]
    public int zombiesRequired = 3;

    [Tooltip("Shown when the player arrives too early. {0} is how many are still missing.")]
    public string notReadyMessage = "The portal is sealed — {0} zombies left";

    [Tooltip("Scene to load once the portal dies. Set this and the creature is skipped entirely.")]
    public string nextScene = "";

    [Tooltip("Seconds between the portal going out and the load, so it lands as a beat")]
    public float pauseBeforeLoading = 1.2f;

    [Tooltip("The boss mutant to reveal (kept disabled until the player arrives)")]
    public GameObject boss;

    [Tooltip("Objective text shown once the boss appears")]
    public string bossObjective = "It found you — DEFEAT IT!";

    [Header("When the fight begins")]
    [Tooltip("Turn these OFF when the boss appears — the portal and its light")]
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

        // the portal goes out — what you walked all this way towards was the trap
        foreach (GameObject go in turnOffOnBoss) if (go != null) go.SetActive(false);
        foreach (GameObject go in turnOnOnBoss) if (go != null) go.SetActive(true);

        if (GameManager.Instance != null) GameManager.Instance.SetObjective(bossObjective);

        if (!string.IsNullOrEmpty(nextScene)) StartCoroutine(LoadNextScene());
        else if (boss != null) boss.SetActive(true);           // the creature appears instead
        else if (GameManager.Instance != null) GameManager.Instance.Win();   // test mode
    }

    IEnumerator LoadNextScene()
    {
        yield return new WaitForSeconds(pauseBeforeLoading);
        SceneManager.LoadScene(nextScene);
    }

    int ZombiesStillNeeded()
    {
        PlayerProgressBetweenScenes progress = PlayerProgressBetweenScenes.Instance;
        if (progress == null) return 0;   // no progress object (testing this scene alone) — let it through
        return Mathf.Max(0, zombiesRequired - progress.ZombiesDefeated);
    }
}
