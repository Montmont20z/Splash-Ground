using UnityEngine;

/// <summary>
/// Pickup item that grants power-ups to the player.
/// Uses different 3D models for each power-up type.
/// </summary>
public class PowerUpPickup : MonoBehaviour
{
    [Header("Power-Up Settings")]
    public PowerUpType powerUpType;
    public float rotationSpeed = 50f;
    public float bobSpeed = 2f;
    public float bobHeight = 0.3f;

    [Header("3D Models for Each Type")]
    public GameObject stunSingleModel;
    public GameObject stunAllModel;
    public GameObject rapidFireModel;
    public GameObject infiniteAmmoModel;
    public GameObject wideSprayModel;
    public GameObject cleanseWaveModel;
    public GameObject destroyAllModel;

    [Header("Audio")]
    public AudioClip pickupSound;

    private Vector3 startPosition;
    private GameObject currentModel;

    void Start()
    {
        startPosition = transform.position;
        SpawnCorrectModel();
    }

    void Update()
    {
        // Rotate pickup
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);

        // Bob up and down
        float newY = startPosition.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }

    void SpawnCorrectModel()
    {
        // Destroy any existing model
        if (currentModel != null)
        {
            Destroy(currentModel);
        }

        // Get the prefab for this power-up type
        GameObject modelPrefab = powerUpType switch
        {
            PowerUpType.StunSingle => stunSingleModel,
            PowerUpType.StunAll => stunAllModel,
            PowerUpType.RapidFire => rapidFireModel,
            PowerUpType.InfiniteAmmo => infiniteAmmoModel,
            PowerUpType.WideSpray => wideSprayModel,
            PowerUpType.CleanseWave => cleanseWaveModel,
            PowerUpType.DestroyAll => destroyAllModel,
            _ => null
        };

        // Instantiate the model as a child
        if (modelPrefab != null)
        {
            currentModel = Instantiate(modelPrefab, transform);
            currentModel.transform.localPosition = Vector3.zero;
            //currentModel.transform.localRotation = ;
        }
        else
        {
            Debug.LogWarning($"[PowerUpPickup] No model assigned for {powerUpType}!");
        }
    }

    // Call this if powerUpType changes at runtime
    public void RefreshModel()
    {
        SpawnCorrectModel();
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PowerUpManager powerUpManager = FindFirstObjectByType<PowerUpManager>();
            if (powerUpManager != null)
            {
                powerUpManager.ActivatePowerUp(powerUpType);

                if (pickupSound != null)
                {
                    AudioSource.PlayClipAtPoint(pickupSound, transform.position);
                }

                Destroy(gameObject);
            }
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
    }

    // Editor helper to preview model changes
    void OnValidate()
    {
        if (Application.isPlaying)
        {
            SpawnCorrectModel();
        }
    }
}