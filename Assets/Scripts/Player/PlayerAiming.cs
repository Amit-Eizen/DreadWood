using UnityEngine;
using UnityEngine.InputSystem;

// HOLD the right mouse button to aim: the view swaps to a first-person camera on the
// player's head, and a left click throws a rock (RockThrow) instead of swinging the axe.
// A quick TAP of the same button is a dodge — PlayerDodge waits for the release and
// checks IsAiming, so this script owns the hold timing and nothing else has to.
public class PlayerAiming : MonoBehaviour
{
    [Header("Cameras")]
    [Tooltip("The normal over-the-shoulder camera (the Main Camera)")]
    public Camera thirdPersonCamera;
    [Tooltip("A camera parented to PlayerCameraRoot, at head height")]
    public Camera firstPersonCamera;

    [Header("Input")]
    [Tooltip("Hold the button longer than this to aim. Anything shorter is a dodge.")]
    public float holdThreshold = 0.2f;

    // other scripts check this (PlayerCombat suppresses the axe, PlayerDodge skips the tap)
    public static bool IsAiming { get; private set; }

    private float pressStartedAt = -1f;

    void Awake()
    {
        IsAiming = false;
    }

    void Start()
    {
        ShowFirstPerson(false);
    }

    // If this component is switched off (menus, death) we must not leave the player
    // stuck looking through the first-person camera.
    void OnDisable()
    {
        IsAiming = false;
        ShowFirstPerson(false);
    }

    void Update()
    {
        if (Mouse.current == null) return;

        if (GameManager.Instance != null && GameManager.Instance.currentHP <= 0)
        {
            SetAiming(false);
            return;
        }

        if (Mouse.current.rightButton.wasPressedThisFrame) pressStartedAt = Time.time;

        bool heldLongEnough = Mouse.current.rightButton.isPressed
                              && pressStartedAt >= 0f
                              && Time.time - pressStartedAt >= holdThreshold;
        SetAiming(heldLongEnough);

        if (Mouse.current.rightButton.wasReleasedThisFrame) pressStartedAt = -1f;
    }

    void SetAiming(bool aiming)
    {
        if (IsAiming == aiming) return;
        IsAiming = aiming;
        ShowFirstPerson(aiming);
    }

    void ShowFirstPerson(bool firstPerson)
    {
        if (thirdPersonCamera != null) thirdPersonCamera.enabled = !firstPerson;
        if (firstPersonCamera != null) firstPersonCamera.enabled = firstPerson;
    }
}
