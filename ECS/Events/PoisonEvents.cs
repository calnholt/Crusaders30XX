using Microsoft.Xna.Framework;

namespace Crusaders30XX.ECS.Events;

public class PoisonDamageEvent
{
    public float DurationSec { get; set; }
    
    // Fade curve parameters (nullable = use overlay defaults)
    public float? AttackDuration { get; set; }
    public float? DecayRate { get; set; }
    
    // Screen shake parameters
    public float? ShakeFrequency { get; set; }
    public float? ShakeAmplitude { get; set; }
    
    // Radial distortion wave parameters
    public float? WaveFrequency { get; set; }
    public float? WaveSpeed { get; set; }
    public float? WaveAmplitude { get; set; }
    
    // Vignette parameters
    public float? VignetteStart { get; set; }
    public float? VignetteIntensity { get; set; }
    
    // Color grading parameters
    public Vector3? PoisonTint { get; set; }
    public float? PoisonMixAmount { get; set; }
    public float? DesaturationAmount { get; set; }
}

