using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct MonsterTypeEntry
{
    public GameObject prefab;
    [Tooltip("Relative weight for weighted random selection. Set 0 to exclude.")]
    public float weight;
    [Tooltip("Sound to play when this monster type spawns (optional)")]
    public AudioClip spawnSound;
}

public enum SpawnPattern
{
    LeftToRight,
    RightToLeft,
    TopToBottom,
    BottomToTop,
    RandomEdge
}

public class MonsterSpawner : MonoBehaviour
{
    [Header("General")]
    public ArenaManager arena;
    public List<MonsterTypeEntry> monsterTypes;
    public float spawnHeight = 0.0f;
    public float spawnPadding = 0.6f;

    [Header("Regular (small) spawns")]
    public bool autoSpawn = true;
    [Tooltip("How often a small spawn group is triggered.")]
    public float smallSpawnInterval = 5.0f;
    [Tooltip("How many monsters spawn per small spawn (random between min/max).")]
    public int smallSpawnMin = 1;
    public int smallSpawnMax = 2;

    [Header("Big wave (periodic)")]
    [Tooltip("How often a big wave happens (seconds).")]
    public float bigWaveInterval = 30f;
    [Tooltip("How many monsters in the big wave total.")]
    public int bigWaveCount = 10;
    [Tooltip("Delay between spawns inside a big wave (seconds).")]
    public float bigWaveBurstDelay = 0.25f;

    [Header("Spawn Patterns")]
    public SpawnPattern pattern = SpawnPattern.RandomEdge;
    [Tooltip("For deterministic level, leave (-1) for random seed")]
    public int levelSeed = -1;

    [Header("Active monster limit")]
    [Tooltip("Maximum number of active monsters allowed at once. Set <= 0 to disable limit.")]
    public int maxActiveMonsters = 50;

    [Header("Audio Settings")]
    [Tooltip("AudioSource for 2D UI sounds (small wave, big wave announcements)")]
    public AudioSource uiAudioSource;

    [Tooltip("Sound played when a small wave starts")]
    public AudioClip smallWaveSound;

    [Tooltip("Sound played when a big wave starts (boss-like)")]
    public AudioClip bigWaveSound;

    [Tooltip("Optional warning sound played X seconds before big wave")]
    public AudioClip bigWaveWarningSound;
    [Tooltip("How many seconds before big wave to play warning")]
    public float bigWaveWarningTime = 3f;

    [Tooltip("Prefab with AudioSource for 3D positional spawn sounds")]
    public GameObject spatialAudioPrefab;

    [Tooltip("Max distance for spatial audio (monsters spawn sounds)")]
    public float spatialAudioMaxDistance = 50f;

    [Tooltip("Volume for spawn sounds (0-1)")]
    [Range(0f, 1f)]
    public float spawnSoundVolume = 0.7f;

    // internal
    private Coroutine regularSpawnRoutine;
    private Coroutine bigWaveRoutine;
    private System.Random rng;
    private int activeMonsterCount = 0;

    private void Awake()
    {
        if (levelSeed == -1)
            rng = new System.Random();
        else
            rng = new System.Random(levelSeed);

        // Setup UI audio source if not assigned
        if (uiAudioSource == null)
        {
            uiAudioSource = gameObject.AddComponent<AudioSource>();
            uiAudioSource.playOnAwake = false;
            uiAudioSource.spatialBlend = 0f; // 2D sound
        }
    }

    void Start()
    {
        if (arena == null)
        {
            Debug.LogError("[MonsterSpawner] ArenaManager not assigned.");
            enabled = false;
            return;
        }
        if (monsterTypes == null || monsterTypes.Count == 0)
        {
            Debug.LogError("[MonsterSpawner] No monster types assigned.");
            enabled = false;
            return;
        }
        if (autoSpawn)
        {
            regularSpawnRoutine = StartCoroutine(RegularSpawnLoop());
            bigWaveRoutine = StartCoroutine(BigWaveLoop());
        }
    }

    void OnDisable()
    {
        if (regularSpawnRoutine != null) StopCoroutine(regularSpawnRoutine);
        if (bigWaveRoutine != null) StopCoroutine(bigWaveRoutine);
    }

    IEnumerator RegularSpawnLoop()
    {
        yield return new WaitForSeconds(1f);
        while (autoSpawn)
        {
            // Play small wave sound
            PlayUISound(smallWaveSound);

            int count = rng.Next(smallSpawnMin, smallSpawnMax + 1);
            for (int i = 0; i < count; i++)
            {
                if (IsAtCap())
                {
                    yield return new WaitForSeconds(0.2f);
                    continue;
                }
                TrySpawnOneRandom();
                yield return new WaitForSeconds(Mathf.Max(0.05f, smallSpawnInterval / Mathf.Max(1, count)));
            }
            yield return new WaitForSeconds(Mathf.Max(0.1f, smallSpawnInterval));
        }
    }

    IEnumerator BigWaveLoop()
    {
        yield return new WaitForSeconds(bigWaveInterval);
        while (autoSpawn)
        {
            // Play warning sound before big wave
            if (bigWaveWarningSound != null && bigWaveWarningTime > 0)
            {
                PlayUISound(bigWaveWarningSound);
                yield return new WaitForSeconds(bigWaveWarningTime);
            }

            // Play big wave sound (boss-like)
            PlayUISound(bigWaveSound);
            Debug.Log("Big Wave Incoming");

            for (int i = 0; i < bigWaveCount; i++)
            {
                if (IsAtCap())
                {
                    yield return new WaitForSeconds(0.25f);
                    i--;
                    continue;
                }
                TrySpawnOneRandom();
                yield return new WaitForSeconds(Mathf.Max(0.02f, bigWaveBurstDelay));
            }
            yield return new WaitForSeconds(Mathf.Max(0.1f, bigWaveInterval));
        }
    }

    bool TrySpawnOneRandom()
    {
        if (IsAtCap()) return false;

        GameObject prefab = ChooseWeightedPrefab(out AudioClip spawnSound);
        if (prefab == null) return false;

        GetSpawnInfo(out Vector3 spawnPos, out Vector3 moveDir, prefab);

        // Play spatial spawn sound at spawn location
        PlaySpatialSound(spawnSound, spawnPos);

        SpawnMonster(prefab, spawnPos, moveDir);
        return true;
    }

    private bool IsAtCap()
    {
        if (maxActiveMonsters <= 0) return false;
        return activeMonsterCount >= maxActiveMonsters;
    }

    private GameObject ChooseWeightedPrefab(out AudioClip spawnSound)
    {
        spawnSound = null;

        float validWeight = 0f;
        foreach (MonsterTypeEntry e in monsterTypes)
            if (e.prefab != null && e.weight > 0f) validWeight += e.weight;

        if (validWeight <= 0f) return null;

        float pick = (float)(rng.NextDouble() * validWeight);

        float cumulative = 0f;
        foreach (MonsterTypeEntry e in monsterTypes)
        {
            if (e.prefab == null || e.weight <= 0f) continue;
            cumulative += e.weight;
            if (pick <= cumulative)
            {
                spawnSound = e.spawnSound;
                return e.prefab;
            }
        }

        foreach (MonsterTypeEntry e in monsterTypes)
        {
            if (e.prefab != null)
            {
                spawnSound = e.spawnSound;
                return e.prefab;
            }
        }
        return null;
    }

    void GetSpawnInfo(out Vector3 outPosition, out Vector3 outDirection, GameObject prefab)
    {
        outPosition = Vector3.zero;
        outDirection = Vector3.right;

        int w = Mathf.Max(1, arena.gridWidth);
        int h = Mathf.Max(1, arena.gridHeight);

        List<(int x, int z)> candidates = new List<(int x, int z)>();

        switch (pattern)
        {
            case SpawnPattern.LeftToRight:
                for (int z = 0; z < h; z++)
                    if (arena.GetTile(0, z) != null) candidates.Add((0, z));
                break;
            case SpawnPattern.RightToLeft:
                for (int z = 0; z < h; z++)
                    if (arena.GetTile(w - 1, z) != null) candidates.Add((w - 1, z));
                break;
            case SpawnPattern.TopToBottom:
                for (int x = 0; x < w; x++)
                    if (arena.GetTile(x, h - 1) != null) candidates.Add((x, h - 1));
                break;
            case SpawnPattern.BottomToTop:
                for (int x = 0; x < w; x++)
                    if (arena.GetTile(x, 0) != null) candidates.Add((x, 0));
                break;
            case SpawnPattern.RandomEdge:
            default:
                for (int x = 0; x < w; x++)
                {
                    if (arena.GetTile(x, 0) != null) candidates.Add((x, 0));
                    if (arena.GetTile(x, h - 1) != null) candidates.Add((x, h - 1));
                }
                for (int z = 0; z < h; z++)
                {
                    if (arena.GetTile(0, z) != null) candidates.Add((0, z));
                    if (arena.GetTile(w - 1, z) != null) candidates.Add((w - 1, z));
                }
                break;
        }

        if (candidates.Count == 0)
        {
            Vector3 arenaCenter = new Vector3((w - 1) * arena.tileSize / 2f, spawnHeight, (h - 1) * arena.tileSize / 2f);
            outPosition = arenaCenter + Vector3.forward * (h / 2f + spawnPadding);
            outDirection = (arenaCenter - outPosition).normalized;
            return;
        }

        var chosen = candidates[rng.Next(0, candidates.Count)];
        Vector3 tileCenter = new Vector3(chosen.x * arena.tileSize, 0f, chosen.z * arena.tileSize);

        Vector3 arenaCenterPos = new Vector3((w - 1) * arena.tileSize / 2f, 0f, (h - 1) * arena.tileSize / 2f);
        Vector3 dirToCenter = (arenaCenterPos - tileCenter).normalized;

        Vector3 spawnOffset = Vector3.zero;
        if (chosen.x == 0) spawnOffset = Vector3.left;
        else if (chosen.x == w - 1) spawnOffset = Vector3.right;
        else if (chosen.z == 0) spawnOffset = Vector3.back;
        else if (chosen.z == h - 1) spawnOffset = Vector3.forward;
        else spawnOffset = -dirToCenter;

        outPosition = tileCenter + spawnOffset * (arena.tileSize * 0.5f + spawnPadding);
        outPosition.y = spawnHeight;

        switch (pattern)
        {
            case SpawnPattern.LeftToRight: outDirection = Vector3.right; break;
            case SpawnPattern.RightToLeft: outDirection = Vector3.left; break;
            case SpawnPattern.TopToBottom: outDirection = Vector3.back; break;
            case SpawnPattern.BottomToTop: outDirection = Vector3.forward; break;
            case SpawnPattern.RandomEdge:
            default:
                outDirection = (arenaCenterPos - outPosition).normalized;
                break;
        }
    }

    public void SpawnMonsterAt(Vector3 position, Vector3 direction, GameObject prefab)
    {
        if (prefab == null) return;
        SpawnMonster(prefab, position, direction);
    }

    public GameObject SpawnMonster(GameObject prefab, Vector3 spawnPosition, Vector3 moveDirection)
    {
        if (IsAtCap()) return null;

        GameObject go = Instantiate(prefab, spawnPosition, Quaternion.LookRotation(moveDirection));
        MonsterBase mb = go.GetComponent<MonsterBase>();
        if (mb != null)
        {
            mb.SetMoveDirection(moveDirection);
        }
        else
        {
            Debug.LogWarning("[MonsterSpawner] Spawned prefab has no MonsterBase component.");
        }

        var tracker = go.AddComponent<SpawnedMonsterTracker>();
        tracker.spawner = this;

        activeMonsterCount++;

        return go;
    }

    internal void NotifyMonsterDestroyed(GameObject go)
    {
        if (activeMonsterCount > 0) activeMonsterCount--;
        activeMonsterCount = Mathf.Max(0, activeMonsterCount);
    }

    public int GetActiveMonsterCount()
    {
        return activeMonsterCount;
    }

    /// <summary>
    /// Play a 2D UI sound (for wave announcements)
    /// </summary>
    private void PlayUISound(AudioClip clip)
    {
        if (clip != null && uiAudioSource != null)
        {
            uiAudioSource.PlayOneShot(clip, spawnSoundVolume);
        }
    }

    /// <summary>
    /// Play a 3D positional sound at spawn location
    /// Creates a temporary AudioSource that auto-destroys after playing
    /// </summary>
    private void PlaySpatialSound(AudioClip clip, Vector3 position)
    {
        if (clip == null) return;

        GameObject soundObj;

        if (spatialAudioPrefab != null)
        {
            // Use prefab if provided (allows for custom AudioSource settings)
            soundObj = Instantiate(spatialAudioPrefab, position, Quaternion.identity);
        }
        else
        {
            // Create temporary GameObject with AudioSource
            soundObj = new GameObject("SpawnSound_Temp");
            soundObj.transform.position = position;
        }

        AudioSource source = soundObj.GetComponent<AudioSource>();
        if (source == null)
        {
            source = soundObj.AddComponent<AudioSource>();
        }

        // Configure 3D spatial audio
        source.clip = clip;
        source.spatialBlend = 1f; // Full 3D
        source.maxDistance = spatialAudioMaxDistance;
        source.rolloffMode = AudioRolloffMode.Linear;
        source.volume = spawnSoundVolume;
        source.Play();

        // Auto-destroy after clip finishes
        Destroy(soundObj, clip.length + 0.1f);
    }

    void OnDrawGizmosSelected()
    {
        if (arena == null) return;
        Gizmos.color = Color.yellow;
        Vector3 center = new Vector3((arena.gridWidth - 1) * arena.tileSize / 2f, 0, (arena.gridHeight - 1) * arena.tileSize / 2f);
        Vector3 size = new Vector3(arena.gridWidth * arena.tileSize, 0.1f, arena.gridHeight * arena.tileSize);
        Gizmos.DrawWireCube(center, size);
    }
}

public class SpawnedMonsterTracker : MonoBehaviour
{
    [NonSerialized] public MonsterSpawner spawner;

    void OnDestroy()
    {
        if (spawner != null)
        {
            spawner.NotifyMonsterDestroyed(gameObject);
        }
    }
}