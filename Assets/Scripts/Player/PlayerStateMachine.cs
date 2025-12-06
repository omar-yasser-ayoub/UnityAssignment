using UnityEngine;
using UnityEngine.InputSystem;
using System;

/// <summary>
/// Main player state machine that manages state transitions and provides
/// shared functionality to all states.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
public class PlayerStateMachine : MonoBehaviour
{
    [Header("References")]
    public Rigidbody2D Rb { get; private set; }
    public Animator Anim { get; private set; }
    public Collider2D MainCollider { get; private set; }
    public SpriteRenderer SpriteRenderer { get; private set; }

    [Header("State Configuration")]
    [SerializeField] private PlayerStateConfig stateConfig;
    public PlayerStateConfig Config => stateConfig;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.1f;
    public LayerMask groundLayer;

    [Header("Particle Prefabs")]
    [Tooltip("Prefab for liquid particles - small rigidbodies that form the liquid")]
    public GameObject liquidParticlePrefab;
    [Tooltip("Prefab for gas particles - floating rigidbodies")]
    public GameObject gasParticlePrefab;
    [Tooltip("Container to hold spawned particles")]
    public Transform particleContainer;

    [Header("Input")]
    public InputActionAsset inputActionAsset;
    
    // Input state
    public Vector2 MoveInput { get; private set; }
    public bool JumpPressed { get; set; }
    public bool IsRunning { get; private set; }
    public bool IsGrounded { get; private set; }

    // State machine
    private IPlayerState currentState;
    public MatterState CurrentStateType => currentState?.StateType ?? MatterState.Solid;

    // States
    private SolidState solidState;
    private LiquidState liquidState;
    private GasState gasState;
    private FrozenState frozenState;

    // Input actions
    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction sprintAction;
    private InputAction transformAction;

    // Events
    public event Action<MatterState, MatterState> OnStateChanged;

    // Original physics values (to restore when returning to solid)
    private float originalGravityScale;
    private float originalMass;
    private float originalDrag;

    void Awake()
    {
        // Get components
        Rb = GetComponent<Rigidbody2D>();
        Anim = GetComponent<Animator>();
        MainCollider = GetComponent<Collider2D>();
        SpriteRenderer = GetComponent<SpriteRenderer>();

        // Store original physics values
        originalGravityScale = Rb.gravityScale;
        originalMass = Rb.mass;
        originalDrag = Rb.linearDamping;

        // Create particle container if not assigned
        if (particleContainer == null)
        {
            var containerObj = new GameObject("ParticleContainer");
            particleContainer = containerObj.transform;
        }

        // Initialize states
        solidState = new SolidState();
        liquidState = new LiquidState();
        gasState = new GasState();
        frozenState = new FrozenState();

        // Setup input
        SetupInput();
    }

    void Start()
    {
        // Start in solid state
        TransitionToState(MatterState.Solid);
    }

    void SetupInput()
    {
        var playerMap = inputActionAsset.FindActionMap("Player");

        moveAction = playerMap.FindAction("Move");
        moveAction.performed += ctx => MoveInput = ctx.ReadValue<Vector2>();
        moveAction.canceled += ctx => MoveInput = Vector2.zero;

        jumpAction = playerMap.FindAction("Jump");
        jumpAction.performed += ctx => JumpPressed = true;

        sprintAction = playerMap.FindAction("Sprint");
        sprintAction.performed += ctx => IsRunning = true;
        sprintAction.canceled += ctx => IsRunning = false;

        // Add transform action (e.g., press 'F' to transform to liquid)
        transformAction = playerMap.FindAction("Transform");
        if (transformAction != null)
        {
            transformAction.performed += ctx => OnTransformPressed();
        }
    }

    void OnEnable()
    {
        moveAction?.Enable();
        jumpAction?.Enable();
        sprintAction?.Enable();
        transformAction?.Enable();
    }

    void OnDisable()
    {
        moveAction?.Disable();
        jumpAction?.Disable();
        sprintAction?.Disable();
        transformAction?.Disable();
    }

    void Update()
    {
        currentState?.Update();

        // Handle sprite flipping (common to all states when visible)
        if (SpriteRenderer.enabled)
        {
            if (MoveInput.x > 0)
                transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
            else if (MoveInput.x < 0)
                transform.localScale = new Vector3(-Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
        }
    }

    void FixedUpdate()
    {
        // Ground check
        if (groundCheck != null)
        {
            IsGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        }

        currentState?.FixedUpdate();
    }

    /// <summary>
    /// Transition to a new state
    /// </summary>
    public void TransitionToState(MatterState newState)
    {
        if (currentState != null && !currentState.CanTransitionTo(newState))
        {
            Debug.Log($"Cannot transition from {currentState.StateType} to {newState}");
            return;
        }

        MatterState? oldStateType = currentState?.StateType;
        
        // Exit current state
        currentState?.Exit();

        // Get new state
        IPlayerState newStateInstance = newState switch
        {
            MatterState.Solid => solidState,
            MatterState.Liquid => liquidState,
            MatterState.Gas => gasState,
            MatterState.Frozen => frozenState,
            _ => solidState
        };

        // Enter new state
        currentState = newStateInstance;
        currentState.Enter(this);

        // Fire event
        if (oldStateType.HasValue)
        {
            OnStateChanged?.Invoke(oldStateType.Value, newState);
        }

        Debug.Log($"Transitioned to {newState} state");
    }

    /// <summary>
    /// Called when transform button is pressed
    /// </summary>
    private void OnTransformPressed()
    {
        // Cycle through states: Solid -> Liquid -> (Gas via interaction) -> Solid
        switch (CurrentStateType)
        {
            case MatterState.Solid:
                TryTransformTo(MatterState.Liquid);
                break;
            case MatterState.Liquid:
                // Liquid can only become gas through lava/cauldron interaction
                // But can return to solid (free)
                TransitionToState(MatterState.Solid);
                break;
            case MatterState.Gas:
                // Gas automatically condenses back to liquid, then can go solid
                TransitionToState(MatterState.Liquid);
                break;
            case MatterState.Frozen:
                TransitionToState(MatterState.Solid);
                break;
        }
    }

    /// <summary>
    /// Try to transform to a new state, checking energy first
    /// </summary>
    private void TryTransformTo(MatterState targetState)
    {
        // Check if we have enough energy
        if (EnergySystem.Instance != null)
        {
            float cost = EnergySystem.Instance.GetTransformationCost(targetState);
            
            if (!EnergySystem.Instance.TryUseEnergy(cost))
            {
                Debug.Log($"[PlayerStateMachine] Not enough energy to transform to {targetState}!");
                // Optionally show a UI notification
                if (UIManager.Instance != null)
                {
                    UIManager.Instance.ShowNotification("Not enough energy!");
                }
                return;
            }
        }

        // Energy check passed (or no energy system), proceed with transformation
        TransitionToState(targetState);
    }

    /// <summary>
    /// Restore original Rigidbody physics values
    /// </summary>
    public void RestoreOriginalPhysics()
    {
        Rb.gravityScale = originalGravityScale;
        Rb.mass = originalMass;
        Rb.linearDamping = originalDrag;
    }

    /// <summary>
    /// Show/hide the main player sprite and collider
    /// </summary>
    public void SetMainBodyVisible(bool visible)
    {
        SpriteRenderer.enabled = visible;
        MainCollider.enabled = visible;
        Rb.simulated = visible;
    }

    /// <summary>
    /// Clear all spawned particles
    /// </summary>
    public void ClearParticles()
    {
        if (particleContainer != null)
        {
            foreach (Transform child in particleContainer)
            {
                Destroy(child.gameObject);
            }
        }
    }

    /// <summary>
    /// Get the center position of all particles (for reconstitution)
    /// </summary>
    public Vector3 GetParticlesCenterPosition()
    {
        if (particleContainer == null || particleContainer.childCount == 0)
            return transform.position;

        Vector3 center = Vector3.zero;
        int count = 0;

        foreach (Transform child in particleContainer)
        {
            center += child.position;
            count++;
        }

        return count > 0 ? center / count : transform.position;
    }

    void OnDrawGizmosSelected()
    {
        // Draw ground check
        if (groundCheck != null)
        {
            Gizmos.color = IsGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}
