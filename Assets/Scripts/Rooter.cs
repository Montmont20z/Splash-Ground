using System.Collections;
using UnityEngine;

public class Rooter : MonsterBase
{
    [Header("Rooter Settings")]
    public float moveSpeedOverride = 1f;
    public float contaminationRadiusOverride = 1f;

    [Header("Jump Settings")]
    public float jumpDuration = 0.35f;
    public float jumpHeight = 1.0f;

    private bool isJumping = false;

    public Animator animator;


    protected override void Start()
    {
        base.Start();
        if (moveSpeedOverride > 0f) moveSpeed = moveSpeedOverride;
        if (contaminationRadiusOverride > 0f) contaminationRadius = contaminationRadiusOverride;
    }

    protected override void ContaminateTiles()
    {
        // Rooter makes tiles HeavyContaminated
        Collider[] hit = Physics.OverlapSphere(transform.position, contaminationRadius, floorTileLayer);
        foreach (var col in hit)
        {
            FloorTile ft = col.GetComponent<FloorTile>();
            if (ft != null && ft.currentState == FloorTile.TileState.Healthy)
            {
                ft.HeavyContaminate();
            }
        }
    }

    protected override void Move()
    {
        if (isJumping || arena == null) return;

        Vector3 forward = moveDirection.normalized;
        if (forward.sqrMagnitude < 0.01f) return;

        // Move forward
        transform.position += forward * moveSpeed * Time.deltaTime;
        transform.rotation = Quaternion.LookRotation(forward);

        float tileSize = arena.tileSize > 0 ? arena.tileSize : 1f;

        // Check current tile
        int curGX = Mathf.RoundToInt(transform.position.x / tileSize);
        int curGZ = Mathf.RoundToInt(transform.position.z / tileSize);
        FloorTile currentTile = arena.GetTile(curGX, curGZ);

        // If standing on empty space, find next tile and jump to it
        if (currentTile == null)
        {
            FloorTile nextTile = FindNextTileInDirection(curGX, curGZ, forward, tileSize);
            if (nextTile != null)
            {
                StartCoroutine(JumpToTile(nextTile.transform.position, tileSize));
            }
        }
    }

    private FloorTile FindNextTileInDirection(int startGX, int startGZ, Vector3 direction, float tileSize)
    {
        int dirX = Mathf.RoundToInt(Mathf.Sign(direction.x));
        int dirZ = Mathf.RoundToInt(Mathf.Sign(direction.z));

        // Search up to max grid size
        int maxSteps = Mathf.Max(arena.gridWidth, arena.gridHeight);
        for (int step = 1; step <= maxSteps; step++)
        {
            int gx = startGX + dirX * step;
            int gz = startGZ + dirZ * step;

            // Out of bounds check
            if (gx < 0 || gz < 0 || gx >= arena.gridWidth || gz >= arena.gridHeight)
                break;

            FloorTile tile = arena.GetTile(gx, gz);
            if (tile != null)
                return tile;
        }

        return null;
    }

    private IEnumerator JumpToTile(Vector3 targetPosition, float tileSize)
    {
        isJumping = true;
        if (animator != null)
        {
            animator.SetTrigger("Jump");
        }

        Vector3 startPos = transform.position;
        Vector3 endPos = new Vector3(targetPosition.x, transform.position.y, targetPosition.z);

        float elapsed = 0f;
        while (elapsed < jumpDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / jumpDuration);

            // Linear horizontal movement
            Vector3 pos = Vector3.Lerp(startPos, endPos, t);

            // Parabolic arc for height
            float arc = Mathf.Sin(t * Mathf.PI) * jumpHeight;
            pos.y = startPos.y + arc;

            transform.position = pos;
            yield return null;
        }

        // Snap to final position
        transform.position = endPos;
        isJumping = false;
    }
}