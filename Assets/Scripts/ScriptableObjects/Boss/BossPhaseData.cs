using UnityEngine;

/// <summary>
/// Defines a phase of a boss fight.
/// Phases change the boss's available attacks and stats based on health thresholds.
/// </summary>
[CreateAssetMenu(fileName = "NewBossPhase", menuName = "Game/Boss/Phase Data")]
public class BossPhaseData : ScriptableObject
{
    [Header("Phase Trigger")]
    [Tooltip("Health percentage threshold to enter this phase (0.5 = 50% HP)")]
    [Range(0f, 1f)]
    public float healthThreshold = 1f;
    
    [Header("Attacks Available")]
    [Tooltip("Attack patterns available during this phase")]
    public BossAttackData[] attackPatterns;
    
    [Header("Stat Modifiers")]
    [Tooltip("Movement speed multiplier during this phase")]
    public float speedMultiplier = 1f;
    
    [Tooltip("Damage multiplier for all attacks during this phase")]
    public float damageMultiplier = 1f;
    
    [Tooltip("Attack cooldown multiplier (lower = faster attacks)")]
    public float cooldownMultiplier = 1f;
    
    [Header("Visuals")]
    [Tooltip("Effect played when entering this phase")]
    public GameObject phaseTransitionEffect;
    
    [Tooltip("Optional color tint for this phase (leave white for no change)")]
    public Color phaseTint = Color.white;
}
