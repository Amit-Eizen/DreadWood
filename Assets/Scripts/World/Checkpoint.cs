using UnityEngine;

// A quiet save point — no message, no pause. Walk past it and this spot is remembered;
// die anywhere after it and GameManager puts you back here instead of ending the run.
[RequireComponent(typeof(Collider))]
public class Checkpoint : MonoBehaviour
{
    [Tooltip("Never revive with less health than this. Touching a checkpoint on your last " +
             "sliver of health would otherwise leave you dying on the spot forever.")]
    public int minimumReviveHealth = 40;

    [Tooltip("Shown after a revive, so it is clear what happened")]
    public string reviveObjective = "You woke up. RUN!";

    // The last checkpoint reached. It lives here as static state because the revive is done
    // by GameManager, which has no way of knowing which of the scene's checkpoints was last.
    public static bool Reached { get; private set; }
    public static Vector3 Position { get; private set; }
    public static int Health { get; private set; }
    public static int Armour { get; private set; }
    public static string Message { get; private set; }

    // Checkpoints belong to the scene that holds them. GameManager calls this on load so a
    // forest checkpoint can never revive the player in the middle of the chase.
    public static void Forget() => Reached = false;

    void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (GameManager.Instance == null || !PlayerTeleport.IsPlayer(other)) return;

        Reached = true;
        Position = transform.position;
        Health = Mathf.Max(minimumReviveHealth, GameManager.Instance.currentHP);
        Armour = GameManager.Instance.currentArmour;
        Message = reviveObjective;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.4f, 1f, 0.5f, 0.8f);
        Gizmos.DrawWireCube(transform.position, new Vector3(4f, 3f, 1f));
    }
}
