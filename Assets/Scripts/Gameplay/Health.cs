using Unity.Netcode;
using UnityEngine;

public class Health : NetworkBehaviour
{
    [SerializeField] private int maxHealth = 100;
    public NetworkVariable<int> currentHealth = new NetworkVariable<int>(100);

    // Flash state tracking
    private Coroutine _flashCoroutine;
    private Color _originalColor = Color.white;
    private SpriteRenderer _cachedSpriteRenderer;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            // FORCE the network variable to match the Inspector setting
            currentHealth.Value = maxHealth;
        }
    }

    public void TakeDamage(int damage)
    {
        if (!IsServer) return;

        currentHealth.Value -= damage;
        
        // Trigger visual feedback on ALL clients (including host)
        TriggerDamageVisualClientRpc();

        if (currentHealth.Value <= 0)
        {
            Die();
        }
    }

    /// <summary>
    /// Called on all clients to trigger visual damage feedback.
    /// </summary>
    [ClientRpc]
    private void TriggerDamageVisualClientRpc()
    {
        // Try SwarmVisuals first (for swarm enemies)
        if (TryGetComponent(out SwarmVisuals swarmVisuals))
        {
            swarmVisuals.OnDamageTaken();
        }
        // Try SpriteRenderer for simple flash (for non-swarm enemies like boss summons and players)
        else if (TryGetComponent(out SpriteRenderer spriteRenderer))
        {
            // Cache sprite renderer and original color on first use
            if (_cachedSpriteRenderer == null)
            {
                _cachedSpriteRenderer = spriteRenderer;
                _originalColor = spriteRenderer.color;
            }
            
            // Stop existing flash before starting new one
            if (_flashCoroutine != null)
            {
                StopCoroutine(_flashCoroutine);
                _cachedSpriteRenderer.color = _originalColor;
            }
            
            _flashCoroutine = StartCoroutine(SimpleFlash());
        }
    }

    private System.Collections.IEnumerator SimpleFlash()
    {
        if (_cachedSpriteRenderer == null) yield break;
        
        _cachedSpriteRenderer.color = Color.red;
        yield return new WaitForSeconds(0.2f);
        _cachedSpriteRenderer.color = _originalColor;
        _flashCoroutine = null;
    }

    protected virtual void Die()
    {
        if (TryGetComponent(out PlayerNetworkState playerState))
        {
            // Player Death Logic (Respawn)...
            ConnectionHandler.Instance.HandlePlayerDeath(OwnerClientId);
            GetComponent<NetworkObject>().Despawn(true);
        }
        else
        {
            // Enemy Death Logic
            if (TryGetComponent(out LootDropper loot))
            {
                loot.DropLoot();
            }

            Debug.Log($"[Health] Enemy {NetworkObjectId} Died.");
            GetComponent<NetworkObject>().Despawn(true);
        }
    }

    // Add this anywhere inside the class
    public void Heal(int amount)
    {
        if (!IsServer) return;

        // Use the variable name defined above
        currentHealth.Value = Mathf.Clamp(currentHealth.Value + amount, 0, maxHealth);
    }

    public void IncreaseMaxHealth(int amount)
    {
        if (!IsServer) return;

        maxHealth += amount;
        currentHealth.Value += amount;
    }

    /// <summary>
    /// Reset health to max. Used by object pooling when recycling enemies.
    /// </summary>
    public void ResetHealth()
    {
        if (!IsServer) return;
        currentHealth.Value = maxHealth;
    }
}
