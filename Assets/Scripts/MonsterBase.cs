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
    protected float contaminationRadius = 1.5f;
    [SerializeField] protected float contaminationInterval = 1.1f;
    [SerializeField] protected LayerMask floorTileLayer;

    [Header("Lifetime")]
    [SerializeField] protected float maxLifetime = 30f;
    protected float spawnTime;
    protected float nextContaminateTime = 0f;

    [Header("Audio")]
    [Tooltip("AudioSource for monster sounds (movement, ambient, etc.)")]
    [SerializeField] protected AudioSource monsterAudioSource;

    [Tooltip("Looping ambient sound while monster is alive")]
    [SerializeField] protected AudioClip ambientSound;

    [Tooltip("Sound played when contaminating tiles")]
    [SerializeField] protected AudioClip contaminateSound;

    [Tooltip("Sound played when monster dies naturally (lifetime expired)")]
    [SerializeField] protected AudioClip deathSound;

    [Tooltip("Volume for contamination sounds (0-1)")]
    [Range(0f, 1f)]
    [SerializeField] protected float contaminateSoundVolume = 0.5f;

    [Tooltip("Max distance for monster audio")]
    [SerializeField] protected float audioMaxDistance = 30f;

    [Tooltip("Distance where audio starts to fade")]
    [SerializeField] protected float audioMinDistance = 5f;

    protected ArenaManager arena;

    protected virtual void Start()
    {
        spawnTime = Time.time;
        if (moveDirection != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(moveDirection);

        // cache arena references
        arena = FindFirstObjectByType<ArenaManager>();
        if (arena == null)
            Debug.LogWarning("[MonsterBase] No ArenaManager found in scene. Tile avoidance disabled.");

        // Setup audio
        SetupAudio();
    }

    /// <summary>
    /// Configure the AudioSource for 3D spatial audio
    /// </summary>
    protected virtual void SetupAudio()
    {
        // Create AudioSource if not assigned
        if (monsterAudioSource == null)
        {
            monsterAudioSource = gameObject.AddComponent<AudioSource>();
        }

        // Configure for 3D spatial audio
        monsterAudioSource.spatialBlend = 1f; // Full 3D
        monsterAudioSource.rolloffMode = AudioRolloffMode.Linear;
        monsterAudioSource.minDistance = audioMinDistance;
        monsterAudioSource.maxDistance = audioMaxDistance;
        monsterAudioSource.playOnAwake = false;
        monsterAudioSource.loop = false;

        // Play ambient sound if assigned
        if (ambientSound != null)
        {
            monsterAudioSource.clip = ambientSound;
            monsterAudioSource.loop = true;
            monsterAudioSource.Play();
        }
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
        bool contaminated = false;

        foreach (var col in hit)
        {
            FloorTile ft = col.GetComponent<FloorTile>();
            if (ft != null && ft.currentState == FloorTile.TileState.Healthy)
            {
                ft.Contaminate();
                contaminated = true;
            }
        }

        // Play contamination sound if any tiles were contaminated
        if (contaminated)
        {
            PlayContaminateSound();
        }
    }

    /// <summary>
    /// Play contamination sound effect
    /// </summary>
    protected virtual void PlayContaminateSound()
    {
        if (contaminateSound != null && monsterAudioSource != null)
        {
            monsterAudioSource.PlayOneShot(contaminateSound, contaminateSoundVolume);
        }
    }

    /// <summary>
    /// Play a one-shot sound effect at this monster's position
    /// </summary>
    protected void PlaySound(AudioClip clip, float volume = 1f)
    {
        if (clip != null && monsterAudioSource != null)
        {
            monsterAudioSource.PlayOneShot(clip, volume);
        }
    }

    /// <summary>
    /// Play death sound and handle cleanup
    /// Called when monster is destroyed
    /// </summary>
    protected virtual void PlayDeathSound()
    {
        if (deathSound != null)
        {
            // Create temporary audio source for death sound
            // (since this GameObject is being destroyed)
            GameObject tempAudio = new GameObject($"{name}_DeathSound");
            tempAudio.transform.position = transform.position;

            AudioSource tempSource = tempAudio.AddComponent<AudioSource>();
            tempSource.clip = deathSound;
            tempSource.spatialBlend = 1f;
            tempSource.rolloffMode = AudioRolloffMode.Linear;
            tempSource.minDistance = audioMinDistance;
            tempSource.maxDistance = audioMaxDistance;
            tempSource.Play();

            // Destroy after sound finishes
            Destroy(tempAudio, deathSound.length + 0.1f);
        }
    }

    protected virtual void CheckLifetime()
    {
        if (Time.time - spawnTime >= maxLifetime)
        {
            PlayDeathSound();
            Destroy(gameObject);
        }

        if (transform.position.magnitude > 200f)
        {
            Destroy(gameObject);
        }
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

    public virtual void SetMoveSpeed(float s) { moveSpeed = s; }

    // For debugging
    protected virtual void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, contaminationRadius);
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(transform.position, moveDirection.normalized * 2f);

        // Draw audio range
        Gizmos.color = new Color(0f, 1f, 1f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, audioMaxDistance);
        Gizmos.color = new Color(0f, 1f, 1f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, audioMinDistance);
    }
}