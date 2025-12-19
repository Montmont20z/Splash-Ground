using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PowerUpManager : MonoBehaviour
{
    [Header("Duration Settings")]
    public float stunSingleDuration = 3f;
    public float stunAllDuration = 5f;
    public float rapidFireDuration = 10f;
    public float infiniteAmmoDuration = 8f;
    public float wideSprayDuration = 12f;
    public float cleanseWaveRadius = 50f;

    [Header("Modifier Values")]
    public float rapidFireMultiplier = 4.5f;
    public float wideSprayMultiplier = 2f;

    [Header("References")]
    public SprayShooter sprayShooter;
    public Transform playerTransform;
    public LayerMask monsterLayer;
    public LayerMask floorTileLayer;

    [Header("Audio")]
    public AudioClip stunSound;
    public AudioClip rapidFireSound;
    public AudioClip cleanseWaveSound;
    public AudioClip destroyAllSound;

    [Header("Epic Destruction Effects")]
    public GameObject mainExplosionPrefab;
    public GameObject enemyDeathParticlePrefab;
    public GameObject shockwavePrefab;
    public GameObject lightningBoltPrefab;

    [Header("Explosion Settings")]
    public float explosionRadius = 50f;
    public float explosionForce = 2000f;
    public float delayBetweenEnemies = 0.08f;
    public float enemyDissolveTime = 0.4f;
    public bool useScreenShake = true;
    public float screenShakeDuration = 0.8f;
    public float screenShakeIntensity = 0.4f;

    private bool isRapidFireActive = false;
    private bool isInfiniteAmmoActive = false;
    private bool isWideSprayActive = false;

    private float originalFireRate;
    private float originalSprayRadius;
    private Camera mainCam;

    void Start()
    {
        if (sprayShooter == null)
            sprayShooter = FindFirstObjectByType<SprayShooter>();

        if (playerTransform == null)
            playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;

        if (sprayShooter != null)
        {
            originalFireRate = sprayShooter.fireRate;
            originalSprayRadius = sprayShooter.sprayRadius;
        }

        mainCam = Camera.main;
    }

    public void ActivatePowerUp(PowerUpType type)
    {
        switch (type)
        {
            case PowerUpType.StunSingle:
                StunNearestEnemy();
                break;
            case PowerUpType.StunAll:
                StunAllEnemies();
                break;
            case PowerUpType.RapidFire:
                StartCoroutine(RapidFireEffect());
                break;
            case PowerUpType.InfiniteAmmo:
                StartCoroutine(InfiniteAmmoEffect());
                break;
            case PowerUpType.WideSpray:
                StartCoroutine(WideSprayEffect());
                break;
            case PowerUpType.CleanseWave:
                CleanseWave();
                break;
            case PowerUpType.DestroyAll:
                StartCoroutine(EpicDestroyAllEnemies());
                break;
        }
    }

    #region Stun Effects

    void StunNearestEnemy()
    {
        if (playerTransform == null) return;

        MonsterBase[] monsters = FindObjectsByType<MonsterBase>(FindObjectsSortMode.None);
        MonsterBase nearest = null;
        float minDist = float.MaxValue;

        foreach (var monster in monsters)
        {
            float dist = Vector3.Distance(playerTransform.position, monster.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                nearest = monster;
            }
        }

        if (nearest != null)
        {
            StartCoroutine(StunMonster(nearest, stunSingleDuration));
            PlaySound(stunSound);
        }
    }

    void StunAllEnemies()
    {
        MonsterBase[] monsters = FindObjectsByType<MonsterBase>(FindObjectsSortMode.None);

        foreach (var monster in monsters)
        {
            StartCoroutine(StunMonster(monster, stunAllDuration));
        }

        PlaySound(stunSound);
    }

    IEnumerator StunMonster(MonsterBase monster, float duration)
    {
        if (monster == null) yield break;

        monster.enabled = false;

        Renderer rend = monster.GetComponent<Renderer>();
        Color originalColor = Color.white;
        if (rend != null)
        {
            originalColor = rend.material.color;
            rend.material.color = Color.cyan;
        }

        yield return new WaitForSeconds(duration);

        if (monster != null)
        {
            monster.enabled = true;
            if (rend != null)
                rend.material.color = originalColor;
        }
    }

    #endregion

    #region Shooter Enhancement Effects

    IEnumerator RapidFireEffect()
    {
        if (sprayShooter == null || isRapidFireActive) yield break;

        isRapidFireActive = true;
        sprayShooter.fireRate = originalFireRate / rapidFireMultiplier;
        PlaySound(rapidFireSound);

        yield return new WaitForSeconds(rapidFireDuration);

        sprayShooter.fireRate = originalFireRate;
        isRapidFireActive = false;
    }

    IEnumerator InfiniteAmmoEffect()
    {
        if (sprayShooter == null || isInfiniteAmmoActive) yield break;

        isInfiniteAmmoActive = true;
        int originalMaxBullet = sprayShooter.maxBullet;
        sprayShooter.maxBullet = 9999;
        sprayShooter.bulletCount = 9999;

        yield return new WaitForSeconds(infiniteAmmoDuration);

        sprayShooter.maxBullet = originalMaxBullet;
        sprayShooter.bulletCount = Mathf.Min(sprayShooter.bulletCount, originalMaxBullet);
        isInfiniteAmmoActive = false;
    }

    IEnumerator WideSprayEffect()
    {
        if (sprayShooter == null || isWideSprayActive) yield break;

        isWideSprayActive = true;
        sprayShooter.sprayRadius = originalSprayRadius * wideSprayMultiplier;

        yield return new WaitForSeconds(wideSprayDuration);

        sprayShooter.sprayRadius = originalSprayRadius;
        isWideSprayActive = false;
    }

    void InstantReload()
    {
        if (sprayShooter == null) return;
        sprayShooter.bulletCount = sprayShooter.maxBullet;
    }

    #endregion

    #region Area Effects

    void CleanseWave()
    {
        if (playerTransform == null) return;

        PlaySound(cleanseWaveSound);

        Collider[] hitColliders = Physics.OverlapSphere(playerTransform.position, cleanseWaveRadius, floorTileLayer);

        int cleansedCount = 0;
        foreach (Collider col in hitColliders)
        {
            FloorTile tile = col.GetComponent<FloorTile>();
            if (tile != null && tile.currentState != FloorTile.TileState.Healthy)
            {
                tile.Cleanse();
                cleansedCount++;
            }
        }

        Debug.Log($"Cleanse Wave: Cleaned {cleansedCount} tiles");
    }

    IEnumerator EpicDestroyAllEnemies()
    {
        MonsterBase[] monsters = FindObjectsByType<MonsterBase>(FindObjectsSortMode.None);

        if (monsters.Length == 0)
        {
            Debug.Log("No enemies to destroy!");
            yield break;
        }

        PlaySound(destroyAllSound);

        // Calculate center point (player or arena center)
        Vector3 explosionCenter = playerTransform != null ? playerTransform.position : Vector3.zero;

        // Spawn main explosion at center
        if (mainExplosionPrefab != null)
        {
            GameObject mainExplosion = Instantiate(mainExplosionPrefab, explosionCenter, Quaternion.identity);
            Destroy(mainExplosion, 5f);
        }

        // Spawn shockwave
        if (shockwavePrefab != null)
        {
            GameObject shockwave = Instantiate(shockwavePrefab, explosionCenter, Quaternion.identity);
            Destroy(shockwave, 3f);
        }

        // Screen shake
        if (useScreenShake && mainCam != null)
        {
            StartCoroutine(ScreenShake());
        }

        // Sort enemies by distance for wave effect
        List<MonsterBase> sortedMonsters = new List<MonsterBase>(monsters);
        sortedMonsters.Sort((a, b) =>
            Vector3.Distance(explosionCenter, a.transform.position)
            .CompareTo(Vector3.Distance(explosionCenter, b.transform.position))
        );

        // Destroy each enemy with epic effects
        int destroyedCount = 0;
        foreach (var monster in sortedMonsters)
        {
            if (monster != null)
            {
                StartCoroutine(DestroyEnemyWithEffect(monster, explosionCenter));
                destroyedCount++;
                yield return new WaitForSeconds(delayBetweenEnemies);
            }
        }

        Debug.Log($"Epic Destruction Complete: Eliminated {destroyedCount} enemies!");
    }

    IEnumerator DestroyEnemyWithEffect(MonsterBase monster, Vector3 explosionPos)
    {
        if (monster == null) yield break;

        // Spawn lightning bolt from sky
        if (lightningBoltPrefab != null)
        {
            Vector3 skyPos = monster.transform.position + Vector3.up * 20f;
            GameObject lightning = Instantiate(lightningBoltPrefab, skyPos, Quaternion.Euler(90, 0, 0));
            Destroy(lightning, 1f);
        }

        // Apply physics force
        Rigidbody rb = monster.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.AddExplosionForce(explosionForce, explosionPos, explosionRadius, 3f, ForceMode.Impulse);
        }

        // Spawn particle effect at enemy
        if (enemyDeathParticlePrefab != null)
        {
            GameObject particles = Instantiate(enemyDeathParticlePrefab, monster.transform.position, Quaternion.identity);
            Destroy(particles, 3f);
        }

        // Dissolve effect
        Renderer renderer = monster.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material mat = renderer.material;
            Color startColor = mat.color;
            Color glowColor = new Color(1f, 0.5f, 0f); // Orange glow
            float elapsed = 0f;

            while (elapsed < enemyDissolveTime)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / enemyDissolveTime;

                // Glow then fade
                if (t < 0.3f)
                {
                    mat.color = Color.Lerp(startColor, glowColor, t / 0.3f);
                }
                else
                {
                    float alpha = Mathf.Lerp(1f, 0f, (t - 0.3f) / 0.7f);
                    mat.color = new Color(glowColor.r, glowColor.g, glowColor.b, alpha);
                }

                // Scale down and rotate
                float scale = Mathf.Lerp(1f, 0f, t);
                monster.transform.localScale = Vector3.one * scale;
                monster.transform.Rotate(Vector3.up, 1000f * Time.deltaTime);

                yield return null;
            }
        }

        Destroy(monster.gameObject);
    }

    IEnumerator ScreenShake()
    {
        if (mainCam == null) yield break;

        Vector3 originalPos = mainCam.transform.localPosition;
        float elapsed = 0f;

        while (elapsed < screenShakeDuration)
        {
            float strength = Mathf.Lerp(screenShakeIntensity, 0f, elapsed / screenShakeDuration);
            float x = Random.Range(-1f, 1f) * strength;
            float y = Random.Range(-1f, 1f) * strength;

            mainCam.transform.localPosition = originalPos + new Vector3(x, y, 0);

            elapsed += Time.deltaTime;
            yield return null;
        }

        mainCam.transform.localPosition = originalPos;
    }

    #endregion

    void PlaySound(AudioClip clip)
    {
        if (clip != null && playerTransform != null)
        {
            AudioSource.PlayClipAtPoint(clip, playerTransform.position);
        }
    }

    public bool IsRapidFireActive() => isRapidFireActive;
    public bool IsInfiniteAmmoActive() => isInfiniteAmmoActive;
    public bool IsWideSprayActive() => isWideSprayActive;
}