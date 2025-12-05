using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Level exit trigger that completes the level when player enters.
/// Place at the end of each level.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class LevelExit : MonoBehaviour
{
    [Header("Requirements")]
    [Tooltip("Require specific state to exit?")]
    public bool requireSpecificState = false;
    public MatterState requiredState = MatterState.Solid;
    
    [Tooltip("Require minimum stars to exit?")]
    public bool requireMinimumStars = false;
    public int minimumStars = 0;

    [Header("Visual")]
    [Tooltip("Visual indicator for exit (door, portal, etc.)")]
    public GameObject exitVisual;
    
    [Tooltip("Locked visual when requirements not met")]
    public GameObject lockedVisual;
    
    [Tooltip("Particle effect on exit")]
    public GameObject exitEffectPrefab;

    [Header("Audio")]
    public AudioClip exitSound;
    public AudioClip lockedSound;
    [Range(0f, 1f)]
    public float volume = 1f;

    [Header("Events")]
    public UnityEvent OnLevelExit;
    public UnityEvent OnExitBlocked;

    private bool hasExited = false;

    void Start()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;

        UpdateVisuals();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (hasExited) return;

        var player = other.GetComponent<PlayerStateMachine>();
        if (player == null)
        {
            // Check for liquid particles
            if (other.GetComponent<LiquidParticle>() != null)
            {
                player = FindFirstObjectByType<PlayerStateMachine>();
            }
        }

        if (player != null)
        {
            TryExit(player);
        }
    }

    private void TryExit(PlayerStateMachine player)
    {
        // Check state requirement
        if (requireSpecificState && player.CurrentStateType != requiredState)
        {
            BlockExit($"Must be in {requiredState} form to exit!");
            return;
        }

        // Check star requirement
        if (requireMinimumStars)
        {
            int stars = LevelManager.Instance != null ? LevelManager.Instance.GetStarsCollected() : 0;
            if (stars < minimumStars)
            {
                BlockExit($"Need {minimumStars} stars to exit! ({stars}/{minimumStars})");
                return;
            }
        }

        // All requirements met - exit level
        ExitLevel();
    }

    private void BlockExit(string message)
    {
        Debug.Log($"[LevelExit] Blocked: {message}");

        if (lockedSound != null)
        {
            AudioSource.PlayClipAtPoint(lockedSound, transform.position, volume);
        }

        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowNotification(message);
        }

        OnExitBlocked?.Invoke();
    }

    private void ExitLevel()
    {
        hasExited = true;

        Debug.Log("[LevelExit] Level completed!");

        // Play effects
        if (exitEffectPrefab != null)
        {
            Instantiate(exitEffectPrefab, transform.position, Quaternion.identity);
        }

        if (exitSound != null)
        {
            AudioSource.PlayClipAtPoint(exitSound, transform.position, volume);
        }

        // Fire event
        OnLevelExit?.Invoke();

        // Tell level manager
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.CompleteLevel();
        }
    }

    private void UpdateVisuals()
    {
        bool canExit = !requireMinimumStars || 
            (LevelManager.Instance != null && LevelManager.Instance.GetStarsCollected() >= minimumStars);

        if (exitVisual != null)
            exitVisual.SetActive(canExit);

        if (lockedVisual != null)
            lockedVisual.SetActive(!canExit);
    }

    void OnDrawGizmos()
    {
        // Draw exit zone
        Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
        
        var col = GetComponent<Collider2D>();
        if (col != null)
        {
            Gizmos.DrawCube(col.bounds.center, col.bounds.size);
        }
        else
        {
            Gizmos.DrawCube(transform.position, new Vector3(1, 2, 0));
        }

        // Draw door frame
        Gizmos.color = Color.green;
        Vector3 pos = transform.position;
        Gizmos.DrawLine(pos + new Vector3(-0.5f, -1f, 0), pos + new Vector3(-0.5f, 1f, 0));
        Gizmos.DrawLine(pos + new Vector3(0.5f, -1f, 0), pos + new Vector3(0.5f, 1f, 0));
        Gizmos.DrawLine(pos + new Vector3(-0.5f, 1f, 0), pos + new Vector3(0.5f, 1f, 0));
    }
}
