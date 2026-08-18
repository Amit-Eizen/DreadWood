using System.Collections;
using UnityEngine;

// Health for an enemy (trail zombie or the final boss).
// - Boss: shows a big health bar across the TOP of the screen while alive;
//   killing it wins the game.
// - Zombie: shows a small health bar ABOVE ITS HEAD once it has been hit.
// On a hit it can flinch / get nudged back so the strike reads. (The visible
// "hit mark" comes from PlayerCombat's slash effect.) At 0 HP it dies (disables
// its AI + colliders, plays the death anim, then is removed).
public class EnemyHealth : MonoBehaviour
{
    public int maxHP = 30;
    public bool isBoss = false;
    public float removeDelay = 2f;

    [Header("Boss bar")]
    public string bossName = "THE CREATURE";

    [Header("Boss knockdowns")]
    [Tooltip("Fractions of health where the boss goes down and a pillar can be brought over on it")]
    public float[] knockdownAt = { 0.75f, 0.5f, 0.25f };

    [Tooltip("Seconds it stays down")]
    public float knockdownSeconds = 6f;

    [Tooltip("Shown on the boss bar while it is down")]
    public string knockdownHint = "DOWN — BRING A PILLAR OVER ON IT";

    [Tooltip("Seconds between the boss dying and the win screen. The screen freezes the game, " +
             "so without this the pillar stops in mid-air.")]
    public float winDelay = 3f;

    [Header("Head bar (zombies)")]
    public float headHeight = 2.2f;   // how high above the pivot the bar floats

    [Header("Hit feedback")]
    [Tooltip("Small backward nudge when hit (0 = none)")]
    public float knockback = 0.12f;
    [Tooltip("Optional animator trigger to play a flinch — leave empty if there's no such state")]
    public string hitAnimTrigger = "";

    // Fired once when this enemy dies (the arena listens to this to return to the forest).
    public System.Action OnDeath;

    private int hp;
    private bool dead = false;
    private int nextKnockdown = 0;
    private Camera cam;
    private Animator anim;
    private MutantAI ai;

    public float HealthFraction => maxHP > 0 ? (float)hp / maxHP : 0f;

    void Awake()
    {
        hp = maxHP;
        cam = Camera.main;
        anim = GetComponent<Animator>();
        ai = GetComponent<MutantAI>();
    }

    public void TakeDamage(int dmg)
    {
        if (dead) return;
        hp = Mathf.Max(0, hp - dmg);

        if (hp > 0)
        {
            if (anim != null && !string.IsNullOrEmpty(hitAnimTrigger) && HasParam(anim, hitAnimTrigger))
                anim.SetTrigger(hitAnimTrigger);
            if (knockback > 0f)
            {
                Vector3 back = -transform.forward * knockback;   // shove away from its facing
                transform.position += new Vector3(back.x, 0f, back.z);
            }
        }

        if (hp > 0) CheckKnockdown();
        else Die();
    }

    // One heavy hit can cross two thresholds at once, so this walks past every one it has
    // gone under rather than only the next in line.
    void CheckKnockdown()
    {
        if (!isBoss || ai == null) return;

        bool crossed = false;
        while (nextKnockdown < knockdownAt.Length && HealthFraction <= knockdownAt[nextKnockdown])
        {
            nextKnockdown++;
            crossed = true;
        }

        if (crossed) ai.KnockOut(knockdownSeconds);
    }

    void Die()
    {
        dead = true;
        foreach (Collider c in GetComponentsInChildren<Collider>()) c.enabled = false;

        if (anim != null && HasParam(anim, "die"))
        {
            if (ai != null) ai.enabled = false;
            anim.SetTrigger("die");
        }
        else if (ai != null)
        {
            ai.KnockOut(999f);   // this rig has no death clip, so it goes down like a knockdown
        }

        if (isBoss && GameManager.Instance != null) StartCoroutine(WinAfterAMoment());

        OnDeath?.Invoke();   // let listeners react (the arena uses this to return to the forest)

        // It has to outlive the win delay, or the coroutine dies with it.
        Destroy(gameObject, Mathf.Max(removeDelay, winDelay + 0.2f));
    }

    IEnumerator WinAfterAMoment()
    {
        yield return new WaitForSeconds(winDelay);
        if (GameManager.Instance != null) GameManager.Instance.Win();
    }

    static bool HasParam(Animator a, string param)
    {
        foreach (AnimatorControllerParameter p in a.parameters)
            if (p.name == param) return true;
        return false;
    }

    void OnGUI()
    {
        if (dead || Hud.Hidden) return;
        if (isBoss) DrawBossBar();
        else if (hp < maxHP) DrawHeadBar();   // zombies: only after first hit
    }

    void DrawBossBar()
    {
        float w = Screen.width * 0.5f, h = 24f;
        float x = (Screen.width - w) / 2f;
        float y = Screen.height - 70f;   // along the bottom, clear of the player's own bars

        Hud.Bar(new Rect(x, y, w, h), HealthFraction, new Color(0.85f, 0.12f, 0.12f));

        bool down = ai != null && ai.IsKnockedOut;
        GUIStyle st = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, fontSize = 15 };
        st.normal.textColor = down ? new Color(1f, 0.9f, 0.4f) : Color.white;
        GUI.Label(new Rect(x, y, w, h), down ? knockdownHint : bossName, st);
    }

    void DrawHeadBar()
    {
        if (cam == null) cam = Camera.main;

        Hud.BarAbove(cam, transform.position + Vector3.up * headHeight,
                     (float)hp / maxHP, new Color(0.85f, 0.12f, 0.12f));
    }
}
