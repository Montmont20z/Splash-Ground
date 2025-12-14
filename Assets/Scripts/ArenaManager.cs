using UnityEngine;
using System.Collections;

public class ArenaManager : MonoBehaviour
{
    [Header("Primary Data Source (ScriptableObject preferred)")]
    [Tooltip("Drag a LevelAsset here to use the ScriptableObject authored level.")]
    public LevelAsset levelAsset;       // PRIMARY - designer authored ScriptableObject

    [Tooltip("Optional fallback: JSON file (TextAsset) in flattened format if LevelAsset is not assigned.")]
    public TextAsset mapJson;           // FALLBACK - JSON (optional)

    [Header("Runtime Settings")]
    public GameObject player;
    public GameObject tilePrefab;
    public float tileSize = 1f;

    [Header("Procedural fallback (used only when no levelAsset and no mapJson)")]
    public int gridWidth = 20;
    public int gridHeight = 20;
    public ArenaShape shape = ArenaShape.Square;
    public enum ArenaShape { Square, Rectangle, Circle }

    [HideInInspector] public FloorTile[,] tiles; // instantiated tile grid

    // JSON data shape (matches editor/export format)
    [System.Serializable]
    private class MapDataSerializable
    {
        public int width;
        public int height;
        public int[] tiles; // flattened: idx = z*width + x (0=Empty,1=Healthy,2=Contaminated)
    }

    void Start()
    {
        if (tilePrefab == null)
        {
            Debug.LogError("[ArenaManager] Assign a tilePrefab (must contain FloorTile component).");
            return;
        }

        // Priority: LevelAsset -> JSON -> Procedural
        if (levelAsset != null)
        {
            BuildFromLevel(levelAsset);
        }
        else if (mapJson != null)
        {
            bool ok = BuildFromJsonText(mapJson.text);
            if (!ok) Debug.LogError("[ArenaManager] Failed to build arena from JSON; check format.");
        }
        else
        {
            GenerateArena();
        }

        StartCoroutine(DeferredPlacePlayerAtArenaCenter());
   }

    #region Build from LevelAsset (PRIORITY)
    /// <summary>
    /// Build arena from a LevelAsset ScriptableObject.
    /// Expects LevelAsset.tiles to be flattened row-major: index = y*width + x.
    /// TileDatum.type == Empty => skip; otherwise spawn tile and set state by TileDatum.initiallyContaminated.
    /// </summary>
    public void BuildFromLevel(LevelAsset lvl)
    {
        if (lvl == null)
        {
            Debug.LogError("[ArenaManager] levelAsset is null.");
            return;
        }

        lvl.EnsureSize();

        ClearExistingTiles();

        int w = lvl.width;
        int h = lvl.height;
        tiles = new FloorTile[w, h];

        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h; y++)
            {
                TileDatum datum = lvl.tiles[y * w + x];
                // We treat non-Empty as a floor tile. Use initiallyContaminated flag to set state.
                if (datum.type == LevelTileType.Empty) continue;

                Vector3 pos = new Vector3(x * tileSize, 0f, y * tileSize);
                GameObject go = Instantiate(tilePrefab, pos, Quaternion.identity, transform);
                go.name = $"Tile_{x}_{y}";
                FloorTile ft = go.GetComponent<FloorTile>();
                if (ft == null)
                {
                    Debug.LogWarning($"[ArenaManager] tilePrefab missing FloorTile component at {x},{y}.");
                    continue;
                }

                ft.currentState = datum.initiallyContaminated ? FloorTile.TileState.Contaminated : FloorTile.TileState.Healthy;
                // Immediately apply visuals using the tile's public material fields
                ApplyTileVisual(ft);

                tiles[x, y] = ft;
            }
        }

        gridWidth = w;
        gridHeight = h;

    }
    #endregion

    #region Build from JSON (fallback)
    /// <summary>
    /// Parse JSON text and build arena. Returns true on success.
    /// JSON format:
    /// { "width":W, "height":H, "tiles":[ ... length W*H, idx = z*W + x ] }
    /// codes: 0=Empty,1=Healthy,2=Contaminated
    /// </summary>
    public bool BuildFromJsonText(string json)
    {
        if (string.IsNullOrEmpty(json))
        {
            Debug.LogError("[ArenaManager] JSON is empty.");
            return false;
        }

        MapDataSerializable data;
        try
        {
            data = JsonUtility.FromJson<MapDataSerializable>(json);
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[ArenaManager] JSON parse error: " + ex.Message);
            return false;
        }

        if (data == null || data.tiles == null || data.width <= 0 || data.height <= 0 || data.tiles.Length != data.width * data.height)
        {
            Debug.LogError("[ArenaManager] Invalid JSON map data. Ensure width*height == tiles.length");
            return false;
        }

        BuildFromMapArray(data.width, data.height, data.tiles);
        return true;
    }

    void BuildFromMapArray(int width, int height, int[] flatTiles)
    {
        ClearExistingTiles();

        tiles = new FloorTile[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                int idx = z * width + x;
                int code = flatTiles[idx];

                if (code == 0) continue; // empty

                Vector3 pos = new Vector3(x * tileSize, 0f, z * tileSize);
                GameObject go = Instantiate(tilePrefab, pos, Quaternion.identity, transform);
                go.name = $"Tile_{x}_{z}";

                FloorTile ft = go.GetComponent<FloorTile>();
                if (ft == null)
                {
                    Debug.LogWarning($"[ArenaManager] tilePrefab missing FloorTile component at {x},{z}.");
                    continue;
                }

                ft.currentState = (code == 2) ? FloorTile.TileState.Contaminated : FloorTile.TileState.Healthy;
                ApplyTileVisual(ft);

                tiles[x, z] = ft;
            }
        }

        gridWidth = width;
        gridHeight = height;


    }
    #endregion

    #region Procedural fallback (unchanged)
    void GenerateArena()
    {
        ClearExistingTiles();

        tiles = new FloorTile[gridWidth, gridHeight];

        for (int x = 0; x < gridWidth; x++)
        {
            for (int z = 0; z < gridHeight; z++)
            {
                if (ShouldSpawnTile(x, z))
                {
                    SpawnTile(x, z);
                }
            }
        }
    }

    bool ShouldSpawnTile(int x, int z)
    {
        switch (shape)
        {
            case ArenaShape.Square:
                return true;
            case ArenaShape.Rectangle:
                return true;
            case ArenaShape.Circle:
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
        if (tile == null)
        {
            Debug.LogWarning("[ArenaManager] tilePrefab does not contain FloorTile component.");
        }
        else
        {
            // default healthy
            tile.currentState = FloorTile.TileState.Healthy;
            ApplyTileVisual(tile);
        }

        tiles[x, z] = tile;
    }
    #endregion

    #region Utilities
    void ClearExistingTiles()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            var children = new System.Collections.Generic.List<GameObject>();
            foreach (Transform t in transform) children.Add(t.gameObject);
            foreach (var c in children) DestroyImmediate(c);
            return;
        }
#endif
        foreach (Transform t in transform) Destroy(t.gameObject);
    }

    void ApplyTileVisual(FloorTile ft)
    {
        if (ft == null) return;
        Renderer r = ft.GetComponent<Renderer>();
        if (r == null) return;

        // Use the materials assigned on the FloorTile instance
        if (ft.currentState == FloorTile.TileState.Healthy)
        {
            if (ft.healthyMaterial != null) r.material = ft.healthyMaterial;
        }
        else
        {
            if (ft.contaminatedMaterial != null) r.material = ft.contaminatedMaterial;
        }
    }

    public FloorTile GetTile(int x, int z)
    {
        if (tiles == null) return null;
        if (x >= 0 && x < tiles.GetLength(0) && z >= 0 && z < tiles.GetLength(1))
            return tiles[x, z];
        return null;
    }

    public float GetHealthPercentage()
    {
        if (tiles == null) return 0f;
        int healthyCount = 0;
        int totalTiles = 0;
        int w = tiles.GetLength(0), h = tiles.GetLength(1);

        for (int x = 0; x < w; x++)
        {
            for (int z = 0; z < h; z++)
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

    void PlacePlayerOnArenaCenter()
    {
        if (player == null) return;

        if (tiles == null)
        {
            Vector3 arenaCenter = new Vector3(gridWidth * tileSize / 2f, player.transform.position.y, gridHeight * tileSize / 2f);
            player.transform.position = arenaCenter;
            return;
        }

        int w = tiles.GetLength(0);
        int h = tiles.GetLength(1);
        Vector3 center = new Vector3((w - 1) * tileSize / 2f, player.transform.position.y, (h - 1) * tileSize / 2f);
        player.transform.position = center;
    }

    // created this because player does not get place at arena center after level restart 
    IEnumerator DeferredPlacePlayerAtArenaCenter()
    {
        // wait a single frame so all Awake/OnEnable/Start of other objects run
        yield return null;

        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player");

        PlacePlayerOnArenaCenter();
    }

    /// <summary>
    /// Ensures there's a FloorTile at grid (x,z). If none exists, instantiate tilePrefab, set its state,
    /// register it in the internal tiles[,] array and return the FloorTile reference.
    /// </summary>
    public FloorTile EnsureTileAt(int x, int z, FloorTile.TileState initialState = FloorTile.TileState.Healthy)
    {
        // bounds check
        if (x < 0 || z < 0) return null;
        // ensure tiles array exists and sized to current gridWidth/gridHeight
        if (tiles == null || tiles.GetLength(0) != gridWidth || tiles.GetLength(1) != gridHeight)
        {
            // try to initialize array to current gridWidth/gridHeight
            tiles = new FloorTile[gridWidth, gridHeight];
        }
        if (x >= tiles.GetLength(0) || z >= tiles.GetLength(1))
            return null;

        if (tiles[x, z] != null) return tiles[x, z];

        // instantiate a tile at that grid position
        Vector3 pos = new Vector3(x * tileSize, 0f, z * tileSize);
        GameObject go = Instantiate(tilePrefab, pos, Quaternion.identity, transform);
        go.name = $"Tile_{x}_{z}";
        FloorTile ft = go.GetComponent<FloorTile>();
        if (ft == null)
        {
            Debug.LogWarning("[ArenaManager] tilePrefab missing FloorTile component.");
            // still return null
            Destroy(go);
            return null;
        }

        // set the requested initial state
        if (initialState == FloorTile.TileState.Healthy) ft.Cleanse();
        else if (initialState == FloorTile.TileState.Contaminated) ft.Contaminate();
        else if (initialState == FloorTile.TileState.HeavyContaminated) ft.HeavyContaminate();

        tiles[x, z] = ft;
        return ft;
    }

    void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;
        if (tiles == null) return;

        Gizmos.color = Color.cyan;
        int w = tiles.GetLength(0);
        int h = tiles.GetLength(1);
        Vector3 center = new Vector3((w - 1) * tileSize / 2f, 0, (h - 1) * tileSize / 2f);
        Vector3 size = new Vector3(w * tileSize, 0.1f, h * tileSize);
        Gizmos.DrawWireCube(center, size);
    }
    #endregion
}
