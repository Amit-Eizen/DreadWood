using UnityEngine;

// Put this on any object in Scene 1 (the forest). When the player comes BACK from the arena,
// it puts them where they were before the fight, instead of at the cabin start.
public class RestorePlayerPositionOnReturn : MonoBehaviour
{
    void Start()
    {
        PlayerProgressBetweenScenes progress = PlayerProgressBetweenScenes.Instance;
        if (progress == null || !progress.hasForestReturnPosition) return;

        GameObject p = GameObject.FindWithTag("Player");
        if (p == null) p = GameObject.Find("PlayerArmature");
        if (p == null) return;

        // Move the player, then tell the physics engine straight away.
        // Without SyncTransforms the CharacterController still thinks it is at the old spot.
        p.transform.position = progress.forestReturnPosition;
        Physics.SyncTransforms();
    }
}
