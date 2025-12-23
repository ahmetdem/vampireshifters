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
        if (!IsServer) return;

        // 1. Hit Enemy (Standard)
        if (other.CompareTag("Enemy"))
        {
            if (other.TryGetComponent(out Health health)) health.TakeDamage(damage);
            DespawnProjectile();
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
    private void DespawnProjectile()
    {
        if (NetworkObject != null && NetworkObject.IsSpawned) NetworkObject.Despawn();
    }
}
