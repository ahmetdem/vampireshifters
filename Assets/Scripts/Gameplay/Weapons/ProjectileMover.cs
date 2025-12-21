using Unity.Netcode;
using UnityEngine;

public class ProjectileMover : NetworkBehaviour
{
    private Vector2 direction;
    private float speed;
    private int damage;
    private float lifeTime = 3f;
    private float timer = 0f;

    public void Initialize(Vector2 dir, float spd, int dmg, float life = 3f)
    {
        direction = dir;
        speed = spd;
        damage = dmg;
        lifeTime = life;
        timer = 0f; // Reset timer on initialization
    }

    private void FixedUpdate()
    {
        // Only the Server moves and manages the lifetime of the projectile
        if (!IsServer) return;

        // Move the projectile
        transform.position += (Vector3)direction * speed * Time.fixedDeltaTime;

        // Count up the timer
        timer += Time.fixedDeltaTime;
        if (timer >= lifeTime)
        {
            Debug.Log($"[Projectile] Bullet {NetworkObjectId} expired after {lifeTime}s.");
            DespawnProjectile();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsServer) return;

        if (other.CompareTag("Enemy"))
        {
            if (other.TryGetComponent(out Health health))
            {
                health.TakeDamage(damage);
            }

            // Despawn immediately on impact
            DespawnProjectile();
        }
    }

    private void DespawnProjectile()
    {
        // Safety check to ensure it's still spawned before despawning
        if (NetworkObject != null && NetworkObject.IsSpawned)
        {
            NetworkObject.Despawn();
        }
    }
}
