using UnityEngine;

/// <summary>
/// Solid state - the default player state with normal platformer controls.
/// Can walk, run, jump, and climb.
/// </summary>
public class SolidState : IPlayerState
{
    public MatterState StateType => MatterState.Solid;

    private PlayerStateMachine player;
    private PlayerStateConfig config;

    public void Enter(PlayerStateMachine player)
    {
        this.player = player;
        this.config = player.Config;

        // Restore normal physics
        player.RestoreOriginalPhysics();

        // Make sure main body is visible
        player.SetMainBodyVisible(true);

        // Clear any leftover particles
        player.ClearParticles();

        // Move player to particle center if coming from particle state
        // (handled by the exiting state)

        // Set animator state
        player.Anim.SetBool("IsLiquid", false);
        player.Anim.SetBool("IsGas", false);
        player.Anim.SetBool("IsFrozen", false);
    }

    public void Update()
    {
        UpdateAnimator();
    }

    public void FixedUpdate()
    {
        HandleMovement(player.MoveInput, player.IsRunning);

        if (player.JumpPressed)
        {
            HandleJump();
        }
    }

    public void Exit()
    {
        // Nothing special needed when exiting solid state
    }

    public void HandleMovement(Vector2 input, bool isRunning)
    {
        float speed = isRunning 
            ? config.solidMoveSpeed * config.solidRunMultiplier 
            : config.solidMoveSpeed;

        player.Rb.linearVelocity = new Vector2(input.x * speed, player.Rb.linearVelocity.y);
    }

    public bool HandleJump()
    {
        if (player.IsGrounded)
        {
            player.Anim.SetTrigger("Jump");
            player.Rb.linearVelocity = new Vector2(player.Rb.linearVelocity.x, config.solidJumpForce);
            player.JumpPressed = false;
            return true;
        }
        player.JumpPressed = false;
        return false;
    }

    public bool CanTransitionTo(MatterState targetState)
    {
        // Solid can transition to Liquid or Frozen
        return targetState == MatterState.Liquid || targetState == MatterState.Frozen;
    }

    private void UpdateAnimator()
    {
        float absVelocityX = Mathf.Abs(player.Rb.linearVelocity.x);

        if (absVelocityX > 0.1f)
        {
            // Moving - set speed based on running or walking
            float speedValue = player.IsRunning ? 0.61f : 0.31f;
            player.Anim.SetFloat("Speed", speedValue);
        }
        else
        {
            // Idle
            player.Anim.SetFloat("Speed", 0f);
        }

        // Set grounded state for jump animations
        player.Anim.SetBool("IsGrounded", player.IsGrounded);
    }
}
