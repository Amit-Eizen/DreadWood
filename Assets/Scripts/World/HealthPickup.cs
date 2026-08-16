using UnityEngine;
using StarterAssets;

// A health pickup that sits on the trail. It can bob and spin so it's easy to
// spot; when the player gets close it heals them and disappears (with an optional
// effect + sound).
//
// Pickup happens in OnTriggerEnter. The trigger is sized in Start from pickupRadius:
// the prefab is scaled down, which shrinks its collider with it, so the radius set in
// the inspector is not the radius you get in the world.
public class HealthPickup : MonoBehaviour
{
    [Header("Heal")]
    [Tooltip("How much HP this restores")]
    public int healAmount = 25;
    [Tooltip("Don't pick up if the player is already at full HP (leaves it for later)")]
    public bool skipIfFull = false;
    [Tooltip("How close (world units) the player must get to collect it")]
    public float pickupRadius = 2.5f;

    [Header("Idle motion")]
    public float spinSpeed = 90f;        // degrees/sec (0 = no spin)
    public float bobHeight = 0.25f;      // how far it floats up/down (0 = static)
    public float bobSpeed = 2f;          // bob cycles/sec

    [Header("Pickup feedback (optional)")]
    public AudioClip pickupSound;
    [Range(0f, 1f)] public float volume = 0.8f;
    [Tooltip("An effect spawned where it was collected (e.g. a green Hovl effect)")]
    public GameObject pickupEffect;
    public float effectLife = 2f;

    private Vector3 basePos;
    private float bobPhase;
    private bool collected = false;

    void Start()
    {
        basePos = transform.position;

        Collider col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;

        // The prefab sits at scale 0.2, which shrinks its collider to a fifth of the
        // radius shown in the inspector. Divide it back out so pickupRadius really is
        // metres in the world and the trigger actually fires.
        SphereCollider sphere = col as SphereCollider;
        if (sphere != null)
        {
            float scale = Mathf.Max(Mathf.Abs(transform.lossyScale.x), 0.0001f);
            sphere.radius = pickupRadius / scale;
        }
    }

    void Update()
    {
        // spin
        if (spinSpeed != 0f) transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);

        // bob up and down around the spawn point
        if (bobHeight != 0f)
        {
            bobPhase += Time.deltaTime * bobSpeed;
            float y = Mathf.Sin(bobPhase * Mathf.PI * 2f) * bobHeight;
            transform.position = basePos + Vector3.up * y;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") || other.gameObject.name == "PlayerArmature"
            || other.GetComponentInParent<ThirdPersonController>() != null)
            Collect();
    }

    void Collect()
    {
        if (collected || GameManager.Instance == null) return;

        // optionally leave it if the player is already full
        if (skipIfFull && GameManager.Instance.currentHP >= GameManager.Instance.maxHP) return;

        collected = true;
        GameManager.Instance.Heal(healAmount);

        if (pickupSound != null) AudioSource.PlayClipAtPoint(pickupSound, transform.position, volume);
        if (pickupEffect != null)
        {
            GameObject fx = Instantiate(pickupEffect, transform.position, Quaternion.identity);
            Destroy(fx, effectLife);
        }

        Destroy(gameObject);
    }
}