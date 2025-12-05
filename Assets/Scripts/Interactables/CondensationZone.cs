using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Condensation zone that transforms gas back to liquid.
/// Used in Cavern levels for the multi-state puzzles.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class CondensationZone : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Time in zone before condensing")]
    public float condensationDelay = 0.5f;

    [Header("Visual")]
    public ParticleSystem mistParticles;
    public Color zoneColor = new Color(0.7f, 0.9f, 1f, 0.3f);

    [Header("Audio")]
    public AudioClip condensationSound;
    [Range(0f, 1f)]
    public float volume = 1f;

    [Header("Events")]
    public UnityEvent OnPlayerCondensed;

    private float timeInZone = 0f;
    private bool playerInZone = false;
    private bool hasCondensed = false;

    void Start()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;

        if (mistParticles != null)
        {
            mistParticles.Play();
        }
    }

    void Update()
    {
        if (playerInZone && !hasCondensed)
        {
            timeInZone += Time.deltaTime;

            if (timeInZone >= condensationDelay)
            {
                CondensePlayer();
            }
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Check for gas particles
        var gasParticle = other.GetComponent<GasParticle>();
        if (gasParticle != null)
        {
            playerInZone = true;
            timeInZone = 0f;
            return;
        }

        // Check for player in gas state
        var player = other.GetComponent<PlayerStateMachine>();
        if (player != null && player.CurrentStateType == MatterState.Gas)
        {
            playerInZone = true;
            timeInZone = 0f;
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        var gasParticle = other.GetComponent<GasParticle>();
        var player = other.GetComponent<PlayerStateMachine>();

        if (gasParticle != null || (player != null && player.CurrentStateType == MatterState.Gas))
        {
            playerInZone = false;
            timeInZone = 0f;
            hasCondensed = false;
        }
    }

    private void CondensePlayer()
    {
        hasCondensed = true;

        var player = FindFirstObjectByType<PlayerStateMachine>();
        if (player != null && player.CurrentStateType == MatterState.Gas)
        {
            Debug.Log("[CondensationZone] Condensing gas to liquid");

            // Play sound
            if (condensationSound != null)
            {
                AudioSource.PlayClipAtPoint(condensationSound, transform.position, volume);
            }

            // Transform to liquid
            player.TransitionToState(MatterState.Liquid);

            OnPlayerCondensed?.Invoke();
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = zoneColor;
        
        var col = GetComponent<Collider2D>();
        if (col != null)
        {
            Gizmos.DrawCube(col.bounds.center, col.bounds.size);
            Gizmos.color = new Color(zoneColor.r, zoneColor.g, zoneColor.b, 1f);
            Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
        }
        else
        {
            Gizmos.DrawCube(transform.position, new Vector3(3, 2, 0));
        }
    }
}
