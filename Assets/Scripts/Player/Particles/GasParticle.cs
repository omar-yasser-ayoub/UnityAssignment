using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Individual gas particle - a small rigidbody that floats upward.
/// Multiple of these create the gas/vapor effect.
/// Includes cohesion to keep the cloud together.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CircleCollider2D))]
public class GasParticle : MonoBehaviour
{
    public Rigidbody2D Rb { get; private set; }
    public CircleCollider2D Collider { get; private set; }

    private PlayerStateConfig config;
    private PlayerStateMachine player;

    // Random drift for natural gas movement
    private float driftOffset;
    private float driftTimer;

    // Reference to other particles for cohesion
    private static List<GasParticle> allParticles = new List<GasParticle>();
    
    // Center tracking for max separation
    private Vector3 groupCenter;

    public void Initialize(PlayerStateConfig config, PlayerStateMachine player)
    {
        this.config = config;
        this.player = player;

        Rb = GetComponent<Rigidbody2D>();
        Collider = GetComponent<CircleCollider2D>();

        // Setup physics for gas behavior - very light, floaty
        Rb.gravityScale = config.gasGravityScale; // Usually 0 or negative
        Rb.mass = config.gasParticleMass;
        Rb.linearDamping = config.gasDrag;
        Rb.angularDamping = config.gasAngularDrag;
        Rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        // Small collider
        Collider.radius = config.gasParticleRadius;

        // Make it a trigger for passing through some objects
        Collider.isTrigger = config.gasIsTrigger;

        // Set layer
        gameObject.layer = LayerMask.NameToLayer("GasParticle");

        // Random drift offset for varied movement
        driftOffset = Random.Range(0f, Mathf.PI * 2f);

        // Register with static list
        allParticles.Add(this);
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

        // Apply random drift for natural gas movement
        ApplyRandomDrift();

        // Apply dispersion (gas particles spread out slightly, but less than before since we have cohesion)
        ApplyDispersion();
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
        if (!config.gasUseCohesion) return;

        Vector2 cohesionDir = Vector2.zero;
        int neighborCount = 0;

        // Attract to nearby particles
        foreach (var other in allParticles)
        {
            if (other == null || other == this || !other.gameObject.activeInHierarchy) 
                continue;

            Vector2 toOther = (Vector2)(other.transform.position - transform.position);
            float distance = toOther.magnitude;

            if (distance < config.gasCohesionRadius && distance > 0.01f)
            {
                // Gentler attraction for gas (floatier feel)
                float strength = distance / config.gasCohesionRadius;
                cohesionDir += toOther.normalized * strength;
                neighborCount++;
            }
        }

        if (neighborCount > 0)
        {
            cohesionDir /= neighborCount;
            Rb.AddForce(cohesionDir * config.gasCohesionForce);
        }

        // Also attract to group center (keeps cloud together)
        Vector2 toCenter = (Vector2)(groupCenter - transform.position);
        float distToCenter = toCenter.magnitude;
        
        if (distToCenter > 0.1f)
        {
            // Gentler pull than liquid - gas is more dispersed
            float centerPull = Mathf.Clamp01(distToCenter / config.gasMaxSeparation);
            Rb.AddForce(toCenter.normalized * config.gasCenterAttraction * centerPull);
        }
    }

    private void EnforceMaxSeparation()
    {
        // Soft limit on how far a particle can get from the group
        Vector2 toCenter = (Vector2)(groupCenter - transform.position);
        float distToCenter = toCenter.magnitude;

        if (distToCenter > config.gasMaxSeparation)
        {
            // Rubber-band force (softer than liquid - no teleporting for gas)
            float overDistance = distToCenter - config.gasMaxSeparation;
            Rb.AddForce(toCenter.normalized * overDistance * config.gasSeparationForce);
        }
    }

    private void ApplyRandomDrift()
    {
        if (!config.gasUseRandomDrift) return;

        driftTimer += Time.fixedDeltaTime;

        // Sinusoidal drift for organic movement
        float driftX = Mathf.Sin(driftTimer * config.gasDriftFrequency + driftOffset) * config.gasDriftAmplitude;
        float driftY = Mathf.Cos(driftTimer * config.gasDriftFrequency * 0.7f + driftOffset) * config.gasDriftAmplitude * 0.5f;
        
        Rb.AddForce(new Vector2(driftX, driftY));
    }

    private void ApplyDispersion()
    {
        if (!config.gasUseDispersion) return;

        // Find nearby gas particles and apply gentle repulsion
        // This prevents particles from clumping too much while cohesion keeps them together
        Collider2D[] nearby = Physics2D.OverlapCircleAll(
            transform.position, 
            config.gasDispersionRadius, 
            LayerMask.GetMask("GasParticle")
        );

        Vector2 dispersionDir = Vector2.zero;

        foreach (var col in nearby)
        {
            if (col.gameObject != gameObject)
            {
                // Push away from other particles
                Vector2 awayDir = (Vector2)(transform.position - col.transform.position);
                float distance = awayDir.magnitude;
                
                if (distance > 0.01f && distance < config.gasDispersionRadius * 0.5f)
                {
                    // Only push when very close
                    dispersionDir += awayDir.normalized / distance;
                }
            }
        }

        if (dispersionDir.magnitude > 0)
        {
            Rb.AddForce(dispersionDir * config.gasDispersionForce);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Check for condensation triggers (cold surfaces, ceilings)
        if (other.CompareTag("Condenser"))
        {
            // Tell the state machine to condense back to liquid
            player.TransitionToState(MatterState.Liquid);
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        // If gas hits a ceiling or condensation surface, might condense
        if (config.gasCondenseOnCeilingHit)
        {
            // Check if we hit from below (ceiling)
            foreach (ContactPoint2D contact in collision.contacts)
            {
                if (contact.normal.y < -0.5f) // Hit from below
                {
                    // Potential condensation point
                    if (collision.collider.CompareTag("Condenser"))
                    {
                        player.TransitionToState(MatterState.Liquid);
                    }
                }
            }
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
    /// Clear the static particle list (call when transitioning out of gas state)
    /// </summary>
    public static void ClearAllParticles()
    {
        allParticles.Clear();
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 1f, 1f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, 0.1f);

        // Draw line to center
        Gizmos.color = Color.white;
        Gizmos.DrawLine(transform.position, groupCenter);
    }
}
