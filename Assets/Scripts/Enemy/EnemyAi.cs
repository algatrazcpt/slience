using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyAi : MonoBehaviour
{
    public enum EnemyState { Idle, Chase, Search, Return }

    [Header("Hız")]
    [SerializeField] private float chaseSpeed = 3.5f;
    [SerializeField] private float returnSpeed = 2f;

    [Header("Davranış")]
    [SerializeField] private float stopDistance = 0.2f;
    [SerializeField] private float searchDuration = 3f;

    [Header("Components")]
    [SerializeField] private Animator animator;

    public EnemyState currentState = EnemyState.Idle;

    private Rigidbody2D rb;
    private Vector2 startPosition;
    private Vector2 heardPosition;
    private Vector2 moveDirection;
    private float searchTimer;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        if (animator == null)
            animator = GetComponent<Animator>();

        startPosition = rb.position;
    }

    // Player'ın ses collider'ı düşmanın collider'ıyla kesişirse duyar
    private void OnTriggerStay2D(Collider2D other)
    {
        if (!other.TryGetComponent<PlayerSoundEmitter>(out _)) return;

        heardPosition = other.transform.position;
        currentState = EnemyState.Chase;
    }

    private void FixedUpdate()
    {
        moveDirection = Vector2.zero;

        switch (currentState)
        {
            case EnemyState.Idle:
                break;

            case EnemyState.Chase:
                if (MoveTo(heardPosition, chaseSpeed))
                {
                    searchTimer = searchDuration;
                    currentState = EnemyState.Search;
                }
                break;

            case EnemyState.Search:
                searchTimer -= Time.fixedDeltaTime;
                if (searchTimer <= 0f)
                    currentState = EnemyState.Return;
                break;

            case EnemyState.Return:
                if (MoveTo(startPosition, returnSpeed))
                    currentState = EnemyState.Idle;
                break;
        }

        rb.linearVelocity = moveDirection;
        UpdateAnimation();
    }

    // Hedefe vardıysa true döner
    private bool MoveTo(Vector2 target, float speed)
    {
        Vector2 toTarget = target - rb.position;

        if (toTarget.magnitude <= stopDistance)
            return true;

        moveDirection = toTarget.normalized * speed;
        return false;
    }

    private void UpdateAnimation()
    {
        if (animator == null) return;

        bool isMoving = moveDirection.sqrMagnitude > 0.01f;
        if (isMoving)
        {
            animator.SetFloat("MoveX", moveDirection.normalized.x);
            animator.SetFloat("MoveY", moveDirection.normalized.y);
        }
        animator.SetBool("IsMoving", isMoving);
    }
}