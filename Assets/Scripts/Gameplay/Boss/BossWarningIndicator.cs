using UnityEngine;

/// <summary>
/// Visual warning indicator for boss attacks.
/// Shows a circle that shrinks/pulses before an attack hits.
/// Attach to a simple sprite circle and configure in BossAttackData.
/// </summary>
public class BossWarningIndicator : MonoBehaviour
{
    [Header("Animation")]
    [SerializeField] private bool pulseAnimation = true;
    [SerializeField] private float pulseSpeed = 3f;
    [SerializeField] private float pulseMinScale = 0.8f;
    [SerializeField] private float pulseMaxScale = 1.0f;
    
    [Header("Fade")]
    [SerializeField] private bool fadeIn = true;
    [SerializeField] private float fadeInDuration = 0.3f;
    
    private SpriteRenderer spriteRenderer;
    private float startTime;
    private Vector3 baseScale;
    private Color baseColor;
    
    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        baseScale = transform.localScale;
        startTime = Time.time;
        
        if (spriteRenderer != null)
        {
            baseColor = spriteRenderer.color;
            if (fadeIn)
            {
                spriteRenderer.color = new Color(baseColor.r, baseColor.g, baseColor.b, 0f);
            }
        }
    }
    
    private void Update()
    {
        float elapsed = Time.time - startTime;
        
        // Fade in
        if (fadeIn && spriteRenderer != null && elapsed < fadeInDuration)
        {
            float alpha = elapsed / fadeInDuration;
            spriteRenderer.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha * baseColor.a);
        }
        
        // Pulse animation
        if (pulseAnimation)
        {
            float pulse = Mathf.Lerp(pulseMinScale, pulseMaxScale, (Mathf.Sin(elapsed * pulseSpeed) + 1f) / 2f);
            transform.localScale = baseScale * pulse;
        }
    }
}
