using UnityEngine;

/// <summary>
/// Individual gas particle - a small rigidbody that floats upward.
/// Multiple of these create the gas/vapor effect.
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
    }

    void FixedUpdate()
    {
        if (config == null) return;

        // Apply random drift for natural gas movement
        ApplyRandomDrift();

        // Apply dispersion (gas particles spread out)
        ApplyDispersion();
    }

    private void ApplyRandomDrift()
    {
        if (!config.gasUseRandomDrift) return;

        driftTimer += Time.fixedDeltaTime;

        // Sinusoidal drift for organic movement
        float driftX = Mathf.Sin(driftTimer * config.gasDriftFrequency + driftOffset) * config.gasDriftAmplitude;
        
        Rb.AddForce(new Vector2(driftX, 0));
    }

    private void ApplyDispersion()
    {
        if (!config.gasUseDispersion) return;

        // Find nearby gas particles and apply gentle repulsion
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
                
                if (distance > 0.01f)
                {
                    // Stronger push when closer
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

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 1f, 1f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, 0.1f);
    }
}
