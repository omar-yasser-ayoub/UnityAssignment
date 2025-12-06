using UnityEngine;

/// <summary>
/// Applies a rightward force to a Rigidbody2D when triggered.
/// Can be used for push zones, wind effects, or conveyor-like mechanics.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class RightwardForceApplier : MonoBehaviour
{
    [Header("Force Settings")]
    [Tooltip("The force magnitude to apply to the right")]
    [SerializeField] private float forceMagnitude = 10f;

    [Tooltip("How the force is applied")]
    [SerializeField] private ForceMode2D forceMode = ForceMode2D.Force;

    [Header("Trigger Settings")]
    [Tooltip("If true, force is applied continuously while in trigger. If false, force is applied once on enter.")]
    [SerializeField] private bool continuousForce = true;

    [Tooltip("Target a specific Rigidbody2D instead of detecting from trigger")]
    [SerializeField] private Rigidbody2D targetRigidbody;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!continuousForce)
        {
            ApplyForceToCollider(other);
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (continuousForce)
        {
            ApplyForceToCollider(other);
        }
    }

    private void ApplyForceToCollider(Collider2D other)
    {
        Rigidbody2D rb = other.attachedRigidbody;
        if (rb != null)
        {
            ApplyRightwardForce(rb);
        }
    }

    private void ApplyRightwardForce(Rigidbody2D rb)
    {
        Vector2 rightForce = Vector2.right * forceMagnitude;
        rb.AddForce(rightForce, forceMode);
    }

    /// <summary>
    /// Manually apply force to the target Rigidbody (can be called from other scripts or UnityEvents)
    /// </summary>
    public void ApplyForce()
    {
        if (targetRigidbody != null)
        {
            ApplyRightwardForce(targetRigidbody);
        }
    }

    /// <summary>
    /// Apply force to a specific Rigidbody2D
    /// </summary>
    public void ApplyForceTo(Rigidbody2D rb)
    {
        if (rb != null)
        {
            ApplyRightwardForce(rb);
        }
    }

    /// <summary>
    /// Set the force magnitude at runtime
    /// </summary>
    public void SetForceMagnitude(float magnitude)
    {
        forceMagnitude = magnitude;
    }
}
