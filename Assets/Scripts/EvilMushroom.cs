using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Concrete implementation of a basic monster (EvilMushroom).
/// Inherits default behavior from MonsterBase; add special visuals or overrides here.
/// Detects being stuck by counting rapid turns within a short window.
/// </summary>
public class EvilMushroom : MonsterBase
{
    [Header("Visuals")]
    public ParticleSystem contaminationTrailEffect;
    public Color trailColor = Color.red;

    [Header("EvilMushroom Audio")]
    [Tooltip("Sound played when turning/avoiding")]
    public AudioClip turnSound;

    [Tooltip("Sound played when stuck and dying")]
    public AudioClip stuckSound;

    [Range(0f, 1f)]
    public float turnSoundVolume = 0.4f;

    [Header("Stuck-by-turning settings")]
    [Tooltip("Number of significant turns within the time window that qualifies as 'stuck'.")]
    public int maxTurnsInWindow = 4;
    [Tooltip("Time window (seconds) to count turns inside.")]
    public float turnWindowSeconds = 1.0f;
    [Tooltip("Minimum angle (degrees) between directions to be considered a 'turn'.")]
    public float turnAngleThreshold = 60f;
    [Tooltip("Seconds after spawn to ignore turn-based stuck checks (grace period).")]
    public float ignoreInitialSeconds = 0.5f;
    [Tooltip("Optional prefab (particle or small object) to spawn when destroyed for being stuck.")]
    public GameObject stuckDeathVfx;

    Vector3 savedOriginalDirection = Vector3.zero;
    bool isAvoiding = false;

    // runtime turn-tracking
    List<float> turnTimestamps = new List<float>();
    Vector3 lastRecordedDirection = Vector3.zero;
    float spawnTimeLocal = 0f;

    protected override void Start()
    {
        base.Start();
        spawnTimeLocal = Time.time;
        lastRecordedDirection = moveDirection.normalized;

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
        // record last-known direction at start of this update (used to detect a turn)
        Vector3 dirBefore = lastRecordedDirection;

        // if no arena assigned, do simple straight-line movement
        if (arena == null)
        {
            transform.position += moveDirection.normalized * moveSpeed * Time.deltaTime;
            CheckAndRegisterTurn(dirBefore, moveDirection.normalized);
            return;
        }

        // Helper local function to test if there's a tile ahead in a given direction
        bool HasTileAhead(Vector3 dir)
        {
            if (dir.sqrMagnitude < 1e-6f) return false;
            float ts = (arena.tileSize > 0f) ? arena.tileSize : 1f;
            Vector3 worldAhead = transform.position + dir.normalized * ts;
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
            }

            // move forward
            transform.position += moveDirection.normalized * moveSpeed * Time.deltaTime;
            transform.rotation = Quaternion.LookRotation(moveDirection.normalized);

            CheckAndRegisterTurn(dirBefore, moveDirection.normalized);
            return;
        }

        // Forward is empty -> need to avoid. If not already avoiding, pick a new direction.
        if (!isAvoiding)
        {
            savedOriginalDirection = moveDirection;
            Vector3 candidate = Vector3.zero;
            bool found = false;

            for (int i = 1; i <= 3; i++)
            {
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

                // Play turn sound when starting to avoid
                PlaySound(turnSound, turnSoundVolume);

                CheckAndRegisterTurn(dirBefore, moveDirection.normalized);
                return;
            }
            else
            {
                // no direction found — reverse
                moveDirection = -savedOriginalDirection.normalized;
                isAvoiding = true;
                transform.rotation = Quaternion.LookRotation(moveDirection.normalized);
                transform.position += moveDirection.normalized * moveSpeed * Time.deltaTime;

                // Play turn sound for 180 turn
                PlaySound(turnSound, turnSoundVolume);

                CheckAndRegisterTurn(dirBefore, moveDirection.normalized);
                return;
            }
        }
        else
        {
            // Already avoiding but forward is still empty
            transform.position += moveDirection.normalized * moveSpeed * Time.deltaTime;
            transform.rotation = Quaternion.LookRotation(moveDirection.normalized);
            CheckAndRegisterTurn(dirBefore, moveDirection.normalized);
            return;
        }
    }

    /// <summary>
    /// Check whether a significant turn occurred (by angle) compared with dirBefore.
    /// If yes, record the timestamp and prune timestamps outside the window; if count exceeds limit, handle stuck.
    /// </summary>
    void CheckAndRegisterTurn(Vector3 dirBefore, Vector3 dirAfter)
    {
        if (dirBefore.sqrMagnitude < 1e-6f)
        {
            lastRecordedDirection = dirAfter;
            return;
        }

        float angle = Vector3.Angle(dirBefore, dirAfter);
        if (angle >= turnAngleThreshold)
        {
            // ignore during initial grace period
            if (Time.time - spawnTimeLocal < ignoreInitialSeconds)
            {
                lastRecordedDirection = dirAfter;
                return;
            }

            // register turn timestamp
            float now = Time.time;
            turnTimestamps.Add(now);

            // prune old timestamps
            float cutoff = now - turnWindowSeconds;
            for (int i = turnTimestamps.Count - 1; i >= 0; i--)
            {
                if (turnTimestamps[i] < cutoff)
                    turnTimestamps.RemoveAt(i);
            }

            // if too many turns in window => stuck
            if (turnTimestamps.Count >= maxTurnsInWindow)
            {
                Debug.Log($"{name} considered stuck by turning: {turnTimestamps.Count} turns in {turnWindowSeconds}s (angle {angle}).");
                HandleStuck();
                return;
            }
        }

        lastRecordedDirection = dirAfter;
    }

    void HandleStuck()
    {
        // Play stuck sound
        if (stuckSound != null)
        {
            GameObject tempAudio = new GameObject($"{name}_StuckSound");
            tempAudio.transform.position = transform.position;

            AudioSource tempSource = tempAudio.AddComponent<AudioSource>();
            tempSource.clip = stuckSound;
            tempSource.spatialBlend = 1f;
            tempSource.rolloffMode = AudioRolloffMode.Linear;
            tempSource.minDistance = audioMinDistance;
            tempSource.maxDistance = audioMaxDistance;
            tempSource.Play();

            Destroy(tempAudio, stuckSound.length + 0.1f);
        }

        // optional spawn VFX
        if (stuckDeathVfx != null)
        {
            Instantiate(stuckDeathVfx, transform.position, Quaternion.identity);
        }

        // stop contamination trail
        if (contaminationTrailEffect != null)
        {
            contaminationTrailEffect.Stop();
        }

        Debug.Log($"EvilMushroom destroyed for being stuck by turning: {name}");
        Destroy(gameObject);
    }
}