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

        // Set animator state (with safety checks)
        SetAnimatorBoolSafe("IsLiquid", false);
        SetAnimatorBoolSafe("IsGas", false);
        SetAnimatorBoolSafe("IsFrozen", false);
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
        Debug.Log($"[SolidState] Jump pressed! IsGrounded: {player.IsGrounded}, groundCheck assigned: {player.groundCheck != null}");
        
        if (player.IsGrounded)
        {
            Debug.Log($"[SolidState] Jumping with force: {config.solidJumpForce}");
            SetAnimatorTriggerSafe("Jump");
            player.Rb.linearVelocity = new Vector2(player.Rb.linearVelocity.x, config.solidJumpForce);
            player.JumpPressed = false;
            return true;
        }
        else
        {
            Debug.Log("[SolidState] Cannot jump - not grounded!");
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
        if (player.Anim == null) return;

        float absVelocityX = Mathf.Abs(player.Rb.linearVelocity.x);

        if (absVelocityX > 0.1f)
        {
            // Moving - set speed based on running or walking
            float speedValue = player.IsRunning ? 0.61f : 0.31f;
            SetAnimatorFloatSafe("Speed", speedValue);
        }
        else
        {
            // Idle
            SetAnimatorFloatSafe("Speed", 0f);
        }

        // Set grounded state for jump animations
        SetAnimatorBoolSafe("IsGrounded", player.IsGrounded);
    }

    // Helper methods to safely set animator parameters
    private void SetAnimatorBoolSafe(string paramName, bool value)
    {
        if (player.Anim == null) return;
        try
        {
            player.Anim.SetBool(paramName, value);
        }
        catch (System.Exception)
        {
            // Parameter doesn't exist - ignore silently
        }
    }

    private void SetAnimatorFloatSafe(string paramName, float value)
    {
        if (player.Anim == null) return;
        try
        {
            player.Anim.SetFloat(paramName, value);
        }
        catch (System.Exception)
        {
            // Parameter doesn't exist - ignore silently
        }
    }

    private void SetAnimatorTriggerSafe(string paramName)
    {
        if (player.Anim == null) return;
        try
        {
            player.Anim.SetTrigger(paramName);
        }
        catch (System.Exception)
        {
            // Parameter doesn't exist - ignore silently
        }
    }
}
