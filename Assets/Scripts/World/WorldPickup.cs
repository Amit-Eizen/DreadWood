using UnityEngine;
using StarterAssets;

// Shared behaviour for anything the player walks into and picks up: health, armour, rocks.
// It handles the trigger, the idle spin and bob, the sound and effect, and removing itself.
// A specific pickup only has to say what collecting it actually does.
[RequireComponent(typeof(Collider))]
public abstract class WorldPickup : MonoBehaviour
{
    [Header("Pickup")]
    [Tooltip("How close (world units) the player must get to collect it")]
    public float pickupRadius = 2.5f;

    [Header("Idle motion")]
    public float spinSpeed = 60f;      // degrees/sec (0 = no spin)
    public float bobHeight = 0.2f;     // how far it floats up/down (0 = static)
    public float bobSpeed = 1.5f;      // bob cycles/sec

    [Header("Feedback (optional)")]
    public AudioClip pickupSound;
    [Range(0f, 1f)] public float volume = 0.8f;
    public GameObject pickupEffect;
    public float effectLife = 2f;

    private Vector3 basePosition;
    private float bobPhase;
    private bool collected = false;

    protected virtual void Start()
    {
        basePosition = transform.position;

        Collider col = GetComponent<Collider>();
        col.isTrigger = true;

        // These prefabs are scaled down, which shrinks their collider with them. Divide the
        // scale back out so pickupRadius really is metres and the trigger actually fires.
        SphereCollider sphere = col as SphereCollider;
        if (sphere != null)
        {
            float scale = Mathf.Max(Mathf.Abs(transform.lossyScale.x), 0.0001f);
            sphere.radius = pickupRadius / scale;
        }
    }

    protected virtual void Update()
    {
        if (spinSpeed != 0f) transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);

        if (bobHeight != 0f)
        {
            bobPhase += Time.deltaTime * bobSpeed;
            transform.position = basePosition + Vector3.up * (Mathf.Sin(bobPhase * Mathf.PI * 2f) * bobHeight);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") || other.GetComponentInParent<ThirdPersonController>() != null)
            TryCollect();
    }

    void TryCollect()
    {
        if (collected || !CanCollect()) return;

        collected = true;
        OnCollected();

        if (pickupSound != null) AudioSource.PlayClipAtPoint(pickupSound, transform.position, volume);
        if (pickupEffect != null)
        {
            GameObject fx = Instantiate(pickupEffect, transform.position, Quaternion.identity);
            Destroy(fx, effectLife);
        }

        Destroy(gameObject);
    }

    // Say no to leave the pickup lying there — e.g. health when the player is already full.
    protected virtual bool CanCollect() => true;

    // What this pickup actually gives the player.
    protected abstract void OnCollected();
}
