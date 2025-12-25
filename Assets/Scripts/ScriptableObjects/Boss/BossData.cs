using UnityEngine;

/// <summary>
/// Main configuration for a boss enemy.
/// Assign this to BossController to create different boss types.
/// </summary>
[CreateAssetMenu(fileName = "NewBoss", menuName = "Game/Boss/Boss Data")]
public class BossData : ScriptableObject
{
    [Header("Identity")]
    public string bossName = "New Boss";
    public Sprite icon;
    
    [Header("Base Stats")]
    [Tooltip("Maximum health of the boss")]
    public int maxHealth = 1000;
    
    [Tooltip("Movement speed")]
    public float moveSpeed = 2f;
    
    [Tooltip("Damage dealt on contact with players")]
    public int contactDamage = 15;
    
    [Tooltip("Distance at which boss starts chasing players")]
    public float aggroRange = 20f;
    
    [Tooltip("Preferred distance to maintain from target")]
    public float preferredDistance = 5f;
    
    [Header("Phases")]
    [Tooltip("Boss phases in order of health thresholds (highest to lowest). Leave empty for no phases.")]
    public BossPhaseData[] phases;
    
    [Header("Default Attacks (No Phases)")]
    [Tooltip("Attack patterns if not using phases")]
    public BossAttackData[] defaultAttacks;
    
    [Header("Behavior")]
    [Tooltip("Time between attack attempts")]
    public float attackInterval = 2f;
    
    [Tooltip("If true, boss will move toward players")]
    public bool chasesPlayers = true;
    
    [Tooltip("Minimum time between phase transitions")]
    public float phaseTransitionCooldown = 1f;
    
    [Header("Visual Effects")]
    [Tooltip("Effect when boss spawns")]
    public GameObject spawnEffectPrefab;
    
    [Tooltip("Effect when boss dies")]
    public GameObject deathEffectPrefab;
    
    /// <summary>
    /// Get the phase that should be active at the given health percentage.
    /// Phase threshold means "activate when health drops TO OR BELOW this value"
    /// E.g., Phase1 threshold=1.0 (always active at 100%), Phase2 threshold=0.5 (active at 50% or less)
    /// </summary>
    public BossPhaseData GetPhaseForHealth(float healthPercent)
    {
        if (phases == null || phases.Length == 0) 
        {
            Debug.Log($"[BossData] No phases configured, using defaultAttacks");
            return null;
        }
        
        // Find the phase with the LOWEST threshold that health is still at or below
        // Example: healthPercent = 0.4 (40% health)
        // Phase 1: threshold 1.0 -> 0.4 <= 1.0 ✓
        // Phase 2: threshold 0.5 -> 0.4 <= 0.5 ✓ <- Pick this one (lower threshold)
        // Phase 3: threshold 0.25 -> 0.4 <= 0.25 ✗
        
        BossPhaseData bestPhase = null;
        
        foreach (var phase in phases)
        {
            if (phase != null && healthPercent <= phase.healthThreshold)
            {
                if (bestPhase == null || phase.healthThreshold < bestPhase.healthThreshold)
                {
                    bestPhase = phase;
                }
            }
        }
        
        Debug.Log($"[BossData] GetPhaseForHealth({healthPercent:F2}) -> {bestPhase?.name ?? "null"}");
        return bestPhase;
    }
    
    /// <summary>
    /// Get attack patterns for the current health percentage.
    /// Uses phase attacks if phases are defined, otherwise uses default attacks.
    /// </summary>
    public BossAttackData[] GetAttacksForHealth(float healthPercent)
    {
        var phase = GetPhaseForHealth(healthPercent);
        if (phase != null && phase.attackPatterns != null && phase.attackPatterns.Length > 0)
        {
            return phase.attackPatterns;
        }
        return defaultAttacks;
    }
}
