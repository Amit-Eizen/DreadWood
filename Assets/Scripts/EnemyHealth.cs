using System.Collections;
using UnityEngine;

// Health for an enemy (trail zombie or the final boss).
// - Boss: shows a big health bar across the TOP of the screen while alive;
//   killing it wins the game.
// - Zombie: shows a small health bar ABOVE ITS HEAD once it has been hit.
// On every hit it FLASHES (so you clearly feel the strike land). When HP hits 0
// it dies (disables its AI + colliders, plays the death anim, then is removed).
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
    public Color hitFlashColor = new Color(1f, 0.25f, 0.25f);
    public float flashTime = 0.09f;
    [Tooltip("Optional: a small backward nudge when hit (0 = none)")]
    public float knockback = 0.12f;
    [Tooltip("Optional: animator trigger to play a flinch — leave empty if the enemy has none")]
    public string hitAnimTrigger = "";

    private int hp;
    private bool dead = false;
    private Camera cam;

    private Renderer[] renderers;
    private Color[] baseColors;
    private bool flashing = false;
    private Animator anim;

    void Awake()
    {
        hp = maxHP;
        cam = Camera.main;
        anim = GetComponent<Animator>();

        // cache renderers + their original colour so we can flash and restore
        renderers = GetComponentsInChildren<Renderer>();
        baseColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
            if (renderers[i].material.HasProperty("_Color"))
                baseColors[i] = renderers[i].material.color;
    }

    public void TakeDamage(int dmg)
    {
        if (dead) return;
        hp = Mathf.Max(0, hp - dmg);

        // FEEDBACK: flash + optional flinch + optional little knockback
        if (gameObject.activeInHierarchy) StartCoroutine(Flash());
        if (anim != null && !string.IsNullOrEmpty(hitAnimTrigger) && hp > 0) anim.SetTrigger(hitAnimTrigger);
        if (knockback > 0f && hp > 0)
        {
            Vector3 back = -transform.forward * knockback;   // shove away from its facing
            transform.position += new Vector3(back.x, 0f, back.z);
        }

        if (hp <= 0) Die();
    }

    IEnumerator Flash()
    {
        if (flashing) yield break;
        flashing = true;
        SetColor(hitFlashColor);
        yield return new WaitForSecondsRealtime(flashTime);
        RestoreColors();
        flashing = false;
    }

    void SetColor(Color c)
    {
        if (renderers == null) return;
        foreach (Renderer r in renderers)
            if (r != null && r.material.HasProperty("_Color")) r.material.color = c;
    }

    void RestoreColors()
    {
        if (renderers == null) return;
        for (int i = 0; i < renderers.Length; i++)
            if (renderers[i] != null && renderers[i].material.HasProperty("_Color"))
                renderers[i].material.color = baseColors[i];
    }

    void Die()
    {
        dead = true;
        RestoreColors();
        MutantAI ai = GetComponent<MutantAI>();
        if (ai != null) ai.enabled = false;
        foreach (Collider c in GetComponentsInChildren<Collider>()) c.enabled = false;

        if (anim != null) anim.SetTrigger("die");   // play the death animation

        if (isBoss && GameManager.Instance != null)
            GameManager.Instance.Win();

        Destroy(gameObject, removeDelay);
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
