using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Gas state - the player becomes floating particles that rise upward
/// with limited horizontal control. Used for reaching high areas.
/// Rendered as a wispy cloud using marching squares.
/// </summary>
public class GasState : IPlayerState
{
    public MatterState StateType => MatterState.Gas;

    private PlayerStateMachine player;
    private PlayerStateConfig config;
    private List<GasParticle> particles = new List<GasParticle>();
    private GasBlobRenderer blobRenderer;
    
    private float stateTimer;

    public void Enter(PlayerStateMachine player)
    {
        this.player = player;
        this.config = player.Config;
        this.stateTimer = 0f;

        // Clear any existing particle references
        GasParticle.ClearAllParticles();

        // Hide the main player body
        player.SetMainBodyVisible(false);

        // Setup blob renderer (marching squares for gas cloud)
        SetupBlobRenderer();

        // Spawn gas particles at player position
        SpawnParticles();

        // Set animator state
        SetAnimatorBoolSafe("IsGas", true);

        Debug.Log($"Entered Gas state with {particles.Count} particles");
    }

    public void Update()
    {
        // Update player position to follow particle center (for camera)
        if (particles.Count > 0)
        {
            player.transform.position = GetParticlesCenter();
        }

        // Track time in gas state (for auto-condensation if desired)
        stateTimer += Time.deltaTime;

        // Optional: Auto-condense after a certain time
        if (config.gasAutocondenseTime > 0 && stateTimer >= config.gasAutocondenseTime)
        {
            Condense();
        }
    }

    public void FixedUpdate()
    {
        HandleMovement(player.MoveInput, player.IsRunning);
        ApplyConstantRise();
    }

    public void Exit()
    {
        // Move player to center of particles before clearing
        player.transform.position = GetParticlesCenter();

        // Clear static list first
        GasParticle.ClearAllParticles();

        // Clear all particles
        ClearParticles();

        // Cleanup blob renderer
        CleanupBlobRenderer();

        SetAnimatorBoolSafe("IsGas", false);
    }

    public void HandleMovement(Vector2 input, bool isRunning)
    {
        // Gas has limited horizontal control - just drifting
        float driftSpeed = config.gasDriftSpeed;

        foreach (var particle in particles)
        {
            if (particle != null && particle.Rb != null)
            {
                // Apply gentle horizontal force for drifting
                Vector2 driftForce = new Vector2(input.x * driftSpeed, 0);
                particle.Rb.AddForce(driftForce);

                // Clamp horizontal speed
                Vector2 vel = particle.Rb.linearVelocity;
                vel.x = Mathf.Clamp(vel.x, -config.gasMaxHorizontalSpeed, config.gasMaxHorizontalSpeed);
                particle.Rb.linearVelocity = vel;
            }
        }
    }

    public bool HandleJump()
    {
        // Gas cannot jump - it's always rising
        player.JumpPressed = false;
        return false;
    }

    public bool CanTransitionTo(MatterState targetState)
    {
        // Gas can only condense back to Liquid
        return targetState == MatterState.Liquid;
    }

    private void SetupBlobRenderer()
    {
        // Create blob renderer object
        GameObject rendererObj = new GameObject("GasBlobRenderer");
        rendererObj.transform.position = player.transform.position;

        // Add required components
        rendererObj.AddComponent<MeshFilter>();
        rendererObj.AddComponent<MeshRenderer>();
        
        blobRenderer = rendererObj.AddComponent<GasBlobRenderer>();
        
        // Configure renderer from config
        blobRenderer.gridSize = config.gasBlobGridSize;
        blobRenderer.gridResolution = config.gasBlobGridResolution;
        blobRenderer.particleRadius = config.gasBlobParticleRadius;
        blobRenderer.surfaceThreshold = config.gasBlobSurfaceThreshold;
        blobRenderer.falloffPower = config.gasBlobFalloffPower;
        blobRenderer.gasColor = config.gasInnerColor;
        blobRenderer.gasEdgeColor = config.gasEdgeColor;
        blobRenderer.useGradient = true;
        blobRenderer.enableWobble = config.gasBlobEnableWobble;
        blobRenderer.wobbleSpeed = config.gasBlobWobbleSpeed;
        blobRenderer.wobbleIntensity = config.gasBlobWobbleIntensity;
        blobRenderer.showDebugGizmos = false;
        
        // Set sorting layer from config
        blobRenderer.sortingLayerName = config.sortingLayerName;
        blobRenderer.sortingOrder = config.sortingOrder;

        // Assign material
        if (config.gasBlobMaterial != null)
        {
            blobRenderer.gasMaterial = config.gasBlobMaterial;
        }
    }

    private void CleanupBlobRenderer()
    {
        if (blobRenderer != null)
        {
            blobRenderer.ClearParticles();
            Object.Destroy(blobRenderer.gameObject);
            blobRenderer = null;
        }
    }

    private void ApplyConstantRise()
    {
        foreach (var particle in particles)
        {
            if (particle != null && particle.Rb != null)
            {
                // Apply constant upward force
                particle.Rb.AddForce(Vector2.up * config.gasRiseForce);

                // Clamp vertical speed
                Vector2 vel = particle.Rb.linearVelocity;
                vel.y = Mathf.Clamp(vel.y, -config.gasMaxRiseSpeed, config.gasMaxRiseSpeed);
                particle.Rb.linearVelocity = vel;
            }
        }
    }

    private void SpawnParticles()
    {
        particles.Clear();

        Vector3 spawnCenter = player.transform.position;

        for (int i = 0; i < config.gasParticleCount; i++)
        {
            // Spawn in a wider area than liquid (gas is more dispersed)
            Vector2 randomOffset = Random.insideUnitCircle * config.gasSpawnRadius;
            Vector3 spawnPos = spawnCenter + new Vector3(randomOffset.x, randomOffset.y, 0);

            GameObject particleObj = Object.Instantiate(
                player.gasParticlePrefab, 
                spawnPos, 
                Quaternion.identity, 
                player.particleContainer
            );

            GasParticle particle = particleObj.GetComponent<GasParticle>();
            if (particle == null)
            {
                particle = particleObj.AddComponent<GasParticle>();
            }

            particle.Initialize(config, player);
            particles.Add(particle);

            // Register with blob renderer
            if (blobRenderer != null)
            {
                blobRenderer.RegisterParticle(particleObj.transform);
            }

            // Hide the sprite renderer since we're using the blob mesh
            SpriteRenderer sr = particleObj.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.enabled = false; // Hide individual particles
            }
        }
    }

    private void ClearParticles()
    {
        foreach (var particle in particles)
        {
            if (particle != null)
            {
                Object.Destroy(particle.gameObject);
            }
        }
        particles.Clear();
    }

    private Vector3 GetParticlesCenter()
    {
        if (particles.Count == 0) return player.transform.position;

        Vector3 center = Vector3.zero;
        int validCount = 0;

        foreach (var particle in particles)
        {
            if (particle != null)
            {
                center += particle.transform.position;
                validCount++;
            }
        }

        return validCount > 0 ? center / validCount : player.transform.position;
    }

    /// <summary>
    /// Called when gas hits a condensation surface or timer expires
    /// </summary>
    public void Condense()
    {
        player.TransitionToState(MatterState.Liquid);
    }

    // Helper method to safely set animator parameters
    private void SetAnimatorBoolSafe(string paramName, bool value)
    {
        if (player.Anim == null) return;
        try
        {
            player.Anim.SetBool(paramName, value);
        }
        catch (System.Exception)
        {
            // Parameter doesn't exist - ignore silently
        }
    }
}
