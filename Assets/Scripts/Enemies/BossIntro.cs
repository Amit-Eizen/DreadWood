using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// Dramatic boss entrance. Put this on the Mutant (the boss) — it runs the moment
// the boss is revealed (SetActive true by ObjectiveLight):
//   1. The boss is scaled up HUGE and starts sunk into the ground.
//   2. It slowly RISES out of the earth ("waking up") while its idle plays.
//   3. A short pause/roar — then its AI switches on and it CHASES the player.
// During the intro the MutantAI is disabled so it doesn't move early.
public class BossIntro : MonoBehaviour
{
    [Header("Size")]
    [Tooltip("Final scale of the boss — bigger = more menacing")]
    public float bossScale = 2.6f;

    [Header("Rise / wake-up")]
    [Tooltip("How deep underground it starts")]
    public float startSunkDepth = 3.5f;
    [Tooltip("How long the rise takes")]
    public float riseTime = 3.0f;
    [Tooltip("Extra pause at full height before it starts chasing (a beat to be scary)")]
    public float roarPause = 0.8f;
    [Tooltip("Play the attack/swipe as a 'roar' at the top of the rise")]
    public bool roarAtTop = true;

    [Header("What happens once it is up")]
    [Tooltip("Leave empty and the creature simply starts fighting here. Name a scene and it " +
             "knocks the player down instead and that scene loads — this is how the forest " +
             "hands over to the chase.")]
    public string loadSceneWhenDone = "";

    [Tooltip("What the knockdown blow costs. It can never be the hit that kills you.")]
    public int knockdownDamage = 20;

    [Tooltip("Objective shown while you are on the floor")]
    public string knockdownObjective = "RUN!";

    [Tooltip("Seconds the player sees that before the next scene loads")]
    public float knockdownPause = 1.8f;

    private MutantAI ai;
    private Animator animator;
    private Terrain ground;

    void Awake()
    {
        // grab + silence the AI immediately so it can't move during the intro
        ai = GetComponent<MutantAI>();
        animator = GetComponent<Animator>();
        ground = Terrain.activeTerrain;
        if (ai != null) ai.enabled = false;
    }

    void OnEnable()
    {
        // (boss starts disabled; this fires when ObjectiveLight reveals it)
        StopAllCoroutines();
        StartCoroutine(RiseSequence());
    }

    IEnumerator RiseSequence()
    {
        if (ai != null) ai.enabled = false;

        // make it huge
        transform.localScale = Vector3.one * bossScale;

        // figure out ground height at the boss position
        Vector3 pos = transform.position;
        float groundY = pos.y;
        if (ground != null) groundY = ground.SampleHeight(pos) + ground.transform.position.y;

        Vector3 buried = new Vector3(pos.x, groundY - startSunkDepth, pos.z);
        Vector3 risen = new Vector3(pos.x, groundY, pos.z);
        transform.position = buried;

        if (animator != null) animator.Play(0, 0, 0f);   // make sure idle is playing

        // rise out of the ground
        float t = 0f;
        while (t < riseTime)
        {
            float k = t / riseTime;
            k = k * k * (3f - 2f * k);   // smoothstep — slow start, slow end
            transform.position = Vector3.Lerp(buried, risen, k);
            t += Time.deltaTime;
            yield return null;
        }
        transform.position = risen;

        // a beat at full height — roar
        if (roarAtTop && animator != null) animator.SetTrigger("attack");
        yield return new WaitForSeconds(roarPause);

        if (string.IsNullOrEmpty(loadSceneWhenDone))
        {
            if (ai != null) ai.enabled = true;    // unleash it
            yield break;
        }

        yield return KnockThePlayerDown();
    }

    // The forest ends with a beating, not a fight: one blow, "RUN!", and the chase begins.
    IEnumerator KnockThePlayerDown()
    {
        if (GameManager.Instance != null)
        {
            // Dying here would freeze the game before the chase ever loaded, so the blow
            // is capped at whatever leaves the player standing.
            int survivable = Mathf.Min(knockdownDamage, GameManager.Instance.currentHP - 1);
            if (survivable > 0) GameManager.Instance.TakeDamage(survivable);

            GameManager.Instance.SetObjective(knockdownObjective);
        }

        yield return new WaitForSeconds(knockdownPause);
        SceneManager.LoadScene(loadSceneWhenDone);
    }
}
