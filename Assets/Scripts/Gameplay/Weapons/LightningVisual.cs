using Unity.Netcode;
using UnityEngine;

public class LightningVisual : NetworkBehaviour
{
    [SerializeField] private float duration = 0.3f;
    [SerializeField] private float startWidth = 0.15f;
    [SerializeField] private Color startColor = new Color(1f, 1f, 0.5f, 1f); // Yellow-ish

    private SpriteRenderer spriteRenderer;
    private float timer;
    private Vector3 startScale;

    public override void OnNetworkSpawn()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.color = startColor;
        }
        startScale = transform.localScale;
    }

    private void Update()
    {
        timer += Time.deltaTime;
        float progress = timer / duration;

        if (progress <= 1f)
        {
            // Fade out alpha
            if (spriteRenderer != null)
            {
                Color c = spriteRenderer.color;
                c.a = 1f - progress;
                spriteRenderer.color = c;
            }

            // Shrink width (Y scale) while keeping length
            float widthMultiplier = 1f - (progress * 0.5f); // Shrinks to 50% width
            transform.localScale = new Vector3(
                startScale.x,
                startScale.y * widthMultiplier,
                startScale.z
            );
        }

        // Despawn after duration (server only)
        if (IsServer && timer >= duration)
        {
            if (IsSpawned) NetworkObject.Despawn();
        }
    }

    /// <summary>
    /// Sets up the lightning bolt to stretch from origin to target.
    /// Call this right after spawning.
    /// </summary>
    public void SetupBolt(Vector3 origin, Vector3 target)
    {
        // Position at midpoint
        Vector3 midpoint = (origin + target) / 2f;
        transform.position = midpoint;

        // Calculate direction and distance
        Vector3 direction = target - origin;
        float distance = direction.magnitude;

        // Rotate to face the target
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);

        // Scale: X = length (distance), Y = width
        transform.localScale = new Vector3(distance, startWidth, 1f);
        startScale = transform.localScale;
    }
}
