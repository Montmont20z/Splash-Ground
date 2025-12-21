using System.Collections;
using UnityEngine;

/// <summary>
/// BlightPetal - A stationary monster that teleports to random arena positions
/// Contaminates tiles around it at regular intervals without moving
/// </summary>
public class BlightPetal : MonsterBase
{
    [Header("Animation")]
    public Animator animator;

    [Header("BlightPetal Teleportation")]
    [Tooltip("Time interval before teleporting to a new position")]
    [SerializeField] private float teleportInterval = 5f;

    [Tooltip("Sound effect when teleporting")]
    [SerializeField] private AudioClip teleportSound;

    [Tooltip("Particle effect when teleporting (optional)")]
    [SerializeField] private GameObject teleportEffect;

    [Tooltip("Minimum distance from arena edges when teleporting")]
    [SerializeField] private float edgePadding = 1f;

    private float nextTeleportTime;

    protected override void Start()
    {
        base.Start();

        // BlightPetal doesn't move, so set speed to 0
        moveSpeed = 0f;
        moveDirection = Vector3.zero;

        // Initialize teleport timer
        nextTeleportTime = Time.time + teleportInterval;

        // Teleport to initial random position
        //TeleportToRandomPosition();
    }

    protected void OnEnable()
    {
        // Wait one frame to let spawner set initial position, then override it
        StartCoroutine(TeleportToRandomPosition());
    }


    protected override void Update()
    {
        // Override Update to handle teleportation instead of movement

        // Contaminate tiles at regular intervals
        if (Time.time >= nextContaminateTime)
        {
            ContaminateTiles();
            nextContaminateTime = Time.time + contaminationInterval;
        }

        // Teleport at regular intervals
        if (Time.time >= nextTeleportTime)
        {   
            // play animation
            if (animator != null) animator.SetBool("isMoving", true);
            StartCoroutine(TeleportToRandomPosition());
            nextTeleportTime = Time.time + teleportInterval;
        }

        // Check lifetime
        CheckLifetime();
    }

    protected override void Move()
    {
        // BlightPetal doesn't move - override to prevent movement
        // (empty implementation)
    }

    /// <summary>
    /// Teleport to a random valid position on the arena
    /// </summary>
    IEnumerator TeleportToRandomPosition()
    {
        yield return new WaitForSeconds(2.0f);
        if (arena == null)
        {
            Debug.LogWarning("[BlightPetal] No ArenaManager found. Cannot teleport.");
            yield break;
        }

        // Get valid arena bounds with padding
        float minX = edgePadding;
        float maxX = (arena.gridWidth - 1) * arena.tileSize - edgePadding;
        float minZ = edgePadding;
        float maxZ = (arena.gridHeight - 1) * arena.tileSize - edgePadding;

        // Clamp to valid ranges
        if (maxX < minX) maxX = minX;
        if (maxZ < minZ) maxZ = minZ;

        // Generate random position within arena bounds
        Vector3 newPosition = new Vector3(
            Random.Range(minX, maxX),
            transform.position.y, // Keep same height
            Random.Range(minZ, maxZ)
        );

        // Spawn teleport effect at old position
        if (teleportEffect != null)
        {
            Instantiate(teleportEffect, transform.position, Quaternion.identity);
        }

        // Move to new position
        transform.position = newPosition;
        // Face random direction
        float randomYaw = Random.Range(0f, 360f);
        transform.rotation = Quaternion.Euler(0f, randomYaw, 0f);

        // Play teleport sound
        if (teleportSound != null)
        {
            PlaySound(teleportSound, 0.7f);
        }

        // Spawn teleport effect at new position
        if (teleportEffect != null)
        {
            Instantiate(teleportEffect, newPosition, Quaternion.identity);
        }

        Debug.Log($"[BlightPetal] Teleported to {newPosition}");
        if (animator != null) animator.SetBool("isMoving", false);
    }

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();

        // Draw teleport timer visualization
        if (Application.isPlaying)
        {
            float timeUntilTeleport = nextTeleportTime - Time.time;
            float teleportProgress = 1f - (timeUntilTeleport / teleportInterval);

            Gizmos.color = Color.Lerp(Color.green, Color.magenta, teleportProgress);
            Gizmos.DrawWireSphere(transform.position, 0.5f);
        }

        // Draw arena bounds if available
        if (arena != null)
        {
            Gizmos.color = new Color(1f, 0f, 1f, 0.3f);
            Vector3 arenaCenter = new Vector3(
                (arena.gridWidth - 1) * arena.tileSize / 2f,
                transform.position.y,
                (arena.gridHeight - 1) * arena.tileSize / 2f
            );
            Vector3 arenaSize = new Vector3(
                arena.gridWidth * arena.tileSize - edgePadding * 2,
                0.1f,
                arena.gridHeight * arena.tileSize - edgePadding * 2
            );
            Gizmos.DrawWireCube(arenaCenter, arenaSize);
        }
    }
}