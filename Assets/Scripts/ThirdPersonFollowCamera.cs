using UnityEngine;

// Simple third-person follow camera (no Cinemachine needed).
// The Starter Assets ThirdPersonController already rotates the player's
// "PlayerCameraRoot" child based on mouse look. This script just keeps the
// camera sitting behind that target and sharing its rotation, giving a
// standard orbit-style third-person camera.
//
// Setup: put this on the Main Camera and drag the player's PlayerCameraRoot
// child into the "Target" slot.
public class ThirdPersonFollowCamera : MonoBehaviour
{
    [Tooltip("Drag the player's PlayerCameraRoot child here")]
    public Transform target;

    [Tooltip("How far behind the target the camera sits")]
    public float distance = 4f;

    [Tooltip("Small vertical nudge so the camera isn't dead-level with the target")]
    public float heightOffset = 0.2f;

    void LateUpdate()
    {
        if (target == null) return;

        // Match the target's look rotation (mouse pitch/yaw is applied to it by the controller)
        transform.rotation = target.rotation;

        // Sit 'distance' units behind the target, slightly raised
        transform.position = target.position - transform.forward * distance + Vector3.up * heightOffset;
    }
}
