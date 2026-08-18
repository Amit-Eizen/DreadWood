using UnityEngine;

// A stack propped up over the escape route — logs, rocks, a leaning beam. Throw a rock at
// it hard enough and the whole thing comes down: every piece turns into a loose physics
// object, and anything caught underneath is floored long enough for you to gain ground.
[RequireComponent(typeof(Rigidbody))]
public class CollapsingBarrier : MonoBehaviour
{
    [Tooltip("How hard the hit has to be. A thrown rock clears this easily; brushing past it does not.")]
    public float minimumImpactSpeed = 6f;

    [Header("When it falls")]
    [Tooltip("Enemies this close to it are floored")]
    public float crushRadius = 4f;

    [Tooltip("How long they stay down")]
    public float knockOutSeconds = 3f;

    [Tooltip("Shove given to the pieces. 0 and they just drop where they stood.")]
    public float burstForce = 250f;

    private bool collapsed = false;

    void Awake()
    {
        // Propped up: it holds its pose until something hits it hard enough.
        foreach (Rigidbody piece in GetComponentsInChildren<Rigidbody>())
            piece.isKinematic = true;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collapsed || collision.relativeVelocity.magnitude < minimumImpactSpeed) return;
        Collapse();
    }

    // Called by PlayerCombat when an axe swing lands on it. Wood against an axe is one blow.
    public void TakeHit()
    {
        if (!collapsed) Collapse();
    }

    void Collapse()
    {
        collapsed = true;

        // The outer shell is what a thrown rock actually hits. Once it is broken it has to
        // stop being solid, or the pieces stay sealed inside it.
        foreach (Collider shell in GetComponents<Collider>()) shell.enabled = false;

        foreach (Rigidbody piece in GetComponentsInChildren<Rigidbody>())
        {
            if (piece.transform == transform) continue;   // the shell itself stays put

            piece.isKinematic = false;
            if (burstForce > 0f) piece.AddExplosionForce(burstForce, transform.position, 3f, 0.3f);
        }

        foreach (MutantAI enemy in FindObjectsByType<MutantAI>(FindObjectsSortMode.None))
            if (Vector3.Distance(enemy.transform.position, transform.position) <= crushRadius)
                enemy.KnockOut(knockOutSeconds);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.4f, 0.3f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, crushRadius);
    }
}
