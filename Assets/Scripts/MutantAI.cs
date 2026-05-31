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

    [Header("Chase / Attack")]
    public float moveSpeed = 2.5f;
    public float rotationSpeed = 6f;
    public float attackRange = 2f;
    public int attackDamage = 12;
    public float attackCooldown = 1.5f;
    public float damageDelay = 1.0f;
    public float attackHold = 1.3f;
    public float loseSightTime = 4f;

    [Header("Wander (idle)")]
    public float wanderRadius = 12f;       // how far it roams from its spawn
    public float wanderSpeed = 1f;         // slow zombie shuffle
    public Vector2 pauseRange = new Vector2(1.5f, 4f); // idle pauses between strolls

    private Animator animator;
    private float lastSeenTime = -999f;
    private float lastAttackTime = -999f;
    private float attackHoldUntil = 0f;
    private bool alerted = false;
    private Terrain ground;

    private Vector3 home;
    private Vector3 wanderTarget;
    private bool hasWanderTarget = false;
    private float pauseUntil = 0f;

    void Start()
    {
        animator = GetComponent<Animator>();
        ground = Terrain.activeTerrain;
        home = transform.position;

        if (player == null)
        {
            GameObject p = GameObject.FindWithTag("Player");
            if (p == null) p = GameObject.Find("PlayerArmature");
            if (p != null) player = p.transform;
        }
    }

    void Update()
    {
        if (player == null) return;
        if (GameManager.Instance != null && GameManager.Instance.currentHP <= 0)
        {
            SetGait(false, false);
            return;
        }

        bool canSee = CanSeePlayer();
        if (canSee) { alerted = true; lastSeenTime = Time.time; }
        else if (alerted && Time.time - lastSeenTime > loseSightTime) alerted = false;

        if (alerted) ChaseAndAttack();
        else Wander();

        SnapToGround();
    }

    bool CanSeePlayer()
    {
        Vector3 eye = transform.position + Vector3.up * eyeHeight;
        Vector3 toPlayer = (player.position + Vector3.up * 1f) - eye;
        float dist = toPlayer.magnitude;
        if (dist > detectionRange) return false;

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
        animator.SetBool("isWandering", wandering);
        animator.SetBool("isChasing", chasing);
    }

    void TryAttack()
    {
        if (Time.time - lastAttackTime < attackCooldown) return;
        lastAttackTime = Time.time;
        animator.SetTrigger("attack");
        attackHoldUntil = Time.time + attackHold;
        Invoke(nameof(ApplyAttackDamage), damageDelay);
    }

    void ApplyAttackDamage()
    {
        if (player == null || GameManager.Instance == null) return;
        if (GameManager.Instance.currentHP <= 0) return;
        if (Vector3.Distance(transform.position, player.position) <= attackRange + 1f)
            GameManager.Instance.TakeDamage(attackDamage);
    }

    void SnapToGround()
    {
        if (ground == null) return;
        Vector3 p = transform.position;
        p.y = ground.SampleHeight(p) + ground.transform.position.y;
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
