using UnityEngine;

/// <summary>
/// Spawns power-ups randomly in the arena.
/// Automatically detects arena bounds from ArenaManager.
/// </summary>
public class PowerUpSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    public GameObject powerUpPrefab;
    public float spawnInterval = 15f;
    public int maxActivePowerUps = 3;
    public float spawnHeight = 1f;

    [Header("Power-Up Probabilities")]
    [Range(0, 100)] public float stunSingleChance = 15f;
    [Range(0, 100)] public float stunAllChance = 5f;
    [Range(0, 100)] public float rapidFireChance = 25f;
    [Range(0, 100)] public float infiniteAmmoChance = 15f;
    [Range(0, 100)] public float wideSprayChance = 20f;
    [Range(0, 100)] public float instantReloadChance = 10f;
    [Range(0, 100)] public float cleanseWaveChance = 8f;
    [Range(0, 100)] public float destroyAllChance = 2f;

    private float nextSpawnTime;
    private int activePowerUpCount = 0;
    private ArenaManager arena;

    void Start()
    {
        nextSpawnTime = Time.time + spawnInterval;
        arena = FindFirstObjectByType<ArenaManager>();

        if (arena == null)
        {
            Debug.LogError("[PowerUpSpawner] No ArenaManager found in scene!");
        }

        if (powerUpPrefab == null)
        {
            Debug.LogError("[PowerUpSpawner] No power-up prefab assigned! Please create and assign a prefab.");
        }
    }

    void Update()
    {
        if (powerUpPrefab == null || arena == null) return;

        if (Time.time >= nextSpawnTime && activePowerUpCount < maxActivePowerUps)
        {
            SpawnRandomPowerUp();
            nextSpawnTime = Time.time + spawnInterval;
        }

        activePowerUpCount = FindObjectsByType<PowerUpPickup>(FindObjectsSortMode.None).Length;
    }

    void SpawnRandomPowerUp()
    {
        if (powerUpPrefab == null)
        {
            Debug.LogWarning("[PowerUpSpawner] No power-up prefab assigned!");
            return;
        }

        if (arena == null || arena.tiles == null)
        {
            Debug.LogWarning("[PowerUpSpawner] Arena not ready!");
            return;
        }

        // Get arena bounds
        int width = arena.gridWidth;
        int height = arena.gridHeight;
        float tileSize = arena.tileSize;

        // Try to find a valid tile position (max 20 attempts)
        Vector3 spawnPos = Vector3.zero;
        bool foundValidPosition = false;

        for (int attempt = 0; attempt < 20; attempt++)
        {
            int randomX = Random.Range(0, width);
            int randomZ = Random.Range(0, height);

            FloorTile tile = arena.GetTile(randomX, randomZ);
            if (tile != null) // Only spawn on existing tiles
            {
                spawnPos = new Vector3(randomX * tileSize, spawnHeight, randomZ * tileSize);
                foundValidPosition = true;
                break;
            }
        }

        if (!foundValidPosition)
        {
            Debug.LogWarning("[PowerUpSpawner] Could not find valid spawn position!");
            return;
        }

        // Spawn power-up
        GameObject powerUp = Instantiate(powerUpPrefab, spawnPos, Quaternion.identity);

        PowerUpPickup pickup = powerUp.GetComponent<PowerUpPickup>();
        if (pickup != null)
        {
            pickup.powerUpType = GetRandomPowerUpType();
        }
        else
        {
            Debug.LogError("[PowerUpSpawner] PowerUpPrefab doesn't have PowerUpPickup component!");
        }

        activePowerUpCount++;
    }

    PowerUpType GetRandomPowerUpType()
    {
        float total = stunSingleChance + stunAllChance + rapidFireChance + infiniteAmmoChance +
                      wideSprayChance + instantReloadChance + cleanseWaveChance + destroyAllChance;
        float random = Random.Range(0f, total);

        float cumulative = 0f;

        cumulative += stunSingleChance;
        if (random < cumulative) return PowerUpType.StunSingle;

        cumulative += stunAllChance;
        if (random < cumulative) return PowerUpType.StunAll;

        cumulative += rapidFireChance;
        if (random < cumulative) return PowerUpType.RapidFire;

        cumulative += infiniteAmmoChance;
        if (random < cumulative) return PowerUpType.InfiniteAmmo;

        cumulative += wideSprayChance;
        if (random < cumulative) return PowerUpType.WideSpray;

        //cumulative += instantReloadChance;
        //if (random < cumulative) return PowerUpType.InstantReload;

        cumulative += cleanseWaveChance;
        if (random < cumulative) return PowerUpType.CleanseWave;

        return PowerUpType.DestroyAll;
    }

    void OnDrawGizmosSelected()
    {
        if (arena != null && arena.tiles != null)
        {
            Gizmos.color = Color.yellow;
            int w = arena.gridWidth;
            int h = arena.gridHeight;
            float tileSize = arena.tileSize;
            Vector3 center = new Vector3((w - 1) * tileSize / 2f, spawnHeight, (h - 1) * tileSize / 2f);
            Vector3 size = new Vector3(w * tileSize, 0.1f, h * tileSize);
            Gizmos.DrawWireCube(center, size);
        }
    }
}