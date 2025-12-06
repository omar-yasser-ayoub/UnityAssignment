using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Liquid state - the player dissolves into multiple small rigidbody particles
/// that flow and can squeeze through tight spaces.
/// Uses marching squares for cohesive blob rendering.
/// </summary>
public class LiquidState : IPlayerState
{
    public MatterState StateType => MatterState.Liquid;

    private PlayerStateMachine player;
    private PlayerStateConfig config;
    private List<LiquidParticle> particles = new List<LiquidParticle>();
    private LiquidBlobRenderer blobRenderer;

    // For controlling all particles
    private Vector2 moveInput;
    private bool isRunning;

    public void Enter(PlayerStateMachine player)
    {
        this.player = player;
        this.config = player.Config;

        // Clear any existing particle references
        LiquidParticle.ClearAllParticles();

        // Hide the main player body
        player.SetMainBodyVisible(false);

        // Setup blob renderer (marching squares)
        SetupBlobRenderer();

        // Spawn liquid particles at player position
        SpawnParticles();

        // Set animator state (in case we have UI or effects)
        player.Anim.SetBool("IsLiquid", true);

        Debug.Log($"Entered Liquid state with {particles.Count} particles");
    }

    public void Update()
    {
        // Update player position to follow particle center (for camera)
        if (particles.Count > 0)
        {
            player.transform.position = GetParticlesCenter();
        }
    }

    public void FixedUpdate()
    {
        HandleMovement(player.MoveInput, player.IsRunning);

        // Check if jump is pressed (liquid has weak jump)
        if (player.JumpPressed)
        {
            HandleJump();
        }
    }

    public void Exit()
    {
        // Move player to center of particles before clearing
        player.transform.position = GetParticlesCenter();

        // Clear all particles
        ClearParticles();

        // Cleanup blob renderer
        CleanupBlobRenderer();

        player.Anim.SetBool("IsLiquid", false);
    }

    public void HandleMovement(Vector2 input, bool isRunning)
    {
        this.moveInput = input;
        this.isRunning = isRunning;

        float speed = isRunning 
            ? config.liquidMoveSpeed * config.liquidRunMultiplier 
            : config.liquidMoveSpeed;

        // Apply force to all particles
        foreach (var particle in particles)
        {
            if (particle != null && particle.Rb != null)
            {
                // Apply horizontal force to movement
                Vector2 force = new Vector2(input.x * speed * config.liquidForceMultiplier, 0);
                particle.Rb.AddForce(force);

                // Clamp max velocity
                Vector2 vel = particle.Rb.linearVelocity;
                vel.x = Mathf.Clamp(vel.x, -config.liquidMaxSpeed, config.liquidMaxSpeed);
                particle.Rb.linearVelocity = vel;
            }
        }
    }

    public bool HandleJump()
    {
        player.JumpPressed = false;

        // Check if any particles are grounded
        bool anyGrounded = false;
        foreach (var particle in particles)
        {
            if (particle != null && particle.IsGrounded)
            {
                anyGrounded = true;
                break;
            }
        }

        if (!anyGrounded) return false;

        // Apply upward impulse to all particles (weaker than solid jump)
        foreach (var particle in particles)
        {
            if (particle != null && particle.Rb != null)
            {
                particle.Rb.AddForce(Vector2.up * config.liquidJumpForce, ForceMode2D.Impulse);
            }
        }

        return true;
    }

    public bool CanTransitionTo(MatterState targetState)
    {
        // Liquid can transition to:
        // - Solid (reform)
        // - Gas (via lava/cauldron)
        // - Frozen (in ice caverns)
        return targetState == MatterState.Solid || 
               targetState == MatterState.Gas || 
               targetState == MatterState.Frozen;
    }

    private void SetupBlobRenderer()
    {
        // Create blob renderer object
        GameObject rendererObj = new GameObject("LiquidBlobRenderer");
        rendererObj.transform.position = player.transform.position;

        // Add required components
        rendererObj.AddComponent<MeshFilter>();
        rendererObj.AddComponent<MeshRenderer>();
        
        blobRenderer = rendererObj.AddComponent<LiquidBlobRenderer>();
        
        // Configure renderer from config
        blobRenderer.gridSize = config.blobGridSize;
        blobRenderer.gridResolution = config.blobGridResolution;
        blobRenderer.particleRadius = config.blobParticleRadius;
        blobRenderer.surfaceThreshold = config.blobSurfaceThreshold;
        blobRenderer.falloffPower = config.blobFalloffPower;
        blobRenderer.blobColor = config.liquidInnerColor;
        blobRenderer.showDebugGizmos = false;
        
        // Set sorting layer from config
        blobRenderer.sortingLayerName = config.sortingLayerName;
        blobRenderer.sortingOrder = config.sortingOrder;

        // Assign material
        if (config.blobMaterial != null)
        {
            blobRenderer.blobMaterial = config.blobMaterial;
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

    private void SpawnParticles()
    {
        particles.Clear();

        Vector3 spawnCenter = player.transform.position;

        for (int i = 0; i < config.liquidParticleCount; i++)
        {
            // Spawn in a small random area around the player
            Vector2 randomOffset = Random.insideUnitCircle * config.liquidSpawnRadius;
            Vector3 spawnPos = spawnCenter + new Vector3(randomOffset.x, randomOffset.y, 0);

            GameObject particleObj = Object.Instantiate(
                player.liquidParticlePrefab, 
                spawnPos, 
                Quaternion.identity, 
                player.particleContainer
            );

            LiquidParticle particle = particleObj.GetComponent<LiquidParticle>();
            if (particle == null)
            {
                particle = particleObj.AddComponent<LiquidParticle>();
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
        // Clear static list first
        LiquidParticle.ClearAllParticles();

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
    /// Called by external triggers (like lava pits) to evaporate liquid
    /// </summary>
    public void Evaporate()
    {
        player.TransitionToState(MatterState.Gas);
    }

    /// <summary>
    /// Called by external triggers (like ice surfaces) to freeze liquid
    /// </summary>
    public void Freeze()
    {
        player.TransitionToState(MatterState.Frozen);
    }
}
