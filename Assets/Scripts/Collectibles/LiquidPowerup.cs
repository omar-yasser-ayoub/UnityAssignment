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
    [Tooltip("Automatically transform player to liquid on entry")]
    public bool autoTransform = true;
    
    [Tooltip("Time before auto-transforming (for dramatic effect)")]
    public float transformDelay = 0.5f;

    [Header("Visual Effects")]
    [Tooltip("Particle effect to show when ability is unlocked")]
    public GameObject unlockEffectPrefab;

    [Header("Audio")]
    public AudioClip unlockSound;
    public AudioClip splashSound;
    [Range(0f, 1f)]
    public float volume = 1f;

    [Header("Events")]
    public UnityEvent OnPowerupCollected;

    private bool isCollected = false;

    void Start()
    {
        // Ensure trigger
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Already collected this session
        if (isCollected) return;
        
        var player = other.GetComponent<PlayerStateMachine>();
        if (player == null) return;

        // Only collect if player is in solid state
        if (player.CurrentStateType != MatterState.Solid) return;

        // Mark as collected immediately
        isCollected = true;

        // Play splash sound
        if (splashSound != null)
        {
            AudioSource.PlayClipAtPoint(splashSound, transform.position, volume);
        }

        // Grant ability and collect
        CollectPowerup(player);
    }

    private void CollectPowerup(PlayerStateMachine player)
    {
        Debug.Log("[LiquidPowerup] Collecting powerup!");

        // Save to GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.UnlockLiquidAbility();
        }

        // Play unlock effect
        if (unlockEffectPrefab != null)
        {
            Instantiate(unlockEffectPrefab, transform.position, Quaternion.identity);
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

        // Auto transform then destroy
        if (autoTransform)
        {
            StartCoroutine(TransformThenDestroy(player));
        }
        else
        {
            // Just destroy immediately
            Destroy(gameObject);
        }
    }

    private System.Collections.IEnumerator TransformThenDestroy(PlayerStateMachine player)
    {
        yield return new WaitForSeconds(transformDelay);

        // Transform player to liquid
        if (player != null && player.CurrentStateType == MatterState.Solid)
        {
            player.TransitionToState(MatterState.Liquid);
        }

        // Destroy the powerup
        Destroy(gameObject);
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        
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
