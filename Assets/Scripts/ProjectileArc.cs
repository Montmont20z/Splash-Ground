using System.Net.Mime;
using UnityEngine;

public class ProjectileArc : MonoBehaviour
{
    private Vector3 startPos;
    private Vector3 endPos;
    private float arcHeight;
    private float speed;
    private float progress = 0f;
    private float journeyTime = 0f;
    private float startTime = 0f;

    // Visual component
    private Renderer projectileRenderer;
    private Light glowLight;
    private ParticleSystem trailParticles;
    private Material projectileMaterial;

    // Visual settings
    [Header("Visual Settings")]
    public Color startColor = new Color(0.4f, 1.0f, 1.0f); // cyan
    public Color endColor = Color.white;
    public float startEmissionIntensity = 2f;
    public float endEmissionIntensity = 5f;

    public void Initialize(Vector3 start, Vector3 end, float height, float travelSpeed)
    {
        startPos = start;
        endPos = end;
        arcHeight = height;
        speed = travelSpeed;

        // Calculate total journey time based on distance and speed
        float distance = Vector3.Distance(start, end);
        journeyTime = distance / speed;
        startTime = Time.time;

        // Cache components
        projectileRenderer = GetComponent<Renderer>();
        glowLight = GetComponentInChildren<Light>();
        trailParticles = GetComponentInChildren<ParticleSystem>();

        // create material instance for each projectile
        if (projectileRenderer != null)
        {
            projectileMaterial = projectileRenderer.material;
        }
    }

    void Update()
    {
        // Calculate progress based on elapsed time
        float elapsedTime = Time.time - startTime;
        progress = elapsedTime / journeyTime;

        if (progress >= 1f)
        {
            // Reached destination
            progress = 1f;
            Destroy(gameObject);
            return;
        }

        UpdatePosition();
        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        // Color transition (cyan -> white) as it travels
        Color currentColor = Color.Lerp(startColor, endColor, progress);

        // Update material color and emission
        if (projectileMaterial != null)
        {
            projectileMaterial.SetColor("_Color", currentColor);

            // Increase emission intensity over time
            float emissionIntensity = Mathf.Lerp(startEmissionIntensity, endEmissionIntensity, progress);
            projectileMaterial.SetColor("_EmissionColor", currentColor * emissionIntensity);
        }

        // Update glow light intensity (pulse effect)
        if (glowLight != null)
        {
            float pulse = 1f + Mathf.Sin(Time.time * 10f) * 0.3f; // Pulsing effect
            glowLight.intensity = Mathf.Lerp(2f, 5f, progress) * pulse;
            glowLight.color = currentColor;
        }

        // Scale projectile slightly (grows as it approaches)
        float scale = Mathf.Lerp(0.8f, 1.2f, progress);
        transform.localScale = Vector3.one * scale;

        // Particle system rate (more particles as it travels)
        if (trailParticles != null)
        {
            var emission = trailParticles.emission;
            emission.rateOverTime = Mathf.Lerp(20f, 50f, progress);
        }


    }

    private void UpdatePosition()
    {
        // Calculate position on arc (parabola)
        Vector3 currentPos = Vector3.Lerp(startPos, endPos, progress);

        // add arc height using sine wave for smooth arc
        float arcProgress = Mathf.Sin(progress * Mathf.PI); // Creates a smooth arc
        currentPos.y += arcHeight * arcProgress;
        transform.position = currentPos;

        // Rotate projectile to face movement direction
        if (progress < 0.99f)
        {
            float nextProgress = Mathf.Min(progress + 0.01f, 1f);
            Vector3 nextPos = Vector3.Lerp(startPos, endPos, nextProgress);
            nextPos.y += arcHeight * Mathf.Sin(nextProgress * Mathf.PI);

            Vector3 direction = (nextPos - currentPos).normalized;
            if (direction != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(nextPos - currentPos);
            }
        }
    }

    // Trail renderer for better visual
    void OnDrawGizmos()
    {
        if (progress > 0)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, 0.1f);
        }
    }
}