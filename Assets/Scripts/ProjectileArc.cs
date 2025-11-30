using UnityEngine;

public class ProjectileArc : MonoBehaviour
{
    private Vector3 startPos;
    private Vector3 endPos;
    private float arcHeight;
    private float speed;
    private float progress = 0f;

    public void Initialize(Vector3 start, Vector3 end, float height, float travelSpeed)
    {
        startPos = start;
        endPos = end;
        arcHeight = height;
        speed = travelSpeed;

        // Calculate total distance for speed calculation
        float distance = Vector3.Distance(start, end);
        speed = distance / travelSpeed; // Adjust speed based on distance
    }

    void Update()
    {
        // Move projectile along arc
        progress += Time.deltaTime * speed;

        if (progress >= 1f)
        {
            // Reached destination
            Destroy(gameObject);
            return;
        }

        // Calculate position on arc (parabola)
        Vector3 currentPos = Vector3.Lerp(startPos, endPos, progress);

        // Add arc height (parabola formula)
        float arcProgress = 1f - Mathf.Pow((progress - 0.5f) * 2f, 2); // Peaks at 0.5
        currentPos.y += arcHeight * arcProgress;

        transform.position = currentPos;

        // Optional: Rotate projectile to face movement direction
        if (progress < 0.99f)
        {
            Vector3 nextPos = Vector3.Lerp(startPos, endPos, progress + 0.01f);
            nextPos.y += arcHeight * (1f - Mathf.Pow(((progress + 0.01f) - 0.5f) * 2f, 2));
            transform.rotation = Quaternion.LookRotation(nextPos - currentPos);
        }
    }

    // Optional: Trail renderer for better visual
    void OnDrawGizmos()
    {
        if (progress > 0)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, 0.1f);
        }
    }
}