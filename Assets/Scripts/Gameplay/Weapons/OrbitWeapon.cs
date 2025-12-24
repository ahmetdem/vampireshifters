using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Spawns orbiting projectiles around the player that damage enemies on contact.
/// Orbitals persist and deal damage periodically when enemies are in range.
/// </summary>
public class OrbitWeapon : BaseWeapon
{
    private List<GameObject> orbitals = new List<GameObject>();
    private int orbitalCount = 2;
    private float orbitRadius;
    private float orbitSpeed = 180f; // degrees per second
    private float currentAngle = 0f;
    private bool initialized = false;

    public override void Initialize(WeaponData weaponData, ulong id)
    {
        base.Initialize(weaponData, id);
        orbitRadius = data.range;
        orbitalCount = Mathf.Max(1, Mathf.RoundToInt(data.duration)); // Use duration as orbital count
        SpawnOrbitals();
        initialized = true;
    }

    private void SpawnOrbitals()
    {
        if (!NetworkManager.Singleton.IsServer) return;
        if (data.projectilePrefab == null) return;

        for (int i = 0; i < orbitalCount; i++)
        {
            float angleOffset = (360f / orbitalCount) * i;
            Vector3 spawnPos = GetOrbitPosition(angleOffset);

            GameObject orbital = Instantiate(data.projectilePrefab, spawnPos, Quaternion.identity);
            
            // Spawn on network FIRST (required by Netcode before any reparenting)
            NetworkObject netObj = orbital.GetComponent<NetworkObject>();
            if (netObj != null)
            {
                netObj.Spawn();
            }

            // Setup orbital damage component
            if (orbital.TryGetComponent(out OrbitalDamage orbDmg))
            {
                orbDmg.Initialize(GetCurrentDamage(), ownerId);
            }

            orbitals.Add(orbital);
        }
        // Note: We don't parent orbitals - we manually update their positions in WeaponUpdate()
    }

    private Vector3 GetOrbitPosition(float angleOffset)
    {
        float angle = (currentAngle + angleOffset) * Mathf.Deg2Rad;
        float x = Mathf.Cos(angle) * orbitRadius;
        float y = Mathf.Sin(angle) * orbitRadius;
        return transform.position + new Vector3(x, y, 0);
    }

    public override void WeaponUpdate()
    {
        if (!NetworkManager.Singleton.IsServer) return;
        if (!initialized) return;

        // Rotate orbitals around player
        currentAngle += orbitSpeed * Time.deltaTime;
        if (currentAngle >= 360f) currentAngle -= 360f;

        for (int i = 0; i < orbitals.Count; i++)
        {
            if (orbitals[i] != null)
            {
                float angleOffset = (360f / orbitalCount) * i;
                orbitals[i].transform.position = GetOrbitPosition(angleOffset);
            }
        }
    }

    // Orbital weapon doesn't use TryAttack - damage is handled by OrbitalDamage on collision
    protected override bool TryAttack()
    {
        return false;
    }

    private void OnDestroy()
    {
        // Clean up orbitals when weapon is destroyed
        foreach (var orbital in orbitals)
        {
            if (orbital != null)
            {
                NetworkObject netObj = orbital.GetComponent<NetworkObject>();
                if (netObj != null && netObj.IsSpawned)
                {
                    netObj.Despawn();
                }
            }
        }
        orbitals.Clear();
    }
}
