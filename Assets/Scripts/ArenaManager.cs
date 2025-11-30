using UnityEngine;

public class ArenaManager : MonoBehaviour
{
    [Header("Arena Settings")]
    public GameObject tilePrefab;
    public int gridWidth = 20;
    public int gridHeight = 20;
    public float tileSize = 1f;

    [Header("Arena Shape")]
    public ArenaShape shape = ArenaShape.Square;

    public enum ArenaShape { Square, Rectangle, Circle }

    private FloorTile[,] tiles;

    void Start()
    {
        GenerateArena();
    }

    void Update()
    {
        //Debug.Log($"Floor Health: {GetHealthPercentage():F1}%");
    }

    void GenerateArena()
    {
        tiles = new FloorTile[gridWidth, gridHeight];

        for (int x = 0; x < gridWidth; x++)
        {
            for (int z = 0; z < gridHeight; z++)
            {
                // Check if tile should be spawned based on shape
                if (ShouldSpawnTile(x, z))
                {
                    SpawnTile(x, z);
                }
            }
        }

        // Center camera on arena
        //CenterCameraOnArena();
    }

    bool ShouldSpawnTile(int x, int z)
    {
        switch (shape)
        {
            case ArenaShape.Square:
                return true; // All tiles

            case ArenaShape.Rectangle:
                return true; // All tiles (adjust gridWidth/gridHeight for rectangle)

            case ArenaShape.Circle:
                // Only spawn tiles within circular bounds
                Vector2 center = new Vector2(gridWidth / 2f, gridHeight / 2f);
                Vector2 tilePos = new Vector2(x, z);
                float radius = Mathf.Min(gridWidth, gridHeight) / 2f;
                return Vector2.Distance(center, tilePos) <= radius;

            default:
                return true;
        }
    }

    void SpawnTile(int x, int z)
    {
        Vector3 position = new Vector3(x * tileSize, 0, z * tileSize);
        GameObject tileObj = Instantiate(tilePrefab, position, Quaternion.identity, transform);
        tileObj.name = $"Tile_{x}_{z}";

        FloorTile tile = tileObj.GetComponent<FloorTile>();
        tiles[x, z] = tile;
    }

    void CenterCameraOnArena()
    {
        // Optional: Position camera to see whole arena
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            Vector3 arenaCenter = new Vector3(
                gridWidth * tileSize / 2f,
                0,
                gridHeight * tileSize / 2f
            );

            mainCam.transform.position = arenaCenter + new Vector3(0, 15, -10);
            mainCam.transform.LookAt(arenaCenter);
        }
    }

    // Get tile at specific grid position
    public FloorTile GetTile(int x, int z)
    {
        if (x >= 0 && x < gridWidth && z >= 0 && z < gridHeight)
            return tiles[x, z];
        return null;
    }

    // Calculate health percentage
    public float GetHealthPercentage()
    {
        int healthyCount = 0;
        int totalTiles = 0;

        for (int x = 0; x < gridWidth; x++)
        {
            for (int z = 0; z < gridHeight; z++)
            {
                if (tiles[x, z] != null)
                {
                    totalTiles++;
                    if (tiles[x, z].currentState == FloorTile.TileState.Healthy)
                        healthyCount++;
                }
            }
        }

        return totalTiles > 0 ? (float)healthyCount / totalTiles * 100f : 0f;
    }

    // For debugging
    void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        Gizmos.color = Color.cyan;
        Vector3 center = new Vector3(gridWidth * tileSize / 2f, 0, gridHeight * tileSize / 2f);
        Vector3 size = new Vector3(gridWidth * tileSize, 0.1f, gridHeight * tileSize);
        Gizmos.DrawWireCube(center, size);
    }
}