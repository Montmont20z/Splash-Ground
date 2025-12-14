using UnityEngine;

/// <summary>
/// Concrete implementation of a basic monster (EvilMushroom).
/// Inherits default behavior from MonsterBase; add special visuals or overrides here.
/// </summary>
public class EvilMushroom : MonsterBase
{
    [Header("Visuals")]
    public ParticleSystem contaminationTrailEffect;
    public Color trailColor = Color.red;
    Vector3 savedOriginalDirection = Vector3.zero;
    bool isAvoiding = false;


    protected override void Start()
    {
        base.Start();
        // Optionally configure particle color etc.
        if (contaminationTrailEffect != null)
        {
            var main = contaminationTrailEffect.main;
            main.startColor = trailColor;
            contaminationTrailEffect.Play();
        }
    }


    protected override void Move()
    {
        // if no arena assigned, do simple straight-line movement
        if (arena == null)
        {
            transform.position += moveDirection.normalized * moveSpeed * Time.deltaTime;
            return;
        }

        // Helper local function to test if there's a tile ahead in a given direction
        bool HasTileAhead(Vector3 dir)
        {
            if (dir.sqrMagnitude < 1e-6f) return false;
            float ts = (arena.tileSize > 0f) ? arena.tileSize : 1f;
            Vector3 worldAhead = transform.position + dir.normalized * ts; // one tile ahead
            int gx = Mathf.RoundToInt(worldAhead.x / ts);
            int gz = Mathf.RoundToInt(worldAhead.z / ts);
            FloorTile t = arena.GetTile(gx, gz);
            return t != null;
        }

        // If current forward direction has a tile ahead, possibly resume original direction.
        if (HasTileAhead(moveDirection))
        {
            // if we were avoiding, check if we can go back to the saved original direction
            if (isAvoiding && savedOriginalDirection.sqrMagnitude > 0f)
            {
                if (HasTileAhead(savedOriginalDirection))
                {
                    moveDirection = savedOriginalDirection;
                    isAvoiding = false;
                    savedOriginalDirection = Vector3.zero;
                }
                // else keep current avoiding direction
            }

            // move forward
            transform.position += moveDirection.normalized * moveSpeed * Time.deltaTime;
            transform.rotation = Quaternion.LookRotation(moveDirection.normalized);
            return;
        }

        // Forward is empty -> need to avoid. If not already avoiding, pick a new direction.
        if (!isAvoiding)
        {
            savedOriginalDirection = moveDirection;
            // try turning left-first in 90-degree increments: left, back, right
            Vector3 candidate = Vector3.zero;
            bool found = false;
            for (int i = 1; i <= 3; i++)
            {
                // rotate left by 90 * i degrees
                candidate = Quaternion.Euler(0f, -90f * i, 0f) * savedOriginalDirection;
                if (HasTileAhead(candidate))
                {
                    found = true;
                    break;
                }
            }

            if (found)
            {
                moveDirection = candidate.normalized;
                isAvoiding = true;
                transform.rotation = Quaternion.LookRotation(moveDirection.normalized);
                transform.position += moveDirection.normalized * moveSpeed * Time.deltaTime;
                return;
            }
            else
            {
                // no direction found around — fallback to turning 180 (reverse) to avoid falling into big void
                moveDirection = -savedOriginalDirection.normalized;
                isAvoiding = true;
                transform.rotation = Quaternion.LookRotation(moveDirection.normalized);
                transform.position += moveDirection.normalized * moveSpeed * Time.deltaTime;
                return;
            }
        }
        else
        {
            // Already avoiding but forward is still empty — just continue moving in current avoiding direction
            transform.position += moveDirection.normalized * moveSpeed * Time.deltaTime;
            transform.rotation = Quaternion.LookRotation(moveDirection.normalized);
            return;
        }

    }
    //protected override void OnDestroy()
    //{
    //    if (contaminationTrailEffect != null)
    //    {
    //        contaminationTrailEffect.Stop();
    //    }
    //    base.OnDestroy();
    //}

    // If you want special contamination behavior, override ContaminateTiles().
    // For now we use base. If you need custom effects when contaminating, add here.
}
