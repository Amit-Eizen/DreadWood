using UnityEngine;
using UnityEngine.InputSystem;
using StarterAssets;

// Hold Ctrl to sneak: the player moves slowly, visibly CROUCHES (the body is
// lowered + tilted forward, since there's no dedicated crouch clip), and enemies
// only notice them from much closer (MutantAI reads IsStealthed to shrink its
// detection range). Sprinting (Shift) cancels stealth — can't run and sneak.
[RequireComponent(typeof(ThirdPersonController))]
public class PlayerStealth : MonoBehaviour
{
    // read by MutantAI (static so enemies don't need a reference)
    public static bool IsStealthed = false;

    [Range(0.1f, 1f)] [Tooltip("Move speed while sneaking, as a fraction of normal")]
    public float stealthMoveSpeed = 0.5f;

    [Header("Crouch animation")]
    [Tooltip("Animator bool set true while sneaking — add a crouch state driven by this (leave matching if your controller has no such param; it's checked safely)")]
    public string crouchBool = "isCrouching";

    [Tooltip("Show the on-screen STEALTH indicator")]
    public bool showIndicator = true;

    private ThirdPersonController controller;
    private Animator animator;
    private bool hasCrouchParam = false;

    void Awake()
    {
        IsStealthed = false;   // reset on load (static persists across plays in the editor)
    }

    void Start()
    {
        controller = GetComponent<ThirdPersonController>();
        animator = GetComponent<Animator>();

        // only drive the crouch bool if the controller actually has it (no warnings)
        if (animator != null)
            foreach (AnimatorControllerParameter p in animator.parameters)
                if (p.name == crouchBool) { hasCrouchParam = true; break; }
    }

    void Update()
    {
        var kb = Keyboard.current;
        bool shift = kb != null && (kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed);
        bool ctrl = kb != null && (kb.leftCtrlKey.isPressed || kb.rightCtrlKey.isPressed);

        IsStealthed = ctrl && !shift;   // running beats sneaking

        if (controller != null)
            controller.stealthSpeedMultiplier = IsStealthed ? stealthMoveSpeed : 1f;

        if (hasCrouchParam) animator.SetBool(crouchBool, IsStealthed);
    }

    void OnDisable()
    {
        IsStealthed = false;
        if (controller != null) controller.stealthSpeedMultiplier = 1f;
        if (hasCrouchParam && animator != null) animator.SetBool(crouchBool, false);
    }

    void OnGUI()
    {
        if (!showIndicator || !IsStealthed) return;

        GUIStyle st = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold,
            fontSize = 16
        };
        st.normal.textColor = new Color(0.6f, 0.85f, 1f, 0.9f);
        GUI.Label(new Rect(0, Screen.height - 60, Screen.width, 24), "～ STEALTH ～", st);
    }
}
