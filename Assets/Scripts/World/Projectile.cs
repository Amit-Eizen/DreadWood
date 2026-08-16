using UnityEngine;

// A thrown rock. It is a real physics object: a Rigidbody carries it, gravity pulls it
// down, and OnCollisionEnter is what makes it hit — no raycasts, no distance checks.
[RequireComponent(typeof(Rigidbody))]
public class Projectile : MonoBehaviour
{
    [Tooltip("Damage dealt to an enemy it hits")]
    public int damage = 10;

    [Tooltip("Which layers it can damage. Everything else it just bounces off.")]
    public LayerMask whatItCanDamage;

    [Tooltip("Destroyed this many seconds after landing, so rocks don't pile up forever")]
    public float lifeAfterLanding = 4f;

    [Tooltip("Optional effect spawned where it hits")]
    public GameObject hitEffect;
    public float hitEffectLife = 2f;

    private bool hasHit = false;

    void Awake()
    {
        // A small fast sphere passes straight through colliders with the default
        // Discrete mode — it would never hit a zombie at all.
        Rigidbody body = GetComponent<Rigidbody>();
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (hasHit) return;
        hasHit = true;

        if (hitEffect != null)
        {
            GameObject fx = Instantiate(hitEffect, collision.GetContact(0).point, Quaternion.identity);
            Destroy(fx, hitEffectLife);
        }

        // Only damage what the mask allows — a rock landing on the terrain must not
        // look for an enemy, and a rock hitting a tree must not damage it.
        // The layer is read from the object that OWNS the health, not from the collider
        // we touched, because that is often a child bone on a different layer.
        EnemyHealth enemy = collision.gameObject.GetComponentInParent<EnemyHealth>();
        if (enemy != null && (whatItCanDamage.value & (1 << enemy.gameObject.layer)) != 0)
            enemy.TakeDamage(damage);

        Destroy(gameObject, lifeAfterLanding);
    }
}
