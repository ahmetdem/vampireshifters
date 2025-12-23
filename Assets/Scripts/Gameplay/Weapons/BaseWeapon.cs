using UnityEngine;
using Unity.Netcode;

// Abstract means you can't put this directly on an object; you must inherit from it
public abstract class BaseWeapon : MonoBehaviour
{
    protected WeaponController controller;

    protected WeaponData data;
    protected ulong ownerId;
    protected float currentCooldown;
    protected float timer;

    // Initialize logic
    public virtual void Initialize(WeaponData weaponData, ulong id)
    {
        data = weaponData;
        ownerId = id;
        currentCooldown = data.baseCooldown;
        timer = 0f;

        // 2. Find the controller on the player
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(id, out NetworkObject playerObj))
        {
            controller = playerObj.GetComponent<WeaponController>();
        }
    }

    public virtual void WeaponUpdate()
    {
        timer -= Time.deltaTime;

        if (timer <= 0f)
        {
            if (TryAttack())
            {
                timer = currentCooldown;
            }
        }
    }

    protected int GetCurrentDamage()
    {
        float multiplier = 1.0f;
        if (controller != null) multiplier = controller.globalDamageMultiplier;

        // Return Base Damage * Multiplier
        return Mathf.RoundToInt(data.baseDamage * multiplier);
    }

    // Returns true if attack successfully fired
    protected abstract bool TryAttack();

    // Helper to find nearest enemy for Auto-Aim weapons
    protected Transform FindNearestEnemy()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, data.range);
        Transform bestTarget = null;
        float closestDistSqr = Mathf.Infinity;

        // Check Global PvP State
        bool isPvP = false;
        if (PvPDirector.Instance != null && PvPDirector.Instance.IsPvPActive.Value)
        {
            isPvP = true;
        }

        foreach (var hit in hits)
        {
            bool isValidTarget = false;

            // 1. Standard Enemy
            if (hit.CompareTag("Enemy"))
            {
                isValidTarget = true;
            }
            // 2. PvP Target (Player)
            else if (isPvP && hit.CompareTag("Player"))
            {
                if (hit.TryGetComponent(out NetworkObject netObj))
                {
                    // FIX IS HERE: Compare NetworkObjectId, not OwnerClientId
                    if (netObj.NetworkObjectId != ownerId)
                    {
                        isValidTarget = true;
                    }
                }
            }

            if (isValidTarget)
            {
                float dSqr = (hit.transform.position - transform.position).sqrMagnitude;
                if (dSqr < closestDistSqr)
                {
                    closestDistSqr = dSqr;
                    bestTarget = hit.transform;
                }
            }
        }
        return bestTarget;
    }
}
