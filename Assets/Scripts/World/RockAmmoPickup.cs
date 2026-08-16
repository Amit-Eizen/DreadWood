using UnityEngine;

// A small pile of rocks. Picking it up refills throwing ammo, which carries between scenes.
public class RockAmmoPickup : WorldPickup
{
    [Header("Rocks")]
    [Tooltip("How many rocks this adds")]
    public int rockAmount = 3;

    [Tooltip("Don't pick up while the player is already carrying the maximum")]
    public bool skipIfFull = true;

    protected override bool CanCollect()
    {
        PlayerProgressBetweenScenes progress = PlayerProgressBetweenScenes.Instance;
        if (progress == null) return false;
        if (skipIfFull && progress.rockAmmo >= progress.maxRockAmmo) return false;
        return true;
    }

    protected override void OnCollected()
    {
        PlayerProgressBetweenScenes.Instance.AddRocks(rockAmount);
    }
}
