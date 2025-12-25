using UnityEngine;

public class SwarmVisuals : MonoBehaviour
{
    [SerializeField] private GameObject visualPrefab;
    [SerializeField] private int swarmCount = 1;
    [SerializeField] private float swarmSpread = 2f; // The "Radius" of the visual group

    [Header("Visual Feedback")]
    [Tooltip("Color to flash when taking damage")]
    [SerializeField] private Color damageFlashColor = Color.white;
    [Tooltip("Number of minions to flash when damage is taken (0 = flash one random minion)")]
    [SerializeField] private int flashCountOnDamage = 1;

    [Header("Rendering Optimization")]
    [Tooltip("Distance from camera beyond which minions are hidden")]
    [SerializeField] private float renderDistance = 100f;
    [SerializeField] private float renderCheckInterval = 0.5f;

    private GameObject[] _minions;
    private SpriteRenderer[] _minionRenderers;
    private MinionFlashFeedback[] _minionFlashFeedbacks;
    private SwarmController _swarmController;
    private float _renderCheckTimer;
    private bool _isVisible = true;

    public float GetSwarmSpread() => swarmSpread; // Getter for the controller
    public GameObject[] GetMinions() => _minions; // For visual feedback access

    /// <summary>
    /// Must be called before Start() to set up damage references.
    /// </summary>
    public void SetSwarmController(SwarmController controller)
    {
        _swarmController = controller;
    }

    private void Start()
    {
        SpawnMinions();
    }
    
    private void SpawnMinions()
    {
        _minions = new GameObject[swarmCount];
        _minionRenderers = new SpriteRenderer[swarmCount];
        _minionFlashFeedbacks = new MinionFlashFeedback[swarmCount];
        
        // Use position-based seed so both host and client generate same "random" positions
        int seed = Mathf.RoundToInt(transform.position.x * 1000 + transform.position.y * 7919);
        System.Random seededRandom = new System.Random(seed);
        
        for (int i = 0; i < swarmCount; i++)
        {
            // Generate consistent "random" offset using seeded random
            float angle = (float)(seededRandom.NextDouble() * Mathf.PI * 2);
            float radius = (float)(seededRandom.NextDouble() * swarmSpread);
            Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            
            _minions[i] = Instantiate(visualPrefab, transform.position + (Vector3)offset, Quaternion.identity);
            _minions[i].transform.SetParent(transform);
            
            // Ensure minions have Enemy tag for weapon detection
            _minions[i].tag = "Enemy";
            
            // Cache renderer for optimization
            _minionRenderers[i] = _minions[i].GetComponent<SpriteRenderer>();
            
            // Cache flash feedback component (must be added to prefab manually for customization)
            _minionFlashFeedbacks[i] = _minions[i].GetComponent<MinionFlashFeedback>();
            
            // Initialize the MinionDamage component (should already exist on prefab)
            if (_minions[i].TryGetComponent(out MinionDamage damage))
            {
                damage.Initialize(_swarmController);
            }
            else
            {
                Debug.LogWarning($"[SwarmVisuals] Visual prefab '{visualPrefab.name}' is missing MinionDamage component!");
            }
        }
    }

    private void Update()
    {
        // Periodically check distance to camera for render optimization
        _renderCheckTimer -= Time.deltaTime;
        if (_renderCheckTimer <= 0f)
        {
            _renderCheckTimer = renderCheckInterval;
            UpdateRenderVisibility();
        }
    }

    private void UpdateRenderVisibility()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        float distSqr = (transform.position - cam.transform.position).sqrMagnitude;
        bool shouldBeVisible = distSqr <= (renderDistance * renderDistance);

        if (shouldBeVisible != _isVisible)
        {
            _isVisible = shouldBeVisible;
            SetRenderersEnabled(_isVisible);
        }
    }

    private void SetRenderersEnabled(bool enabled)
    {
        if (_minionRenderers == null) return;
        
        foreach (var renderer in _minionRenderers)
        {
            if (renderer != null)
            {
                renderer.enabled = enabled;
            }
        }
    }

    public void SetSwarmDensity(float multiplier)
    {
        // 1. Calculate new count
        int newCount = Mathf.RoundToInt(swarmCount * multiplier);

        // 2. FIX: Clamp to 1 instead of 5 so you can test small groups
        swarmCount = Mathf.Clamp(newCount, 1, 50);

        // Note: If this runs after Start(), you might need to manually trigger a respawn 
        // of the visual minions here, otherwise this number only changes for the NEXT wave.
    }

    #region Visual Feedback System (Client-Only)

    /// <summary>
    /// Call this when the swarm takes damage to trigger visual feedback.
    /// Flashes random minion(s) to give visual indication of damage.
    /// </summary>
    public void OnDamageTaken()
    {
        int count = flashCountOnDamage <= 0 ? 1 : flashCountOnDamage;
        for (int i = 0; i < count; i++)
        {
            FlashRandomMinion();
        }
    }

    /// <summary>
    /// Flash a random minion with the damage color.
    /// </summary>
    public void FlashRandomMinion()
    {
        if (_minionFlashFeedbacks == null || _minionFlashFeedbacks.Length == 0) return;
        
        int randomIndex = Random.Range(0, _minionFlashFeedbacks.Length);
        FlashMinion(randomIndex, damageFlashColor);
    }

    /// <summary>
    /// Flash a specific minion by index.
    /// </summary>
    public void FlashMinion(int index)
    {
        FlashMinion(index, damageFlashColor);
    }

    /// <summary>
    /// Flash a specific minion with a custom color.
    /// </summary>
    public void FlashMinion(int index, Color color)
    {
        if (_minionFlashFeedbacks == null) return;
        if (index < 0 || index >= _minionFlashFeedbacks.Length) return;
        
        var feedback = _minionFlashFeedbacks[index];
        if (feedback != null)
        {
            feedback.Flash(color);
        }
    }

    /// <summary>
    /// Flash all minions at once (for big hits or death).
    /// </summary>
    public void FlashAllMinions()
    {
        FlashAllMinions(damageFlashColor);
    }

    /// <summary>
    /// Flash all minions with a custom color.
    /// </summary>
    public void FlashAllMinions(Color color)
    {
        if (_minionFlashFeedbacks == null) return;
        
        foreach (var feedback in _minionFlashFeedbacks)
        {
            if (feedback != null)
            {
                feedback.Flash(color);
            }
        }
    }

    #endregion
}

