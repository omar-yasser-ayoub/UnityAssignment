using UnityEngine;

/// <summary>
/// Frozen state - the player becomes a frozen ice block.
/// Movement is super slippery, no jumping, no animations, tinted blue.
/// </summary>
public class FrozenState : IPlayerState
{
    public MatterState StateType => MatterState.Frozen;

    private PlayerStateMachine player;
    private PlayerStateConfig config;
    private Color originalColor;
    private float originalAnimSpeed;

    // Frozen visual settings
    private static readonly Color FROZEN_TINT = new Color(0.5f, 0.7f, 1f, 1f); // Blue tint

    public void Enter(PlayerStateMachine player)
    {
        this.player = player;
        this.config = player.Config;

        // IMPORTANT: Get the particle center position BEFORE clearing particles
        // This ensures we spawn at the correct location when transitioning from Liquid/Gas
        Vector3 particleCenter = player.GetParticlesCenterPosition();
        
        // Move player to particle center before making visible
        player.transform.position = particleCenter;

        // Clear any particles (must be done after getting center position)
        player.ClearParticles();

        // Make sure main body is visible (after positioning)
        player.SetMainBodyVisible(true);

        // Store original color and apply blue tint
        originalColor = player.SpriteRenderer.color;
        player.SpriteRenderer.color = FROZEN_TINT;

        // Disable animations by setting speed to 0
        originalAnimSpeed = player.Anim.speed;
        player.Anim.speed = 0f;

        // Modify physics for super slippery movement
        player.Rb.mass = config.frozenMass;
        player.Rb.linearDamping = config.frozenDrag; // Very low drag = super slippery

        // Set animator state (for any state-based logic)
        player.Anim.SetBool("IsFrozen", true);
        player.Anim.SetBool("IsLiquid", false);
        player.Anim.SetBool("IsGas", false);

        Debug.Log("Entered Frozen state - blue, slippery, no animations!");
    }

    public void Update()
    {
        // No animator updates since animations are frozen
    }

    public void FixedUpdate()
    {
        HandleMovement(player.MoveInput, player.IsRunning);

        // Frozen state cannot jump
        if (player.JumpPressed)
        {
            player.JumpPressed = false;
        }
    }

    public void Exit()
    {
        // Restore original color
        player.SpriteRenderer.color = originalColor;

        // Restore animation speed
        player.Anim.speed = originalAnimSpeed;

        // Restore normal physics
        player.RestoreOriginalPhysics();

        player.Anim.SetBool("IsFrozen", false);
    }

    public void HandleMovement(Vector2 input, bool isRunning)
    {
        // Super slippery force-based movement - only left and right
        float moveForce = config.frozenMoveForce;

        // Apply horizontal force only (no vertical control)
        player.Rb.AddForce(new Vector2(input.x * moveForce, 0));

        // Clamp max speed
        Vector2 vel = player.Rb.linearVelocity;
        float maxSpeed = config.frozenMaxSpeed;
        vel.x = Mathf.Clamp(vel.x, -maxSpeed, maxSpeed);
        player.Rb.linearVelocity = vel;
    }

    public bool HandleJump()
    {
        // Frozen cannot jump!
        player.JumpPressed = false;
        return false;
    }

    public bool CanTransitionTo(MatterState targetState)
    {
        // Frozen can melt back to Liquid or directly to Solid
        return targetState == MatterState.Liquid || targetState == MatterState.Solid;
    }

    /// <summary>
    /// Called when frozen player touches heat source
    /// </summary>
    public void Melt()
    {
        player.TransitionToState(MatterState.Liquid);
    }

    /// <summary>
    /// Called when player manually unfreezes (e.g., holding a button)
    /// </summary>
    public void Thaw()
    {
        player.TransitionToState(MatterState.Solid);
    }
}
