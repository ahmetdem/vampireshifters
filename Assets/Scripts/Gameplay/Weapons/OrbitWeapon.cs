using Unity.Netcode;
using UnityEngine;
using System.Collections;
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

    private bool initialized = false;

    public override void Initialize(WeaponData weaponData, ulong id)
    {
        base.Initialize(weaponData, id);
        orbitRadius = data.range;
        orbitalCount = Mathf.Max(1, Mathf.RoundToInt(data.duration)); // Use duration as orbital count
        
        Debug.Log($"[OrbitWeapon] Initialize called! IsServer={NetworkManager.Singleton?.IsServer}, range={orbitRadius}, orbitalCount={orbitalCount}, prefab={(data.projectilePrefab != null ? data.projectilePrefab.name : "NULL")}");
        
        // Delay spawning to avoid scene sync race condition
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
        {
            Debug.Log("[OrbitWeapon] Starting DelayedSpawnOrbitals coroutine...");
            StartCoroutine(DelayedSpawnOrbitals());
        }
        else
        {
            Debug.Log("[OrbitWeapon] Not server or NetworkManager null - skipping orbital spawn");
        }
        
        initialized = true;
    }

    private IEnumerator DelayedSpawnOrbitals()
    {
        Debug.Log("[OrbitWeapon] DelayedSpawnOrbitals started, waiting 0.5s...");
        // Wait for scene synchronization to complete
        yield return new WaitForSeconds(0.5f);
        
        Debug.Log("[OrbitWeapon] Delay complete, calling SpawnOrbitals...");
        SpawnOrbitals();
    }

    private void SpawnOrbitals()
    {
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("[OrbitWeapon] SpawnOrbitals: NetworkManager is NULL!");
            return;
        }
        
        if (!NetworkManager.Singleton.IsServer)
        {
            Debug.LogWarning("[OrbitWeapon] SpawnOrbitals: Not server, aborting");
            return;
        }
        
        if (data == null)
        {
            Debug.LogError("[OrbitWeapon] SpawnOrbitals: data is NULL!");
            return;
        }
        
        if (data.projectilePrefab == null)
        {
            Debug.LogError($"[OrbitWeapon] SpawnOrbitals: projectilePrefab is NULL! Check WeaponData '{data.weaponName}'");
            return;
        }

        Debug.Log($"[OrbitWeapon] SpawnOrbitals: Spawning {orbitalCount} orbitals...");

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
                Debug.Log($"[OrbitWeapon] Spawned orbital {i} at {spawnPos}");
            }
            else
            {
                Debug.LogError($"[OrbitWeapon] Orbital prefab missing NetworkObject component!");
            }

            // Setup orbital damage component with prediction data
            if (orbital.TryGetComponent(out OrbitalDamage orbDmg))
            {
                orbDmg.Initialize(GetCurrentDamage(), ownerId, i, orbitalCount, orbitRadius);
            }

            orbitals.Add(orbital);
        }
        
        Debug.Log($"[OrbitWeapon] Successfully spawned {orbitals.Count} orbitals");
    }

    private Vector3 GetOrbitPosition(float angleOffset)
    {
        float angle = angleOffset * Mathf.Deg2Rad;
        float x = Mathf.Cos(angle) * orbitRadius;
        float y = Mathf.Sin(angle) * orbitRadius;
        return transform.position + new Vector3(x, y, 0);
    }

    public override void WeaponUpdate()
    {
        if (!NetworkManager.Singleton.IsServer) return;
        if (!initialized) return;

        // Note: Movement is now handled autonomously by OrbitalDamage.cs on each client
        // This ensures visuals are smooth and attached to the player representation on that client
    }

    // Orbital weapon doesn't use TryAttack - damage is handled by OrbitalDamage on collision
    protected override bool TryAttack()
    {
        return false;
    }

    private void OnDisable()
    {
        Debug.LogWarning($"[OrbitWeapon] OnDisable called! This might destroy orbitals. Stack trace follows.");
        Debug.LogWarning(System.Environment.StackTrace);
    }

    private void OnDestroy()
    {
        Debug.LogWarning($"[OrbitWeapon] OnDestroy called! Orbitals will be despawned.");
        
        // Safety check for shutdown/scene change where NetworkManager might already be gone
        if (NetworkManager.Singleton == null)
        {
            Debug.Log("[OrbitWeapon] OnDestroy: NetworkManager null, clearing orbital references");
            orbitals.Clear();
            return;
        }

        // Only server can despawn network objects
        if (!NetworkManager.Singleton.IsServer) 
        {
            Debug.Log("[OrbitWeapon] OnDestroy: Not server, clearing orbital references only");
            orbitals.Clear();
            return;
        }

        // Server: copy list, clear it, then despawn
        // This prevents issues if despawn triggers other callbacks
        var orbitalsToDestroy = new List<GameObject>(orbitals);
        orbitals.Clear();

        Debug.Log($"[OrbitWeapon] OnDestroy: Server despawning {orbitalsToDestroy.Count} orbitals");
        
        foreach (var orbital in orbitalsToDestroy)
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
    }
}
