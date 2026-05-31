using UnityEngine;
using UnityEngine.InputSystem;

// Simple melee attack: left-click swings, hitting any enemy in front within
// range. (Sword model + swing animation can be added later — this is the
// gameplay logic.) Put this on the PlayerArmature.
public class PlayerCombat : MonoBehaviour
{
    public int damage = 15;
    public float range = 2.8f;
    public float coneAngle = 100f;
    public float cooldown = 0.55f;

    private float lastAttack = -999f;

    void Update()
    {
        bool clicked = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
        if (clicked && Time.time - lastAttack >= cooldown)
        {
            lastAttack = Time.time;
            Attack();
        }
    }

    void Attack()
    {
        // hit every enemy in front of the player within range
        EnemyHealth[] enemies = Object.FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None);
        foreach (EnemyHealth e in enemies)
        {
            Vector3 to = e.transform.position - transform.position;
            to.y = 0f;
            if (to.magnitude <= range && Vector3.Angle(transform.forward, to) <= coneAngle * 0.5f)
                e.TakeDamage(damage);
        }
    }
}
