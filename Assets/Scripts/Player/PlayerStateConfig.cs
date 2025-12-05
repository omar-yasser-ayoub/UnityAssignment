using UnityEngine;

/// <summary>
/// ScriptableObject containing all configuration values for player states.
/// Create via: Right-click in Project > Create > Matter > Player State Config
/// This allows easy tuning of all physics values without modifying code.
/// </summary>
[CreateAssetMenu(fileName = "PlayerStateConfig", menuName = "Matter/Player State Config")]
public class PlayerStateConfig : ScriptableObject
{
    [Header("=== SOLID STATE ===")]
    [Tooltip("Base movement speed in solid form")]
    public float solidMoveSpeed = 5f;
    
    [Tooltip("Multiplier when running")]
    public float solidRunMultiplier = 1.5f;
    
    [Tooltip("Jump force in solid form")]
    public float solidJumpForce = 10f;

    [Header("=== LIQUID STATE ===")]
    [Tooltip("Number of liquid particles to spawn")]
    [Range(5, 50)]
    public int liquidParticleCount = 15;
    
    [Tooltip("Radius within which particles spawn")]
    public float liquidSpawnRadius = 0.5f;
    
    [Tooltip("Base movement speed (force applied)")]
    public float liquidMoveSpeed = 3f;
    
    [Tooltip("Run multiplier for liquid")]
    public float liquidRunMultiplier = 1.3f;
    
    [Tooltip("Force multiplier for movement")]
    public float liquidForceMultiplier = 10f;
    
    [Tooltip("Maximum horizontal speed")]
    public float liquidMaxSpeed = 4f;
    
    [Tooltip("Jump force (weaker than solid)")]
    public float liquidJumpForce = 5f;

    [Header("Liquid Particle Physics")]
    [Tooltip("Gravity scale for liquid particles")]
    public float liquidGravityScale = 1f;
    
    [Tooltip("Mass of each particle")]
    public float liquidParticleMass = 0.1f;
    
    [Tooltip("Radius of each particle collider")]
    public float liquidParticleRadius = 0.15f;
    
    [Tooltip("Linear drag on particles")]
    public float liquidDrag = 1f;
    
    [Tooltip("Angular drag on particles")]
    public float liquidAngularDrag = 0.5f;
    
    [Tooltip("Physics material for liquid particles (for bounciness)")]
    public PhysicsMaterial2D liquidPhysicsMaterial;

    [Header("Liquid Cohesion (Keeps Blob Together)")]
    [Tooltip("Enable cohesion between liquid particles")]
    public bool liquidUseCohesion = true;
    
    [Tooltip("Radius to check for nearby particles")]
    public float liquidCohesionRadius = 2f;
    
    [Tooltip("Force attracting particles to each other")]
    public float liquidCohesionForce = 5f;
    
    [Tooltip("Force attracting particles to group center")]
    public float liquidCenterAttraction = 8f;
    
    [Tooltip("Maximum distance a particle can be from group center")]
    public float liquidMaxSeparation = 2f;
    
    [Tooltip("Use hard limit (teleport) vs soft limit (strong force)")]
    public bool liquidHardSeparationLimit = true;
    
    [Tooltip("Force applied when beyond max separation (if soft limit)")]
    public float liquidSeparationForce = 50f;

    [Header("Liquid Blob Rendering (Marching Squares)")]
    [Tooltip("Material with the LiquidBlob shader")]
    public Material blobMaterial;
    
    [Tooltip("Size of the marching squares grid in world units")]
    public float blobGridSize = 6f;
    
    [Tooltip("Resolution of the grid (higher = smoother but slower)")]
    [Range(20, 80)]
    public int blobGridResolution = 50;
    
    [Tooltip("Radius of influence for each particle in the field")]
    public float blobParticleRadius = 0.8f;
    
    [Tooltip("Threshold for surface - LOWER = bigger blobs")]
    [Range(0.05f, 0.5f)]
    public float blobSurfaceThreshold = 0.15f;
    
    [Tooltip("Falloff power - higher = sharper edges")]
    [Range(1f, 4f)]
    public float blobFalloffPower = 2f;

    [Header("Liquid Colors")]
    [Tooltip("Inner/deep color of the liquid")]
    public Color liquidInnerColor = new Color(0.1f, 0.3f, 0.8f, 1f);
    
    [Tooltip("Outer/surface color of the liquid")]
    public Color liquidOuterColor = new Color(0.3f, 0.6f, 1f, 0.9f);
    
    [Tooltip("Edge highlight color")]
    public Color liquidEdgeColor = new Color(0.7f, 0.9f, 1f, 1f);

    [Header("=== GAS STATE ===")]
    [Tooltip("Number of gas particles to spawn")]
    [Range(5, 30)]
    public int gasParticleCount = 10;
    
    [Tooltip("Radius within which particles spawn")]
    public float gasSpawnRadius = 0.8f;
    
    [Tooltip("Constant upward force applied")]
    public float gasRiseForce = 5f;
    
    [Tooltip("Maximum rise speed")]
    public float gasMaxRiseSpeed = 3f;
    
    [Tooltip("Horizontal drift speed from input")]
    public float gasDriftSpeed = 2f;
    
    [Tooltip("Maximum horizontal speed")]
    public float gasMaxHorizontalSpeed = 2f;
    
    [Tooltip("Time before gas automatically condenses (0 = never)")]
    public float gasAutocondenseTime = 5f;

    [Header("Gas Particle Physics")]
    [Tooltip("Gravity scale (0 or negative for floating)")]
    public float gasGravityScale = 0f;
    
    [Tooltip("Mass of each gas particle")]
    public float gasParticleMass = 0.01f;
    
    [Tooltip("Radius of each particle collider")]
    public float gasParticleRadius = 0.1f;
    
    [Tooltip("Linear drag")]
    public float gasDrag = 2f;
    
    [Tooltip("Angular drag")]
    public float gasAngularDrag = 1f;
    
    [Tooltip("Make gas particles triggers (pass through objects)")]
    public bool gasIsTrigger = false;
    
    [Tooltip("Condense when hitting ceiling")]
    public bool gasCondenseOnCeilingHit = true;

    [Header("Gas Behavior")]
    [Tooltip("Enable random drift for organic movement")]
    public bool gasUseRandomDrift = true;
    
    [Tooltip("Frequency of the drift oscillation")]
    public float gasDriftFrequency = 2f;
    
    [Tooltip("Amplitude of the drift")]
    public float gasDriftAmplitude = 0.5f;
    
    [Tooltip("Enable dispersion (particles spread out)")]
    public bool gasUseDispersion = true;
    
    [Tooltip("Radius to check for dispersion")]
    public float gasDispersionRadius = 0.5f;
    
    [Tooltip("Force pushing particles apart")]
    public float gasDispersionForce = 1f;

    [Header("=== FROZEN STATE ===")]
    [Tooltip("Mass when frozen (heavy!)")]
    public float frozenMass = 5f;
    
    [Tooltip("Drag when frozen (low = slippery)")]
    public float frozenDrag = 0.5f;
    
    [Tooltip("Force applied for movement")]
    public float frozenMoveForce = 30f;
    
    [Tooltip("Maximum speed when frozen")]
    public float frozenMaxSpeed = 3f;

    [Header("=== ENERGY SYSTEM ===")]
    [Tooltip("Maximum energy")]
    public float maxEnergy = 100f;
    
    [Tooltip("Energy cost to transform to liquid")]
    public float liquidTransformCost = 20f;
    
    [Tooltip("Whether cave levels have unlimited energy")]
    public bool caveLevelUnlimitedEnergy = true;
}
