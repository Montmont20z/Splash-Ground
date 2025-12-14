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
}

public enum SpawnPattern
{
    LeftToRight, // X increasing
    RightToLeft, // X decreasing
    TopToBottom, // Z decreasing
    BottomToTop, // Z increasing
    RandomEdge
}

public class MonsterSpawner : MonoBehaviour
{
    [Header("General")]
    public ArenaManager arena;
    public List<MonsterTypeEntry> monsterTypes;
    public float spawnHeight = 0.0f;  // Y position above ground for spawns
    public float spawnPadding = 0.6f;  // how far extra space outside the edge to spawn

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

    // internal
    private Coroutine regularSpawnRoutine;
    private Coroutine bigWaveRoutine;
    private System.Random rng;

    private void Awake()
    {
        if (levelSeed == -1)
        {
            rng = new System.Random();
        } else
        {
            rng = new System.Random(levelSeed);
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
        yield return new WaitForSeconds(1f); // initial small delay
        while (autoSpawn)
        {
            int count = rng.Next(smallSpawnMin, smallSpawnMax + 1);
            // Spawn random number of monster base on smallSpawnMin & smallSpawnMax
            for (int i = 0; i < count; i++)
            {
                SpawnOneRandom();
                // “Wait a short amount of time between spawns,
                // scaling the delay based on how many objects I’m spawning,
                // but never go faster than 0.05 seconds per spawn.”
                //Debug.Log("Regular Spawn Coroutine Running");
                yield return new WaitForSeconds(Mathf.Max(0.05f, smallSpawnInterval / Mathf.Max(1, count)));
            }
            yield return new WaitForSeconds(Mathf.Max(0.1f, smallSpawnInterval));
        }
    }
    IEnumerator BigWaveLoop()
    {
        yield return new WaitForSeconds(bigWaveInterval); // first big wave after interval
        while (autoSpawn)
        {
            // Spawn bigWaveCount monsters, with small delays between them
            Debug.Log("Big Wave Incoming");
            for (int i = 0; i < bigWaveCount; i++)
            {
                SpawnOneRandom();
                yield return new WaitForSeconds(Mathf.Max(0.02f, bigWaveBurstDelay));
            }
            yield return new WaitForSeconds(Mathf.Max(0.1f, bigWaveInterval));
        }
    }

    /// <summary>Pick a prefab by weighted random and spawn it. </summary>
    void SpawnOneRandom()
    {
        GameObject prefab = ChooseWeightedPrefab();
        if (prefab == null) return;

        // get spawn pos and direction using the pattern
        GetSpawnInfo(out Vector3 spawnPos, out Vector3 moveDir, prefab);
        SpawnMonster(prefab, spawnPos, moveDir);
    }

    private GameObject ChooseWeightedPrefab()
    {
        float validWeight = 0f;
        foreach (MonsterTypeEntry e in monsterTypes)
            if (e.prefab != null && e.weight > 0f) validWeight += e.weight; // get valid weight 

        if (validWeight <= 0f) return null;

        float pick = (float)(rng.NextDouble() * validWeight); // pick a number between [0, validWeight]

        // create weight intervals
        //e.g. Monster	Range
        //Slime   0 → 50
        //Goblin  50 → 80
        //Dragon  80 → 85
        // if
        // pick = 42.7 → Slime
        // pick = 77.2 → Goblin
        // pick = 82.1 → Dragon
        float cumulative = 0f;
        foreach (MonsterTypeEntry e in monsterTypes)
        {
            if (e.prefab == null || e.weight <= 0f) continue; // skip if no prefab or weight is 0
            cumulative += e.weight;
            if (pick <= cumulative) return e.prefab;
        }

        // fallback to first non-null prefab
        foreach (MonsterTypeEntry e in monsterTypes) if (e.prefab != null) return e.prefab;
        return null;
    }

   
    /// <summary>
    /// Spawn logic: find an edge tile according to pattern, spawn slightly outside the tile,
    /// and compute a movement direction that points towards the arena center (or along axis depending on pattern).
    /// </summary>
    void GetSpawnInfo(out Vector3 outPosition, out Vector3 outDirection, GameObject prefab)
    {
        outPosition = Vector3.zero;
        outDirection  = Vector3.right;
        SpawnPattern selectedPattern = pattern;

        int w = Mathf.Max(1, arena.gridWidth);
        int h = Mathf.Max(1, arena.gridHeight);

        // collect candidate edge tiles depending on pattern
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
                // any edge tile that exists
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
            // fallback: spawn at outside center
            Vector3 arenaCenter = new Vector3((w - 1) * arena.tileSize / 2f, spawnHeight, (h - 1) * arena.tileSize / 2f);
            outPosition = arenaCenter + Vector3.forward * (h / 2f + spawnPadding);
            outDirection = (arenaCenter - outPosition).normalized;
            return;
        }

        // pick random candidate
        var chosen = candidates[rng.Next(0, candidates.Count)];
        Vector3 tileCenter = new Vector3(chosen.x * arena.tileSize, 0f, chosen.z * arena.tileSize);

        // determine spawn outside position and direction based on which edge tile was selected
        Vector3 arenaCenterPos = new Vector3((w - 1) * arena.tileSize / 2f, 0f, (h - 1) * arena.tileSize / 2f);
        Vector3 dirToCenter = (arenaCenterPos - tileCenter).normalized;

        // spawn just outside the tile toward the outside side
        // find which side: if selected x == 0 => spawn left; x == w-1 => spawn right; z == 0 => bottom; z == h-1 => top
        Vector3 spawnOffset = Vector3.zero;
        if (chosen.x == 0) spawnOffset = Vector3.left;
        else if (chosen.x == w - 1) spawnOffset = Vector3.right;
        else if (chosen.z == 0) spawnOffset = Vector3.back;
        else if (chosen.z == h - 1) spawnOffset = Vector3.forward;
        else spawnOffset = -dirToCenter; // fallback

        outPosition = tileCenter + spawnOffset * (arena.tileSize * 0.5f + spawnPadding);
        outPosition.y = spawnHeight;

        // decide movement direction:
        // - if specific pattern axis, use axis direction (left->right etc.)
        switch (pattern)
        {
            case SpawnPattern.LeftToRight: outDirection = Vector3.right; break;
            case SpawnPattern.RightToLeft: outDirection = Vector3.left; break;
            case SpawnPattern.TopToBottom: outDirection = Vector3.back; break;
            case SpawnPattern.BottomToTop: outDirection = Vector3.forward; break;
            case SpawnPattern.RandomEdge:
            default:
                // by default, head towards the arena center (so monsters enter the playable area)
                outDirection = (arenaCenterPos - outPosition).normalized;
                break;
        }

        //// if random pick a random pattern
        //if (pattern == SpawnPattern.Random)
        //{
        //    Array values = Enum.GetValues(typeof(SpawnPattern));
        //    System.Random random = new System.Random();
        //    selectedPattern = (SpawnPattern)values.GetValue(random.Next(values.Length - 1)); // exclude Random itself
        //}

        //switch (selectedPattern)
        //{
        //    case SpawnPattern.LeftToRight:
        //        position = new Vector3(
        //            -2f, // X start just outside left boundary by 2 units
        //            monsterHeight,
        //            rng.Next(0f, arenaHeight) // Z anywhere in arena height // need to adjust
        //        );
        //        direction = Vector3.right;
        //        break;
        //    case SpawnPattern.RightToLeft:
        //        // Spawn on right side, move left
        //        position = new Vector3(
        //            arenaWidth + 2f, // Just off right edge
        //            monsterHeight,
        //            rng.Next(0f, arenaHeight)
        //        );
        //        direction = Vector3.left;
        //        break;
        //    case  SpawnPattern.TopToBottom:
        //        // Spawn on top, move down
        //        position = new Vector3(
        //            rng.Next(0f, arenaWidth),
        //            monsterHeight,
        //            arenaHeight + 2f // Just off top edge
        //        );
        //        direction = Vector3.back; // -Z direction
        //        break;

        //    case SpawnPattern.BottomToTop:
        //        // Spawn on bottom, move up
        //        position = new Vector3(
        //            rng.Next(0f, arenaWidth),
        //            monsterHeight,
        //            -2f // Just off bottom edge
        //        );
        //        direction = Vector3.forward; // +Z direction
        //        break;

        //    default:
        //        position = Vector3.zero;
        //        direction = Vector3.right;
        //        break;
        //}
    }

    /// <summary>
    /// Manual spawn helper (public).
    /// </summary>
    public void SpawnMonsterAt(Vector3 position, Vector3 direction, GameObject prefab)
    {
        if (prefab == null) return;
        SpawnMonster(prefab, position, direction);
    }

    /// <summary>
    /// Instantiates the monster prefab and assigns its movement direction (if it has a MonsterBase).
    /// </summary>
    public void SpawnMonster(GameObject prefab, Vector3 spawnPosition, Vector3 moveDirection)
    {
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
