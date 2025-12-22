using UnityEngine;

public class SwarmVisuals : MonoBehaviour
{
    [SerializeField] private GameObject visualPrefab;
    [SerializeField] private int swarmCount = 1;
    [SerializeField] private float swarmSpread = 2f; // The "Radius" of the visual group

    private GameObject[] _minions;

    public float GetSwarmSpread() => swarmSpread; // Getter for the controller

    private void Start()
    {
        _minions = new GameObject[swarmCount];
        for (int i = 0; i < swarmCount; i++)
        {
            // Source 49: Spawns visual-only zombie prefabs locally
            Vector2 randomOffset = Random.insideUnitCircle * swarmSpread;
            _minions[i] = Instantiate(visualPrefab, transform.position + (Vector3)randomOffset, Quaternion.identity);
            _minions[i].transform.SetParent(transform);
        }
    }

    public void SetSwarmDensity(float multiplier)
    {
        // 1. Calculate new count
        int newCount = Mathf.RoundToInt(swarmCount * multiplier);

        // 2. FIX: Clamp to 1 instead of 5 so you can test small groups
        swarmCount = Mathf.Clamp(newCount, 1, 50);

        // Note: If this runs after Start(), you might need to manually trigger a respawn 
        // of the visual minions here, otherwise this number only changes for the NEXT wave.
    }
}
