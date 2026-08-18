using UnityEngine;
using UnityEngine.AI;

// Stealth-aware enemy AI for zombies / the mutant.
// - Idle: PATROLS between its waypoints, waiting a beat at each one.
// - Sees the player (within range + vision cone + line of sight) -> chases & attacks.
// - Loses sight long enough -> goes back to patrolling.
// With a NavMeshAgent it walks around walls. Without one it moves in a straight line,
// which is all a scene with nothing in the way needs.
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

    [Header("Patrol (idle)")]
    [Tooltip("Walks between these in order. Leave empty and it roams near where it started.")]
    public Transform[] patrolPoints;

    [Tooltip("Seconds it stands and looks around at each point")]
    public float waitSecondsAtEachPoint = 2.5f;

    public float wanderRadius = 12f;       // how far it roams when it has no patrol points
    public float wanderSpeed = 1f;         // slow zombie shuffle
    public Vector2 pauseRange = new Vector2(1.5f, 4f); // idle pauses between strolls

    [Header("Knocked out")]
    [Tooltip("How far it tips over while floored. 0 leaves it standing.")]
    public float knockedOverAngle = 78f;

    [Tooltip("How fast it goes down and gets back up")]
    public float fallOverSpeed = 2.5f;

    [Tooltip("Lifts the body while it is lying down. Tipping it over swings part of the model " +
             "below the floor, and nothing here uses physics to stop that.")]
    public float knockedOverLift = 0.5f;

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

    private NavMeshAgent agent;
    private bool agentPausedForKnockOut = false;
    private float knockedOutUntil = 0f;
    private Vector3 home;
    private Vector3 wanderTarget;
    private bool hasWanderTarget = false;
    private int patrolIndex = 0;
    private float pauseUntil = 0f;

    // True when a baked NavMesh is doing the walking, so we must not also shove the
    // transform around ourselves — the two fight each other and the enemy jitters.
    bool AgentDriving => agent != null && agent.enabled && agent.isOnNavMesh;

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

        // We turn the body ourselves so it can face the player while backing off or
        // standing still — the agent's own turning would fight that.
        agent = GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            agent.updateRotation = false;

            // Left on, this stands the body back upright every frame and it can never lie down.
            agent.updateUpAxis = false;

            agent.stoppingDistance = attackRange * 0.8f;

            // Standing off the baked mesh, an agent still overwrites the transform every
            // frame with a position that never changes — so it freezes on the spot.
            if (!agent.isOnNavMesh)
            {
                Debug.LogWarning("[MutantAI] " + name + " is not standing on a baked NavMesh. " +
                                 "Moving it in a straight line instead — bake the scene, or " +
                                 "drop it onto the blue area.", this);
                agent.enabled = false;
            }
        }

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
            StopMoving();
            TipOver(true);
            SnapToGround();
            return;
        }
        TipOver(false);

        // Back on its feet — hand the steering back. Re-enabling drops it onto the nearest
        // point of the mesh, which is where it is already standing.
        if (agentPausedForKnockOut)
        {
            agentPausedForKnockOut = false;
            if (agent != null) agent.enabled = true;
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
        else Patrol();

        // Shoving the transform around fights the agent for control of it, and the agent
        // already keeps its own distance from the others.
        if (!AgentDriving) KeepApartFromOtherEnemies();
        SnapToGround();
    }

    public bool IsKnockedOut => Time.time < knockedOutUntil;

    // The animator has no floored state, so the body is simply tipped over and stood back up.
    void TipOver(bool down)
    {
        float tilt = Mathf.LerpAngle(transform.eulerAngles.x, down ? knockedOverAngle : 0f,
                                     Time.deltaTime * fallOverSpeed);
        transform.rotation = Quaternion.Euler(tilt, transform.eulerAngles.y, 0f);
    }

    // Floored for a few seconds: no chasing, no attacking. Pending attacks are cancelled,
    // because an Invoke already scheduled still lands while the enemy is meant to be down.
    public void KnockOut(float seconds)
    {
        CancelInvoke(nameof(ApplyAttackDamage));
        knockedOutUntil = Time.time + seconds;
        attackHoldUntil = 0f;

        // The agent keeps steering and standing the body upright. Switching it off leaves the
        // body lying exactly where it fell.
        if (agent != null && agent.enabled)
        {
            agent.enabled = false;
            agentPausedForKnockOut = true;
        }
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
            StopMoving();
            FaceDirection(player.position - transform.position);
            return;
        }

        FaceDirection(player.position - transform.position);

        if (distance <= attackRange)
        {
            SetGait(false, false);
            StopMoving();
            TryAttack();
        }
        else
        {
            SetGait(false, true);   // RUN at the player
            MoveTowards(player.position, moveSpeed);
        }
    }

    // Walks its beat: on to the next point, stand there a moment, on to the one after.
    // With no points set it falls back to drifting around wherever it started.
    void Patrol()
    {
        if (Time.time < pauseUntil) { SetGait(false, false); StopMoving(); return; }

        Vector3 target = NextPatrolTarget();
        Vector3 flatGap = target - transform.position;
        flatGap.y = 0f;

        if (flatGap.magnitude < 1.5f)
        {
            ArriveAtPatrolPoint();
            return;
        }

        SetGait(true, false);   // WALK while patrolling
        FaceDirection(flatGap);
        MoveTowards(target, wanderSpeed);
    }

    Vector3 NextPatrolTarget()
    {
        if (patrolPoints != null && patrolPoints.Length > 0)
        {
            Transform point = patrolPoints[patrolIndex % patrolPoints.Length];
            if (point != null) return point.position;
        }

        if (!hasWanderTarget)
        {
            Vector2 circle = Random.insideUnitCircle * wanderRadius;
            wanderTarget = home + new Vector3(circle.x, 0f, circle.y);
            hasWanderTarget = true;
        }
        return wanderTarget;
    }

    void ArriveAtPatrolPoint()
    {
        SetGait(false, false);
        StopMoving();

        if (patrolPoints != null && patrolPoints.Length > 0)
        {
            patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
            pauseUntil = Time.time + waitSecondsAtEachPoint;
            return;
        }

        // No patrol route: pick a fresh spot, and sometimes just stand and look around.
        hasWanderTarget = false;
        if (Random.value < 0.4f) pauseUntil = Time.time + Random.Range(pauseRange.x, pauseRange.y);
    }

    void MoveTowards(Vector3 target, float speed)
    {
        if (AgentDriving)
        {
            agent.speed = speed;
            agent.isStopped = false;
            agent.SetDestination(target);
            return;
        }

        Vector3 direction = target - transform.position;
        direction.y = 0f;
        transform.position += direction.normalized * speed * Time.deltaTime;
    }

    void StopMoving()
    {
        if (AgentDriving) agent.isStopped = true;
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
        // With an agent the NavMesh decides the height, so the lift goes through its own
        // offset. That offset is already in the model's own scale — no second multiply.
        if (AgentDriving)
        {
            agent.baseOffset = IsKnockedOut ? knockedOverLift : 0f;
            return;
        }

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

        // Multiplied by the model's own size: the boss is scaled up 2.6x, and so is the
        // slab of body that would otherwise end up under the floor.
        if (IsKnockedOut) p.y += knockedOverLift * transform.lossyScale.y;
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

        if (patrolPoints == null || patrolPoints.Length == 0) return;

        // The route as a closed loop, so it is obvious at a glance where this one walks.
        Gizmos.color = new Color(0.4f, 1f, 0.5f, 0.9f);
        for (int i = 0; i < patrolPoints.Length; i++)
        {
            Transform point = patrolPoints[i];
            Transform next = patrolPoints[(i + 1) % patrolPoints.Length];
            if (point == null) continue;

            Gizmos.DrawWireSphere(point.position, 0.6f);
            if (next != null) Gizmos.DrawLine(point.position, next.position);
        }
    }
}
