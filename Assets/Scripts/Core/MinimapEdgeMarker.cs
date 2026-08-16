using UnityEngine;

// Keeps a minimap marker useful even when its target is far away.
// While the target is inside the minimap's view the marker sits on it normally.
// Once it goes off the edge, the marker sticks to the rim of the map in the target's
// direction — so the portal is always pointed at, from anywhere in the forest.
//
// Put this on the marker quad itself and set Target to the thing it points at.
public class MinimapEdgeMarker : MonoBehaviour
{
    [Tooltip("What this marker points at. Leave empty to use this marker's parent.")]
    public Transform target;

    [Tooltip("The minimap camera — its orthographic size decides where the rim is")]
    public Camera minimapCamera;

    [Tooltip("How high above the world the marker floats (must be under the minimap camera)")]
    public float markerHeight = 40f;

    [Tooltip("How far out the marker sits when clamped. 1 = exactly on the rim, 0.85 = just inside.")]
    [Range(0.5f, 1f)] public float edgeFraction = 0.85f;

    private Transform player;

    void Start()
    {
        if (target == null && transform.parent != null) target = transform.parent;

        GameObject found = GameObject.FindWithTag("Player");
        if (found != null) player = found.transform;

        if (minimapCamera == null)
        {
            GameObject cam = GameObject.Find("MinimapCamera");
            if (cam != null) minimapCamera = cam.GetComponent<Camera>();
        }
    }

    // LateUpdate so the minimap camera has already followed the player this frame.
    void LateUpdate()
    {
        if (target == null || player == null || minimapCamera == null) return;

        // How far the target is from the player, ignoring height.
        Vector3 offset = target.position - player.position;
        offset.y = 0f;

        // The minimap shows orthographicSize metres from its centre to the top edge,
        // so anything further than that would be drawn outside the map — clamp it to the rim.
        float rim = minimapCamera.orthographicSize * edgeFraction;
        if (offset.magnitude > rim) offset = offset.normalized * rim;

        Vector3 markerPosition = player.position + offset;
        markerPosition.y = player.position.y + markerHeight;
        transform.position = markerPosition;
    }
}
