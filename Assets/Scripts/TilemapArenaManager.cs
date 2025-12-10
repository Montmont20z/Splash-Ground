using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public class TilemapArenaManager : MonoBehaviour
{
    [Header("Tilemap References")]
    public Tilemap tilemap;
    public TileBase healthyTile;
    public TileBase contaminatedTile;
    public TileBase transitionTile;

    [Header("Arena Settings")]
    public int gridWidth = 50;
    public int gridHeight = 50;

    [Header("Visual Settings")]
    [Range(0f, 1f)]
    public float blendSmoothness = 0.3f;
    public bool useTransitionTiles = true;

    [Header("Auto-Fill on Start")]
    public bool autoFillHealthyOnStart = true;

    // Track tile states
    private Dictionary<Vector3Int, TileState> tileStates = new Dictionary<Vector3Int, TileState>();
    private Dictionary<Vector3Int, float> contaminationLevels = new Dictionary<Vector3Int, float>();

    public enum TileState { Healthy, Contaminated, Transition }

    void Awake()
    {
        if (tilemap == null)
            tilemap = GetComponentInChildren<Tilemap>();

        if (autoFillHealthyOnStart)
        {
            FillArenaWithHealthyTiles();
        }

        InitializeTileStates();
    }

    void Start()
    {
        Debug.Log($"Tilemap Arena initialized with {tileStates.Count} tiles");
    }

    void Update()
    {
        if (useTransitionTiles)
        {
            UpdateTransitionTiles();
        }
    }

    public void FillArenaWithHealthyTiles()
    {
        if (tilemap == null || healthyTile == null)
        {
            Debug.LogError("Tilemap or HealthyTile not assigned!");
            return;
        }

        // Clear existing tiles
        tilemap.ClearAllTiles();

        // Fill with healthy tiles
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                Vector3Int tilePos = new Vector3Int(x, y, 0);
                tilemap.SetTile(tilePos, healthyTile);
            }
        }

        Debug.Log($"Filled arena with {gridWidth * gridHeight} healthy tiles");
    }

    void InitializeTileStates()
    {
        tileStates.Clear();
        contaminationLevels.Clear();

        // Get all tiles currently in tilemap
        BoundsInt bounds = tilemap.cellBounds;

        foreach (Vector3Int pos in bounds.allPositionsWithin)
        {
            if (tilemap.HasTile(pos))
            {
                tileStates[pos] = TileState.Healthy;
                contaminationLevels[pos] = 0f;
            }
        }
    }

    void UpdateTransitionTiles()
    {
        if (transitionTile == null) return;

        List<Vector3Int> tilesToUpdate = new List<Vector3Int>();

        foreach (var kvp in tileStates)
        {
            Vector3Int pos = kvp.Key;
            TileState state = kvp.Value;

            // Check neighbors to determine if this should be a transition tile
            int healthyNeighbors = 0;
            int contaminatedNeighbors = 0;

            Vector3Int[] neighbors = new Vector3Int[]
            {
                pos + Vector3Int.up,
                pos + Vector3Int.down,
                pos + Vector3Int.left,
                pos + Vector3Int.right
            };

            foreach (Vector3Int neighbor in neighbors)
            {
                if (tileStates.ContainsKey(neighbor))
                {
                    if (tileStates[neighbor] == TileState.Healthy)
                        healthyNeighbors++;
                    else if (tileStates[neighbor] == TileState.Contaminated)
                        contaminatedNeighbors++;
                }
            }

            // Transition tile if bordering both types
            if (healthyNeighbors > 0 && contaminatedNeighbors > 0)
            {
                if (state != TileState.Transition)
                {
                    tilemap.SetTile(pos, transitionTile);
                    tileStates[pos] = TileState.Transition;
                }
            }
        }
    }

    // Public API - Contaminate by grid position
    public void ContaminateTile(Vector3Int tilePos)
    {
        if (!tilemap.HasTile(tilePos)) return;

        tilemap.SetTile(tilePos, contaminatedTile);
        tileStates[tilePos] = TileState.Contaminated;
        contaminationLevels[tilePos] = 1f;
    }

    public void CleanseTile(Vector3Int tilePos)
    {
        if (!tilemap.HasTile(tilePos)) return;

        tilemap.SetTile(tilePos, healthyTile);
        tileStates[tilePos] = TileState.Healthy;
        contaminationLevels[tilePos] = 0f;
    }

    // Public API - Contaminate by world position
    public void ContaminateArea(Vector3 worldPosition, float radius)
    {
        // Convert world position to grid position
        Vector3Int centerCell = tilemap.WorldToCell(worldPosition);
        int radiusCells = Mathf.CeilToInt(radius);

        for (int x = -radiusCells; x <= radiusCells; x++)
        {
            for (int y = -radiusCells; y <= radiusCells; y++)
            {
                Vector3Int cellPos = centerCell + new Vector3Int(x, y, 0);

                if (tilemap.HasTile(cellPos))
                {
                    Vector3 cellWorldPos = tilemap.GetCellCenterWorld(cellPos);
                    float distance = Vector2.Distance(
                        new Vector2(worldPosition.x, worldPosition.z),
                        new Vector2(cellWorldPos.x, cellWorldPos.y)
                    );

                    if (distance <= radius)
                    {
                        ContaminateTile(cellPos);
                    }
                }
            }
        }
    }

    public void CleanseArea(Vector3 worldPosition, float radius)
    {
        Vector3Int centerCell = tilemap.WorldToCell(worldPosition);
        int radiusCells = Mathf.CeilToInt(radius);

        for (int x = -radiusCells; x <= radiusCells; x++)
        {
            for (int y = -radiusCells; y <= radiusCells; y++)
            {
                Vector3Int cellPos = centerCell + new Vector3Int(x, y, 0);

                if (tilemap.HasTile(cellPos))
                {
                    Vector3 cellWorldPos = tilemap.GetCellCenterWorld(cellPos);
                    float distance = Vector2.Distance(
                        new Vector2(worldPosition.x, worldPosition.z),
                        new Vector2(cellWorldPos.x, cellWorldPos.y)
                    );

                    if (distance <= radius)
                    {
                        CleanseTile(cellPos);
                    }
                }
            }
        }
    }

    public float GetHealthPercentage()
    {
        int healthyCount = 0;
        int totalTiles = 0;

        foreach (var kvp in tileStates)
        {
            totalTiles++;
            if (kvp.Value == TileState.Healthy)
                healthyCount++;
        }

        return totalTiles > 0 ? (float)healthyCount / totalTiles * 100f : 0f;
    }

    // Helper to get tile at world position
    public TileState GetTileStateAtWorldPos(Vector3 worldPosition)
    {
        Vector3Int cellPos = tilemap.WorldToCell(worldPosition);

        if (tileStates.ContainsKey(cellPos))
            return tileStates[cellPos];

        return TileState.Healthy;
    }

    // Debug visualization
    void OnDrawGizmos()
    {
        if (tilemap == null) return;

        Gizmos.color = Color.cyan;
        BoundsInt bounds = tilemap.cellBounds;

        Vector3 size = new Vector3(
            bounds.size.x * tilemap.cellSize.x,
            0.1f,
            bounds.size.y * tilemap.cellSize.y
        );

        Vector3 center = tilemap.GetCellCenterWorld(new Vector3Int(
            bounds.xMin + bounds.size.x / 2,
            bounds.yMin + bounds.size.y / 2,
            0
        ));

        Gizmos.DrawWireCube(center, size);
    }
}