using System;
using UnityEngine;

public class Monster : MonoBehaviour
{
    [Header("Movement Settings")]
    public Vector3 moveDirection = Vector3.right; // Default to right
    public float moveSpeed = 3.0f;

    [Header("Contamination Settings")]
    public float contaminationRadius = 1.5f;
    public float contaminationRate = 0.1f;
    public LayerMask floorTileLayer;

    [Header("Lifetime")]
    public float maxLifetime = 30.0f;

    [Header("Visual Effects")]
    public ParticleSystem contaminationTrailEffect;
    public Color trailColor = Color.red;

    private float nextContaminateTime = 0f;
    private float spawnTime;


    void Start()
    {
        spawnTime = Time.time;

        // rotate to face move direction at start
        if ( moveDirection != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(moveDirection);
        }
        //Debug.Log($"Monster spawned at {transform.position}, moving {moveDirection}");
    }

    void Update()
    {
        Move();
        ContaminateTiles();
        CheckLifetime();
    }

    void CheckLifetime()
    {
        // Destroy if too old or too far from arena
        float age = Time.time - spawnTime;

        if (age >= maxLifetime)
        {
            Debug.Log($"Monster destroyed - exceeded lifetime ({maxLifetime}s)");
            Destroy(gameObject);
        }

        // Also destroy if too far from origin (failsafe)
        if (transform.position.magnitude > 100f)
        {
            Debug.Log("Monster destroyed - too far from arena");
            Destroy(gameObject);
        }
    }

    void ContaminateTiles()
    {
        // Only contaminate at intervals (not eveyy frame for performance)
        if (Time.time >= nextContaminateTime)
        {
            //Debug.Log("Time.time >= nextContaminatedTime");
            // Find all tiles in contamination radius
            Collider[] hitTiles = Physics.OverlapSphere(
                transform.position,
                contaminationRadius,
                floorTileLayer
            );

            int contaminatedCount = 0;

            foreach (Collider col in hitTiles)
            {
                FloorTile tile = col.GetComponent<FloorTile>();
                if (tile != null && tile.currentState == FloorTile.TileState.Healthy)
                {
                    tile.Contaminate();
                    contaminatedCount++;
                }
            }

            if (contaminatedCount > 0)
            {
                //Debug.Log($"Monster contaminated {contaminatedCount} tiles at {transform.position}");
            }

            nextContaminateTime = Time.time + contaminationRate;
        }
    }

    void Move()
    {
        transform.position += moveDirection.normalized * moveSpeed * Time.deltaTime;
    }

    // Visualize contamination radius in Scene view
    void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0f, 0f, 0.3f); // Transparent red
        Gizmos.DrawSphere(transform.position, contaminationRadius);

        // Draw movement direction
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(transform.position, moveDirection.normalized * 2f);
    }

    void OnDrawGizmosSelected()
    {
        // Show contamination radius more clearly when selected
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, contaminationRadius);
    }

}
