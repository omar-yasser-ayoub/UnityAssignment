using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// The first liquid pool in the cave that grants the player the liquid ability.
/// When the player enters, they automatically transform to liquid.
/// This is the "crack in the wall" moment from Level 1.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class LiquidPowerup : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Only grant ability once (saved to GameManager)")]
    public bool oneTimeUnlock = true;
    
    [Tooltip("Automatically transform player to liquid on entry")]
    public bool autoTransform = true;
    
    [Tooltip("Time before auto-transforming (for dramatic effect)")]
    public float transformDelay = 0.5f;

    [Header("Visual Effects")]
    [Tooltip("Particle effect to show when ability is unlocked")]
    public GameObject unlockEffectPrefab;
    
    [Tooltip("Optional: Change pool color after used")]
    public SpriteRenderer poolSprite;
    
    [Tooltip("Color when ability already obtained")]
    public Color usedColor = new Color(0.3f, 0.3f, 0.5f, 0.8f);

    [Header("Audio")]
    public AudioClip unlockSound;
    public AudioClip splashSound;
    [Range(0f, 1f)]
    public float volume = 1f;

    [Header("Events")]
    public UnityEvent OnPowerupCollected;
    public UnityEvent OnPlayerEnterPool;

    private bool abilityGranted = false;
    private bool playerInPool = false;

    void Start()
    {
        // Check if player already has the ability
        if (GameManager.Instance != null && GameManager.Instance.HasLiquidAbility)
        {
            abilityGranted = true;
            UpdateVisuals();
        }

        // Ensure trigger
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        var player = other.GetComponent<PlayerStateMachine>();
        if (player == null) return;

        playerInPool = true;
        OnPlayerEnterPool?.Invoke();

        // Play splash sound
        if (splashSound != null)
        {
            AudioSource.PlayClipAtPoint(splashSound, transform.position, volume);
        }

        // Only grant ability if player is in solid state and doesn't have it yet
        if (player.CurrentStateType == MatterState.Solid)
        {
            if (!abilityGranted || !oneTimeUnlock)
            {
                GrantLiquidAbility(player);
            }
            else if (autoTransform)
            {
                // Already have ability, just transform
                StartCoroutine(DelayedTransform(player));
            }
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.GetComponent<PlayerStateMachine>() != null)
        {
            playerInPool = false;
        }
    }

    private void GrantLiquidAbility(PlayerStateMachine player)
    {
        abilityGranted = true;

        // Save to GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.UnlockLiquidAbility();
        }

        // Play unlock effect
        if (unlockEffectPrefab != null)
        {
            Instantiate(unlockEffectPrefab, player.transform.position, Quaternion.identity);
        }

        // Play sound
        if (unlockSound != null)
        {
            AudioSource.PlayClipAtPoint(unlockSound, transform.position, volume);
        }

        // Fire event
        OnPowerupCollected?.Invoke();

        // Show UI notification
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowNotification("Liquid Form Unlocked!\nPress F to transform");
        }

        // Update visuals
        UpdateVisuals();

        // Auto transform
        if (autoTransform)
        {
            StartCoroutine(DelayedTransform(player));
        }

        Debug.Log("[LiquidPowerup] Liquid ability granted!");
    }

    private System.Collections.IEnumerator DelayedTransform(PlayerStateMachine player)
    {
        yield return new WaitForSeconds(transformDelay);

        if (playerInPool && player.CurrentStateType == MatterState.Solid)
        {
            player.TransitionToState(MatterState.Liquid);
        }
    }

    private void UpdateVisuals()
    {
        if (poolSprite != null && abilityGranted)
        {
            poolSprite.color = usedColor;
        }
    }

    void OnDrawGizmos()
    {
        // Draw pool indicator
        Gizmos.color = abilityGranted ? Color.gray : Color.blue;
        
        var col = GetComponent<Collider2D>();
        if (col != null)
        {
            Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
        }
        else
        {
            Gizmos.DrawWireCube(transform.position, new Vector3(2, 1, 0));
        }

        // Draw water drops icon
        Gizmos.color = new Color(0.2f, 0.5f, 1f, 0.8f);
        Gizmos.DrawSphere(transform.position + Vector3.up * 0.5f, 0.15f);
    }
}
