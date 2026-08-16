using UnityEngine;

// Armour lying on the trail. It soaks up damage before health does, and it carries
// across scenes with the rest of the player's progress.
public class ArmourPickup : WorldPickup
{
    [Header("Armour")]
    [Tooltip("How much armour this restores")]
    public int armourAmount = 25;

    [Tooltip("Don't pick up while the armour bar is already full (leaves it for later)")]
    public bool skipIfFull = true;

    protected override bool CanCollect()
    {
        if (GameManager.Instance == null) return false;
        if (skipIfFull && GameManager.Instance.currentArmour >= GameManager.Instance.maxArmour) return false;
        return true;
    }

    protected override void OnCollected()
    {
        GameManager.Instance.AddArmour(armourAmount);
    }
}
