using UnityEngine;
using UnityEngine.SceneManagement;

// The way out of a level. Walk into it and the next scene loads.
[RequireComponent(typeof(Collider))]
public class SceneExit : MonoBehaviour
{
    [Tooltip("Scene to load. Leave empty to end on the win screen instead.")]
    public string nextScene = "";

    [Tooltip("Shown the moment the player reaches it")]
    public string arrivalObjective = "";

    private bool used = false;

    void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (used || !PlayerTeleport.IsPlayer(other)) return;
        used = true;

        if (GameManager.Instance != null && !string.IsNullOrEmpty(arrivalObjective))
            GameManager.Instance.SetObjective(arrivalObjective);

        if (!string.IsNullOrEmpty(nextScene))
        {
            SceneManager.LoadScene(nextScene);
            return;
        }

        if (GameManager.Instance != null) GameManager.Instance.Win();
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.9f, 0.3f, 0.8f);
        Gizmos.DrawWireCube(transform.position, new Vector3(9f, 5f, 1f));
    }
}
