using System;
using UnityEngine;

public class SprayShooter : MonoBehaviour
{
    [Header("Spray Settings")]
    public float sprayRange = 20f;
    public float sprayRadius = 2f;
    public float sprayArcHeight = 3f;
    public float fireRate = 0.2f; // Shots per second
    public int sprayPower = 5; // how many tiles to cleanse per shot

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

    private float nextFireTime = 0f;
    private Camera mainCamera;

    

    void Start()
    {
        mainCamera = Camera.main;
        if (sprayOrigin == null)
            sprayOrigin = transform; // default to camera position
    }

    void Update()
    {
        ShowAimPreview();

        if (Input.GetButton("Fire1") && Time.time >= nextFireTime)
        {
            Spray();
            nextFireTime = Time.time + fireRate;
        }

    }

    void Spray()
    {
        Ray ray = mainCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
        RaycastHit hit;

        
        if(Physics.Raycast(ray, out hit, sprayRange, groundLayer))
        {
            Vector3 impactPoint = hit.point;

            // Spawn Visual Projectile
            if(projectilePrefab != null)
            {
                SpawnProjectileVisual(sprayOrigin.position, impactPoint);
            }

            // Spawn Impact Effect
            if (sprayImpactEffect != null)
            {
                GameObject effect = Instantiate(sprayImpactEffect, impactPoint, Quaternion.identity);
                Destroy(effect, 2f);
            }

            CleanseTilesInArea(impactPoint, sprayRadius);

        }

    }

    void CleanseTilesInArea(Vector3 center, float radius)
    {
        // Find all floor tiles within the radius
        Collider[] hitColliders = Physics.OverlapSphere(center, radius, floorTileLayer);

        int cleansedCount = 0;

        foreach(Collider col in hitColliders)
        {
            FloorTile tile = col.GetComponent<FloorTile>();
            if(tile != null && tile.currentState == FloorTile.TileState.Contaminated)
            {
                tile.Cleanse();
                cleansedCount++;
            }

        }
        Debug.Log($"Cleansed {cleansedCount} tiles.");
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

        Ray ray = mainCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, sprayRange, groundLayer))
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(hit.point, sprayRadius);
        }
    }


}
