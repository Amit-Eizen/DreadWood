using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using StarterAssets;

// Melee combat for the player (Brute).
//  - Left-click = a combo swing (cycles through Combo Triggers).
//  - Sprint (Shift) + left-click = a running jump attack that LEAPS forward
//    (driven in code, since Root Motion is off) and hits hard.
//  - While swinging the player slows down (responsive, barely slides).
//  - On a hit: brief hit-stop + optional sound + optional slash effect = "impact".
// Tag your attack states "Attack" in the Animator so the slow-down matches the swing.
[RequireComponent(typeof(Animator))]
public class PlayerCombat : MonoBehaviour
{
    [Header("Hit area")]
    public float range = 2.8f;
    public float coneAngle = 100f;

    [Header("Combo")]
    public int damage = 15;
    [Tooltip("Combo swing triggers, in order — must match the Trigger params + states in the Animator")]
    public string[] comboTriggers = { "attack1", "attack3" };
    [Tooltip("Minimum time between swings")]
    public float cooldown = 0.45f;
    [Tooltip("Wait longer than this between clicks and the combo restarts at the first swing")]
    public float comboResetTime = 1.2f;
    [Tooltip("Seconds into a swing before the hit lands")]
    public float hitMoment = 0.25f;

    [Header("Running attack (Shift + click while moving)")]
    public bool useRunAttack = true;
    public string runAttackTrigger = "runAttack";
    public int runAttackDamage = 25;
    [Tooltip("Seconds into the running attack before the hit lands")]
    public float runHitMoment = 0.45f;
    [Tooltip("How far/fast the leap pushes forward")]
    public float runLeapSpeed = 7f;
    [Tooltip("How long the forward leap lasts")]
    public float runLeapTime = 0.4f;
    [Tooltip("Bigger hit area for the leaping strike")]
    public float runRange = 3.4f;

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
    private int pendingDamage;
    private float pendingRange;
    private float swingUntil = 0f;
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

        // movement feel: the leap drives motion itself (freeze normal control);
        // a normal swing just slows you; otherwise full speed.
        if (controller != null)
        {
            if (leaping) controller.combatSpeedMultiplier = 0f;
            else
            {
                bool swinging = (animator != null && animator.GetCurrentAnimatorStateInfo(0).IsTag("Attack")) || Time.time < swingUntil;
                controller.combatSpeedMultiplier = swinging ? attackMoveSlow : 1f;
            }
        }

        if (Time.time - lastAttack > comboResetTime) comboStep = 0;

        bool clicked = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
        if (clicked && Time.time - lastAttack >= cooldown)
            Swing();
    }

    void Swing()
    {
        lastAttack = Time.time;
        swingUntil = Time.time + cooldown;

        bool running = useRunAttack && input != null && input.sprint && input.move != Vector2.zero;

        if (running)
        {
            if (animator != null) animator.SetTrigger(runAttackTrigger);
            pendingDamage = runAttackDamage;
            pendingRange = runRange;
            comboStep = 0;
            StartCoroutine(RunLeap());                 // propel forward like the animation
            Invoke(nameof(DealDamage), runHitMoment);
        }
        else if (comboTriggers.Length > 0)
        {
            comboStep %= comboTriggers.Length;
            if (animator != null) animator.SetTrigger(comboTriggers[comboStep]);
            pendingDamage = damage;
            pendingRange = range;
            comboStep = (comboStep + 1) % comboTriggers.Length;
            Invoke(nameof(DealDamage), hitMoment);
        }
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
            float speed = Mathf.Lerp(runLeapSpeed, 1f, t / runLeapTime);   // fast, then ease out
            if (cc != null && cc.enabled) cc.Move(dir * speed * Time.deltaTime);
            t += Time.deltaTime;
            yield return null;
        }
        leaping = false;
    }

    void DealDamage()
    {
        bool hitSomething = false;
        Vector3 hitPoint = transform.position + transform.forward * (pendingRange * 0.5f) + Vector3.up;

        EnemyHealth[] enemies = Object.FindObjectsByType<EnemyHealth>();
        foreach (EnemyHealth e in enemies)
        {
            Vector3 to = e.transform.position - transform.position;
            to.y = 0f;
            if (to.magnitude <= pendingRange && Vector3.Angle(transform.forward, to) <= coneAngle * 0.5f)
            {
                e.TakeDamage(pendingDamage);
                hitSomething = true;
                hitPoint = e.transform.position + Vector3.up;
            }
        }

        if (hitSomething) ImpactFeedback(hitPoint);
    }

    void ImpactFeedback(Vector3 point)
    {
        if (hitSound != null) AudioSource.PlayClipAtPoint(hitSound, point, hitVolume);

        if (hitEffect != null)
        {
            GameObject fx = Instantiate(hitEffect, point, Quaternion.LookRotation(transform.forward));
            Destroy(fx, hitEffectLife);
        }

        if (hitStopDuration > 0f) StartCoroutine(HitStop());
    }

    IEnumerator HitStop()
    {
        Time.timeScale = hitStopScale;
        yield return new WaitForSecondsRealtime(hitStopDuration);
        Time.timeScale = 1f;
    }
}
