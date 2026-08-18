using UnityEngine;

// Stealth-aware enemy AI for zombies / the mutant.
// - Idle: WANDERS slowly around its spawn area (shuffles to random points).
// - Sees the player (within range + vision cone + line of sight) -> chases & attacks.
// - Loses sight long enough -> goes back to wandering.
// Animator params (driven here):
//   "isWandering" (bool) = slow WALK while roaming
//   "isChasing"   (bool) = RUN while chasing the player
//   "attack" (trigger), "die" (trigger)
[RequireComponent(typeof(Animator))]
public class MutantAI : MonoBehaviour
{
    [Header("Target")]
    public Transform player;

    [Header("Vision (stealth)")]
    public float detectionRange = 16f;
    public float visionAngle = 90f;
    public float eyeHeight = 1.6f;
    public LayerMask sightBlockers = ~0;
    [Tooltip("Detection range is multiplied by this while the player sneaks (Ctrl)")]
    public float stealthDetectionMultiplier = 0.35f;

    [Header("Chase / Attack")]
    public float moveSpeed = 2.5f;
    public float rotationSpeed = 6f;
    public float attackRange = 2f;
    public int attackDamage = 12;
    [Tooltip("Hits land this much harder while the player still has armour, so armour changes the fight instead of just stretching it out")]
    public float damageMultiplierVsArmour = 1.6f;
    public float attackCooldown = 1.5f;
    public float damageDelay = 1.0f;
    public float attackHold = 1.3f;
    public float loseSightTime = 4f;

    [Header("Wander (idle)")]
    public float wanderRadius = 12f;       // how far it roams from its spawn
    public float wanderSpeed = 1f;         // slow zombie shuffle
    public Vector2 pauseRange = new Vector2(1.5f, 4f); // idle pauses between strolls

    [Header("Footing")]
    [Tooltip("What counts as floor in scenes with no Terrain. Keep this to Ground and " +
             "Obstacle only — on Everything the ray finds the enemy's own body.")]
    public LayerMask groundLayers = 0;

    [Header("Boss mode")]
    [Tooltip("Always aggressive: never wanders — chases & attacks the player the moment it's active. Use for the boss mutant.")]
    public bool alwaysAggressive = false;

    private Animator animator;
    private float lastSeenTime = -999f;
    private float lastAttackTime = -999f;
    private float attackHoldUntil = 0f;
    private bool alerted = false;
    private Terrain ground;

    private float knockedOutUntil = 0f;
    private Vector3 home;
    private Vector3 wanderTarget;
    private bool hasWanderTarget = false;
    private float pauseUntil = 0f;

    // which animator params actually exist (the boss's MutantController lacks some) —
    // checked so we never spam "parameter does not exist" warnings
    private bool hasWander, hasChasing, hasAttack, hasDie;

    // Sensible default the moment the component is added — on Everything the footing ray
    // would find the enemy's own body and it would climb itself.
    void Reset()
    {
        groundLayers = LayerMask.GetMask("Ground", "Obstacle");
    }

    void Start()
    {
        animator = GetComponent<Animator>();
        ground = Terrain.activeTerrain;
        home = transform.position;

        foreach (AnimatorControllerParameter p in animator.parameters)
        {
            if (p.name == "isWandering") hasWander = true;
            else if (p.name == "isChasing") hasChasing = true;
            else if (p.name == "attack") hasAttack = true;
            else if (p.name == "die") hasDie = true;
        }

        if (player == null) player = PlayerTeleport.Find();
    }

    void Update()
    {
        if (player == null) return;
        if (GameManager.Instance != null && GameManager.Instance.currentHP <= 0)
        {
            SetGait(false, false);
            return;
        }

        if (IsKnockedOut)
        {
            SetGait(false, false);
            SnapToGround();
            return;
        }

        if (alwaysAggressive)
        {
            // boss: skip stealth/vision entirely — straight to the fight
            alerted = true;
        }
        else
        {
            bool canSee = CanSeePlayer();
            if (canSee) { alerted = true; lastSeenTime = Time.time; }
            else if (alerted && Time.time - lastSeenTime > loseSightTime) alerted = false;
        }

        if (alerted) ChaseAndAttack();
        else Wander();

        KeepApartFromOtherEnemies();
        SnapToGround();
    }

    public bool IsKnockedOut => Time.time < knockedOutUntil;

    // Floored for a few seconds: no chasing, no attacking. Pending attacks are cancelled,
    // because an Invoke already scheduled still lands while the enemy is meant to be down.
    public void KnockOut(float seconds)
    {
        CancelInvoke(nameof(ApplyAttackDamage));
        knockedOutUntil = Time.time + seconds;
        attackHoldUntil = 0f;
    }

    bool CanSeePlayer()
    {
        Vector3 eye = transform.position + Vector3.up * eyeHeight;
        Vector3 toPlayer = (player.position + Vector3.up * 1f) - eye;
        float dist = toPlayer.magnitude;

        // sneaking (Ctrl) shrinks how far the enemy can notice the player
        float effRange = PlayerStealth.IsStealthed ? detectionRange * stealthDetectionMultiplier : detectionRange;
        if (dist > effRange) return false;

        float angle = Vector3.Angle(transform.forward, new Vector3(toPlayer.x, 0f, toPlayer.z));
        if (angle > visionAngle * 0.5f) return false;

        RaycastHit[] hits = Physics.RaycastAll(eye, toPlayer.normalized, dist, sightBlockers, QueryTriggerInteraction.Ignore);
        foreach (RaycastHit hit in hits)
        {
            Transform t = hit.collider.transform;
            if (t == transform || t.IsChildOf(transform)) continue;
            if (t == player || t.IsChildOf(player)) continue;
            return false;
        }
        return true;
    }

    void ChaseAndAttack()
    {
        float distance = Vector3.Distance(transform.position, player.position);

        if (Time.time < attackHoldUntil)
        {
            SetGait(false, false);
            FaceDirection(player.position - transform.position);
            return;
        }

        if (distance <= attackRange)
        {
            SetGait(false, false);
            FaceDirection(player.position - transform.position);
            TryAttack();
        }
        else
        {
            SetGait(false, true);   // RUN at the player
            FaceDirection(player.position - transform.position);
            Vector3 dir = player.position - transform.position; dir.y = 0f;
            transform.position += dir.normalized * moveSpeed * Time.deltaTime;
        }
    }

    void Wander()
    {
        // standing-idle pause between strolls
        if (Time.time < pauseUntil) { SetGait(false, false); return; }

        Vector3 flatPos = new Vector3(transform.position.x, 0f, transform.position.z);
        Vector3 flatTarget = new Vector3(wanderTarget.x, 0f, wanderTarget.z);

        if (!hasWanderTarget || Vector3.Distance(flatPos, flatTarget) < 1f)
        {
            Vector2 r = Random.insideUnitCircle * wanderRadius;
            wanderTarget = home + new Vector3(r.x, 0f, r.y);
            hasWanderTarget = true;

            // sometimes just stand and look around for a bit
            if (Random.value < 0.4f)
            {
                pauseUntil = Time.time + Random.Range(pauseRange.x, pauseRange.y);
                SetGait(false, false);
                return;
            }
        }

        Vector3 dir = wanderTarget - transform.position; dir.y = 0f;
        SetGait(true, false);   // WALK while roaming
        FaceDirection(dir);
        transform.position += dir.normalized * wanderSpeed * Time.deltaTime;
    }

    void FaceDirection(Vector3 look)
    {
        look.y = 0f;
        if (look.sqrMagnitude < 0.001f) return;
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(look), rotationSpeed * Time.deltaTime);
    }

    // wandering = slow WALK, chasing = RUN; both false = Idle
    void SetGait(bool wandering, bool chasing)
    {
        if (hasWander) animator.SetBool("isWandering", wandering);
        if (hasChasing) animator.SetBool("isChasing", chasing);
    }

    void TryAttack()
    {
        if (Time.time - lastAttackTime < attackCooldown) return;
        lastAttackTime = Time.time;
        if (hasAttack) animator.SetTrigger("attack");
        attackHoldUntil = Time.time + attackHold;
        Invoke(nameof(ApplyAttackDamage), damageDelay);
    }

    void ApplyAttackDamage()
    {
        if (player == null || GameManager.Instance == null) return;
        if (GameManager.Instance.currentHP <= 0) return;
        if (Vector3.Distance(transform.position, player.position) > attackRange + 1f) return;

        // Armour absorbs damage, so without this a fight against an armoured player would
        // simply take twice as long. Hitting harder keeps the pace and makes armour a
        // trade — you survive longer, but each blow costs more of it.
        int damage = GameManager.Instance.HasArmour
            ? Mathf.RoundToInt(attackDamage * damageMultiplierVsArmour)
            : attackDamage;
        GameManager.Instance.TakeDamage(damage);
    }

    // Enemies all run at the same point, so they end up standing inside each other.
    void KeepApartFromOtherEnemies()
    {
        foreach (MutantAI other in FindObjectsByType<MutantAI>())
        {
            if (other == this) continue;

            Vector3 away = transform.position - other.transform.position;
            away.y = 0f;
            float gap = away.magnitude;
            if (gap > 1.6f || gap < 0.01f) continue;   // 1.6 = a bit wider than the 0.8 body

            transform.position += away.normalized * 2f * Time.deltaTime;
        }
    }

    // The forest has a Terrain to read heights from. The chase corridor is built from boxes
    // and has none, so there we look for the floor with a ray straight down instead.
    void SnapToGround()
    {
        Vector3 p = transform.position;

        if (ground != null)
        {
            p.y = ground.SampleHeight(p) + ground.transform.position.y;
        }
        else if (Physics.Raycast(p + Vector3.up * 3f, Vector3.down, out RaycastHit floor,
                                 12f, groundLayers, QueryTriggerInteraction.Ignore))
        {
            p.y = floor.point.y;
        }
        else return;

        transform.position = p;
    }

    void OnDrawGizmosSelected()
    {
        Vector3 eye = transform.position + Vector3.up * eyeHeight;
        Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.9f);
        Vector3 left = Quaternion.Euler(0f, -visionAngle * 0.5f, 0f) * transform.forward;
        Vector3 right = Quaternion.Euler(0f, visionAngle * 0.5f, 0f) * transform.forward;
        Gizmos.DrawLine(eye, eye + left * detectionRange);
        Gizmos.DrawLine(eye, eye + right * detectionRange);
        Gizmos.DrawLine(eye + left * detectionRange, eye + right * detectionRange);
    }
}
