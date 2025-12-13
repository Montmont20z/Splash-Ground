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
