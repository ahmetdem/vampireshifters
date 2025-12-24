using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Simple projectile fired by enemies. Damages players on contact.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class EnemyProjectile : NetworkBehaviour
{
    [Header("Settings")]
    [SerializeField] private float lifetime = 5f;

    private Vector2 direction;
    private float speed;
    private int damage;
    private float timer;
    private bool initialized;

    public void Initialize(Vector2 dir, float spd, int dmg)
    {
        direction = dir;
        speed = spd;
        damage = dmg;
        timer = 0f;
        initialized = true;
    }

    private void Update()
    {
        if (!IsServer || !initialized) return;

        // Move projectile
        transform.position += (Vector3)(direction * speed * Time.deltaTime);

        // Lifetime check
        timer += Time.deltaTime;
        if (timer >= lifetime)
        {
            DespawnProjectile();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsServer) return;

        // Ignore other enemies - projectiles pass through them
        if (other.CompareTag("Enemy")) return;

        // Hit player
        if (other.CompareTag("Player"))
        {
            Health health = other.GetComponentInParent<Health>();
            if (health != null)
            {
                health.TakeDamage(damage);
                Debug.Log($"[EnemyProjectile] Hit player for {damage} damage!");
            }
            DespawnProjectile();
        }
        // Hit wall/obstacle (optional - add "Wall" tag if needed)
        else if (other.CompareTag("Wall"))
        {
            DespawnProjectile();
        }
    }

    private void DespawnProjectile()
    {
        if (NetworkObject != null && NetworkObject.IsSpawned)
        {
            NetworkObject.Despawn();
        }
    }
}
