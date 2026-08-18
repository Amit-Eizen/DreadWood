using UnityEngine;
using StarterAssets;

// The player's control scripts, listed once. The pause menu and the tutorial card both
// switch them all off.
public static class PlayerControls
{
    public static MonoBehaviour[] FindAll()
    {
        Transform player = PlayerTeleport.Find();
        if (player == null) return new MonoBehaviour[0];

        GameObject p = player.gameObject;
        return new MonoBehaviour[]
        {
            p.GetComponent<ThirdPersonController>(),
            p.GetComponent<StarterAssetsInputs>(),
            p.GetComponent<PlayerCombat>(),
            p.GetComponent<PlayerDodge>(),
            p.GetComponent<PlayerStealth>(),
            p.GetComponent<PlayerAiming>(),
            p.GetComponent<RockThrow>(),
        };
    }

    public static void SetEnabled(MonoBehaviour[] scripts, bool enabled)
    {
        if (scripts == null) return;

        foreach (MonoBehaviour script in scripts)
            if (script != null) script.enabled = enabled;
    }
}
