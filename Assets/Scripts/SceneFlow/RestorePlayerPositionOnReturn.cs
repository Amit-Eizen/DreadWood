using UnityEngine;

// Put this on any object in Scene 1 (the forest). When the player comes BACK from the arena,
// it puts them where they were before the fight, instead of at the cabin start.
public class RestorePlayerPositionOnReturn : MonoBehaviour
{
    void Start()
    {
        PlayerProgressBetweenScenes progress = PlayerProgressBetweenScenes.Instance;
        if (progress == null || !progress.hasForestReturnPosition) return;

        PlayerTeleport.MoveTo(progress.forestReturnPosition);
    }
}
