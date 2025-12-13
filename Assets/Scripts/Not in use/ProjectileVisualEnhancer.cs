using UnityEngine;

public class ProjectileVisualEnhancer : MonoBehaviour
{
    public ParticleSystem cleansingParticles;
    public ParticleSystem trailParticles;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // setup particles to follow the projectile
        if (cleansingParticles != null)
        {
            var main = cleansingParticles.main;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = 0.5f;
            main.startSize = 0.2f;
            main.startSpeed = 1f;

            // Green particles for cleansing effect
            var colorOverLifetime = cleansingParticles.colorOverLifetime;
            colorOverLifetime.enabled = true;

            // Create a gradient from green to white
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] { new GradientColorKey(Color.green, 0.0f), new GradientColorKey(Color.white, 1.0f) },
                new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(0.0f, 1.0f) }
            );

            colorOverLifetime.color = gradient;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (cleansingParticles != null)
        {
            cleansingParticles.transform.position = transform.position;
        }
    }
}
