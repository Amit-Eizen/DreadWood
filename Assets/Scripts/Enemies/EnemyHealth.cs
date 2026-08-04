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
    private Camera cam;
    private Animator anim;

    void Awake()
    {
        hp = maxHP;
        cam = Camera.main;
        anim = GetComponent<Animator>();
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

        if (hp <= 0) Die();
    }

    void Die()
    {
        dead = true;
        MutantAI ai = GetComponent<MutantAI>();
        if (ai != null) ai.enabled = false;
        foreach (Collider c in GetComponentsInChildren<Collider>()) c.enabled = false;

        // play the death animation only if this controller has a "die" trigger
        // (the boss's MutantController doesn't — avoids a console warning)
        if (anim != null && HasParam(anim, "die")) anim.SetTrigger("die");

        if (isBoss && GameManager.Instance != null)
            GameManager.Instance.Win();

        OnDeath?.Invoke();   // let listeners react (the arena uses this to return to the forest)

        Destroy(gameObject, removeDelay);
    }

    static bool HasParam(Animator a, string param)
    {
        foreach (AnimatorControllerParameter p in a.parameters)
            if (p.name == param) return true;
        return false;
    }

    void OnGUI()
    {
        if (dead) return;
        if (isBoss) DrawBossBar();
        else if (hp < maxHP) DrawHeadBar();   // zombies: only after first hit
    }

    void DrawBossBar()
    {
        float w = Screen.width * 0.5f, h = 24f;
        float x = (Screen.width - w) / 2f, y = 22f;
        float frac = (float)hp / maxHP;

        GUI.color = new Color(0f, 0f, 0f, 0.6f);
        GUI.DrawTexture(new Rect(x - 3, y - 3, w + 6, h + 6), Texture2D.whiteTexture);
        GUI.color = new Color(0.25f, 0f, 0f, 1f);
        GUI.DrawTexture(new Rect(x, y, w, h), Texture2D.whiteTexture);
        GUI.color = new Color(0.85f, 0.12f, 0.12f, 1f);
        GUI.DrawTexture(new Rect(x, y, w * frac, h), Texture2D.whiteTexture);
        GUI.color = Color.white;

        GUIStyle st = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, fontSize = 15 };
        st.normal.textColor = Color.white;
        GUI.Label(new Rect(x, y, w, h), bossName, st);
    }

    void DrawHeadBar()
    {
        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        Vector3 sp = cam.WorldToScreenPoint(transform.position + Vector3.up * headHeight);
        if (sp.z <= 0f) return;   // behind the camera

        float w = 64f, h = 8f;
        float x = sp.x - w / 2f;
        float y = Screen.height - sp.y - h;   // OnGUI y is top-down
        float frac = (float)hp / maxHP;

        GUI.color = new Color(0f, 0f, 0f, 0.7f);
        GUI.DrawTexture(new Rect(x - 1, y - 1, w + 2, h + 2), Texture2D.whiteTexture);
        GUI.color = new Color(0.2f, 0f, 0f, 1f);
        GUI.DrawTexture(new Rect(x, y, w, h), Texture2D.whiteTexture);
        GUI.color = new Color(0.85f, 0.12f, 0.12f, 1f);
        GUI.DrawTexture(new Rect(x, y, w * frac, h), Texture2D.whiteTexture);
        GUI.color = Color.white;
    }
}
