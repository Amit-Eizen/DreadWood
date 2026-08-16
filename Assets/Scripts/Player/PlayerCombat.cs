using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using StarterAssets;

// Melee combat for the player (Brute).
//  - Left-click = a combo swing (cycles through Combo Triggers).
//  - Sprint (Shift) + left-click = a running jump attack that LEAPS forward.
//  - Each swing opens a HIT WINDOW that checks continuously, so a moving enemy
//    still gets caught (no more "looked like a hit but missed").
//  - On a hit: slash effect + optional sound + brief hit-stop = "impact".
// Tag your attack states "Attack" in the Animator so the slow-down matches the swing.
[RequireComponent(typeof(Animator))]
public class PlayerCombat : MonoBehaviour
{
    [Header("Hit area")]
    public float range = 3.0f;
    public float coneAngle = 120f;
    [Tooltip("How long the swing stays 'live' after Hit Moment — catches moving enemies")]
    public float hitWindow = 0.25f;

    [Header("Combo")]
    public int damage = 15;
    [Tooltip("Combo swing triggers, in order — must match the Trigger params + states in the Animator")]
    public string[] comboTriggers = { "attack1", "attack3" };
    [Tooltip("Minimum time between swings")]
    public float cooldown = 0.45f;
    [Tooltip("Wait longer than this between clicks and the combo restarts at the first swing")]
    public float comboResetTime = 1.2f;
    [Tooltip("Seconds into a swing before the hit window opens")]
    public float hitMoment = 0.25f;

    [Header("Running attack (Shift + click while moving)")]
    public bool useRunAttack = true;
    public string runAttackTrigger = "runAttack";
    public int runAttackDamage = 25;
    public float runHitMoment = 0.45f;
    public float runLeapSpeed = 7f;
    public float runLeapTime = 0.4f;
    public float runRange = 3.6f;

    [Header("Feel")]
    [Range(0f, 1f)] [Tooltip("Move speed kept during a normal swing (1 = full, 0 = frozen)")]
    public float attackMoveSlow = 0.4f;
    [Tooltip("Brief slow-motion when a hit lands — the 'impact' feel (0 = off)")]
    public float hitStopDuration = 0.07f;
    [Range(0f, 1f)] public float hitStopScale = 0.1f;

    [Header("Hit feedback (optional — drag your own assets)")]
    public AudioClip hitSound;
    [Range(0f, 1f)] public float hitVolume = 0.8f;
    [Tooltip("A slash/spark prefab spawned at the hit (e.g. from the Hovl effects pack)")]
    public GameObject hitEffect;
    public float hitEffectLife = 2f;

    private Animator animator;
    private StarterAssetsInputs input;
    private ThirdPersonController controller;
    private CharacterController cc;
    private float lastAttack = -999f;
    private int comboStep = 0;
    private bool leaping = false;

    void Start()
    {
        animator = GetComponent<Animator>();
        input = GetComponent<StarterAssetsInputs>();
        controller = GetComponent<ThirdPersonController>();
        cc = GetComponent<CharacterController>();
    }

    void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.currentHP <= 0)
        {
            if (controller != null) controller.combatSpeedMultiplier = 1f;
            return;
        }

        if (controller != null)
        {
            if (leaping) controller.combatSpeedMultiplier = 0f;
            else
            {
                bool swinging = (animator != null && animator.GetCurrentAnimatorStateInfo(0).IsTag("Attack")) || Time.time < lastAttack + cooldown;
                controller.combatSpeedMultiplier = swinging ? attackMoveSlow : 1f;
            }
        }

        if (Time.time - lastAttack > comboResetTime) comboStep = 0;

        // While aiming, the left click throws a rock (RockThrow) instead of swinging.
        bool clicked = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
        if (clicked && !PlayerAiming.IsAiming && Time.time - lastAttack >= cooldown)
            Swing();
    }

    void Swing()
    {
        lastAttack = Time.time;

        bool running = useRunAttack && input != null && input.sprint && input.move != Vector2.zero;

        if (running)
        {
            if (animator != null) animator.SetTrigger(runAttackTrigger);
            comboStep = 0;
            StartCoroutine(RunLeap());
            StartCoroutine(HitWindow(runHitMoment, runAttackDamage, runRange));
        }
        else if (comboTriggers.Length > 0)
        {
            comboStep %= comboTriggers.Length;
            if (animator != null) animator.SetTrigger(comboTriggers[comboStep]);
            comboStep = (comboStep + 1) % comboTriggers.Length;
            StartCoroutine(HitWindow(hitMoment, damage, range));
        }
    }

    // Opens a live window that keeps checking for enemies in front, so a moving
    // target is still hit. Each enemy is damaged at most once per swing.
    IEnumerator HitWindow(float delay, int dmg, float r)
    {
        yield return new WaitForSeconds(delay);

        HashSet<EnemyHealth> alreadyHit = new HashSet<EnemyHealth>();
        bool didHitStop = false;
        float t = 0f;

        while (t < hitWindow)
        {
            EnemyHealth[] enemies = Object.FindObjectsByType<EnemyHealth>();
            foreach (EnemyHealth e in enemies)
            {
                if (e == null || alreadyHit.Contains(e)) continue;

                Vector3 to = e.transform.position - transform.position;
                to.y = 0f;
                if (to.magnitude <= r && Vector3.Angle(transform.forward, to) <= coneAngle * 0.5f)
                {
                    e.TakeDamage(dmg);
                    alreadyHit.Add(e);

                    Vector3 point = e.transform.position + Vector3.up;
                    SpawnSlash(point);
                    if (hitSound != null) AudioSource.PlayClipAtPoint(hitSound, point, hitVolume);
                    if (!didHitStop && hitStopDuration > 0f) { didHitStop = true; StartCoroutine(HitStop()); }
                }
            }
            t += Time.deltaTime;
            yield return null;
        }
    }

    void SpawnSlash(Vector3 point)
    {
        if (hitEffect == null) return;
        GameObject fx = Instantiate(hitEffect, point, Quaternion.LookRotation(transform.forward));
        Destroy(fx, hitEffectLife);
    }

    // Drives the forward leap of the running attack (Root Motion is off, so we move it ourselves)
    IEnumerator RunLeap()
    {
        leaping = true;
        Vector3 dir = transform.forward; dir.y = 0f;
        if (dir.sqrMagnitude > 0.001f) dir.Normalize();

        float t = 0f;
        while (t < runLeapTime)
        {
            float speed = Mathf.Lerp(runLeapSpeed, 1f, t / runLeapTime);
            if (cc != null && cc.enabled) cc.Move(dir * speed * Time.deltaTime);
            t += Time.deltaTime;
            yield return null;
        }
        leaping = false;
    }

    IEnumerator HitStop()
    {
        // never hit-stop once the game is over (would un-freeze the win/lose screen)
        if (GameManager.Instance != null && (GameManager.Instance.currentHP <= 0)) yield break;

        Time.timeScale = hitStopScale;
        yield return new WaitForSecondsRealtime(hitStopDuration);
        if (Time.timeScale != 0f) Time.timeScale = 1f;   // don't override a real freeze (win/lose)
    }

    // safety net: if this object is disabled mid hit-stop, restore normal time
    void OnDisable()
    {
        if (Time.timeScale > 0f && Time.timeScale < 1f) Time.timeScale = 1f;
    }
}
