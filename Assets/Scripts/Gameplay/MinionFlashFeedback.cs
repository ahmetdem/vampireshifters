using System.Collections;
using UnityEngine;

/// <summary>
/// Client-only visual feedback component for individual minions.
/// Flashes the sprite white/red when the minion is "hit" visually.
/// Attach to each visual minion prefab alongside MinionDamage.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class MinionFlashFeedback : MonoBehaviour
{
    [Header("Flash Settings")]
    [Tooltip("Assign a material here that renders the sprite as solid white (e.g. Unlit/Color with white texture, or GUI/Text Shader)")]
    [SerializeField] private Material flashMaterial; 
    [SerializeField] private float flashDuration = 0.1f;
    [SerializeField] private int flashCount = 1;

    private SpriteRenderer _spriteRenderer;
    private Material _originalMaterial;
    private Coroutine _flashCoroutine;

    private void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        if (_spriteRenderer != null)
        {
            _originalMaterial = _spriteRenderer.material;
        }
    }

    /// <summary>
    /// Trigger a flash effect on this minion. Call this from SwarmVisuals when damage is taken.
    /// </summary>
    public void Flash()
    {
        // Debug.Log($"[MinionFlashFeedback] Flash requested on {gameObject.name}");
        if (_flashCoroutine != null)
        {
            StopCoroutine(_flashCoroutine);
        }
        _flashCoroutine = StartCoroutine(FlashRoutine());
    }

    /// <summary>
    /// Legacy overload to keep compatibility, ignoring color for now as we use material swap.
    /// </summary>
    public void Flash(Color color)
    {
        Flash();
    }

    private IEnumerator FlashRoutine()
    {
        // Safety check
        if (flashMaterial == null)
        {
             // Fallback to simple color toggle if no material assigned (Red tint)
             _spriteRenderer.color = Color.red;
             yield return new WaitForSeconds(flashDuration);
             _spriteRenderer.color = Color.white;
             yield break;
        }

        for (int i = 0; i < flashCount; i++)
        {
            // Flash ON (Swap Material)
            SetMaterial(flashMaterial);
            yield return new WaitForSeconds(flashDuration);

            // Flash OFF (Original Material)
            SetMaterial(_originalMaterial);
            yield return new WaitForSeconds(flashDuration);
        }

        _flashCoroutine = null;
    }

    private void SetMaterial(Material mat)
    {
        if (_spriteRenderer == null || mat == null) return;
        _spriteRenderer.material = mat;
    }

    /// <summary>
    /// Reset to original state (call when pooling/reusing the minion).
    /// </summary>
    public void ResetColor()
    {
        if (_flashCoroutine != null)
        {
            StopCoroutine(_flashCoroutine);
            _flashCoroutine = null;
        }
        
        if (_spriteRenderer != null)
        {
            _spriteRenderer.color = Color.white; // Reset color just in case fallback was used
            if (_originalMaterial != null)
            {
                _spriteRenderer.material = _originalMaterial;
            }
        }
    }

    private void OnDisable()
    {
        // Clean up when disabled (e.g., returned to pool)
        ResetColor();
    }
}
