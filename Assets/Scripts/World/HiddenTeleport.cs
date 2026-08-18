using UnityEngine;

// A doorway the player walks through without ever realising it moved them: step into the
// trigger and you come out at `destination`. Hide it in a tunnel mouth or a doorway during
// the chase and a short piece of level can stand in for a long run.
[RequireComponent(typeof(Collider))]
public class HiddenTeleport : MonoBehaviour
{
    [Tooltip("Where the player comes out. An empty GameObject is enough — point its blue arrow the way they should be facing.")]
    public Transform destination;

    [Tooltip("Also turn the player to face the destination's forward direction")]
    public bool turnThePlayer = true;

    [Tooltip("Seconds before ANY teleport can fire again")]
    public float cooldown = 1f;

    // Shared by every teleport in the scene, not one each: the exit often sits inside
    // another teleport's trigger, and a private timer would let them ping-pong.
    private static float readyAgainAt = 0f;

    void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (destination == null || Time.time < readyAgainAt) return;
        if (!PlayerTeleport.IsPlayer(other)) return;

        Transform player = PlayerTeleport.Find();
        if (player == null) return;

        readyAgainAt = Time.time + cooldown;
        PlayerTeleport.MoveTo(player, destination.position,
                              turnThePlayer ? destination.rotation : player.rotation);
    }

    // Draw the jump in the Scene view so it is obvious where an invisible trigger leads.
    void OnDrawGizmos()
    {
        if (destination == null) return;
        Gizmos.color = new Color(0.4f, 0.9f, 1f, 0.9f);
        Gizmos.DrawLine(transform.position, destination.position);
        Gizmos.DrawWireSphere(destination.position, 0.5f);
    }
}
