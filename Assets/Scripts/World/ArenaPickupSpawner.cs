using System.Collections.Generic;
using UnityEngine;

// Keeps a few pickups on the arena floor. Each one comes back somewhere else a while after
// it is taken, a limited number of times — enough supply for a long fight, not enough to
// make the fight unloseable.
public class ArenaPickupSpawner : MonoBehaviour
{
    [System.Serializable]
    public class Supply
    {
        public GameObject prefab;

        [Tooltip("How many of these lie on the floor at once")]
        public int onTheFloor = 2;

        [Tooltip("How many times each one comes back after being taken")]
        public int refills = 2;

        [Tooltip("Seconds between being taken and reappearing")]
        public float refillDelay = 20f;
    }

    public Supply health = new Supply();
    public Supply armour = new Supply();

    [Header("Where they land")]
    [Tooltip("Pickups appear within this distance of this object")]
    public float spreadRadius = 15f;

    [Tooltip("Never put one this close to another — they have to be worth walking to")]
    public float minimumGap = 5f;

    public float dropHeight = 0.5f;

    [Tooltip("What counts as floor. Left empty they land at this object's height, which is fine on a flat arena.")]
    public LayerMask floorLayers;

    // One pickup's whole life: what is on the floor now, how many comebacks are left, and
    // when the next one is due.
    class Slot
    {
        public Supply supply;
        public GameObject onFloor;
        public int refillsLeft;
        public float dueAt;
    }

    private readonly List<Slot> slots = new List<Slot>();

    void Start()
    {
        Fill(health);
        Fill(armour);
    }

    void Fill(Supply supply)
    {
        if (supply.prefab == null) return;

        for (int i = 0; i < supply.onTheFloor; i++)
        {
            Slot slot = new Slot { supply = supply, refillsLeft = supply.refills };
            slots.Add(slot);
            Spawn(slot);
        }
    }

    void Update()
    {
        foreach (Slot slot in slots)
        {
            if (slot.onFloor != null) continue;

            // Gone. Start the clock the first frame we notice, then wait it out.
            if (slot.dueAt == 0f)
            {
                if (slot.refillsLeft <= 0) continue;
                slot.refillsLeft--;
                slot.dueAt = Time.time + slot.supply.refillDelay;
            }
            else if (Time.time >= slot.dueAt)
            {
                Spawn(slot);
            }
        }
    }

    void Spawn(Slot slot)
    {
        slot.dueAt = 0f;
        slot.onFloor = Instantiate(slot.supply.prefab, FreeSpot(), Quaternion.identity, transform);
    }

    // A random spot that is not on top of another pickup. After enough tries it takes the
    // last one anyway — a slightly crowded arena beats a missing pickup.
    Vector3 FreeSpot()
    {
        Vector3 spot = transform.position;

        for (int attempt = 0; attempt < 20; attempt++)
        {
            Vector2 circle = Random.insideUnitCircle * spreadRadius;
            spot = transform.position + new Vector3(circle.x, 0f, circle.y);

            if (Physics.Raycast(spot + Vector3.up * 20f, Vector3.down, out RaycastHit floor,
                                40f, floorLayers, QueryTriggerInteraction.Ignore))
                spot = floor.point;

            spot += Vector3.up * dropHeight;
            if (NothingElseNearby(spot)) break;
        }

        return spot;
    }

    bool NothingElseNearby(Vector3 spot)
    {
        foreach (Slot slot in slots)
            if (slot.onFloor != null && Vector3.Distance(slot.onFloor.transform.position, spot) < minimumGap)
                return false;

        return true;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.4f, 1f, 0.6f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, spreadRadius);
    }
}
