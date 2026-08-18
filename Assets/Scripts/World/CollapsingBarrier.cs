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

    void Collapse()
    {
        collapsed = true;

        // Let go of every piece at once — the pile falls apart on its own from there.
        foreach (Rigidbody piece in GetComponentsInChildren<Rigidbody>())
            piece.isKinematic = false;

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
