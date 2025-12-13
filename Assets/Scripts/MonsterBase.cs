using UnityEngine;

/// <summary>
/// Abstract base class for monsters. Provides common movement, contamination and lifetime handling.
/// Derived classes override damage/visuals/unique behavior.
/// </summary>
[RequireComponent(typeof(Collider))]
public abstract class MonsterBase : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] protected Vector3 moveDirection = Vector3.right;
    [SerializeField] protected float moveSpeed = 3f;

    [Header("Contamination")]
    [SerializeField] protected float contaminationRadius = 1.5f;
    [SerializeField] protected float contaminationInterval = 0.1f;
    [SerializeField] protected LayerMask floorTileLayer;

    [Header("Lifetime")]
    [SerializeField] protected float maxLifetime = 30f;

    protected float spawnTime;
    protected float nextContaminateTime = 0f;

    protected virtual void Start()
    {
        spawnTime = Time.time;
        if (moveDirection != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(moveDirection);
    }

    protected virtual void Update()
    {
        Move();
        if (Time.time >= nextContaminateTime)
        {
            ContaminateTiles();
            nextContaminateTime = Time.time + contaminationInterval;
        }
        CheckLifetime();
    }

    protected virtual void Move()
    {
        transform.position += moveDirection.normalized * moveSpeed * Time.deltaTime;
    }

    /// <summary>
    /// Default contamination: find FloorTile components in radius and call Contaminate() on healthy tiles.
    /// Derived classes may override for different patterns.
    /// </summary>
    protected virtual void ContaminateTiles()
    {
        Collider[] hit = Physics.OverlapSphere(transform.position, contaminationRadius, floorTileLayer);
        foreach (var col in hit)
        {
            FloorTile ft = col.GetComponent<FloorTile>();
            if (ft != null && ft.currentState == FloorTile.TileState.Healthy)
            {
                ft.Contaminate();
            }
        }
    }

    protected virtual void CheckLifetime()
    {
        if (Time.time - spawnTime >= maxLifetime) Destroy(gameObject);
        if (transform.position.magnitude > 200f) Destroy(gameObject);
    }

    /// <summary>Set movement vector from spawner</summary>
    public virtual void SetMoveDirection(Vector3 dir)
    {
        if (dir.sqrMagnitude > 0.0001f)
        {
            moveDirection = dir.normalized;
            transform.rotation = Quaternion.LookRotation(moveDirection);
        }
    }

    // Optional: expose move speed setter
    public virtual void SetMoveSpeed(float s) { moveSpeed = s; }

    // For debugging
    protected virtual void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, contaminationRadius);
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(transform.position, moveDirection.normalized * 2f);
    }
}
