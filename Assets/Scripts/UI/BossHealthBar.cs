using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Boss health bar UI that displays at the top of the screen during boss fights.
/// Shows/hides automatically based on BossEventDirector state.
/// </summary>
public class BossHealthBar : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject barContainer;
    [SerializeField] private Image fillImage;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private TextMeshProUGUI bossNameText;
    [SerializeField] private TextMeshProUGUI healthPercentText;
    
    [Header("Colors")]
    [SerializeField] private Color healthColor = new Color(0.8f, 0.1f, 0.1f); // Dark red
    [SerializeField] private Color criticalColor = new Color(1f, 0.3f, 0.1f); // Orange-red
    [SerializeField] private float criticalThreshold = 0.25f;
    
    [Header("Animation")]
    [SerializeField] private float lerpSpeed = 3f;
    [SerializeField] private float showHideSpeed = 5f;
    
    private BossHealth currentBoss;
    private float targetFill = 1f;
    private float maxHealth;
    private CanvasGroup canvasGroup;
    private bool shouldShow = false;
    
    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        
        // Start hidden via alpha (NOT by deactivating the GameObject!)
        // Deactivating would make FindObjectOfType unable to find this component
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false; // Don't block clicks when hidden
        
        // DON'T deactivate - just hide via alpha
        // if (barContainer != null) barContainer.SetActive(false);
        
        Debug.Log("[BossHealthBar] Awake - hidden via alpha, ready to be found");
    }
    
    private void Update()
    {
        // Animate show/hide via alpha (never SetActive - keeps component findable)
        float targetAlpha = shouldShow ? 1f : 0f;
        canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, targetAlpha, Time.deltaTime * showHideSpeed);
        
        // Control raycast blocking based on visibility
        canvasGroup.blocksRaycasts = shouldShow && canvasGroup.alpha > 0.01f;
        
        // Don't process health updates when hidden
        if (!shouldShow && canvasGroup.alpha < 0.01f)
        {
            return;
        }
        
        // Update health bar fill
        if (fillImage != null && currentBoss != null)
        {
            fillImage.fillAmount = Mathf.Lerp(fillImage.fillAmount, targetFill, Time.deltaTime * lerpSpeed);
            
            // Change color at critical health
            if (fillImage.fillAmount <= criticalThreshold)
            {
                fillImage.color = criticalColor;
            }
            else
            {
                fillImage.color = healthColor;
            }
            
            // Update percentage text
            if (healthPercentText != null)
            {
                int percent = Mathf.RoundToInt(fillImage.fillAmount * 100);
                healthPercentText.text = $"{percent}%";
            }
        }
    }
    
    /// <summary>
    /// Call this when a boss fight starts to show the health bar.
    /// </summary>
    public void ShowBossHealth(BossHealth boss, string bossName)
    {
        currentBoss = boss;
        maxHealth = boss.currentHealth.Value;
        targetFill = 1f;
        shouldShow = true;
        
        if (bossNameText != null)
        {
            bossNameText.text = bossName;
        }
        
        if (fillImage != null)
        {
            fillImage.fillAmount = 1f;
            fillImage.color = healthColor;
        }
        
        // Subscribe to health changes
        boss.currentHealth.OnValueChanged += OnBossHealthChanged;
        
        Debug.Log($"[BossHealthBar] Showing health bar for: {bossName}");
    }
    
    /// <summary>
    /// Call this when boss fight ends to hide the health bar.
    /// </summary>
    public void HideBossHealth()
    {
        shouldShow = false;
        
        if (currentBoss != null)
        {
            currentBoss.currentHealth.OnValueChanged -= OnBossHealthChanged;
            currentBoss = null;
        }
        
        Debug.Log("[BossHealthBar] Hiding boss health bar");
    }
    
    private void OnBossHealthChanged(int previousValue, int newValue)
    {
        targetFill = maxHealth > 0 ? (float)newValue / maxHealth : 0f;
        targetFill = Mathf.Clamp01(targetFill);
    }
    
    private void OnDestroy()
    {
        if (currentBoss != null)
        {
            currentBoss.currentHealth.OnValueChanged -= OnBossHealthChanged;
        }
    }
}
