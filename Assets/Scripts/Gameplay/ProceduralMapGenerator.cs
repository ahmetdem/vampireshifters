using UnityEngine;
using UnityEngine.Tilemaps;
using Unity.Netcode;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Procedurally generates a tilemap at runtime using Perlin noise.
/// Can also bake a map in the editor for static use (recommended for multiplayer).
/// </summary>
public class ProceduralMapGenerator : NetworkBehaviour
{
    [Header("Required References")]
    [SerializeField] private Tilemap groundTilemap;
    [SerializeField] private Tilemap decorationTilemap;
    [SerializeField] private Tilemap collisionTilemap;
    
    [Header("Configuration")]
    [SerializeField] private MapGeneratorConfig config;
    
    [Header("Baked Map Mode")]
    [Tooltip("If true, map is already baked into the scene - skip runtime generation")]
    [SerializeField] private bool useBakedMap = false;
    
    [Header("Generation Settings (only used if useBakedMap is false)")]
    [Tooltip("If true, generates map on Start. If false, call GenerateMap() manually.")]
    [SerializeField] private bool generateOnStart = true;
    
    [Tooltip("If true, uses a random seed each time. If false, uses fixedSeed.")]
    [SerializeField] private bool useRandomSeed = true;
    
    [Tooltip("Fixed seed for reproducible maps (only used if useRandomSeed is false)")]
    [SerializeField] private int fixedSeed = 12345;

    [Header("Boundary Settings")]
    [Tooltip("Generate invisible walls around map edges to keep player inside")]
    [SerializeField] private bool generateBoundaryWalls = true;
    
    [Tooltip("Generate a camera bounds collider for Cinemachine Confiner")]
    [SerializeField] private bool generateCameraBounds = true;
    
    [Tooltip("How much padding inside the map edge for camera bounds (in tiles)")]
    [SerializeField] private float cameraBoundsPadding = 5f;

    // Network synced seed
    private NetworkVariable<int> networkSeed = new NetworkVariable<int>(
        0, 
        NetworkVariableReadPermission.Everyone, 
        NetworkVariableWritePermission.Server
    );

    private int currentSeed;
    private bool hasGenerated = false;
    
    // Generated boundary objects
    private GameObject boundaryWallsObject;
    private GameObject cameraBoundsObject;

    /// <summary>
    /// Returns the camera bounds collider for Cinemachine Confiner.
    /// </summary>
    public Collider2D CameraBoundsCollider { get; private set; }

    private void Start()
    {
        // If using baked map, only generate runtime objects (boundaries, camera bounds)
        if (useBakedMap)
        {
            Debug.Log("[ProceduralMapGenerator] Using baked map - skipping tile generation");
            
            // Still need to generate boundaries at runtime (they're not saved in scene)
            if (generateBoundaryWalls)
            {
                GenerateBoundaryWalls();
            }
            if (generateCameraBounds)
            {
                GenerateCameraBounds();
            }
            return;
        }

        if (generateOnStart)
        {
            // In multiplayer, wait for network seed
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            {
                if (IsServer)
                {
                    // Host/Server generates seed and map
                    currentSeed = useRandomSeed ? Random.Range(int.MinValue, int.MaxValue) : fixedSeed;
                    networkSeed.Value = currentSeed;
                    GenerateMap(currentSeed);
                }
                else
                {
                    // Client waits for seed then generates
                    networkSeed.OnValueChanged += OnSeedChanged;
                    if (networkSeed.Value != 0)
                    {
                        GenerateMap(networkSeed.Value);
                    }
                }
            }
            else
            {
                // Singleplayer - generate immediately
                currentSeed = useRandomSeed ? Random.Range(int.MinValue, int.MaxValue) : fixedSeed;
                GenerateMap(currentSeed);
            }
        }
    }

    private void OnSeedChanged(int previousValue, int newValue)
    {
        if (!hasGenerated && newValue != 0)
        {
            GenerateMap(newValue);
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        // If client joins late and seed is already set
        if (!IsServer && networkSeed.Value != 0 && !hasGenerated)
        {
            GenerateMap(networkSeed.Value);
        }
    }

    // How many tiles to process per frame before yielding (prevents Relay timeout)
    private const int TILES_PER_FRAME = 5000;

    /// <summary>
    /// Generates the map with the given seed.
    /// </summary>
    public void GenerateMap(int seed)
    {
        StartCoroutine(GenerateMapCoroutine(seed));
    }

    private IEnumerator GenerateMapCoroutine(int seed)
    {
        if (config == null)
        {
            Debug.LogError("ProceduralMapGenerator: No MapGeneratorConfig assigned!");
            yield break;
        }

        hasGenerated = true;
        currentSeed = seed;
        Random.InitState(seed);
        
        Debug.Log($"[ProceduralMapGenerator] Starting async map generation with seed: {seed}");

        // Clear existing tiles and boundaries
        ClearAllTilemaps();
        ClearBoundaries();

        // Generate noise offset for variety
        float noiseOffsetX = Random.Range(0f, 10000f);
        float noiseOffsetY = Random.Range(0f, 10000f);

        // Generate ground layer (yields periodically)
        yield return StartCoroutine(GenerateGroundLayerCoroutine(noiseOffsetX, noiseOffsetY));

        // Generate decorations (yields periodically)
        yield return StartCoroutine(GenerateDecorationsCoroutine(noiseOffsetX, noiseOffsetY));

        // Generate boundary walls (fast, no yield needed)
        if (generateBoundaryWalls)
        {
            GenerateBoundaryWalls();
        }

        // Generate camera bounds (fast, no yield needed)
        if (generateCameraBounds)
        {
            GenerateCameraBounds();
        }

        Debug.Log($"[ProceduralMapGenerator] Map generation complete! Size: {config.mapWidth}x{config.mapHeight}");
    }

    private void ClearAllTilemaps()
    {
        if (groundTilemap != null) groundTilemap.ClearAllTiles();
        if (decorationTilemap != null) decorationTilemap.ClearAllTiles();
        if (collisionTilemap != null) collisionTilemap.ClearAllTiles();
    }

    private void ClearBoundaries()
    {
        if (boundaryWallsObject != null)
        {
            Destroy(boundaryWallsObject);
            boundaryWallsObject = null;
        }
        if (cameraBoundsObject != null)
        {
            Destroy(cameraBoundsObject);
            cameraBoundsObject = null;
            CameraBoundsCollider = null;
        }
    }

    private void GenerateBoundaryWalls()
    {
        // Create parent object for walls
        boundaryWallsObject = new GameObject("MapBoundaryWalls");
        boundaryWallsObject.transform.SetParent(transform);
        boundaryWallsObject.layer = LayerMask.NameToLayer("Default");

        float halfWidth = config.mapWidth / 2f;
        float halfHeight = config.mapHeight / 2f;
        float wallThickness = 1f;

        // Create 4 wall colliders (top, bottom, left, right)
        CreateWall("TopWall", new Vector2(0, halfHeight + wallThickness / 2f), new Vector2(config.mapWidth + 2, wallThickness));
        CreateWall("BottomWall", new Vector2(0, -halfHeight - wallThickness / 2f), new Vector2(config.mapWidth + 2, wallThickness));
        CreateWall("LeftWall", new Vector2(-halfWidth - wallThickness / 2f, 0), new Vector2(wallThickness, config.mapHeight + 2));
        CreateWall("RightWall", new Vector2(halfWidth + wallThickness / 2f, 0), new Vector2(wallThickness, config.mapHeight + 2));

        Debug.Log("[ProceduralMapGenerator] Boundary walls generated");
    }

    private void CreateWall(string name, Vector2 position, Vector2 size)
    {
        GameObject wall = new GameObject(name);
        wall.transform.SetParent(boundaryWallsObject.transform);
        wall.transform.position = position;
        
        BoxCollider2D collider = wall.AddComponent<BoxCollider2D>();
        collider.size = size;
    }

    private void GenerateCameraBounds()
    {
        // Create camera bounds object with PolygonCollider2D for Cinemachine Confiner
        cameraBoundsObject = new GameObject("CameraBounds");
        cameraBoundsObject.transform.SetParent(transform);
        cameraBoundsObject.transform.position = Vector3.zero;

        float halfWidth = (config.mapWidth / 2f) - cameraBoundsPadding;
        float halfHeight = (config.mapHeight / 2f) - cameraBoundsPadding;

        PolygonCollider2D poly = cameraBoundsObject.AddComponent<PolygonCollider2D>();
        poly.isTrigger = true; // Cinemachine Confiner uses trigger colliders
        
        // Define the bounds as a rectangle
        Vector2[] points = new Vector2[]
        {
            new Vector2(-halfWidth, -halfHeight), // Bottom-left
            new Vector2(-halfWidth, halfHeight),  // Top-left
            new Vector2(halfWidth, halfHeight),   // Top-right
            new Vector2(halfWidth, -halfHeight)   // Bottom-right
        };
        poly.SetPath(0, points);

        CameraBoundsCollider = poly;

        Debug.Log("[ProceduralMapGenerator] Camera bounds generated. Assign CameraBoundsCollider to Cinemachine Confiner.");
    }


    private IEnumerator GenerateGroundLayerCoroutine(float noiseOffsetX, float noiseOffsetY)
    {
        if (groundTilemap == null) yield break;

        int halfWidth = config.mapWidth / 2;
        int halfHeight = config.mapHeight / 2;
        int tilesProcessed = 0;

        for (int x = -halfWidth; x < halfWidth; x++)
        {
            for (int y = -halfHeight; y < halfHeight; y++)
            {
                Vector3Int tilePos = new Vector3Int(x, y, 0);
                
                // Sample Perlin noise
                float noiseValue = Mathf.PerlinNoise(
                    (x + noiseOffsetX) * config.noiseScale,
                    (y + noiseOffsetY) * config.noiseScale
                );

                // Determine tile type based on noise
                TileBase tileToPlace;
                
                if (noiseValue > config.grassThreshold)
                {
                    // Grass area - use fill or variation
                    if (config.groundVariationTiles != null && config.groundVariationTiles.Length > 0 
                        && Random.value < 0.1f)
                    {
                        tileToPlace = config.groundVariationTiles[Random.Range(0, config.groundVariationTiles.Length)];
                    }
                    else
                    {
                        tileToPlace = config.grassFillTile;
                    }
                }
                else
                {
                    // Dirt area
                    tileToPlace = config.dirtFillTile;
                }

                if (tileToPlace != null)
                {
                    groundTilemap.SetTile(tilePos, tileToPlace);
                }

                // Yield periodically to allow network updates
                tilesProcessed++;
                if (tilesProcessed >= TILES_PER_FRAME)
                {
                    tilesProcessed = 0;
                    yield return null;
                }
            }
        }

        // Apply edge tiles for smooth transitions
        yield return StartCoroutine(ApplyEdgeTilesCoroutine(noiseOffsetX, noiseOffsetY));
    }

    private IEnumerator ApplyEdgeTilesCoroutine(float noiseOffsetX, float noiseOffsetY)
    {
        if (config.grassEdgeTiles == null || config.grassEdgeTiles.Length < 9) yield break;

        int halfWidth = config.mapWidth / 2;
        int halfHeight = config.mapHeight / 2;
        int tilesProcessed = 0;

        // Edge tile mapping based on neighbor analysis
        // Tiles should be arranged in the array as:
        // 0: top-left corner, 1: top, 2: top-right corner
        // 3: left, 4: center (fill), 5: right
        // 6: bottom-left corner, 7: bottom, 8: bottom-right corner

        for (int x = -halfWidth; x < halfWidth; x++)
        {
            for (int y = -halfHeight; y < halfHeight; y++)
            {
                float centerNoise = GetNoiseAt(x, y, noiseOffsetX, noiseOffsetY);
                
                // Only process grass tiles at edges
                if (centerNoise <= config.grassThreshold) continue;

                // Check neighbors
                bool topGrass = GetNoiseAt(x, y + 1, noiseOffsetX, noiseOffsetY) > config.grassThreshold;
                bool bottomGrass = GetNoiseAt(x, y - 1, noiseOffsetX, noiseOffsetY) > config.grassThreshold;
                bool leftGrass = GetNoiseAt(x - 1, y, noiseOffsetX, noiseOffsetY) > config.grassThreshold;
                bool rightGrass = GetNoiseAt(x + 1, y, noiseOffsetX, noiseOffsetY) > config.grassThreshold;

                // Determine which edge tile to use
                int tileIndex = GetEdgeTileIndex(topGrass, bottomGrass, leftGrass, rightGrass);
                
                if (tileIndex >= 0 && tileIndex < config.grassEdgeTiles.Length && 
                    config.grassEdgeTiles[tileIndex] != null)
                {
                    Vector3Int tilePos = new Vector3Int(x, y, 0);
                    groundTilemap.SetTile(tilePos, config.grassEdgeTiles[tileIndex]);
                }

                // Yield periodically to allow network updates
                tilesProcessed++;
                if (tilesProcessed >= TILES_PER_FRAME)
                {
                    tilesProcessed = 0;
                    yield return null;
                }
            }
        }
    }

    private float GetNoiseAt(int x, int y, float offsetX, float offsetY)
    {
        return Mathf.PerlinNoise(
            (x + offsetX) * config.noiseScale,
            (y + offsetY) * config.noiseScale
        );
    }

    private int GetEdgeTileIndex(bool top, bool bottom, bool left, bool right)
    {
        // All neighbors are grass - center fill tile
        if (top && bottom && left && right) return 4;
        
        // Corners
        if (!top && !left && bottom && right) return 0;  // top-left corner
        if (!top && left && bottom && !right) return 2;  // top-right corner
        if (top && !left && !bottom && right) return 6;  // bottom-left corner
        if (top && left && !bottom && !right) return 8;  // bottom-right corner
        
        // Edges
        if (!top && left && bottom && right) return 1;   // top edge
        if (top && left && !bottom && right) return 7;   // bottom edge
        if (top && !left && bottom && right) return 3;   // left edge
        if (top && left && bottom && !right) return 5;   // right edge

        return 4; // Default to center
    }

    private IEnumerator GenerateDecorationsCoroutine(float noiseOffsetX, float noiseOffsetY)
    {
        if (decorationTilemap == null) yield break;

        int halfWidth = config.mapWidth / 2;
        int halfHeight = config.mapHeight / 2;
        int centerX = 0;
        int centerY = 0;
        int tilesProcessed = 0;

        for (int x = -halfWidth; x < halfWidth; x++)
        {
            for (int y = -halfHeight; y < halfHeight; y++)
            {
                Vector3Int tilePos = new Vector3Int(x, y, 0);
                
                // Check if in spawn safe zone
                float distFromCenter = Mathf.Sqrt((x - centerX) * (x - centerX) + (y - centerY) * (y - centerY));
                bool inSafeZone = distFromCenter < config.spawnSafeRadius;

                // Only place decorations on grass areas
                float noiseValue = GetNoiseAt(x, y, noiseOffsetX, noiseOffsetY);
                if (noiseValue <= config.grassThreshold) continue;

                // Try to place large decoration (not in safe zone)
                if (!inSafeZone && config.largeDecorations != null && config.largeDecorations.Length > 0)
                {
                    if (Random.value < config.largeDecorationChance)
                    {
                        TileBase largeDeco = config.largeDecorations[Random.Range(0, config.largeDecorations.Length)];
                        if (largeDeco != null)
                        {
                            decorationTilemap.SetTile(tilePos, largeDeco);
                            
                            // Also add collision
                            if (collisionTilemap != null)
                            {
                                collisionTilemap.SetTile(tilePos, largeDeco);
                            }
                            goto NextTile; // Don't place other decorations here
                        }
                    }
                }

                // Try to place medium decoration
                if (config.mediumDecorations != null && config.mediumDecorations.Length > 0)
                {
                    if (Random.value < config.mediumDecorationChance)
                    {
                        TileBase medDeco = config.mediumDecorations[Random.Range(0, config.mediumDecorations.Length)];
                        if (medDeco != null)
                        {
                            decorationTilemap.SetTile(tilePos, medDeco);
                            goto NextTile;
                        }
                    }
                }

                // Try to place small decoration
                if (config.smallDecorations != null && config.smallDecorations.Length > 0)
                {
                    if (Random.value < config.smallDecorationChance)
                    {
                        TileBase smallDeco = config.smallDecorations[Random.Range(0, config.smallDecorations.Length)];
                        if (smallDeco != null)
                        {
                            decorationTilemap.SetTile(tilePos, smallDeco);
                        }
                    }
                }

                NextTile:
                // Yield periodically to allow network updates
                tilesProcessed++;
                if (tilesProcessed >= TILES_PER_FRAME)
                {
                    tilesProcessed = 0;
                    yield return null;
                }
            }
        }
    }

    /// <summary>
    /// Regenerates the map with a new random seed.
    /// </summary>
    [ContextMenu("Regenerate Map")]
    public void RegenerateMap()
    {
        int newSeed = Random.Range(int.MinValue, int.MaxValue);
        
        if (IsServer || !NetworkManager.Singleton?.IsListening == true)
        {
            if (IsServer)
            {
                networkSeed.Value = newSeed;
            }
            GenerateMap(newSeed);
        }
    }

    public override void OnDestroy()
    {
        networkSeed.OnValueChanged -= OnSeedChanged;
        base.OnDestroy();
    }

#if UNITY_EDITOR
    /// <summary>
    /// Bakes the map in the editor. Tiles are saved permanently to the scene.
    /// After baking, enable "Use Baked Map" to skip runtime generation.
    /// </summary>
    [ContextMenu("Bake Map In Editor")]
    public void BakeMapInEditor()
    {
        if (config == null)
        {
            Debug.LogError("ProceduralMapGenerator: No MapGeneratorConfig assigned!");
            return;
        }

        if (Application.isPlaying)
        {
            Debug.LogError("Cannot bake map while in Play mode. Exit Play mode first.");
            return;
        }

        int seed = useRandomSeed ? Random.Range(int.MinValue, int.MaxValue) : fixedSeed;
        Debug.Log($"[ProceduralMapGenerator] Baking map with seed: {seed}");

        // Clear existing tiles
        if (groundTilemap != null) groundTilemap.ClearAllTiles();
        if (decorationTilemap != null) decorationTilemap.ClearAllTiles();
        if (collisionTilemap != null) collisionTilemap.ClearAllTiles();

        Random.InitState(seed);

        // Generate noise offset
        float noiseOffsetX = Random.Range(0f, 10000f);
        float noiseOffsetY = Random.Range(0f, 10000f);

        // Generate tiles (synchronous in editor)
        BakeGroundLayer(noiseOffsetX, noiseOffsetY);
        BakeEdgeTiles(noiseOffsetX, noiseOffsetY);
        BakeDecorations(noiseOffsetX, noiseOffsetY);

        // Mark tilemaps as dirty so they save
        EditorUtility.SetDirty(groundTilemap);
        if (decorationTilemap != null) EditorUtility.SetDirty(decorationTilemap);
        if (collisionTilemap != null) EditorUtility.SetDirty(collisionTilemap);

        // Auto-enable baked map mode
        useBakedMap = true;
        EditorUtility.SetDirty(this);

        Debug.Log($"[ProceduralMapGenerator] Map baked successfully! Size: {config.mapWidth}x{config.mapHeight}");
        Debug.Log("[ProceduralMapGenerator] Remember to save the scene (Ctrl+S) to keep the baked tiles!");
    }

    private void BakeGroundLayer(float noiseOffsetX, float noiseOffsetY)
    {
        if (groundTilemap == null) return;

        int halfWidth = config.mapWidth / 2;
        int halfHeight = config.mapHeight / 2;

        for (int x = -halfWidth; x < halfWidth; x++)
        {
            for (int y = -halfHeight; y < halfHeight; y++)
            {
                Vector3Int tilePos = new Vector3Int(x, y, 0);
                
                float noiseValue = Mathf.PerlinNoise(
                    (x + noiseOffsetX) * config.noiseScale,
                    (y + noiseOffsetY) * config.noiseScale
                );

                TileBase tileToPlace;
                
                if (noiseValue > config.grassThreshold)
                {
                    if (config.groundVariationTiles != null && config.groundVariationTiles.Length > 0 
                        && Random.value < 0.1f)
                    {
                        tileToPlace = config.groundVariationTiles[Random.Range(0, config.groundVariationTiles.Length)];
                    }
                    else
                    {
                        tileToPlace = config.grassFillTile;
                    }
                }
                else
                {
                    tileToPlace = config.dirtFillTile;
                }

                if (tileToPlace != null)
                {
                    groundTilemap.SetTile(tilePos, tileToPlace);
                }
            }
        }
    }

    private void BakeEdgeTiles(float noiseOffsetX, float noiseOffsetY)
    {
        if (config.grassEdgeTiles == null || config.grassEdgeTiles.Length < 9) return;

        int halfWidth = config.mapWidth / 2;
        int halfHeight = config.mapHeight / 2;

        for (int x = -halfWidth; x < halfWidth; x++)
        {
            for (int y = -halfHeight; y < halfHeight; y++)
            {
                float centerNoise = GetNoiseAt(x, y, noiseOffsetX, noiseOffsetY);
                if (centerNoise <= config.grassThreshold) continue;

                bool topGrass = GetNoiseAt(x, y + 1, noiseOffsetX, noiseOffsetY) > config.grassThreshold;
                bool bottomGrass = GetNoiseAt(x, y - 1, noiseOffsetX, noiseOffsetY) > config.grassThreshold;
                bool leftGrass = GetNoiseAt(x - 1, y, noiseOffsetX, noiseOffsetY) > config.grassThreshold;
                bool rightGrass = GetNoiseAt(x + 1, y, noiseOffsetX, noiseOffsetY) > config.grassThreshold;

                int tileIndex = GetEdgeTileIndex(topGrass, bottomGrass, leftGrass, rightGrass);
                
                if (tileIndex >= 0 && tileIndex < config.grassEdgeTiles.Length && 
                    config.grassEdgeTiles[tileIndex] != null)
                {
                    Vector3Int tilePos = new Vector3Int(x, y, 0);
                    groundTilemap.SetTile(tilePos, config.grassEdgeTiles[tileIndex]);
                }
            }
        }
    }

    private void BakeDecorations(float noiseOffsetX, float noiseOffsetY)
    {
        if (decorationTilemap == null) return;

        int halfWidth = config.mapWidth / 2;
        int halfHeight = config.mapHeight / 2;

        for (int x = -halfWidth; x < halfWidth; x++)
        {
            for (int y = -halfHeight; y < halfHeight; y++)
            {
                Vector3Int tilePos = new Vector3Int(x, y, 0);
                
                float distFromCenter = Mathf.Sqrt(x * x + y * y);
                bool inSafeZone = distFromCenter < config.spawnSafeRadius;

                float noiseValue = GetNoiseAt(x, y, noiseOffsetX, noiseOffsetY);
                if (noiseValue <= config.grassThreshold) continue;

                if (!inSafeZone && config.largeDecorations != null && config.largeDecorations.Length > 0)
                {
                    if (Random.value < config.largeDecorationChance)
                    {
                        TileBase largeDeco = config.largeDecorations[Random.Range(0, config.largeDecorations.Length)];
                        if (largeDeco != null)
                        {
                            decorationTilemap.SetTile(tilePos, largeDeco);
                            if (collisionTilemap != null) collisionTilemap.SetTile(tilePos, largeDeco);
                            continue;
                        }
                    }
                }

                if (config.mediumDecorations != null && config.mediumDecorations.Length > 0)
                {
                    if (Random.value < config.mediumDecorationChance)
                    {
                        TileBase medDeco = config.mediumDecorations[Random.Range(0, config.mediumDecorations.Length)];
                        if (medDeco != null)
                        {
                            decorationTilemap.SetTile(tilePos, medDeco);
                            continue;
                        }
                    }
                }

                if (config.smallDecorations != null && config.smallDecorations.Length > 0)
                {
                    if (Random.value < config.smallDecorationChance)
                    {
                        TileBase smallDeco = config.smallDecorations[Random.Range(0, config.smallDecorations.Length)];
                        if (smallDeco != null)
                        {
                            decorationTilemap.SetTile(tilePos, smallDeco);
                        }
                    }
                }
            }
        }
    }

    [ContextMenu("Clear Baked Map")]
    public void ClearBakedMap()
    {
        if (Application.isPlaying)
        {
            Debug.LogError("Cannot clear map while in Play mode. Exit Play mode first.");
            return;
        }

        if (groundTilemap != null) groundTilemap.ClearAllTiles();
        if (decorationTilemap != null) decorationTilemap.ClearAllTiles();
        if (collisionTilemap != null) collisionTilemap.ClearAllTiles();

        EditorUtility.SetDirty(groundTilemap);
        if (decorationTilemap != null) EditorUtility.SetDirty(decorationTilemap);
        if (collisionTilemap != null) EditorUtility.SetDirty(collisionTilemap);

        useBakedMap = false;
        EditorUtility.SetDirty(this);

        Debug.Log("[ProceduralMapGenerator] Baked map cleared. Remember to save the scene!");
    }
#endif
}
