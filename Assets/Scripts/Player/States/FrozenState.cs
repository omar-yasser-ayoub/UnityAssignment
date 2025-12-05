using UnityEngine;

/// <summary>
/// Frozen state - the player becomes a heavy ice block.
/// Can activate pressure plates, but has limited mobility.
/// Movement is slippery and cannot jump.
/// </summary>
public class FrozenState : IPlayerState
{
    public MatterState StateType => MatterState.Frozen;

    private PlayerStateMachine player;
    private PlayerStateConfig config;

    public void Enter(PlayerStateMachine player)
    {
        this.player = player;
        this.config = player.Config;

        // Make sure main body is visible
        player.SetMainBodyVisible(true);

        // Clear any particles
        player.ClearParticles();

        // Modify physics for heavy, slippery movement
        player.Rb.mass = config.frozenMass;
        player.Rb.linearDamping = config.frozenDrag; // Low drag = slippery

        // Set animator state
        player.Anim.SetBool("IsFrozen", true);
        player.Anim.SetBool("IsLiquid", false);
        player.Anim.SetBool("IsGas", false);

        Debug.Log("Entered Frozen state - heavy and slippery!");
    }

    public void Update()
    {
        UpdateAnimator();
    }

    public void FixedUpdate()
    {
        HandleMovement(player.MoveInput, player.IsRunning);

        // Frozen state cannot jump
        if (player.JumpPressed)
        {
            player.JumpPressed = false;
            // Could play a "can't jump" sound or animation here
        }
    }

    public void Exit()
    {
        // Restore normal physics
        player.RestoreOriginalPhysics();

        player.Anim.SetBool("IsFrozen", false);
    }

    public void HandleMovement(Vector2 input, bool isRunning)
    {
        // Frozen movement is force-based for slippery feel
        float moveForce = config.frozenMoveForce;

        // Apply horizontal force (slippery - momentum carries)
        player.Rb.AddForce(new Vector2(input.x * moveForce, 0));

        // Clamp max speed (slower than normal due to ice physics)
        Vector2 vel = player.Rb.linearVelocity;
        float maxSpeed = config.frozenMaxSpeed;
        vel.x = Mathf.Clamp(vel.x, -maxSpeed, maxSpeed);
        player.Rb.linearVelocity = vel;
    }

    public bool HandleJump()
    {
        // Frozen cannot jump - too heavy!
        player.JumpPressed = false;
        return false;
    }

    public bool CanTransitionTo(MatterState targetState)
    {
        // Frozen can melt back to Liquid or directly to Solid
        return targetState == MatterState.Liquid || targetState == MatterState.Solid;
    }

    private void UpdateAnimator()
    {
        float absVelocityX = Mathf.Abs(player.Rb.linearVelocity.x);

        if (absVelocityX > 0.1f)
        {
            // Sliding animation
            player.Anim.SetFloat("Speed", 0.2f); // Slow sliding animation
        }
        else
        {
            player.Anim.SetFloat("Speed", 0f);
        }

        player.Anim.SetBool("IsGrounded", player.IsGrounded);
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
