using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Individual liquid particle - a small rigidbody that is part of the liquid state.
/// Multiple of these create the fluid simulation effect.
/// Uses strong cohesion forces to prevent separation.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CircleCollider2D))]
public class LiquidParticle : MonoBehaviour
{
    public Rigidbody2D Rb { get; private set; }
    public CircleCollider2D Collider { get; private set; }
    public bool IsGrounded { get; private set; }

    private PlayerStateConfig config;
    private PlayerStateMachine player;
    private SpriteRenderer spriteRenderer;

    // Reference to other particles for cohesion
    private static List<LiquidParticle> allParticles = new List<LiquidParticle>();
    
    // Center tracking for max separation
    private Vector3 groupCenter;

    // Ground check
    private float groundCheckTimer;
    private const float GROUND_CHECK_INTERVAL = 0.1f;

    public void Initialize(PlayerStateConfig config, PlayerStateMachine player)
    {
        this.config = config;
        this.player = player;

        Rb = GetComponent<Rigidbody2D>();
        Collider = GetComponent<CircleCollider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        // Setup physics for liquid behavior
        Rb.gravityScale = config.liquidGravityScale;
        Rb.mass = config.liquidParticleMass;
        Rb.linearDamping = config.liquidDrag;
        Rb.angularDamping = config.liquidAngularDrag;
        Rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        // Small collider for squeezing through gaps
        Collider.radius = config.liquidParticleRadius;

        // Use a physics material for bounciness/friction
        if (config.liquidPhysicsMaterial != null)
        {
            Collider.sharedMaterial = config.liquidPhysicsMaterial;
        }

        // Set layer for particle-particle interactions
        gameObject.layer = LayerMask.NameToLayer("LiquidParticle");

        // Register with static list
        allParticles.Add(this);

        // Setup soft circle material if available
        SetupVisuals();
    }

    void SetupVisuals()
    {
        if (spriteRenderer == null) return;

        // The sprite renderer will be captured by the metaball camera
        // Make sure it's visible to that camera's culling mask
        spriteRenderer.sortingOrder = 0;
    }

    void FixedUpdate()
    {
        if (config == null) return;

        // Calculate group center
        UpdateGroupCenter();

        // Apply cohesion force to stay together with other particles
        ApplyCohesion();

        // Enforce maximum separation distance
        EnforceMaxSeparation();

        // Check if grounded (for jump detection)
        groundCheckTimer -= Time.fixedDeltaTime;
        if (groundCheckTimer <= 0)
        {
            CheckGrounded();
            groundCheckTimer = GROUND_CHECK_INTERVAL;
        }
    }

    private void UpdateGroupCenter()
    {
        if (allParticles.Count == 0)
        {
            groupCenter = transform.position;
            return;
        }

        Vector3 center = Vector3.zero;
        int count = 0;

        foreach (var particle in allParticles)
        {
            if (particle != null && particle.gameObject.activeInHierarchy)
            {
                center += particle.transform.position;
                count++;
            }
        }

        groupCenter = count > 0 ? center / count : transform.position;
    }

    private void ApplyCohesion()
    {
        if (!config.liquidUseCohesion) return;

        Vector2 cohesionDir = Vector2.zero;
        int neighborCount = 0;

        // Attract to nearby particles
        foreach (var other in allParticles)
        {
            if (other == null || other == this || !other.gameObject.activeInHierarchy) 
                continue;

            Vector2 toOther = (Vector2)(other.transform.position - transform.position);
            float distance = toOther.magnitude;

            if (distance < config.liquidCohesionRadius && distance > 0.01f)
            {
                // Stronger attraction when further apart (within radius)
                float strength = distance / config.liquidCohesionRadius;
                cohesionDir += toOther.normalized * strength;
                neighborCount++;
            }
        }

        if (neighborCount > 0)
        {
            cohesionDir /= neighborCount;
            Rb.AddForce(cohesionDir * config.liquidCohesionForce);
        }

        // Also attract to group center (keeps blob together)
        Vector2 toCenter = (Vector2)(groupCenter - transform.position);
        float distToCenter = toCenter.magnitude;
        
        if (distToCenter > 0.1f)
        {
            // Stronger pull when further from center
            float centerPull = Mathf.Clamp01(distToCenter / config.liquidMaxSeparation);
            Rb.AddForce(toCenter.normalized * config.liquidCenterAttraction * centerPull);
        }
    }

    private void EnforceMaxSeparation()
    {
        // Hard limit on how far a particle can get from the group
        Vector2 toCenter = (Vector2)(groupCenter - transform.position);
        float distToCenter = toCenter.magnitude;

        if (distToCenter > config.liquidMaxSeparation)
        {
            // Teleport back or apply very strong force
            if (config.liquidHardSeparationLimit)
            {
                // Hard teleport to max distance
                Vector2 maxPos = (Vector2)groupCenter - toCenter.normalized * config.liquidMaxSeparation;
                transform.position = new Vector3(maxPos.x, maxPos.y, transform.position.z);
                
                // Also zero out outward velocity
                Vector2 vel = Rb.linearVelocity;
                float outwardVel = Vector2.Dot(vel, -toCenter.normalized);
                if (outwardVel > 0)
                {
                    Rb.linearVelocity = vel + toCenter.normalized * outwardVel;
                }
            }
            else
            {
                // Very strong rubber-band force
                float overDistance = distToCenter - config.liquidMaxSeparation;
                Rb.AddForce(toCenter.normalized * overDistance * config.liquidSeparationForce);
            }
        }
    }

    private void CheckGrounded()
    {
        // Simple ground check using raycast
        RaycastHit2D hit = Physics2D.Raycast(
            transform.position, 
            Vector2.down, 
            Collider.radius + 0.05f, 
            player.groundLayer
        );

        IsGrounded = hit.collider != null;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Check for evaporation triggers (lava, cauldron)
        if (other.CompareTag("Evaporator"))
        {
            // Tell the state machine to transition to gas
            player.TransitionToState(MatterState.Gas);
        }

        // Check for freeze triggers
        if (other.CompareTag("Freezer"))
        {
            player.TransitionToState(MatterState.Frozen);
        }
    }

    void OnDestroy()
    {
        // Unregister from static list
        allParticles.Remove(this);
    }

    void OnDisable()
    {
        allParticles.Remove(this);
    }

    void OnEnable()
    {
        if (!allParticles.Contains(this))
        {
            allParticles.Add(this);
        }
    }

    /// <summary>
    /// Clear the static particle list (call when transitioning out of liquid state)
    /// </summary>
    public static void ClearAllParticles()
    {
        allParticles.Clear();
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = IsGrounded ? Color.blue : Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.1f);

        // Draw line to center
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, groupCenter);
    }
}
