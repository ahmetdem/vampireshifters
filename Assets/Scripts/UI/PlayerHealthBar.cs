using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Player health bar UI that displays the local player's HP.
/// Attach to a Canvas with a fill-based Image for the health bar.
/// </summary>
public class PlayerHealthBar : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image fillImage;
    [SerializeField] private TextMeshProUGUI healthText;
    
    [Header("Colors")]
    [SerializeField] private Color fullHealthColor = Color.green;
    [SerializeField] private Color lowHealthColor = Color.red;
    [SerializeField] private float lowHealthThreshold = 0.3f;
    
    [Header("Animation")]
    [SerializeField] private float lerpSpeed = 5f;
    
    private Health playerHealth;
    private float targetFill = 1f;
    private float displayedHealth;
    private float maxHealth;
    
    private void Update()
    {
        // Find local player's health if not cached
        if (playerHealth == null)
        {
            FindLocalPlayerHealth();
            return;
        }
        
        // Smoothly lerp the fill amount
        if (fillImage != null)
        {
            fillImage.fillAmount = Mathf.Lerp(fillImage.fillAmount, targetFill, Time.deltaTime * lerpSpeed);
            
            // Update color based on health percentage
            fillImage.color = Color.Lerp(lowHealthColor, fullHealthColor, fillImage.fillAmount / lowHealthThreshold);
            if (fillImage.fillAmount > lowHealthThreshold)
            {
                fillImage.color = fullHealthColor;
            }
        }
        
        // Smoothly lerp displayed health number
        displayedHealth = Mathf.Lerp(displayedHealth, playerHealth.currentHealth.Value, Time.deltaTime * lerpSpeed);
        
        // Update text
        if (healthText != null)
        {
            healthText.text = $"{Mathf.RoundToInt(displayedHealth)} / {Mathf.RoundToInt(maxHealth)}";
        }
    }
    
    private void FindLocalPlayerHealth()
    {
        if (NetworkManager.Singleton == null) return;
        if (NetworkManager.Singleton.LocalClient == null) return;
        if (NetworkManager.Singleton.LocalClient.PlayerObject == null) return;
        
        var localPlayerObj = NetworkManager.Singleton.LocalClient.PlayerObject;
        playerHealth = localPlayerObj.GetComponent<Health>();
        
        if (playerHealth != null)
        {
            // Subscribe to health changes
            playerHealth.currentHealth.OnValueChanged += OnHealthChanged;
            
            // Initialize with current values using the MaxHealth getter
            maxHealth = playerHealth.MaxHealth;
            displayedHealth = playerHealth.currentHealth.Value;
            targetFill = maxHealth > 0 ? (float)displayedHealth / maxHealth : 1f;
            
            Debug.Log($"[PlayerHealthBar] Found local player health! HP: {displayedHealth}/{maxHealth}");
        }
    }
    
    private void OnHealthChanged(int previousValue, int newValue)
    {
        // Update max health if current exceeds it (player got HP upgrade)
        if (newValue > maxHealth)
        {
            maxHealth = newValue;
        }
        
        // Calculate fill percentage
        targetFill = maxHealth > 0 ? (float)newValue / maxHealth : 0f;
    }
    
    private void OnDestroy()
    {
        // Unsubscribe to prevent memory leaks
        if (playerHealth != null)
        {
            playerHealth.currentHealth.OnValueChanged -= OnHealthChanged;
        }
    }
}
