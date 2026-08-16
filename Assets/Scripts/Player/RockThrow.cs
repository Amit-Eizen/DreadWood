using UnityEngine;
using UnityEngine.InputSystem;

// Throws a rock while aiming. Left click swings the axe normally; hold right mouse to
// aim (PlayerAiming) and the same click throws instead, along the first-person camera's
// forward direction so the rock goes exactly where you are looking.
public class RockThrow : MonoBehaviour
{
    [Tooltip("The rock prefab — needs a Rigidbody and the Projectile script")]
    public GameObject rockPrefab;

    [Tooltip("Where the rock leaves your hand. Usually an empty child of the first-person camera.")]
    public Transform throwPoint;

    [Tooltip("How fast it leaves your hand, in metres per second")]
    public float throwSpeed = 22f;

    [Tooltip("Tilts the throw upwards a little so it arcs instead of going flat. 0 = dead straight.")]
    public float upwardArc = 0.15f;

    [Tooltip("Seconds between throws")]
    public float cooldown = 0.6f;

    private float lastThrow = -999f;

    void Update()
    {
        if (!PlayerAiming.IsAiming) return;
        if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame) return;
        if (Time.time - lastThrow < cooldown) return;
        if (rockPrefab == null || throwPoint == null) return;

        // Rocks are a resource that carries between scenes, so an empty pocket means
        // no throw. Picking up a rock pile refills it.
        PlayerProgressBetweenScenes progress = PlayerProgressBetweenScenes.Instance;
        if (progress != null && !progress.TryUseRock()) return;

        lastThrow = Time.time;
        Throw();
    }

    void Throw()
    {
        GameObject rock = Instantiate(rockPrefab, throwPoint.position, throwPoint.rotation);

        // Setting the velocity directly, rather than AddForce, keeps the throw the same
        // speed whatever the rock's mass is — the arc then comes from gravity alone.
        Rigidbody body = rock.GetComponent<Rigidbody>();
        if (body != null)
        {
            Vector3 direction = (throwPoint.forward + Vector3.up * upwardArc).normalized;
            body.linearVelocity = direction * throwSpeed;
        }

        // Stop the rock colliding with the player who just threw it.
        Collider rockCollider = rock.GetComponent<Collider>();
        Collider playerCollider = GetComponent<Collider>();
        if (rockCollider != null && playerCollider != null)
            Physics.IgnoreCollision(rockCollider, playerCollider);
    }
}
