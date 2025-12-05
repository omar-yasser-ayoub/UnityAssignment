using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

/// <summary>
/// Manages the current level's state, star collection, and completion.
/// Place one in each level scene.
/// </summary>
public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    [Header("Level Info")]
    [Tooltip("Index of this level (0-based)")]
    public int levelIndex = 0;
    
    [Tooltip("Display name for UI")]
    public string levelDisplayName = "The Cave";

    [Header("Stars")]
    [Tooltip("Star collectibles in this level (auto-found if empty)")]
    public List<StarCollectible> stars = new List<StarCollectible>();

    [Header("Spawn Points")]
    [Tooltip("Where player spawns at level start")]
    public Transform playerSpawnPoint;
    
    [Tooltip("Current checkpoint (updated by checkpoint triggers)")]
    public Transform currentCheckpoint;

    [Header("Level Bounds")]
    [Tooltip("If player falls below this Y, respawn")]
    public float deathPlaneY = -10f;

    [Header("Cave Level Settings")]
    [Tooltip("Is this a cave level with unlimited energy?")]
    public bool isCaveLevel = true;

    [Header("Events")]
    public UnityEvent OnLevelStart;
    public UnityEvent OnLevelComplete;
    public UnityEvent<int> OnStarCollected; // Passes current star count

    // Runtime state
    private int starsCollected = 0;
    private bool levelCompleted = false;
    private PlayerStateMachine player;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        // Find player
        player = FindFirstObjectByType<PlayerStateMachine>();

        // Auto-find stars if not assigned
        if (stars.Count == 0)
        {
            stars.AddRange(FindObjectsByType<StarCollectible>(FindObjectsSortMode.None));
        }

        // Subscribe to star collection
        foreach (var star in stars)
        {
            if (star != null)
            {
                star.OnCollected += HandleStarCollected;
            }
        }

        // Set initial checkpoint
        if (currentCheckpoint == null && playerSpawnPoint != null)
        {
            currentCheckpoint = playerSpawnPoint;
        }

        // Move player to spawn point
        if (player != null && playerSpawnPoint != null)
        {
            player.transform.position = playerSpawnPoint.position;
        }

        OnLevelStart?.Invoke();
    }

    void Update()
    {
        // Check death plane
        if (player != null && player.transform.position.y < deathPlaneY)
        {
            RespawnPlayer();
        }
    }

    void OnDestroy()
    {
        // Unsubscribe from stars
        foreach (var star in stars)
        {
            if (star != null)
            {
                star.OnCollected -= HandleStarCollected;
            }
        }
    }

    #region Star Collection

    private void HandleStarCollected(StarCollectible star)
    {
        starsCollected++;
        OnStarCollected?.Invoke(starsCollected);
        
        Debug.Log($"[LevelManager] Star collected! {starsCollected}/{stars.Count}");

        // Update UI
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateStarCount(starsCollected, stars.Count);
        }
    }

    public int GetStarsCollected() => starsCollected;
    public int GetTotalStars() => stars.Count;

    #endregion

    #region Checkpoints & Respawn

    public void SetCheckpoint(Transform checkpoint)
    {
        currentCheckpoint = checkpoint;
        Debug.Log($"[LevelManager] Checkpoint set at {checkpoint.position}");
    }

    public void RespawnPlayer()
    {
        if (player == null) return;

        Vector3 respawnPos = currentCheckpoint != null 
            ? currentCheckpoint.position 
            : (playerSpawnPoint != null ? playerSpawnPoint.position : Vector3.zero);

        // Force back to solid state
        if (player.CurrentStateType != MatterState.Solid)
        {
            player.TransitionToState(MatterState.Solid);
        }

        // Reset position and velocity
        player.transform.position = respawnPos;
        player.Rb.linearVelocity = Vector2.zero;

        Debug.Log($"[LevelManager] Player respawned at {respawnPos}");
    }

    #endregion

    #region Level Completion

    public void CompleteLevel()
    {
        if (levelCompleted) return;
        levelCompleted = true;

        Debug.Log($"[LevelManager] Level {levelIndex} completed with {starsCollected} stars!");

        // Save progress
        if (GameManager.Instance != null)
        {
            GameManager.Instance.CompleteLevel(levelIndex, starsCollected);
        }

        OnLevelComplete?.Invoke();

        // Show completion UI
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowLevelComplete(starsCollected, stars.Count);
        }
    }

    public bool IsLevelCompleted() => levelCompleted;

    #endregion

    #region Player Damage

    /// <summary>
    /// Called when player takes damage from hazards
    /// </summary>
    public void PlayerTakeDamage()
    {
        // For now, just respawn. You could add health system later.
        RespawnPlayer();
    }

    #endregion

    void OnDrawGizmosSelected()
    {
        // Draw spawn point
        if (playerSpawnPoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(playerSpawnPoint.position, 0.5f);
            Gizmos.DrawLine(playerSpawnPoint.position, playerSpawnPoint.position + Vector3.up);
        }

        // Draw death plane
        Gizmos.color = Color.red;
        Vector3 center = new Vector3(0, deathPlaneY, 0);
        Gizmos.DrawLine(center + Vector3.left * 100, center + Vector3.right * 100);
    }
}
