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

    [Tooltip("Off: left click throws on its own, no aiming first. For the chase, where " +
             "stopping to aim costs more ground than you have.")]
    public bool requiresAiming = true;

    [Tooltip("How far ahead to look for what you are aiming at")]
    public float aimRange = 60f;

    [Tooltip("Draw a dot in the middle of the screen while you can throw")]
    public bool showCrosshair = true;

    private float lastThrow = -999f;
    private PlayerAiming aiming;

    void Awake()
    {
        aiming = GetComponent<PlayerAiming>();
    }

    void Update()
    {
        if (requiresAiming && !PlayerAiming.IsAiming) return;
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
        if (body != null) body.linearVelocity = AimDirection() * throwSpeed;

        // Stop the rock colliding with the player who just threw it.
        Collider rockCollider = rock.GetComponent<Collider>();
        Collider playerCollider = GetComponent<Collider>();
        if (rockCollider != null && playerCollider != null)
            Physics.IgnoreCollision(rockCollider, playerCollider);
    }

    // Throw at whatever sits under the middle of the screen. The hand is off to the side of
    // the third-person camera, so throwing straight out of it misses what you were looking at.
    Vector3 AimDirection()
    {
        Camera view = aiming != null ? aiming.ActiveCamera : Camera.main;
        if (view == null) return (throwPoint.forward + Vector3.up * upwardArc).normalized;

        Ray screenCentre = view.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        Vector3 target = Physics.Raycast(screenCentre, out RaycastHit hit, aimRange,
                                         ~0, QueryTriggerInteraction.Ignore)
            ? hit.point
            : screenCentre.GetPoint(aimRange);

        Vector3 toTarget = (target - throwPoint.position).normalized;
        return (toTarget + Vector3.up * upwardArc).normalized;
    }

    // Only up when there is actually something to throw — the ROCKS counter already says
    // when there is not, and an aiming dot you cannot use is just clutter.
    void OnGUI()
    {
        if (!showCrosshair || rockPrefab == null || throwPoint == null) return;
        if (requiresAiming && !PlayerAiming.IsAiming) return;

        PlayerProgressBetweenScenes progress = PlayerProgressBetweenScenes.Instance;
        if (progress != null && progress.rockAmmo <= 0) return;

        const float size = 6f;
        Color previous = GUI.color;
        GUI.color = new Color(1f, 1f, 1f, 0.85f);
        GUI.DrawTexture(new Rect(Screen.width / 2f - size / 2f, Screen.height / 2f - size / 2f,
                                 size, size), Texture2D.whiteTexture);
        GUI.color = previous;
    }
}
