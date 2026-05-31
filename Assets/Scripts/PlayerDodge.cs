using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using StarterAssets;

// Quick combat dodge/roll: press Space (or right-click) to dash in the direction
// you're moving — or backwards if standing still. Brief i-frames optional. Lets
// you reposition during the boss fight instead of just trading blows.
// Drives an optional "dodge" animator trigger if the controller has one.
[RequireComponent(typeof(CharacterController))]
public class PlayerDodge : MonoBehaviour
{
    [Header("Dodge")]
    [Tooltip("How fast the dash moves")]
    public float dodgeSpeed = 9f;
    [Tooltip("How long the dash lasts")]
    public float dodgeTime = 0.28f;
    [Tooltip("Cooldown before you can dodge again")]
    public float cooldown = 0.7f;

    [Header("Invincibility (i-frames)")]
    [Tooltip("Take no damage during the dodge")]
    public bool invincibleWhileDodging = true;

    [Header("Input")]
    [Tooltip("Use the jump key (Space) to dodge — OFF so Space stays for jumping")]
    public bool useSpace = false;
    [Tooltip("Right mouse button to dodge")]
    public bool useRightMouse = true;

    // other scripts can check this (e.g. to skip damage)
    public static bool IsDodging = false;

    private CharacterController cc;
    private StarterAssetsInputs input;
    private ThirdPersonController controller;
    private Animator animator;
    private bool hasDodgeTrigger = false;
    private float lastDodge = -999f;

    void Awake() { IsDodging = false; }

    void Start()
    {
        cc = GetComponent<CharacterController>();
        input = GetComponent<StarterAssetsInputs>();
        controller = GetComponent<ThirdPersonController>();
        animator = GetComponent<Animator>();

        if (animator != null)
            foreach (AnimatorControllerParameter p in animator.parameters)
                if (p.name == "dodge") { hasDodgeTrigger = true; break; }
    }

    void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.currentHP <= 0) return;
        if (IsDodging || Time.time - lastDodge < cooldown) return;

        bool pressed = false;
        if (useSpace && Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame) pressed = true;
        if (useRightMouse && Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame) pressed = true;

        if (pressed) StartCoroutine(Dodge());
    }

    IEnumerator Dodge()
    {
        lastDodge = Time.time;
        IsDodging = true;

        // direction: where you're steering, else straight back
        Vector3 dir;
        if (input != null && input.move.sqrMagnitude > 0.01f)
        {
            // movement is relative to the camera, like the controller's own movement
            Transform cam = Camera.main != null ? Camera.main.transform : null;
            Vector3 fwd = cam != null ? cam.forward : transform.forward;
            Vector3 right = cam != null ? cam.right : transform.right;
            fwd.y = 0f; right.y = 0f; fwd.Normalize(); right.Normalize();
            dir = (fwd * input.move.y + right * input.move.x).normalized;
        }
        else
        {
            dir = -transform.forward;   // standing still -> hop backwards
            dir.y = 0f; dir.Normalize();
        }

        // face the dodge direction (looks better than sliding sideways)
        if (dir.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.LookRotation(dir);

        if (hasDodgeTrigger) animator.SetTrigger("dodge");
        if (controller != null) controller.combatSpeedMultiplier = 0f;   // suppress normal walk during the dash

        float t = 0f;
        while (t < dodgeTime)
        {
            float speed = Mathf.Lerp(dodgeSpeed, dodgeSpeed * 0.3f, t / dodgeTime);
            if (cc != null && cc.enabled) cc.Move(dir * speed * Time.deltaTime);
            t += Time.deltaTime;
            yield return null;
        }

        if (controller != null) controller.combatSpeedMultiplier = 1f;
        IsDodging = false;
    }

    void OnDisable()
    {
        IsDodging = false;
        if (controller != null) controller.combatSpeedMultiplier = 1f;
    }
}