using UnityEngine;
using StarterAssets;

// A health pickup that sits on the trail. It can bob and spin so it's easy to
// spot; when the player gets close it heals them and disappears (with an optional
// effect + sound).
//
// The player is found by its ThirdPersonController component (only the player has
// one) — tag-independent. Pickup is by DISTANCE each frame (robust, no trigger
// physics needed); OnTriggerEnter is kept as a bonus.
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
    private Transform player;
    private bool collected = false;

    void Start()
    {
        basePos = transform.position;

        // if there's a collider, make it a trigger (the bonus OnTriggerEnter path)
        Collider col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;

        player = FindPlayer();
    }

    static Transform FindPlayer()
    {
        // the player is whoever has the ThirdPersonController (tag-independent)
        ThirdPersonController tpc = Object.FindFirstObjectByType<ThirdPersonController>();
        if (tpc != null) return tpc.transform;
        GameObject p = GameObject.FindWithTag("Player");
        return p != null ? p.transform : null;
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

        // distance-based pickup (robust — ignores Y so height doesn't matter)
        if (player == null) player = FindPlayer();
        if (player != null)
        {
            Vector3 a = transform.position; a.y = 0f;
            Vector3 b = player.position; b.y = 0f;
            if (Vector3.Distance(a, b) <= pickupRadius) Collect();
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