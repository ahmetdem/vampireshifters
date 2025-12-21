using UnityEngine;

public class SwarmVisuals : MonoBehaviour
{
    [SerializeField] private GameObject visualPrefab;
    [SerializeField] private int entityCount = 5;
    [SerializeField] private float swarmSpread = 2f; // The "Radius" of the visual group

    private GameObject[] _minions;

    public float GetSwarmSpread() => swarmSpread; // Getter for the controller

    private void Start()
    {
        _minions = new GameObject[entityCount];
        for (int i = 0; i < entityCount; i++)
        {
            // Source 49: Spawns visual-only zombie prefabs locally
            Vector2 randomOffset = Random.insideUnitCircle * swarmSpread;
            _minions[i] = Instantiate(visualPrefab, transform.position + (Vector3)randomOffset, Quaternion.identity);
            _minions[i].transform.SetParent(transform);
        }
    }
}
