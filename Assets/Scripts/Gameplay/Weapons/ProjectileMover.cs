using Unity.Netcode;
using UnityEngine;

public class ProjectileMover : NetworkBehaviour
{
    private Vector2 direction;
    private float speed;
    private int damage;
    private float lifeTime = 3f;
    private float timer = 0f;

    // NEW: Track who fired this
    private ulong ownerId;

    // UPDATE: Add ownerId to the parameters
    public void Initialize(Vector2 dir, float spd, int dmg, ulong owner, float life = 3f)
    {
        direction = dir;
        speed = spd;
        damage = dmg;
        ownerId = owner; // Store it!
        lifeTime = life;
        timer = 0f;
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
                if (health != null) health.TakeDamage(damage);
                DespawnProjectile();
            }
            // Client creates visual prediction on despawn naturally by network object disappearing or we could hide it locally immediately if needed,
            // but for now we just want the flash.
        }
        // 2. Hit Player (PvP Logic)
        else if (other.CompareTag("Player"))
        {
            if (PvPDirector.Instance != null && PvPDirector.Instance.IsPvPActive.Value)
            {
                // Try to get NetworkObject from the collider or its parents
                NetworkObject netObj = other.GetComponentInParent<NetworkObject>();
                if (netObj != null)
                {
                    // Skip if this is our own projectile owner
                    if (netObj.NetworkObjectId == ownerId) return;

                    // SERVER LOGIC
                    if (IsServer)
                    {
                        Debug.Log($"[PvP] Bullet Hit Player {netObj.OwnerClientId}! Dealing {damage} dmg.");

                        // Get Health from the same GameObject as NetworkObject
                        Health health = netObj.GetComponent<Health>();
                        if (health != null)
                        {
                            health.TakeDamage(damage);
                        }
                        DespawnProjectile();
                    }
                }
            }
        }
    }
    private void DespawnProjectile()
    {
        if (NetworkObject != null && NetworkObject.IsSpawned) NetworkObject.Despawn();
    }
}
