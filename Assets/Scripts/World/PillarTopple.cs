using System.Collections;
using UnityEngine;

// A hall pillar. Hit it with the axe and a meter fills; fill it and the pillar goes over,
// crushing whatever is underneath. Dropping one on the boss while it is floored is what the
// fight is actually about — chopping at the boss alone would take all day.
public class PillarTopple : MonoBehaviour
{
    [Tooltip("Axe hits needed to bring it down")]
    public int hitsToTopple = 5;

    [Tooltip("Damage to anything caught under it")]
    public int crushDamage = 120;

    [Tooltip("How wide a patch the fallen pillar counts as hitting")]
    public float crushRadius = 6f;

    public float fallSeconds = 1.2f;

    [Header("When it lands")]
    [Tooltip("The pillar broken into pieces. Empty = the whole pillar stays lying there.")]
    public GameObject shatteredPillar;

    [Tooltip("Seconds until the rubble disappears. 0 = it stays.")]
    public float clearAwayAfter = 0f;

    [Tooltip("Shove given to the pieces. 0 and they just drop; higher and they scatter.")]
    public float shatterForce = 350f;

    [Tooltip("Seconds the meter stays up after the last hit")]
    public float showMeterFor = 3f;

    [Tooltip("Topple towards an enemy standing within reach. Off: it always falls away from you.")]
    public bool aimAtEnemies = true;

    private int hits = 0;
    private bool falling = false;
    private float height;
    private float lastHitAt = -999f;

    void Awake()
    {
        height = transform.lossyScale.y * 2f;   // a Unity cylinder is 2 units tall at scale 1
    }

    // Called by PlayerCombat when a swing lands on this pillar.
    public void TakeHit()
    {
        if (falling) return;

        hits++;
        lastHitAt = Time.time;
        if (hits >= hitsToTopple) StartCoroutine(Fall());
    }

    IEnumerator Fall()
    {
        falling = true;

        Vector3 away = FallDirection();
        Vector3 foot = transform.position - Vector3.up * (height / 2f);
        Vector3 axis = Vector3.Cross(Vector3.up, away);

        float turned = 0f;
        while (turned < 90f)
        {
            float step = 90f * Time.deltaTime / fallSeconds;
            transform.RotateAround(foot, axis, step);
            turned += step;
            yield return null;
        }

        Crush(foot, away);
        Shatter();
    }

    // Swaps the whole pillar for its broken version, whose pieces are loose bodies and fall
    // apart on their own. A pillar left lying there is a mark of one you have already used.
    void Shatter()
    {
        if (shatteredPillar != null)
        {
            GameObject rubble = Instantiate(shatteredPillar, transform.position, transform.rotation);

            // Without a shove the pieces just sag into a heap.
            foreach (Rigidbody piece in rubble.GetComponentsInChildren<Rigidbody>())
                piece.AddExplosionForce(shatterForce, transform.position, height, 0.4f);

            if (clearAwayAfter > 0f) Destroy(rubble, clearAwayAfter);
            Destroy(gameObject);
            return;
        }

        if (clearAwayAfter > 0f) Destroy(gameObject, clearAwayAfter);
    }

    // It goes over towards whatever it can actually reach, so chopping the pillar beside the
    // floored boss lands on it. With nothing in range it falls away from whoever chopped it.
    Vector3 FallDirection()
    {
        EnemyHealth target = aimAtEnemies ? NearestEnemy() : null;
        Vector3 direction;

        if (target != null)
        {
            direction = target.transform.position - transform.position;
        }
        else
        {
            Transform player = PlayerTeleport.Find();
            direction = player != null ? transform.position - player.position : transform.forward;
        }

        direction.y = 0f;
        return direction.sqrMagnitude > 0.01f ? direction.normalized : transform.forward;
    }

    EnemyHealth NearestEnemy()
    {
        EnemyHealth nearest = null;
        float best = float.MaxValue;

        foreach (EnemyHealth enemy in FindObjectsByType<EnemyHealth>())
        {
            float distance = Vector3.Distance(enemy.transform.position, transform.position);
            if (distance < best) { best = distance; nearest = enemy; }
        }

        return nearest;
    }

    // Anything lying under the fallen pillar is hit — along its whole length, not just where
    // the tip happens to land.
    void Crush(Vector3 foot, Vector3 direction)
    {
        Vector3 tip = foot + direction * height;

        foreach (EnemyHealth enemy in FindObjectsByType<EnemyHealth>())
        {
            Vector3 spot = enemy.transform.position;
            spot.y = foot.y;   // the pillar is flat on the floor now, so height is not the point

            if (DistanceToSegment(spot, foot, tip) <= crushRadius)
                enemy.TakeDamage(crushDamage);
        }
    }

    static float DistanceToSegment(Vector3 point, Vector3 from, Vector3 to)
    {
        Vector3 span = to - from;
        float along = Mathf.Clamp01(Vector3.Dot(point - from, span) / span.sqrMagnitude);
        return Vector3.Distance(point, from + span * along);
    }

    // Drawn at a fixed place on screen, not above the pillar: standing at the foot of a 12m
    // column puts its top off the top of the screen, which is exactly when you need to read it.
    void OnGUI()
    {
        if (Hud.Hidden || falling || hits == 0 || Time.time - lastHitAt > showMeterFor) return;

        float w = Screen.width * 0.3f, h = 18f;
        float x = (Screen.width - w) / 2f;
        float y = Screen.height - 104f;   // just above the boss bar

        Hud.Bar(new Rect(x, y, w, h), (float)hits / hitsToTopple, new Color(1f, 0.75f, 0.25f));

        GUIStyle style = new GUIStyle(GUI.skin.label)
        { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, fontSize = 13 };
        style.normal.textColor = Color.black;
        GUI.Label(new Rect(x, y, w, h), "PILLAR   " + hits + " / " + hitsToTopple, style);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.6f, 0.2f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, crushRadius);
    }
}
