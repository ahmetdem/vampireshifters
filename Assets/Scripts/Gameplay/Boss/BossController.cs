using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Main controller for boss enemies.
/// Manages attack selection, phase transitions, and movement.
/// </summary>
public class BossController : NetworkBehaviour
{
    [Header("Configuration")]
    [SerializeField] private BossData bossData;
    
    [Header("Runtime State")]
    private List<BaseBossAttack> attackBehaviors = new List<BaseBossAttack>();
    private BossPhaseData currentPhase;
    private int currentPhaseIndex = -1;
    private float attackTimer;
    private Transform currentTarget;
    private BossHealth bossHealth;
    
    // Public accessors
    public BossData Data => bossData;
    public BossPhaseData CurrentPhase => currentPhase;
    
    private void Awake()
    {
        bossHealth = GetComponent<BossHealth>();
    }
    
    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            InitializeBoss();
        }
        
        // Spawn effect on all clients
        if (bossData != null && bossData.spawnEffectPrefab != null)
        {
            Instantiate(bossData.spawnEffectPrefab, transform.position, Quaternion.identity);
        }
    }
    
    private void InitializeBoss()
    {
        if (bossData == null)
        {
            Debug.LogError("[BossController] No BossData assigned!");
            return;
        }
        
        Debug.Log($"[BossController] Initializing boss: {bossData.bossName}");
        
        // Set initial health
        if (bossHealth != null)
        {
            bossHealth.SetMaxHealth(bossData.maxHealth);
        }
        
        // Initialize attack timer
        attackTimer = bossData.attackInterval;
        
        // Set initial phase
        UpdatePhase(1f);
        
        // Create attack behaviors for current attacks
        RefreshAttackBehaviors();
    }
    
    private void Update()
    {
        if (!IsServer) return;
        if (bossData == null) return;
        
        // Update target
        currentTarget = GetClosestPlayer();
        
        // Each attack fires independently when ready
        foreach (var attack in attackBehaviors)
        {
            attack.UpdateCooldown();
            
            // If attack is ready and we have a target in range, fire it
            if (attack.IsReady && currentTarget != null)
            {
                float distanceToTarget = Vector2.Distance(transform.position, currentTarget.position);
                if (distanceToTarget <= attack.Range)
                {
                    attack.Execute(currentTarget);
                }
            }
        }
        
        // Movement toward player
        if (bossData.chasesPlayers && currentTarget != null)
        {
            MoveTowardTarget();
        }
    }
    
    private void MoveTowardTarget()
    {
        if (currentTarget == null) return;
        
        float distance = Vector2.Distance(transform.position, currentTarget.position);
        
        // Only move if outside preferred distance
        if (distance > bossData.preferredDistance)
        {
            Vector2 direction = ((Vector2)currentTarget.position - (Vector2)transform.position).normalized;
            float speed = bossData.moveSpeed * (currentPhase?.speedMultiplier ?? 1f);
            
            transform.position += (Vector3)(direction * speed * Time.deltaTime);
        }
    }
    
    private void TryExecuteAttack()
    {
        if (attackBehaviors.Count == 0) return;
        
        // Get attacks that are ready and in range
        List<BaseBossAttack> readyAttacks = new List<BaseBossAttack>();
        float distanceToTarget = Vector2.Distance(transform.position, currentTarget.position);
        
        foreach (var attack in attackBehaviors)
        {
            if (attack.IsReady && distanceToTarget <= attack.Range)
            {
                readyAttacks.Add(attack);
            }
        }
        
        if (readyAttacks.Count == 0) return;
        
        // Weighted random selection
        float totalWeight = 0f;
        foreach (var attack in readyAttacks)
        {
            totalWeight += attack.SelectionWeight;
        }
        
        float random = Random.Range(0f, totalWeight);
        float cumulative = 0f;
        
        foreach (var attack in readyAttacks)
        {
            cumulative += attack.SelectionWeight;
            if (random <= cumulative)
            {
                attack.Execute(currentTarget);
                return;
            }
        }
        
        // Fallback: execute first ready attack
        readyAttacks[0].Execute(currentTarget);
    }
    
    /// <summary>
    /// Called by BossHealth when damage is taken.
    /// Checks for phase transitions.
    /// </summary>
    public void OnHealthChanged(float healthPercent)
    {
        if (!IsServer) return;
        
        UpdatePhase(healthPercent);
    }
    
    private void UpdatePhase(float healthPercent)
    {
        if (bossData.phases == null || bossData.phases.Length == 0) return;
        
        BossPhaseData newPhase = bossData.GetPhaseForHealth(healthPercent);
        
        if (newPhase != null && newPhase != currentPhase)
        {
            int newPhaseIndex = System.Array.IndexOf(bossData.phases, newPhase);
            
            if (newPhaseIndex != currentPhaseIndex)
            {
                Debug.Log($"[BossController] Phase transition: {currentPhase?.name ?? "None"} -> {newPhase.name}");
                
                currentPhase = newPhase;
                currentPhaseIndex = newPhaseIndex;
                
                // Spawn phase transition effect
                if (currentPhase.phaseTransitionEffect != null)
                {
                    var effect = Instantiate(currentPhase.phaseTransitionEffect, transform.position, Quaternion.identity);
                    if (effect.TryGetComponent(out NetworkObject netObj))
                    {
                        netObj.Spawn();
                    }
                }
                
                // Notify clients about phase change
                OnPhaseChangeClientRpc(currentPhaseIndex);
                
                // Refresh attack behaviors for new phase
                RefreshAttackBehaviors();
            }
        }
    }
    
    private void RefreshAttackBehaviors()
    {
        Debug.Log($"[BossController] RefreshAttackBehaviors called. Health: {GetHealthPercent():F2}");
        
        // Clear existing attack components
        foreach (var attack in attackBehaviors)
        {
            if (attack != null)
            {
                Destroy(attack);
            }
        }
        attackBehaviors.Clear();
        
        // Get attacks for current phase
        BossAttackData[] attacks = bossData.GetAttacksForHealth(GetHealthPercent());
        
        if (attacks == null || attacks.Length == 0)
        {
            Debug.LogWarning("[BossController] No attacks configured for current phase! Check your BossData - make sure phases have attackPatterns OR defaultAttacks is set.");
            return;
        }
        
        Debug.Log($"[BossController] Found {attacks.Length} attacks to add");
        
        // Create attack behavior components
        foreach (var attackData in attacks)
        {
            if (attackData == null) 
            {
                Debug.LogWarning("[BossController] Found null attack in array - skipping");
                continue;
            }
            
            BaseBossAttack behavior = CreateAttackBehavior(attackData.attackType);
            if (behavior != null)
            {
                behavior.Initialize(attackData, this);
                attackBehaviors.Add(behavior);
                Debug.Log($"[BossController] Added attack: {attackData.attackName} ({attackData.attackType})");
            }
        }
        
        Debug.Log($"[BossController] Total active attacks: {attackBehaviors.Count}");
    }
    
    private BaseBossAttack CreateAttackBehavior(BossAttackType attackType)
    {
        switch (attackType)
        {
            case BossAttackType.Projectile:
                return gameObject.AddComponent<BossProjectileAttack>();
            case BossAttackType.Charge:
                return gameObject.AddComponent<BossChargeAttack>();
            case BossAttackType.Slam:
                return gameObject.AddComponent<BossSlamAttack>();
            case BossAttackType.Summon:
                return gameObject.AddComponent<BossSummonAttack>();
            default:
                Debug.LogWarning($"[BossController] Unknown attack type: {attackType}");
                return null;
        }
    }
    
    private float GetHealthPercent()
    {
        if (bossHealth == null) return 1f;
        return (float)bossHealth.currentHealth.Value / bossData.maxHealth;
    }
    
    private Transform GetClosestPlayer()
    {
        if (NetworkManager.Singleton == null) return null;
        
        Transform closest = null;
        float minDist = float.MaxValue;
        
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (client.PlayerObject != null)
            {
                float dist = Vector2.Distance(transform.position, client.PlayerObject.transform.position);
                if (dist < minDist)
                {
                    minDist = dist;
                    closest = client.PlayerObject.transform;
                }
            }
        }
        return closest;
    }
    
    #region ClientRpc Methods
    
    [ClientRpc]
    public void OnPhaseChangeClientRpc(int phaseIndex)
    {
        if (bossData != null && bossData.phases != null && phaseIndex < bossData.phases.Length)
        {
            var phase = bossData.phases[phaseIndex];
            Debug.Log($"[BossController] Client: Phase changed to {phase?.name}");
            
            // Apply visual changes (like color tint)
            if (phase != null && TryGetComponent(out SpriteRenderer sr))
            {
                sr.color = phase.phaseTint;
            }
        }
    }
    
    [ClientRpc]
    public void OnAttackWindupClientRpc(Vector3 bossPos, Vector3 targetPos, float windupTime)
    {
        // Clients can show warning visuals here
        Debug.Log($"[BossController] Client: Attack windup started");
    }
    
    [ClientRpc]
    public void OnChargeStartClientRpc(Vector2 direction)
    {
        Debug.Log($"[BossController] Client: Charge started in direction {direction}");
    }
    
    [ClientRpc]
    public void OnChargeEndClientRpc()
    {
        Debug.Log($"[BossController] Client: Charge ended");
    }
    
    [ClientRpc]
    public void OnSlamWarningClientRpc(Vector3 position, float radius, float delay)
    {
        Debug.Log($"[BossController] Client: Slam warning at {position} with radius {radius}");
        // Clients could show warning indicator here
    }
    
    [ClientRpc]
    public void OnSlamImpactClientRpc(Vector3 position, float radius)
    {
        Debug.Log($"[BossController] Client: Slam impact at {position}");
        // Clients could show impact effect here
    }
    
    #endregion
    
    private void OnDrawGizmosSelected()
    {
        if (bossData != null)
        {
            // Draw aggro range
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, bossData.aggroRange);
            
            // Draw preferred distance
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, bossData.preferredDistance);
        }
    }
}
