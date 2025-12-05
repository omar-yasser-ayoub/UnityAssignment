using UnityEngine;

/// <summary>
/// Interface for all player states in the Matter game.
/// Each state (Solid, Liquid, Gas, Frozen) implements this interface.
/// </summary>
public interface IPlayerState
{
    /// <summary>
    /// The type of matter this state represents
    /// </summary>
    MatterState StateType { get; }

    /// <summary>
    /// Called when entering this state
    /// </summary>
    void Enter(PlayerStateMachine player);

    /// <summary>
    /// Called every frame while in this state
    /// </summary>
    void Update();

    /// <summary>
    /// Called every fixed update while in this state (for physics)
    /// </summary>
    void FixedUpdate();

    /// <summary>
    /// Called when exiting this state
    /// </summary>
    void Exit();

    /// <summary>
    /// Handle horizontal movement input
    /// </summary>
    void HandleMovement(Vector2 input, bool isRunning);

    /// <summary>
    /// Handle jump input - returns true if jump was performed
    /// </summary>
    bool HandleJump();

    /// <summary>
    /// Check if this state can transition to the target state
    /// </summary>
    bool CanTransitionTo(MatterState targetState);
}

/// <summary>
/// Enum representing the different states of matter
/// </summary>
public enum MatterState
{
    Solid,
    Liquid,
    Gas,
    Frozen
}
