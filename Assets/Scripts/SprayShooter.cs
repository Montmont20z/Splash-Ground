using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

public class SprayShooter : MonoBehaviour
{
    [Header("Spray Settings")]
    public float sprayRange = 20f;
    public float sprayRadius = 2f;
    public float sprayArcHeight = 3f;
    public float fireRate = 0.8f; // Shots per second
    public int sprayPower = 5; // how many tiles to cleanse per shot

    [Header("Ammo/Reload")]
    public int bulletCount = 5;
    public int maxBullet = 5;
    public float reloadTime = 1.2f;
    public bool allowInterruptReload = false; // if true, you can cancel reload by firing or other actions

    [Header("Projectile Visual")]
    public GameObject projectilePrefab;
    public float projectileSpeed = 15f;
    public Transform sprayOrigin;

    [Header("Effects")]
    public GameObject sprayImpactEffect;
    public LayerMask groundLayer;
    public LayerMask floorTileLayer;

    [Header("UI")]
    public GameObject aimReticle;

    [Header("Audio / Animation")]
    public AudioClip reloadClip;
    public AudioClip spraySound;
    public AudioClip impactSound;
    public AudioSource audioSource; // inspector-assignable; will add one if missing
    public Animator animator; // reload animation

    private float nextFireTime = 0f;
    private Camera mainCamera;
    private bool isReloading = false;

    void Start()
    {
        mainCamera = Camera.main;
        if (sprayOrigin == null)
            sprayOrigin = transform; // default to this object's transform

        // Ensure audioSource exists: prefer inspector-assigned, otherwise add/get one.
        if (audioSource == null)
        {
            audioSource = gameObject.GetComponent<AudioSource>();
            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    void Update()
    {
        ShowAimPreview();

        // Reload button
        if (Input.GetButtonDown("Reload"))
        {
            if (!isReloading && bulletCount < maxBullet)
            {
                StartCoroutine(Reload());
            }
        }

        // Fire input
        if (Input.GetButton("Fire1") && Time.time >= nextFireTime)
        {
            // If currently reloading, optionally interrupt
            if (isReloading)
            {
                if (allowInterruptReload && Input.GetButtonDown("Fire1"))
                {
                    // interrupt handled inside Reload coroutine as well, but stop here for safety
                    isReloading = false;
                }
                else
                {
                    // do not fire while reloading
                    return;
                }
            }

            if (bulletCount > 0)
            {
                Spray();
                bulletCount--;
            }
            else
            {
                // Auto reload when out of bullets
                if (!isReloading)
                    StartCoroutine(Reload());
            }

            nextFireTime = Time.time + fireRate;
        }
    }

    IEnumerator Reload()
    {
        isReloading = true;

        // Play animator trigger
        if (animator != null) animator.SetTrigger("Reload");
        if (audioSource != null && reloadClip != null) audioSource.PlayOneShot(reloadClip);

        float timer = 0f;
        while (timer < reloadTime)
        {
            if (allowInterruptReload)
            {
                // interrupt: user fires
                // add more if needed (e.g., movement, switch weapon)
                if (Input.GetButtonDown("Fire1"))
                {
                    // Cancel reload and exit
                    isReloading = false;
                    yield break;
                }
            }
            timer += Time.deltaTime;
            yield return null;
        }

        bulletCount = maxBullet;
        isReloading = false;
        yield break;
    }

    void Spray()
    {
        // Play spray sound immediately
        if (spraySound != null && audioSource != null)
        {
            audioSource.PlayOneShot(spraySound, 0.5f);
        }

        Ray ray = mainCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, sprayRange, groundLayer))
        {
            Vector3 impactPoint = hit.point;

            // Spawn Visual Projectile
            if (projectilePrefab != null)
            {
                SpawnProjectileVisual(sprayOrigin.position, impactPoint);
            }

            // Spawn Impact Effect
            if (sprayImpactEffect != null)
            {
                GameObject effect = Instantiate(sprayImpactEffect, impactPoint, Quaternion.identity);
                Destroy(effect, 2f);
            }

            // Play impact sound (optional)
            if (impactSound != null && audioSource != null)
            {
                // Play after small delay or immediately; since we spawn projectile, we delay actual cleansing,
                // but we can play impact sound now or when projectile arrives. Here we play on arrival inside DelayedCleanse.
            }

            // Delay tile cleanse until projectile "arrives"
            StartCoroutine(DelayedCleanse(impactPoint, sprayRadius));
        }
    }

    IEnumerator DelayedCleanse(Vector3 center, float radius)
    {
        // Calculate travel time based on distance and projectile speed
        float distance = Vector3.Distance((sprayOrigin != null) ? sprayOrigin.position : transform.position, center);
        float travelTime = (projectileSpeed > 0f) ? distance / projectileSpeed : 0f;
        yield return new WaitForSeconds(travelTime);

        // play impact sound at arrival
        if (impactSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(impactSound, 0.8f);
        }

        CleanseTilesInArea(center, radius);
        yield break;
    }

    void CleanseTilesInArea(Vector3 center, float radius)
    {
        // Find all floor tiles within the radius
        Collider[] hitColliders = Physics.OverlapSphere(center, radius, floorTileLayer);

        int cleansedCount = 0;

        foreach (Collider col in hitColliders)
        {
            FloorTile tile = col.GetComponent<FloorTile>();
            if (tile != null && tile.currentState == FloorTile.TileState.Contaminated)
            {
                tile.Cleanse();
                cleansedCount++;
            }
        }
        // Optionally: Debug.Log($"Cleansed {cleansedCount} tiles.");
    }

    void ShowAimPreview()
    {
        Ray ray = mainCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, sprayRange, groundLayer))
        {
            // Could draw debug sphere or update UI indicator here
            Debug.DrawLine(mainCamera.transform.position, hit.point, Color.cyan);
        }
    }

    void SpawnProjectileVisual(Vector3 start, Vector3 end)
    {
        GameObject projectile = Instantiate(projectilePrefab, start, Quaternion.identity);
        ProjectileArc arcScript = projectile.AddComponent<ProjectileArc>();
        arcScript.Initialize(start, end, sprayArcHeight, projectileSpeed);
    }

    // Visualize spray radius in Scene view
    void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;

        if (mainCamera == null)
            mainCamera = Camera.main;

        Ray ray = mainCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, sprayRange, groundLayer))
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(hit.point, sprayRadius);
        }
    }
}
