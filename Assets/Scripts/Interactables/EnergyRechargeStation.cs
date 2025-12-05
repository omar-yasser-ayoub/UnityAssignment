using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Energy recharge station that refills player's transformation energy.
/// Used in Forest and Ice Cavern levels.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class EnergyRechargeStation : MonoBehaviour
{
    [Header("Recharge Settings")]
    [Tooltip("Energy restored per second while in zone")]
    public float rechargeRate = 25f;
    
    [Tooltip("Instant recharge on first contact")]
    public bool instantFullRecharge = false;
    
    [Tooltip("One-time use only")]
    public bool singleUse = false;

    [Header("Visual")]
    public GameObject activeVisual;
    public GameObject depletedVisual;
    public ParticleSystem rechargeParticles;

    [Header("Audio")]
    public AudioClip rechargeSound;
    public AudioSource audioSource;
    [Range(0f, 1f)]
    public float volume = 0.5f;

    [Header("Events")]
    public UnityEvent OnRechargeStart;
    public UnityEvent OnRechargeComplete;
    public UnityEvent OnDepleted;

    private bool isRecharging = false;
    private bool isDepleted = false;

    void Start()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;

        UpdateVisuals();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (isDepleted) return;

        if (other.CompareTag("Player") || other.GetComponent<PlayerStateMachine>() != null)
        {
            StartRecharging();
        }
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (isDepleted || !isRecharging) return;

        if (other.CompareTag("Player") || other.GetComponent<PlayerStateMachine>() != null)
        {
            // Continuous recharge
            if (EnergySystem.Instance != null && !instantFullRecharge)
            {
                EnergySystem.Instance.AddEnergy(rechargeRate * Time.deltaTime);

                // Check if fully recharged
                if (EnergySystem.Instance.NormalizedEnergy >= 1f)
                {
                    CompleteRecharge();
                }
            }
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player") || other.GetComponent<PlayerStateMachine>() != null)
        {
            StopRecharging();
        }
    }

    private void StartRecharging()
    {
        if (isRecharging) return;
        isRecharging = true;

        if (instantFullRecharge && EnergySystem.Instance != null)
        {
            EnergySystem.Instance.RestoreFullEnergy();
            CompleteRecharge();
            return;
        }

        // Start particles
        if (rechargeParticles != null)
        {
            rechargeParticles.Play();
        }

        // Start sound
        if (audioSource != null && rechargeSound != null)
        {
            audioSource.clip = rechargeSound;
            audioSource.loop = true;
            audioSource.volume = volume;
            audioSource.Play();
        }

        OnRechargeStart?.Invoke();

        Debug.Log("[EnergyStation] Started recharging");
    }

    private void StopRecharging()
    {
        if (!isRecharging) return;
        isRecharging = false;

        // Stop particles
        if (rechargeParticles != null)
        {
            rechargeParticles.Stop();
        }

        // Stop sound
        if (audioSource != null)
        {
            audioSource.Stop();
        }
    }

    private void CompleteRecharge()
    {
        StopRecharging();

        if (singleUse)
        {
            isDepleted = true;
            UpdateVisuals();
            OnDepleted?.Invoke();
        }

        OnRechargeComplete?.Invoke();

        Debug.Log("[EnergyStation] Recharge complete");
    }

    private void UpdateVisuals()
    {
        if (activeVisual != null)
            activeVisual.SetActive(!isDepleted);

        if (depletedVisual != null)
            depletedVisual.SetActive(isDepleted);
    }

    void OnDrawGizmos()
    {
        Gizmos.color = isDepleted ? Color.gray : Color.cyan;
        
        var col = GetComponent<Collider2D>();
        if (col != null)
        {
            Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
        }
        else
        {
            Gizmos.DrawWireCube(transform.position, new Vector3(2, 2, 0));
        }

        // Draw energy icon
        Gizmos.color = new Color(0, 1, 1, 0.5f);
        Gizmos.DrawSphere(transform.position + Vector3.up, 0.3f);
    }
}
