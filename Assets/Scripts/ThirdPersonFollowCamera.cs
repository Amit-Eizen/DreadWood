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

    [Tooltip("How close the camera may be pulled in when a wall is in the way")]
    public float minDistance = 0.6f;

    void LateUpdate()
    {
        if (target == null) return;

        // Match the target's look rotation (mouse pitch/yaw is applied to it by the controller)
        transform.rotation = target.rotation;

        Vector3 origin = target.position + Vector3.up * heightOffset;
        Vector3 desired = origin - transform.forward * distance;
        Vector3 dir = (desired - origin).normalized;

        // Pull the camera in if a wall/roof sits between it and the player, so it
        // never clips through the cabin or trees.
        float allowed = distance;
        RaycastHit[] hits = Physics.SphereCastAll(origin, 0.25f, dir, distance, ~0, QueryTriggerInteraction.Ignore);
        Transform playerRoot = target.root;
        foreach (RaycastHit h in hits)
        {
            if (h.collider.transform.IsChildOf(playerRoot)) continue; // ignore the player's own colliders
            if (h.distance < allowed) allowed = h.distance;
        }
        allowed = Mathf.Max(allowed - 0.1f, minDistance);

        transform.position = origin + dir * allowed;
    }
}
