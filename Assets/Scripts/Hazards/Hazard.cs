using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Base hazard class for damaging elements like spikes, falling traps, etc.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class Hazard : MonoBehaviour
{
    public enum HazardType
    {
        Spikes,         // Static ground spikes
        FallingSpike,   // Falls when player walks under
        Boulder,        // Rolling boulder trap
        Lava,           // Lava pit (also triggers gas transformation)
        Generic         // Generic damage zone
    }

    [Header("Hazard Settings")]
    public HazardType hazardType = HazardType.Generic;
    
    [Tooltip("Does this hazard only affect certain states?")]
    public bool affectsSolid = true;
    public bool affectsLiquid = true;
    public bool affectsGas = false; // Gas usually passes through
    public bool affectsFrozen = true;

    [Header("Damage")]
    [Tooltip("Instant kill or just damage?")]
    public bool instantKill = true;
    
    [Tooltip("Damage amount (if not instant kill)")]
    public int damage = 1;

    [Header("Effects")]
    public GameObject hitEffectPrefab;
    public AudioClip hitSound;
    [Range(0f, 1f)]
    public float volume = 1f;

    [Header("Events")]
    public UnityEvent OnPlayerHit;

    // For falling spike
    [Header("Falling Spike Settings")]
    public float triggerDistance = 2f;
    public float fallDelay = 0.3f;
    public float fallSpeed = 10f;
    public bool respawnAfterFall = true;
    public float respawnDelay = 3f;

    // For boulder
    [Header("Boulder Settings")]
    public float rollSpeed = 5f;
    public Vector2 rollDirection = Vector2.left;

    // Runtime
    private bool triggered = false;
    private Vector3 startPosition;
    private Rigidbody2D rb;

    void Start()
    {
        startPosition = transform.position;
        rb = GetComponent<Rigidbody2D>();

        // Setup based on type
        switch (hazardType)
        {
            case HazardType.FallingSpike:
                if (rb != null)
                {
                    rb.bodyType = RigidbodyType2D.Kinematic;
                }
                break;
            case HazardType.Boulder:
                if (rb == null)
                {
                    rb = gameObject.AddComponent<Rigidbody2D>();
                }
                rb.bodyType = RigidbodyType2D.Kinematic;
                rb.gravityScale = 0;
                break;
        }
    }

    void Update()
    {
        // Falling spike trigger detection
        if (hazardType == HazardType.FallingSpike && !triggered)
        {
            CheckForPlayerBelow();
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        HandleCollision(other);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        HandleCollision(collision.collider);

        // Boulder/Falling spike hit ground
        if (hazardType == HazardType.FallingSpike || hazardType == HazardType.Boulder)
        {
            if (collision.gameObject.layer == LayerMask.NameToLayer("Ground") ||
                collision.collider.CompareTag("Ground"))
            {
                if (respawnAfterFall)
                {
                    StartCoroutine(RespawnAfterDelay());
                }
                else
                {
                    // Stop moving
                    if (rb != null)
                    {
                        rb.bodyType = RigidbodyType2D.Static;
                    }
                }
            }
        }
    }

    private void HandleCollision(Collider2D other)
    {
        // Check for player
        var player = other.GetComponent<PlayerStateMachine>();
        if (player != null)
        {
            // Check if this hazard affects the player's current state
            bool shouldDamage = player.CurrentStateType switch
            {
                MatterState.Solid => affectsSolid,
                MatterState.Liquid => affectsLiquid,
                MatterState.Gas => affectsGas,
                MatterState.Frozen => affectsFrozen,
                _ => true
            };

            if (shouldDamage)
            {
                HitPlayer(player);
            }
        }

        // Check for liquid particles
        if (affectsLiquid && other.GetComponent<LiquidParticle>() != null)
        {
            // Find the player and damage them
            var playerMachine = FindFirstObjectByType<PlayerStateMachine>();
            if (playerMachine != null && playerMachine.CurrentStateType == MatterState.Liquid)
            {
                HitPlayer(playerMachine);
            }
        }
    }

    private void HitPlayer(PlayerStateMachine player)
    {
        Debug.Log($"[Hazard] {hazardType} hit player!");

        // Effects
        if (hitEffectPrefab != null)
        {
            Instantiate(hitEffectPrefab, player.transform.position, Quaternion.identity);
        }

        if (hitSound != null)
        {
            AudioSource.PlayClipAtPoint(hitSound, transform.position, volume);
        }

        // Fire event
        OnPlayerHit?.Invoke();

        // Damage player
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.PlayerTakeDamage();
        }
    }

    #region Falling Spike

    private void CheckForPlayerBelow()
    {
        // Raycast down to check for player
        RaycastHit2D hit = Physics2D.Raycast(
            transform.position, 
            Vector2.down, 
            triggerDistance,
            LayerMask.GetMask("Player", "Default")
        );

        if (hit.collider != null)
        {
            if (hit.collider.CompareTag("Player") || hit.collider.GetComponent<PlayerStateMachine>() != null)
            {
                TriggerFall();
            }
        }
    }

    public void TriggerFall()
    {
        if (triggered) return;
        triggered = true;

        StartCoroutine(FallAfterDelay());
    }

    private System.Collections.IEnumerator FallAfterDelay()
    {
        // Shake/warning
        float shakeTime = fallDelay;
        Vector3 originalPos = transform.position;
        
        while (shakeTime > 0)
        {
            transform.position = originalPos + new Vector3(Random.Range(-0.05f, 0.05f), 0, 0);
            shakeTime -= Time.deltaTime;
            yield return null;
        }

        // Fall
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 3f;
        }
    }

    #endregion

    #region Boulder

    public void TriggerBoulder()
    {
        if (triggered) return;
        triggered = true;

        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.linearVelocity = rollDirection.normalized * rollSpeed;
        }
    }

    #endregion

    #region Respawn

    private System.Collections.IEnumerator RespawnAfterDelay()
    {
        yield return new WaitForSeconds(respawnDelay);

        // Reset
        triggered = false;
        transform.position = startPosition;
        
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.linearVelocity = Vector2.zero;
        }

        gameObject.SetActive(true);
    }

    #endregion

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;

        // Draw trigger distance for falling spike
        if (hazardType == HazardType.FallingSpike)
        {
            Gizmos.DrawLine(transform.position, transform.position + Vector3.down * triggerDistance);
        }

        // Draw roll direction for boulder
        if (hazardType == HazardType.Boulder)
        {
            Gizmos.DrawRay(transform.position, rollDirection.normalized * 2);
        }
    }
}
