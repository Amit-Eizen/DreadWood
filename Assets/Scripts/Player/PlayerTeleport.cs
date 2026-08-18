using UnityEngine;
using StarterAssets;

// Finding the player and moving them are worth one home. The CharacterController keeps its
// own copy of the position and writes it back every frame, so it has to be switched off
// around a move — otherwise the player snaps straight back to where they were.
public static class PlayerTeleport
{
    public static Transform Find()
    {
        GameObject tagged = GameObject.FindWithTag("Player");
        if (tagged != null) return tagged.transform;

        ThirdPersonController controller = Object.FindFirstObjectByType<ThirdPersonController>();
        return controller != null ? controller.transform : null;
    }

    // True when this collider is the player or any part of them — hands and feet are
    // separate colliders on the model, and none of them carry the Player tag.
    public static bool IsPlayer(Collider collider)
    {
        return collider.CompareTag("Player")
            || collider.GetComponentInParent<ThirdPersonController>() != null;
    }

    public static void MoveTo(Vector3 position)
    {
        Transform player = Find();
        if (player != null) MoveTo(player, position, player.rotation);
    }

    public static void MoveTo(Transform player, Vector3 position, Quaternion rotation)
    {
        CharacterController body = player.GetComponent<CharacterController>();

        if (body != null) body.enabled = false;
        player.SetPositionAndRotation(position, rotation);
        if (body != null) body.enabled = true;

        Physics.SyncTransforms();
    }
}
