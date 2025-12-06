using UnityEngine;

/// <summary>
/// Zone that freezes the player when they enter.
/// Works with all player states (Solid, Liquid, Gas).
/// Uses collision detection so the zone can be a walkable surface.
/// </summary>
public class FreezeZone : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Delay before freezing (seconds)")]
    [SerializeField] private float freezeDelay = 0f;

    [Header("Effects")]
    [Tooltip("Optional effect to spawn when freezing")]
    [SerializeField] private GameObject freezeEffectPrefab;

    [Tooltip("Optional sound to play when freezing")]
    [SerializeField] private AudioClip freezeSound;

    [Range(0f, 1f)]
    [SerializeField] private float volume = 1f;

    private bool hasTriggered = false;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (hasTriggered) return;

        // Check for solid player
        PlayerStateMachine player = collision.collider.GetComponent<PlayerStateMachine>();
        if (player != null)
        {
            TriggerFreeze(player);
            return;
        }

        // Check for liquid particle
        LiquidParticle liquidParticle = collision.collider.GetComponent<LiquidParticle>();
        if (liquidParticle != null)
        {
            player = FindFirstObjectByType<PlayerStateMachine>();
            if (player != null && player.CurrentStateType == MatterState.Liquid)
            {
                TriggerFreeze(player);
                return;
            }
        }

        // Check for gas particle
        GasParticle gasParticle = collision.collider.GetComponent<GasParticle>();
        if (gasParticle != null)
        {
            player = FindFirstObjectByType<PlayerStateMachine>();
            if (player != null && player.CurrentStateType == MatterState.Gas)
            {
                TriggerFreeze(player);
                return;
            }
        }
    }

    private void TriggerFreeze(PlayerStateMachine player)
    {
        hasTriggered = true;

        if (freezeDelay > 0f)
        {
            StartCoroutine(FreezeAfterDelay(player));
        }
        else
        {
            FreezePlayer(player);
        }
    }

    private System.Collections.IEnumerator FreezeAfterDelay(PlayerStateMachine player)
    {
        yield return new WaitForSeconds(freezeDelay);
        FreezePlayer(player);
    }

    private void FreezePlayer(PlayerStateMachine player)
    {
        // Spawn effect
        if (freezeEffectPrefab != null)
        {
            Instantiate(freezeEffectPrefab, player.transform.position, Quaternion.identity);
        }

        // Play sound
        if (freezeSound != null)
        {
            AudioSource.PlayClipAtPoint(freezeSound, transform.position, volume);
        }

        // Transition to frozen state
        player.TransitionToState(MatterState.Frozen);

        Debug.Log("[FreezeZone] Player frozen!");
    }

    /// <summary>
    /// Reset the trigger (call this if the zone should be reusable)
    /// </summary>
    public void ResetTrigger()
    {
        hasTriggered = false;
    }
}
