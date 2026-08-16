using UnityEngine;

// This script keeps that camera above the player, so the minimap always shows the area
// around them. The camera keeps its own height and never rotates, so north stays up.
public class MinimapCameraFollow : MonoBehaviour
{
    [Tooltip("The player to follow. Left empty, it finds the player by tag on Start.")]
    public Transform player;

    void Start()
    {
        // Find the player once here, not every frame in Update.
        if (player == null)
        {
            GameObject playerObject = GameObject.FindWithTag("Player");
            if (playerObject != null)
            {
                player = playerObject.transform;
            }
        }
    }

    // LateUpdate runs after the player has moved this frame, so the map doesn't lag behind.
    void LateUpdate()
    {
        if (player == null) return;

        Vector3 newPosition = player.position;
        newPosition.y = transform.position.y;   // keep the camera's own height
        transform.position = newPosition;
    }
}
