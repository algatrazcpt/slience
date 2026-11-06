using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyAI : MonoBehaviour
{
    public enum EnemyState { Idle, Chase, Search, Return }

    [Header("Algı Tipi")]
    [SerializeField] private bool canHear = true;
    [SerializeField] private bool canSee = true;

    [Header("Görüş")]
    [SerializeField] private Transform player;
    [SerializeField] private float viewDistance = 6f;
    [SerializeField, Range(10f, 360f)] private float viewAngle = 90f;
    [SerializeField] private LayerMask obstacleMask; // Duvarlar vs. (oyuncu layer'ı EKLEME)
    [SerializeField] private Vector2 startFacing = Vector2.down;

    [Header("Hız")]
    [SerializeField] private float chaseSpeed = 3.5f;
    [SerializeField] private float returnSpeed = 2f;

    [Header("Davranış")]
    [SerializeField] private float stopDistance = 0.2f;
    [SerializeField] private float searchDuration = 3f;

    [Header("Components")]
    [SerializeField] private Animator animator;

    public EnemyState currentState = EnemyState.Idle;
    public bool IsSeeingPlayer { get; private set; }

    private Rigidbody2D rb;
    private Vector2 startPosition;
    private Vector2 targetPosition;
    private Vector2 moveVelocity;
    private Vector2 facingDirection;
    private float searchTimer;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        if (animator == null)
            animator = GetComponent<Animator>();

        if (player == null)
        {
            var pm = FindFirstObjectByType<PlayerMovement>();
            if (pm != null) player = pm.transform;
        }

        startPosition = rb.position;
        facingDirection = startFacing.normalized;
    }

    // SES: player'ın ses collider'ı düşmana değerse
    private void OnTriggerStay2D(Collider2D other)
    {
        if (!canHear) return;
        if (!other.TryGetComponent<PlayerSoundEmitter>(out _)) return;

        targetPosition = other.transform.position;
        currentState = EnemyState.Chase;
    }

    private void FixedUpdate()
    {
        // GÖRME
        IsSeeingPlayer = canSee && CheckVision();
        if (IsSeeingPlayer)
        {
            targetPosition = player.position;
            currentState = EnemyState.Chase;
        }

        moveVelocity = Vector2.zero;

        switch (currentState)
        {
            case EnemyState.Idle:
                break;

            case EnemyState.Chase:
                if (MoveTo(targetPosition, chaseSpeed))
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
                {
                    facingDirection = startFacing.normalized;
                    currentState = EnemyState.Idle;
                }
                break;
        }

        rb.linearVelocity = moveVelocity;

        if (moveVelocity.sqrMagnitude > 0.01f)
            facingDirection = moveVelocity.normalized;

        UpdateAnimation();
    }

    private bool CheckVision()
    {
        if (player == null) return false;

        Vector2 origin = rb.position;
        Vector2 toPlayer = (Vector2)player.position - origin;
        float distance = toPlayer.magnitude;

        if (distance > viewDistance) return false;

        // Açı kontrolü
        if (Vector2.Angle(facingDirection, toPlayer) > viewAngle * 0.5f) return false;

        // Arada duvar var mı?
        if (Physics2D.Raycast(origin, toPlayer.normalized, distance, obstacleMask))
            return false;

        return true;
    }

    // Hedefe vardıysa true döner
    private bool MoveTo(Vector2 target, float speed)
    {
        Vector2 toTarget = target - rb.position;

        if (toTarget.magnitude <= stopDistance)
            return true;

        moveVelocity = toTarget.normalized * speed;
        return false;
    }

    private void UpdateAnimation()
    {
        if (animator == null) return;

        bool isMoving = moveVelocity.sqrMagnitude > 0.01f;
        animator.SetFloat("MoveX", facingDirection.x);
        animator.SetFloat("MoveY", facingDirection.y);
        animator.SetBool("IsMoving", isMoving);
    }

    private void OnDrawGizmosSelected()
    {
        if (!canSee) return;

        Vector2 facing = Application.isPlaying ? facingDirection : startFacing.normalized;
        float half = viewAngle * 0.5f;

        Gizmos.color = IsSeeingPlayer ? Color.red : Color.green;
        Vector3 left = Quaternion.Euler(0, 0, -half) * facing * viewDistance;
        Vector3 right = Quaternion.Euler(0, 0, half) * facing * viewDistance;

        Gizmos.DrawLine(transform.position, transform.position + left);
        Gizmos.DrawLine(transform.position, transform.position + right);
        Gizmos.DrawWireSphere(transform.position, viewDistance);
    }
}