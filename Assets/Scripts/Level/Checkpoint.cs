using UnityEngine;

/// <summary>
/// Checkpoint that saves player's respawn position.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class Checkpoint : MonoBehaviour
{
    [Header("Visual")]
    [Tooltip("Inactive checkpoint visual")]
    public GameObject inactiveVisual;
    
    [Tooltip("Active checkpoint visual")]
    public GameObject activeVisual;
    
    [Tooltip("Activation effect")]
    public GameObject activateEffectPrefab;

    [Header("Audio")]
    public AudioClip activateSound;
    [Range(0f, 1f)]
    public float volume = 1f;

    private bool isActivated = false;
    private static Checkpoint currentActiveCheckpoint;

    void Start()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;

        UpdateVisuals();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (isActivated) return;

        var player = other.GetComponent<PlayerStateMachine>();
        if (player != null)
        {
            Activate();
        }
    }

    private void Activate()
    {
        // Deactivate previous checkpoint
        if (currentActiveCheckpoint != null && currentActiveCheckpoint != this)
        {
            currentActiveCheckpoint.Deactivate();
        }

        isActivated = true;
        currentActiveCheckpoint = this;

        // Set in level manager
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.SetCheckpoint(transform);
        }

        // Effects
        if (activateEffectPrefab != null)
        {
            Instantiate(activateEffectPrefab, transform.position, Quaternion.identity);
        }

        if (activateSound != null)
        {
            AudioSource.PlayClipAtPoint(activateSound, transform.position, volume);
        }

        UpdateVisuals();

        Debug.Log($"[Checkpoint] Activated: {gameObject.name}");
    }

    private void Deactivate()
    {
        isActivated = false;
        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        if (inactiveVisual != null)
            inactiveVisual.SetActive(!isActivated);

        if (activeVisual != null)
            activeVisual.SetActive(isActivated);
    }

    void OnDrawGizmos()
    {
        Gizmos.color = isActivated ? Color.green : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
        
        // Draw flag
        Gizmos.DrawLine(transform.position, transform.position + Vector3.up * 1.5f);
        Vector3[] flag = new Vector3[]
        {
            transform.position + Vector3.up * 1.5f,
            transform.position + Vector3.up * 1.5f + Vector3.right * 0.5f,
            transform.position + Vector3.up * 1.2f,
            transform.position + Vector3.up * 1.5f
        };
        for (int i = 0; i < flag.Length - 1; i++)
        {
            Gizmos.DrawLine(flag[i], flag[i + 1]);
        }
    }
}
