using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;
using Unity.Netcode.Components;

/// <summary>
/// Attach to orbital prefabs to handle damage on collision.
/// Also handles client-side visual prediction for smooth orbital movement.
/// </summary>
public class OrbitalDamage : NetworkBehaviour
{
    private int damage;
    private float hitCooldown = 0.5f;
    private Dictionary<ulong, float> recentHits = new Dictionary<ulong, float>();

    // Client-side prediction data
    private Transform ownerTransform;
    private float orbitSpeed = 180f;
    private float currentAngle = 0f;
    private bool isInitialized = false;

    // Network synced data for client-side prediction
    public NetworkVariable<ulong> OwnerNetworkObjectId = new NetworkVariable<ulong>();
    public NetworkVariable<int> OrbitalIndex = new NetworkVariable<int>();
    public NetworkVariable<int> TotalOrbitals = new NetworkVariable<int>(2); // Default to avoid /0
    public NetworkVariable<float> OrbitRadius = new NetworkVariable<float>(2f);

    /// <summary>
    /// Called by OrbitWeapon on server to initialize the orbital.
    /// </summary>
    public void Initialize(int dmg, ulong ownerNetObjId, int index, int total, float radius)
    {
        damage = dmg;
        
        // Set network variables so clients can calculate positions
        if (IsServer)
        {
            OwnerNetworkObjectId.Value = ownerNetObjId;
            OrbitalIndex.Value = index;
            TotalOrbitals.Value = Mathf.Max(1, total); // Prevent division by zero
            OrbitRadius.Value = radius;
        }
        
        FindOwnerTransform();
        isInitialized = true;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        Debug.Log($"[OrbitalDamage] OnNetworkSpawn! OwnerNetworkObjectId={OwnerNetworkObjectId.Value}, IsServer={IsServer}");
        
        // Subscribe to owner ID changes so clients can find owner when value arrives
        OwnerNetworkObjectId.OnValueChanged += OnOwnerIdChanged;
        
        // Try to find owner now (might work if value already set)
        if (OwnerNetworkObjectId.Value != 0)
        {
            FindOwnerTransform();
        }
        else
        {
            Debug.LogWarning("[OrbitalDamage] OnNetworkSpawn: OwnerNetworkObjectId is 0, waiting for value change...");
        }

        // Disable NetworkTransform on all clients/server to allow local visual prediction
        // We want the orbital to be visually attached to the player on THAT client
        if (TryGetComponent(out NetworkTransform netTransform))
        {
            netTransform.enabled = false;
        }
    }

    public override void OnNetworkDespawn()
    {
        OwnerNetworkObjectId.OnValueChanged -= OnOwnerIdChanged;
        base.OnNetworkDespawn();
    }

    private void OnOwnerIdChanged(ulong oldValue, ulong newValue)
    {
        Debug.Log($"[OrbitalDamage] OnOwnerIdChanged: {oldValue} -> {newValue}");
        FindOwnerTransform();
    }

    private void FindOwnerTransform()
    {
        if (OwnerNetworkObjectId.Value == 0)
        {
            Debug.LogWarning("[OrbitalDamage] FindOwnerTransform: OwnerNetworkObjectId is 0!");
            return;
        }
        if (NetworkManager.Singleton == null)
        {
            Debug.LogWarning("[OrbitalDamage] FindOwnerTransform: NetworkManager is null!");
            return;
        }

        // Find the NetworkObject with this ID
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(OwnerNetworkObjectId.Value, out NetworkObject ownerNetObj))
        {
            ownerTransform = ownerNetObj.transform;
            isInitialized = true;
            Debug.Log($"[OrbitalDamage] FindOwnerTransform SUCCESS: Found owner at {ownerTransform.position}");
        }
        else
        {
            Debug.LogWarning($"[OrbitalDamage] FindOwnerTransform FAILED: NetworkObjectId {OwnerNetworkObjectId.Value} not found in SpawnedObjects! SpawnedObjects count: {NetworkManager.Singleton.SpawnManager.SpawnedObjects.Count}");
        }
    }

    /// <summary>
    /// Check if this orbital belongs to the local player (for client-side prediction)
    /// </summary>
    private bool IsLocalPlayerOrbital()
    {
        if (NetworkManager.Singleton == null) return false;
        if (NetworkManager.Singleton.LocalClient == null) return false;
        if (NetworkManager.Singleton.LocalClient.PlayerObject == null) return false;
        
        return OwnerNetworkObjectId.Value == NetworkManager.Singleton.LocalClient.PlayerObject.NetworkObjectId;
    }

    private void Update()
    {
        // Clean up old hit records (server only)
        if (IsServer)
        {
            CleanupHitRecords();
        }

        // Non-server: try to find owner if not yet initialized
        if (!IsServer && !isInitialized && OwnerNetworkObjectId.Value != 0)
        {
            FindOwnerTransform();
        }

        // Universal visual prediction: Run on ALL clients (including server)
        // Since we disabled NetworkTransform, everyone calculates their own visual position
        // relative to where THEY see the owner.
        if (isInitialized && ownerTransform != null)
        {
            UpdateOrbitalPosition();
        }
    }

    private void CleanupHitRecords()
    {
        List<ulong> toRemove = new List<ulong>();
        foreach (var kvp in recentHits)
        {
            if (Time.time > kvp.Value)
            {
                toRemove.Add(kvp.Key);
            }
        }
        foreach (var id in toRemove)
        {
            recentHits.Remove(id);
        }
    }

    /// <summary>
    /// Client-side position update for smooth visuals.
    /// </summary>
    private void UpdateOrbitalPosition()
    {
        // Safety check - don't calculate if we don't have valid data
        if (TotalOrbitals.Value <= 0) return;

        // Rotate angle
        currentAngle += orbitSpeed * Time.deltaTime;
        if (currentAngle >= 360f) currentAngle -= 360f;

        // Calculate position using synced network values
        float angleOffset = (360f / TotalOrbitals.Value) * OrbitalIndex.Value;
        float angle = (currentAngle + angleOffset) * Mathf.Deg2Rad;
        float x = Mathf.Cos(angle) * OrbitRadius.Value;
        float y = Mathf.Sin(angle) * OrbitRadius.Value;

        transform.position = ownerTransform.position + new Vector3(x, y, 0);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 1. Hit Enemy
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
                int instanceId = other.gameObject.GetInstanceID();
                ulong enemyId = (ulong)(instanceId & 0x7FFFFFFF);

                if (!CanHit(enemyId)) return;

                Health health = other.GetComponentInParent<Health>();
                if (health != null)
                {
                    health.TakeDamage(damage);
                    RecordHit(enemyId);
                }
            }
        }
        // 2. Hit Player (PvP)
        else if (other.CompareTag("Player"))
        {
            if (PvPDirector.Instance != null && PvPDirector.Instance.IsPvPActive.Value)
            {
                NetworkObject netObj = other.GetComponentInParent<NetworkObject>();
                if (netObj != null && netObj.NetworkObjectId != OwnerNetworkObjectId.Value)
                {
                    // SERVER LOGIC
                    if (IsServer)
                    {
                        if (!CanHit(netObj.NetworkObjectId)) return;

                        Health health = netObj.GetComponent<Health>();
                        if (health != null)
                        {
                            health.TakeDamage(damage);
                            RecordHit(netObj.NetworkObjectId);
                            Debug.Log($"[PvP] Orbital hit Player {netObj.OwnerClientId}! Dealing {damage} dmg.");
                        }
                    }
                }
            }
        }
    }

    private bool CanHit(ulong targetId)
    {
        return !recentHits.ContainsKey(targetId);
    }

    private void RecordHit(ulong targetId)
    {
        recentHits[targetId] = Time.time + hitCooldown;
    }
}

