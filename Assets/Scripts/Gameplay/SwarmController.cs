using Unity.Netcode;
using UnityEngine;

public class SwarmController : NetworkBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float wanderRadius = 5f;
    [SerializeField] private float speed = 2f;

    [Header("Collision Tuning")]
    [SerializeField] private CircleCollider2D swarmCollider;
    [SerializeField] private float padding = 1.2f; // Extra buffer so it feels fair

    private Vector2 wanderTarget;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            PickNewTarget();

            // Adjust collider size to match the visual swarm spread
            if (TryGetComponent(out SwarmVisuals visuals))
            {
                // We use the spread value from visuals to set the radius
                // Source 50: 1 Network Transform updates movement of the entire group
                swarmCollider.radius = visuals.GetSwarmSpread() * padding;
            }
        }
    }

    private void FixedUpdate()
    {
        if (!IsServer) return;

        Vector2 currentPos = transform.position;
        Vector2 direction = (wanderTarget - currentPos).normalized;
        transform.position += (Vector3)direction * speed * Time.fixedDeltaTime;

        if (Vector2.Distance(currentPos, wanderTarget) < 0.5f)
        {
            PickNewTarget();
        }
    }

    private void PickNewTarget()
    {
        wanderTarget = (Vector2)transform.position + Random.insideUnitCircle * wanderRadius;
    }

    private void OnDrawGizmosSelected()
    {
        if (swarmCollider != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, swarmCollider.radius);
        }
    }
}
