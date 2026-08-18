using UnityEngine;

// A boulder that comes down the corridor at you. It sits parked until the player gets
// close, then gravity takes over and a shove sends it rolling. Everything after that is
// real physics — it bounces off walls, shoves props aside, and hurts if it reaches you.
[RequireComponent(typeof(Rigidbody))]
public class RollingBoulder : MonoBehaviour
{
    [Header("Release")]
    [Tooltip("Starts rolling once the player is this close")]
    public float releaseDistance = 20f;

    [Tooltip("Speed of the shove it gets, along this object's own blue arrow")]
    public float rollSpeed = 9f;

    [Tooltip("Removed this long after it starts rolling, so boulders don't pile up")]
    public float lifeAfterRelease = 25f;

    [Header("Hitting the player")]
    public int damage = 20;

    [Tooltip("Seconds before the same boulder can hurt you again, so a slow scrape along it isn't instant death")]
    public float damageCooldown = 1.5f;

    private Rigidbody body;
    private bool rolling = false;
    private float canHurtAgainAt = 0f;

    void Awake()
    {
        body = GetComponent<Rigidbody>();

        // Parked, not falling — a boulder waiting on a ledge should stay on it.
        body.isKinematic = true;

        // Heavy and fast: without continuous detection it can pass straight through a wall.
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }

    void Update()
    {
        if (rolling) return;

        Transform player = PlayerTeleport.Find();
        if (player == null) return;
        if (Vector3.Distance(transform.position, player.position) > releaseDistance) return;

        Release();
    }

    void Release()
    {
        rolling = true;
        body.isKinematic = false;
        body.linearVelocity = transform.forward * rollSpeed;
        Destroy(gameObject, lifeAfterRelease);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!rolling || Time.time < canHurtAgainAt) return;
        if (GameManager.Instance == null) return;
        if (!PlayerTeleport.IsPlayer(collision.collider)) return;

        canHurtAgainAt = Time.time + damageCooldown;
        GameManager.Instance.TakeDamage(damage);
    }

    // Show which way it will roll, so it can be aimed down the corridor in the Scene view.
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.6f, 0.2f);
        Gizmos.DrawLine(transform.position, transform.position + transform.forward * 5f);
        Gizmos.DrawWireSphere(transform.position, releaseDistance);
    }
}
