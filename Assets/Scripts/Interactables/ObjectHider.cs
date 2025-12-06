using UnityEngine;

/// <summary>
/// Hides a target object when the player enters the trigger/collision zone.
/// Works with both solid player and liquid particles.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class ObjectHider : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("The object to hide when triggered")]
    [SerializeField] private GameObject objectToHide;

    [Header("Settings")]
    [Tooltip("Should the object reappear when the player exits?")]
    [SerializeField] private bool showOnExit = false;

    [Tooltip("Delay before hiding the object (seconds)")]
    [SerializeField] private float hideDelay = 0f;

    // Trigger detection (when Is Trigger = ON)
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (IsPlayerOrParticle(other))
        {
            HideWithDelay();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (showOnExit && IsPlayerOrParticle(other))
        {
            ShowObject();
        }
    }

    // Collision detection (when Is Trigger = OFF)
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (IsPlayerOrParticle(collision.collider))
        {
            HideWithDelay();
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (showOnExit && IsPlayerOrParticle(collision.collider))
        {
            ShowObject();
        }
    }

    private bool IsPlayerOrParticle(Collider2D other)
    {
        // Check for solid player
        if (other.CompareTag("Player") || other.GetComponent<PlayerStateMachine>() != null)
        {
            return true;
        }

        // Check for liquid particle
        if (other.GetComponent<LiquidParticle>() != null)
        {
            return true;
        }

        // Check for gas particle
        if (other.GetComponent<GasParticle>() != null)
        {
            return true;
        }

        return false;
    }

    private void HideWithDelay()
    {
        if (objectToHide != null)
        {
            if (hideDelay > 0f)
            {
                StartCoroutine(HideAfterDelay());
            }
            else
            {
                objectToHide.SetActive(false);
            }
        }
    }

    private System.Collections.IEnumerator HideAfterDelay()
    {
        yield return new WaitForSeconds(hideDelay);
        if (objectToHide != null)
        {
            objectToHide.SetActive(false);
        }
    }

    /// <summary>
    /// Manually trigger hiding the object (can be called from other scripts or UnityEvents)
    /// </summary>
    public void HideObject()
    {
        if (objectToHide != null)
        {
            objectToHide.SetActive(false);
        }
    }

    /// <summary>
    /// Manually show the object again
    /// </summary>
    public void ShowObject()
    {
        if (objectToHide != null)
        {
            objectToHide.SetActive(true);
        }
    }
}
