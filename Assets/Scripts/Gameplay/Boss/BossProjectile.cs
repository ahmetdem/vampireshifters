using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Projectile fired by boss attacks.
/// Similar to EnemyProjectile but with boss-specific behavior.
/// </summary>
public class BossProjectile : NetworkBehaviour
{
    [Header("Settings")]
    [SerializeField] private float lifetime = 5f;
    
    private Vector2 direction;
    private float speed;
    private int damage;
    private float lifeTimer;
    private bool initialized;
    
    /// <summary>
    /// Initialize the projectile with movement parameters.
    /// Must be called after Network Spawn.
    /// </summary>
    public void Initialize(Vector2 dir, float spd, int dmg)
    {
        direction = dir.normalized;
        speed = spd;
        damage = dmg;
        initialized = true;
        lifeTimer = lifetime;
        
        Debug.Log($"[BossProjectile] Initialized: dir={direction}, speed={speed}, damage={damage}");
    }
    
    private void Update()
    {
        if (!IsServer) return;
        if (!initialized) return;
        
        // Move projectile
        transform.position += (Vector3)(direction * speed * Time.deltaTime);
        
        // Lifetime check
        lifeTimer -= Time.deltaTime;
        if (lifeTimer <= 0f)
        {
            DestroyProjectile();
        }
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsServer) return;
        
        // Ignore other enemies and bosses
        if (other.CompareTag("Enemy") || other.CompareTag("Boss")) return;
        
        // Hit player
        if (other.CompareTag("Player"))
        {
            if (other.TryGetComponent(out Health playerHealth))
            {
                playerHealth.TakeDamage(damage);
                Debug.Log($"[BossProjectile] Hit player for {damage} damage!");
            }
            DestroyProjectile();
        }
        // Hit wall/obstacle
        else if (other.gameObject.layer == LayerMask.NameToLayer("Obstacle"))
        {
            DestroyProjectile();
        }
    }
    
    private void DestroyProjectile()
    {
        if (IsSpawned)
        {
            GetComponent<NetworkObject>().Despawn(true);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
