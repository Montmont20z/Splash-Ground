using System;
using UnityEngine;

public enum SpawnPattern
{
    LeftToRight, // X increasing
    RightToLeft, // X decreasing
    TopToBottom, // Z decreasing
    BottomToTop, // Z increasing
    Random 
}

public class MonsterSpawner : MonoBehaviour
{
    [Header("Spawning Settings")]
    public GameObject monsterPrefab;
    public float spawnInterval = 5.0f;
    public bool autoSpawn = true;

    [Header("Arena Bounds")]
    public float arenaWidth = 0f;
    public float arenaHeight = 0f;

    [Header("Spawn Patterns")]
    public SpawnPattern pattern = SpawnPattern.LeftToRight;

    private float nextSpawnTime = 0f;


    void Start()
    {
        if (arenaHeight == 0f || arenaWidth == 0f)
        {
            throw new System.Exception("Arena height and width must be set to a non-zero value.");
        }

        if (autoSpawn)
        {
            nextSpawnTime = Time.time + 2f; // first spwan after 2 seconds
        }
    }

    void Update()
    {
        if (autoSpawn && Time.time >= nextSpawnTime)
        {
            SpawnMonster();
            nextSpawnTime = Time.time + spawnInterval;
        }

        // Manual spawn for testing (press M key)
        if (Input.GetKeyDown(KeyCode.M))
        {
            SpawnMonster();
            Debug.Log("Manual monster spawn (M key)");
        }

    }

    void SpawnMonster()
    {
        if (monsterPrefab == null)
        {
            Debug.LogError("Monster prefab is not assigned in MonsterSpawner.");
            return;
        }

        // Get spawn positon and direction based on pattern
        Vector3 spawnPosition;
        Vector3 moveDirection;
        GetSpawnInfo(out spawnPosition, out moveDirection);

        // Spawn monster
        GameObject monster = Instantiate(monsterPrefab, spawnPosition, Quaternion.identity);

        // Set movement direction on monster script
        Monster monsterScript = monster.GetComponent<Monster>();
        if (monsterScript != null)
        {
            monsterScript.moveDirection = moveDirection;
        }
        else
        {
            Debug.LogError("Monster prefab does not have a Monster script attached.");
        }

    }

    void GetSpawnInfo(out Vector3 position, out Vector3 direction)
    {
        float monsterHeight = 0.6f; // default height above ground // need to change to use dynamic update
        SpawnPattern selectedPattern = pattern;

        // if random pick a random pattern
        if (pattern == SpawnPattern.Random)
        {
            Array values = Enum.GetValues(typeof(SpawnPattern));
            System.Random random = new System.Random();
            selectedPattern = (SpawnPattern)values.GetValue(random.Next(values.Length - 1)); // exclude Random itself
        }

        switch (selectedPattern)
        {
            case SpawnPattern.LeftToRight:
                position = new Vector3(
                    -2f, // X start just outside left boundary by 2 units
                    monsterHeight,
                    UnityEngine.Random.Range(0f, arenaHeight) // Z anywhere in arena height // need to adjust
                );
                direction = Vector3.right;
                break;
            case SpawnPattern.RightToLeft:
                // Spawn on right side, move left
                position = new Vector3(
                    arenaWidth + 2f, // Just off right edge
                    monsterHeight,
                    UnityEngine.Random.Range(0f, arenaHeight)
                );
                direction = Vector3.left;
                break;
            case  SpawnPattern.TopToBottom:
                // Spawn on top, move down
                position = new Vector3(
                    UnityEngine.Random.Range(0f, arenaWidth),
                    monsterHeight,
                    arenaHeight + 2f // Just off top edge
                );
                direction = Vector3.back; // -Z direction
                break;

            case SpawnPattern.BottomToTop:
                // Spawn on bottom, move up
                position = new Vector3(
                    UnityEngine.Random.Range(0f, arenaWidth),
                    monsterHeight,
                    -2f // Just off bottom edge
                );
                direction = Vector3.forward; // +Z direction
                break;

            default:
                position = Vector3.zero;
                direction = Vector3.right;
                break;
        }
    }

    public void SpawnMonsterAt(Vector3 position, Vector3 direction)
    {
        if (monsterPrefab == null)
        {
            Debug.LogError("Monster prefab is not assigned in MonsterSpawner.");
            return;
        }

        GameObject monster = Instantiate(monsterPrefab, position, Quaternion.identity);
        Monster monsterScript = monster.GetComponent<Monster>();
        if (monsterScript != null)
        {
            monsterScript.moveDirection = direction;
        }
        else
        {
            Debug.LogError("Monster prefab does not have a Monster script attached.");
        }
    }


    // Visualize spawn zones in Scene view
    void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        Gizmos.color = Color.yellow;

        // Draw arena bounds
        Vector3 center = new Vector3(arenaWidth / 2f, 0, arenaHeight / 2f);
        Vector3 size = new Vector3(arenaWidth, 0.1f, arenaHeight);
        Gizmos.DrawWireCube(center, size);

        // Draw spawn zones based on pattern
        Gizmos.color = new Color(1f, 0f, 0f, 0.3f);

        switch (pattern)
        {
            case SpawnPattern.LeftToRight:
                Gizmos.DrawCube(new Vector3(-1f, 0.5f, arenaHeight / 2f), new Vector3(2f, 1f, arenaHeight));
                break;
            case SpawnPattern.RightToLeft:
                Gizmos.DrawCube(new Vector3(arenaWidth + 1f, 0.5f, arenaHeight / 2f), new Vector3(2f, 1f, arenaHeight));
                break;
            case SpawnPattern.TopToBottom:
                Gizmos.DrawCube(new Vector3(arenaWidth / 2f, 0.5f, arenaHeight + 1f), new Vector3(arenaWidth, 1f, 2f));
                break;
            case SpawnPattern.BottomToTop:
                Gizmos.DrawCube(new Vector3(arenaWidth / 2f, 0.5f, -1f), new Vector3(arenaWidth, 1f, 2f));
                break;
        }
    }



}
