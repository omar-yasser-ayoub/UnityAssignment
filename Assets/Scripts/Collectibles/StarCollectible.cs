using UnityEngine;
using System;

/// <summary>
/// Collectable star that players can pick up.
/// Each level has 3 stars placed in various locations.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class StarCollectible : MonoBehaviour
{
    [Header("Visual Settings")]
    [Tooltip("Rotation speed in degrees per second")]
    public float rotationSpeed = 90f;
    
    [Tooltip("Bob up and down")]
    public bool bobEnabled = true;
    
    [Tooltip("Bob height")]
    public float bobHeight = 0.2f;
    
    [Tooltip("Bob speed")]
    public float bobSpeed = 2f;

    [Header("Collection Effects")]
    [Tooltip("Particle effect on collection (optional)")]
    public GameObject collectEffectPrefab;
    
    [Tooltip("Sound to play on collection")]
    public AudioClip collectSound;
    
    [Tooltip("Volume of collect sound")]
    [Range(0f, 1f)]
    public float soundVolume = 1f;

    [Header("State")]
    [SerializeField] private bool collected = false;

    // Event fired when collected
    public event Action<StarCollectible> OnCollected;

    // For bobbing animation
    private Vector3 startPosition;
    private float bobTimer;

    void Start()
    {
        startPosition = transform.position;
        
        // Ensure trigger is set
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    void Update()
    {
        if (collected) return;

        // Rotation animation
        if (rotationSpeed != 0)
        {
            transform.Rotate(0, 0, rotationSpeed * Time.deltaTime);
        }

        // Bobbing animation
        if (bobEnabled)
        {
            bobTimer += Time.deltaTime * bobSpeed;
            float yOffset = Mathf.Sin(bobTimer) * bobHeight;
            transform.position = startPosition + new Vector3(0, yOffset, 0);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (collected) return;

        // Check if player touched the star
        if (other.CompareTag("Player") || other.GetComponent<PlayerStateMachine>() != null)
        {
            Collect();
        }

        // Also check for liquid particles
        if (other.GetComponent<LiquidParticle>() != null)
        {
            Collect();
        }
    }

    private void Collect()
    {
        if (collected) return;
        collected = true;

        Debug.Log($"[Star] Collected: {gameObject.name}");

        // Fire event
        OnCollected?.Invoke(this);

        // Spawn effect
        if (collectEffectPrefab != null)
        {
            Instantiate(collectEffectPrefab, transform.position, Quaternion.identity);
        }

        // Play sound
        if (collectSound != null)
        {
            AudioSource.PlayClipAtPoint(collectSound, transform.position, soundVolume);
        }

        // Hide/destroy the star
        gameObject.SetActive(false);
    }

    /// <summary>
    /// Reset star state (for level restart without reloading scene)
    /// </summary>
    public void ResetStar()
    {
        collected = false;
        transform.position = startPosition;
        gameObject.SetActive(true);
    }

    public bool IsCollected => collected;

    void OnDrawGizmos()
    {
        // Draw star icon in editor
        Gizmos.color = collected ? Color.gray : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 0.3f);
        
        // Draw a simple star shape
        Vector3 pos = transform.position;
        for (int i = 0; i < 5; i++)
        {
            float angle1 = (i * 72 - 90) * Mathf.Deg2Rad;
            float angle2 = ((i + 2) * 72 - 90) * Mathf.Deg2Rad;
            Vector3 p1 = pos + new Vector3(Mathf.Cos(angle1), Mathf.Sin(angle1), 0) * 0.3f;
            Vector3 p2 = pos + new Vector3(Mathf.Cos(angle2), Mathf.Sin(angle2), 0) * 0.3f;
            Gizmos.DrawLine(p1, p2);
        }
    }
}
