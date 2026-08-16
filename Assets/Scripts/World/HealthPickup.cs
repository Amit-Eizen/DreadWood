using UnityEngine;

// Health lying on the trail. Everything about walking into it, spinning, bobbing and
// removing itself lives in WorldPickup — this only says what collecting it gives you.
public class HealthPickup : WorldPickup
{
    [Header("Heal")]
    [Tooltip("How much HP this restores")]
    public int healAmount = 25;

    [Tooltip("Don't pick up if the player is already at full HP (leaves it for later)")]
    public bool skipIfFull = false;

    protected override bool CanCollect()
    {
        if (GameManager.Instance == null) return false;
        if (skipIfFull && GameManager.Instance.currentHP >= GameManager.Instance.maxHP) return false;
        return true;
    }

    protected override void OnCollected()
    {
        GameManager.Instance.Heal(healAmount);
    }
}
