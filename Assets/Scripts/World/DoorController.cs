using UnityEngine;
using UnityEngine.InputSystem;

// A door the player opens/closes by pressing Y when nearby.
// Put this on the door's hinge object (the pivot the panel is parented to).
// While the player is in range a small "Press Y" prompt is shown on screen.
// The panel keeps its collider, so a shut door physically blocks the doorway;
// swinging open moves the collider aside and clears the way (entry/exit).
public class DoorController : MonoBehaviour
{
    [Tooltip("Local Y angle when shut")]
    public float closedAngle = 0f;

    [Tooltip("Local Y angle when open")]
    public float openAngle = -95f;

    [Tooltip("How near the player must be to interact")]
    public float interactDistance = 3.5f;

    [Tooltip("Swing speed in degrees per second")]
    public float speed = 220f;

    [Tooltip("Key that opens the door")]
    public Key openKey = Key.Y;

    [Tooltip("Seconds after opening before the door swings shut by itself")]
    public float autoCloseDelay = 4f;

    [Tooltip("On-screen prompt font size")]
    public int promptFontSize = 20;

    private Transform player;
    private bool isOpen = false;
    private bool playerNear = false;
    private float openTimer = 0f;

    void Start()
    {
        player = PlayerTeleport.Find();
    }

    void Update()
    {
        playerNear = player != null &&
                     Vector3.Distance(transform.position, player.position) < interactDistance;

        // Press Y to open while near; the door swings shut by itself a few seconds later
        if (playerNear && !isOpen && Keyboard.current != null && Keyboard.current[openKey].wasPressedThisFrame)
        {
            isOpen = true;
            openTimer = autoCloseDelay;
        }

        if (isOpen)
        {
            openTimer -= Time.deltaTime;
            if (openTimer <= 0f) isOpen = false;
        }

        float targetAngle = isOpen ? openAngle : closedAngle;
        transform.localRotation = Quaternion.RotateTowards(
            transform.localRotation, Quaternion.Euler(0f, targetAngle, 0f), speed * Time.deltaTime);
    }

    void OnGUI()
    {
        if (!playerNear || isOpen) return; // prompt only when near and still shut

        string msg = "Press Y to open";
        GUIStyle style = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = promptFontSize,
            fontStyle = FontStyle.Bold
        };

        float w = 320f, h = 36f;
        Rect r = new Rect((Screen.width - w) / 2f, Screen.height - 110f, w, h);

        // drop shadow for readability
        style.normal.textColor = Color.black;
        GUI.Label(new Rect(r.x + 1.5f, r.y + 1.5f, r.width, r.height), msg, style);
        style.normal.textColor = Color.white;
        GUI.Label(r, msg, style);
    }
}
