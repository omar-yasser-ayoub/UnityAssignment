using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 2f;
    public float runSpeedMultiplier = 1.35f;
    public float jumpForce = 0.5f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.01f;
    public LayerMask groundLayer;

    private Rigidbody2D rb;
    private Animator anim;
    private Vector2 moveInput;
    private bool isGrounded = true;
    private bool jumpPressed = false;
    private bool isRunning = false;

    public InputActionAsset inputActionAsset;
    public InputAction moveAction;
    public InputAction jumpAction;
    public InputAction sprintAction;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
    }

    void Awake()
    {
        moveAction = inputActionAsset.FindActionMap("Player").FindAction("Move");
        moveAction.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        moveAction.canceled += ctx => moveInput = Vector2.zero;

        jumpAction = inputActionAsset.FindActionMap("Player").FindAction("Jump");
        jumpAction.performed += ctx => jumpPressed = true;

        sprintAction = inputActionAsset.FindActionMap("Player").FindAction("Sprint");
        sprintAction.performed += ctx => isRunning = true;
        sprintAction.canceled += ctx => isRunning = false;
    }

    void OnEnable()
    {
        moveAction.Enable();
        jumpAction.Enable();
        sprintAction.Enable();
    }

    void OnDisable()
    {
        moveAction.Disable();
        jumpAction.Disable();
        sprintAction.Disable();
    }

    void Update()
    {
        // Flip sprite
        if (moveInput.x > 0)
            transform.localScale = new Vector3(10, 10, 10);
        else if (moveInput.x < 0)
            transform.localScale = new Vector3(-10, 10, 10);

        if (rb.linearVelocity.x != 0)
        {
            // Update animator
            if (isRunning)
            {
                anim.SetFloat("Speed", 0.61f);
            }
            else
            {
                anim.SetFloat("Speed", 0.31f);
            }
        }
        else
        {
            anim.SetFloat("Speed", 0.0f);
        }
    }

    void FixedUpdate()
    {
        if (groundCheck != null)
        {
            isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        }
        
        // Handle jumping
        if (jumpPressed && isGrounded)
        {
            anim.SetTrigger("Jump");
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 5.0f);
            jumpPressed = false;
        }

        float currentSpeed = isRunning ? moveSpeed * runSpeedMultiplier : moveSpeed;

        // Move player
        rb.linearVelocity = new Vector2(moveInput.x * currentSpeed, rb.linearVelocity.y);
    }
}