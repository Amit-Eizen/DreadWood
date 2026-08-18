using UnityEngine;
using UnityEngine.InputSystem;

// A door on a real hinge. Press Y nearby and the joint's motor swings it open, then reverses
// and lets it fall shut a few seconds later. Goes on the door panel itself — the object with
// the Rigidbody — not on a parent pivot.
[RequireComponent(typeof(HingeJoint))]
public class DoorController : MonoBehaviour
{
    [Tooltip("How near the player must be to interact")]
    public float interactDistance = 3.5f;

    [Tooltip("Key that opens the door")]
    public Key openKey = Key.Y;

    [Tooltip("Seconds after opening before it swings shut by itself")]
    public float autoCloseDelay = 4f;

    [Tooltip("Swing speed in degrees per second")]
    public float swingSpeed = 200f;

    [Tooltip("How hard the motor pushes. Too low and the door stalls against its own weight.")]
    public float motorForce = 40f;

    [Tooltip("On-screen prompt font size")]
    public int promptFontSize = 20;

    private HingeJoint hinge;
    private Transform player;
    private bool isOpen = false;
    private bool playerNear = false;
    private float openTimer = 0f;

    void Start()
    {
        hinge = GetComponent<HingeJoint>();
        hinge.useMotor = true;
        player = PlayerTeleport.Find();
    }

    void Update()
    {
        playerNear = player != null &&
                     Vector3.Distance(transform.position, player.position) < interactDistance;

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

        // The motor drives it one way or the other and the joint limits catch it at each end,
        // so the door is never placed by hand — the physics decides where it actually is.
        JointMotor motor = hinge.motor;
        motor.targetVelocity = isOpen ? swingSpeed : -swingSpeed;
        motor.force = motorForce;
        hinge.motor = motor;
    }

    void OnGUI()
    {
        if (Hud.Hidden || !playerNear || isOpen) return;

        string msg = "Press Y to open";
        GUIStyle style = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = promptFontSize,
            fontStyle = FontStyle.Bold
        };

        float w = 320f, h = 36f;
        Rect r = new Rect((Screen.width - w) / 2f, Screen.height - 110f, w, h);

        style.normal.textColor = Color.black;
        GUI.Label(new Rect(r.x + 1.5f, r.y + 1.5f, r.width, r.height), msg, style);
        style.normal.textColor = Color.white;
        GUI.Label(r, msg, style);
    }
}
