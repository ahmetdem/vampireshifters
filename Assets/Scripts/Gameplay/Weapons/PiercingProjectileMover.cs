using Unity.Netcode;
using UnityEngine;

/// <summary>
/// A projectile that can pierce through multiple targets before despawning.
/// Used for weapons like Vampire Fang.
/// </summary>
public class PiercingProjectileMover : NetworkBehaviour
{
    private Vector2 direction;
    private float speed;
    private int damage;
    private float lifeTime = 3f;
    private float timer = 0f;
    private int maxPierces = 3;
    private int pierceCount = 0;

    private ulong ownerId;

    public void Initialize(Vector2 dir, float spd, int dmg, ulong owner, int pierces = 3, float life = 3f)
    {
        direction = dir;
        speed = spd;
        damage = dmg;
        ownerId = owner;
        maxPierces = pierces;
        lifeTime = life;
        timer = 0f;
        pierceCount = 0;
    }

    private void FixedUpdate()
    {
        if (!IsServer) return;
        transform.position += (Vector3)direction * speed * Time.fixedDeltaTime;

        timer += Time.fixedDeltaTime;
        if (timer >= lifeTime) DespawnProjectile();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        bool didHit = false;

        // 1. Hit Enemy (Standard)
        if (other.CompareTag("Enemy"))
        {
            // CLIENT VISUALS
            if (other.TryGetComponent(out MinionFlashFeedback feedback))
            {
                feedback.Flash();
            }

            // SERVER LOGIC
            if (IsServer)
            {
                // Use GetComponentInParent to find Health on parent (for individual minions)
                Health health = other.GetComponentInParent<Health>();
                if (health != null) 
                {
                    health.TakeDamage(damage);
                    didHit = true;
                }
            }
        }
        // 2. Hit Player (PvP Logic)
        else if (other.CompareTag("Player"))
        {
            if (PvPDirector.Instance != null && PvPDirector.Instance.IsPvPActive.Value)
            {
                NetworkObject netObj = other.GetComponentInParent<NetworkObject>();
                if (netObj != null && netObj.NetworkObjectId != ownerId)
                {
                    // SERVER LOGIC
                    if (IsServer)
                    {
                        Health health = netObj.GetComponent<Health>();
                        if (health != null)
                        {
                            health.TakeDamage(damage);
                            didHit = true;
                            Debug.Log($"[PvP] Piercing projectile hit Player {netObj.OwnerClientId}! Dealing {damage} dmg. Pierces left: {maxPierces - pierceCount - 1}");
                        }
                    }
                }
            }
        }

        // Handle pierce logic (Server Only)
        if (IsServer && didHit)
        {
            pierceCount++;
            if (pierceCount >= maxPierces)
            {
                DespawnProjectile();
            }
        }
    }

    private void DespawnProjectile()
    {
        if (NetworkObject != null && NetworkObject.IsSpawned) NetworkObject.Despawn();
    }
}
