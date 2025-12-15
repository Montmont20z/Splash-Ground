using UnityEngine;

/// <summary>
/// Defines all available power-up types in the game
/// </summary>
public enum PowerUpType
{
    StunSingle,      // Stuns nearest enemy
    StunAll,         // Stuns all enemies in arena
    RapidFire,       // Increases fire rate temporarily
    InfiniteAmmo,    // No reload needed for duration
    WideSpray,       // Increases spray radius
    CleanseWave,      // Cleanses large area around player
    DestroyAll       // Destroys all enemies in arena
}