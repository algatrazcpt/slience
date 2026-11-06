
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference crouchAction;
    [SerializeField] private InputActionReference slowWalkAction;
    [SerializeField] private InputActionReference runAction;

    [Header("Components")]
    [SerializeField] private Animator animator;

    [Header("Movement")]
    [SerializeField] private float crouchSpeed = 1.5f;
    [SerializeField] private float slowWalkSpeed = 2.5f;
    [SerializeField] private float walkSpeed = 4f;
    [SerializeField] private float runSpeed = 6f;

    [Header("Current State")]
    public MovementState currentState;
    public SurfaceType currentSurface = SurfaceType.Ground;

    public Vector2 LastDirection { get; private set; } = Vector2.down;
    public PlayerDirection LastDirectionState { get; private set; } = PlayerDirection.Down;
    public Vector2 MovementInput { get; private set; }

    private Rigidbody2D rb;
    private float currentSpeed;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        if (animator == null)
            animator = GetComponent<Animator>();
    }

    private void OnEnable()
    {
        moveAction.action.Enable();
        crouchAction.action.Enable();
        slowWalkAction.action.Enable();
        runAction.action.Enable();
    }

    private void OnDisable()
    {
        moveAction.action.Disable();
        crouchAction.action.Disable();
        slowWalkAction.action.Disable();
        runAction.action.Disable();
    }

    private void Update()
    {
        ReadInput();
        UpdateMovementState();
        UpdateAnimation();
    }

    private void FixedUpdate()
    {
        rb.linearVelocity = MovementInput * currentSpeed;
    }

    private void ReadInput()
    {
        MovementInput = Vector2.ClampMagnitude(
            moveAction.action.ReadValue<Vector2>(), 1f
        );

        if (MovementInput.sqrMagnitude > 0.01f)
            LastDirection = MovementInput.normalized;
    }


    private void UpdateMovementState()
    {
        bool isMoving = MovementInput.sqrMagnitude > 0.01f;

        if (!isMoving)
            currentState = MovementState.Idle;
        else if (crouchAction.action.IsPressed())
            currentState = MovementState.Crouch;
        else if (slowWalkAction.action.IsPressed())
            currentState = MovementState.SlowWalk;
        else if (runAction.action.IsPressed())
            currentState = MovementState.Run;
        else
            currentState = MovementState.Walk;

        switch (currentState)
        {
            case MovementState.Idle:
                currentSpeed = 0f;
                break;

            case MovementState.Crouch:
                currentSpeed = crouchSpeed;
                break;

            case MovementState.SlowWalk:
                currentSpeed = slowWalkSpeed;
                break;

            case MovementState.Walk:
                currentSpeed = walkSpeed;
                break;

            case MovementState.Run:
                currentSpeed = runSpeed;
                break;
        }
        if (MovementInput.sqrMagnitude > 0.01f)
        {
            UpdateDirection();
        }
    }

    private void UpdateDirection()
    {
        if (MovementInput.sqrMagnitude < 0.01f)
            return;

        float angle = Mathf.Atan2(
            MovementInput.y,
            MovementInput.x
        ) * Mathf.Rad2Deg;

        if (angle < 0)
            angle += 360f;

        if (angle >= 337.5f || angle < 22.5f)
            LastDirectionState = PlayerDirection.Right;

        else if (angle < 67.5f)
            LastDirectionState = PlayerDirection.UpRight;

        else if (angle < 112.5f)
            LastDirectionState = PlayerDirection.Up;

        else if (angle < 157.5f)
            LastDirectionState = PlayerDirection.UpLeft;

        else if (angle < 202.5f)
            LastDirectionState = PlayerDirection.Left;

        else if (angle < 247.5f)
            LastDirectionState = PlayerDirection.DownLeft;

        else if (angle < 292.5f)
            LastDirectionState = PlayerDirection.Down;

        else
            LastDirectionState = PlayerDirection.DownRight;

        LastDirection = LastDirectionState switch
        {
            PlayerDirection.Up => Vector2.up,
            PlayerDirection.Down => Vector2.down,
            PlayerDirection.Left => Vector2.left,
            PlayerDirection.Right => Vector2.right,
            PlayerDirection.UpLeft => new Vector2(-1, 1).normalized,
            PlayerDirection.UpRight => new Vector2(1, 1).normalized,
            PlayerDirection.DownLeft => new Vector2(-1, -1).normalized,
            PlayerDirection.DownRight => new Vector2(1, -1).normalized,
            _ => Vector2.down
        };
    }

    private void UpdateAnimation()
    {
        if (animator == null) return;

        animator.SetFloat("MoveX", LastDirection.x);
        animator.SetFloat("MoveY", LastDirection.y);
        animator.SetBool("IsMoving", currentState != MovementState.Idle);
        animator.SetInteger("MovementState", (int)currentState);
    }

    public void SetSurface(SurfaceType surface)
    {
        currentSurface = surface;
    }
}
