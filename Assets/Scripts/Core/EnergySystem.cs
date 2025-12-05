using UnityEngine;
using System;

/// <summary>
/// Manages player's transformation energy.
/// Cave levels have unlimited energy, other levels require management.
/// </summary>
public class EnergySystem : MonoBehaviour
{
    public static EnergySystem Instance { get; private set; }

    [Header("Settings")]
    [Tooltip("Reference to player config for energy values")]
    public PlayerStateConfig config;

    [Header("Runtime")]
    [SerializeField] private float currentEnergy;
    [SerializeField] private bool isUnlimited = false;

    // Events
    public event Action<float> OnEnergyChanged; // normalized value 0-1
    public event Action OnEnergyDepleted;
    public event Action OnEnergyRestored;

    public float CurrentEnergy => currentEnergy;
    public float MaxEnergy => config != null ? config.maxEnergy : 100f;
    public float NormalizedEnergy => currentEnergy / MaxEnergy;
    public bool IsUnlimited => isUnlimited;
    public bool HasEnoughEnergy(float cost) => isUnlimited || currentEnergy >= cost;

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
        // Initialize energy
        currentEnergy = MaxEnergy;

        // Check if this is a cave level (unlimited energy)
        if (LevelManager.Instance != null)
        {
            isUnlimited = LevelManager.Instance.isCaveLevel;
        }

        // Also check config setting
        if (config != null && config.caveLevelUnlimitedEnergy)
        {
            // Already handled by LevelManager check
        }

        UpdateUI();
    }

    /// <summary>
    /// Try to use energy for a transformation
    /// </summary>
    public bool TryUseEnergy(float cost)
    {
        if (isUnlimited)
        {
            return true;
        }

        if (currentEnergy >= cost)
        {
            currentEnergy -= cost;
            OnEnergyChanged?.Invoke(NormalizedEnergy);
            UpdateUI();

            if (currentEnergy <= 0)
            {
                OnEnergyDepleted?.Invoke();
            }

            Debug.Log($"[Energy] Used {cost}, remaining: {currentEnergy}");
            return true;
        }

        Debug.Log($"[Energy] Not enough energy! Need {cost}, have {currentEnergy}");
        return false;
    }

    /// <summary>
    /// Add energy (from recharge stations)
    /// </summary>
    public void AddEnergy(float amount)
    {
        if (isUnlimited) return;

        float previousEnergy = currentEnergy;
        currentEnergy = Mathf.Min(currentEnergy + amount, MaxEnergy);
        
        OnEnergyChanged?.Invoke(NormalizedEnergy);
        UpdateUI();

        if (previousEnergy <= 0 && currentEnergy > 0)
        {
            OnEnergyRestored?.Invoke();
        }

        Debug.Log($"[Energy] Added {amount}, now: {currentEnergy}");
    }

    /// <summary>
    /// Fully restore energy
    /// </summary>
    public void RestoreFullEnergy()
    {
        currentEnergy = MaxEnergy;
        OnEnergyChanged?.Invoke(NormalizedEnergy);
        OnEnergyRestored?.Invoke();
        UpdateUI();
    }

    /// <summary>
    /// Set whether energy is unlimited (for cave levels)
    /// </summary>
    public void SetUnlimited(bool unlimited)
    {
        isUnlimited = unlimited;
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (UIManager.Instance != null)
        {
            if (isUnlimited)
            {
                UIManager.Instance.SetEnergyUnlimited(true);
            }
            else
            {
                UIManager.Instance.UpdateEnergy(NormalizedEnergy);
            }
        }
    }

    /// <summary>
    /// Get the energy cost for a specific transformation
    /// </summary>
    public float GetTransformationCost(MatterState targetState)
    {
        if (config == null) return 20f;

        return targetState switch
        {
            MatterState.Liquid => config.liquidTransformCost,
            MatterState.Gas => 0f, // Gas is triggered by environment
            MatterState.Frozen => 0f, // Frozen is triggered by environment
            MatterState.Solid => 0f, // Returning to solid is free
            _ => 0f
        };
    }
}
