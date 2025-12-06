using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Lava pit that transforms liquid players into gas.
/// Solid players take damage. Gas players pass through.
/// Used in Cave and Cavern levels for vertical traversal.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class LavaPit : MonoBehaviour
{
    [Header("Transformation")]
    [Tooltip("Delay before transforming to gas")]
    public float transformDelay = 0.3f;
    
    [Tooltip("Launch force applied when becoming gas")]
    public float launchForce = 8f;

    [Header("Damage")]
    [Tooltip("Does solid form take damage from lava?")]
    public bool damagesSolid = true;
    
    [Tooltip("If true, solid players transform to gas instead of taking damage")]
    public bool solidCanEvaporate = false;

    [Header("Visual Effects")]
    public GameObject steamEffectPrefab;
    public GameObject bubbleEffectPrefab;

    [Header("Audio")]
    public AudioClip evaporateSound;
    public AudioClip burnSound;
    [Range(0f, 1f)]
    public float volume = 1f;

    [Header("Events")]
    public UnityEvent OnPlayerEvaporated;
    public UnityEvent OnPlayerBurned;

    private bool isTransforming = false;

    void Start()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Check for player
        var player = other.GetComponent<PlayerStateMachine>();
        if (player != null)
        {
            HandlePlayerContact(player);
            return;
        }

        // Check for liquid particles
        var liquidParticle = other.GetComponent<LiquidParticle>();
        if (liquidParticle != null)
        {
            // Find the player state machine
            var playerMachine = FindFirstObjectByType<PlayerStateMachine>();
            if (playerMachine != null && playerMachine.CurrentStateType == MatterState.Liquid)
            {
                HandlePlayerContact(playerMachine);
            }
        }
    }

    private void HandlePlayerContact(PlayerStateMachine player)
    {
        switch (player.CurrentStateType)
        {
            case MatterState.Solid:
                if (solidCanEvaporate)
                {
                    // Transform solid directly to gas (skip liquid)
                    if (!isTransforming)
                    {
                        StartCoroutine(EvaporatePlayer(player));
                    }
                }
                else if (damagesSolid)
                {
                    BurnPlayer();
                }
                break;

            case MatterState.Liquid:
                if (!isTransforming)
                {
                    StartCoroutine(EvaporatePlayer(player));
                }
                break;

            case MatterState.Gas:
                // Gas passes through - no interaction
                break;

            case MatterState.Frozen:
                // Frozen melts into liquid first, then evaporates
                player.TransitionToState(MatterState.Liquid);
                break;
        }
    }

    private System.Collections.IEnumerator EvaporatePlayer(PlayerStateMachine player)
    {
        isTransforming = true;

        Debug.Log("[LavaPit] Evaporating liquid player!");

        // Spawn steam effect at center of liquid blob
        if (steamEffectPrefab != null)
        {
            Instantiate(steamEffectPrefab, player.transform.position, Quaternion.identity);
        }

        // Play sound
        if (evaporateSound != null)
        {
            AudioSource.PlayClipAtPoint(evaporateSound, transform.position, volume);
        }

        yield return new WaitForSeconds(transformDelay);

        // Transform to gas
        player.TransitionToState(MatterState.Gas);

        // Apply upward launch force to gas particles
        // (The GasState should handle this, but we can add extra force)
        if (player.particleContainer != null)
        {
            foreach (Transform child in player.particleContainer)
            {
                var rb = child.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    rb.AddForce(Vector2.up * launchForce, ForceMode2D.Impulse);
                }
            }
        }

        // Fire event
        OnPlayerEvaporated?.Invoke();

        // Unlock gas ability if not already
        if (GameManager.Instance != null && !GameManager.Instance.HasGasAbility)
        {
            GameManager.Instance.UnlockGasAbility();
            
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowNotification("Gas Form Unlocked!\nFloat upward through vents");
            }
        }

        isTransforming = false;
    }

    private void BurnPlayer()
    {
        Debug.Log("[LavaPit] Solid player burned by lava!");

        // Spawn effect
        if (bubbleEffectPrefab != null)
        {
            Instantiate(bubbleEffectPrefab, transform.position, Quaternion.identity);
        }

        // Play sound
        if (burnSound != null)
        {
            AudioSource.PlayClipAtPoint(burnSound, transform.position, volume);
        }

        // Fire event
        OnPlayerBurned?.Invoke();

        // Damage player
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.PlayerTakeDamage();
        }
    }

    void OnDrawGizmos()
    {
        // Draw lava indicator
        Gizmos.color = new Color(1f, 0.3f, 0f, 0.5f);
        
        var col = GetComponent<Collider2D>();
        if (col != null)
        {
            Gizmos.DrawCube(col.bounds.center, col.bounds.size);
        }
        else
        {
            Gizmos.DrawCube(transform.position, new Vector3(3, 1, 0));
        }

        // Draw upward arrow to show gas launch direction
        Gizmos.color = Color.white;
        Vector3 arrowStart = transform.position + Vector3.up;
        Gizmos.DrawLine(arrowStart, arrowStart + Vector3.up * 2);
        Gizmos.DrawLine(arrowStart + Vector3.up * 2, arrowStart + Vector3.up * 1.5f + Vector3.left * 0.3f);
        Gizmos.DrawLine(arrowStart + Vector3.up * 2, arrowStart + Vector3.up * 1.5f + Vector3.right * 0.3f);
    }
}
