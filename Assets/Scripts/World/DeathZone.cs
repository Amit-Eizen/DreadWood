using UnityEngine;

// The empty space under the level. Miss a jump, land in here, and you wake up at the last
// checkpoint.
[RequireComponent(typeof(Collider))]
public class DeathZone : MonoBehaviour
{
    void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (GameManager.Instance == null || !PlayerTeleport.IsPlayer(other)) return;

        // Enough to get through any armour the player is wearing — a fall is a fall.
        GameManager.Instance.TakeDamage(9999);
    }
}
