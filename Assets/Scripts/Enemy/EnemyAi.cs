using UnityEngine;
using UnityEngine.Events;


[RequireComponent(typeof(Rigidbody2D))]
public class EnemyAI : MonoBehaviour
{
    public enum EnemyState { Idle, Chase, Search, Return }

    public enum StartDirection
    {
        Up, Down, Left, Right,
        UpLeft, UpRight, DownLeft, DownRight,
        Custom
    }
    [Header("Saldırı")]
    [SerializeField] private EnemyAttack attack;







    [Header("Fark Etme")]
    public UnityEvent onNoticedPlayer; // Chase'e ilk geçtiği an (ünlem efekti, ses vs. için)


    [Header("Algı Tipi")]
    [SerializeField] private bool canHear = true;
    [SerializeField] private bool canSee = true;

    [Header("Görüş")]
    [SerializeField] private Transform player;
    [SerializeField] private float viewDistance = 6f;
    [SerializeField, Range(10f, 360f)] private float viewAngle = 90f;
    [SerializeField] private LayerMask obstacleMask; // Duvarlar vs. (oyuncu layer'ı EKLEME)

    [Header("Başlangıç Yönü")]
    [SerializeField] private StartDirection startDirection = StartDirection.Down;
    [Tooltip("Sadece StartDirection = Custom iken kullanılır. 0 = sağ, 90 = yukarı, 180 = sol, 270 = aşağı")]
    [SerializeField, Range(0f, 360f)] private float customAngle = 0f;

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
        facingDirection = GetStartFacing();
        if (attack == null)
            attack = GetComponent<EnemyAttack>();
    }

    private Vector2 GetStartFacing()
    {
        return startDirection switch
        {
            StartDirection.Up => Vector2.up,
            StartDirection.Down => Vector2.down,
            StartDirection.Left => Vector2.left,
            StartDirection.Right => Vector2.right,
            StartDirection.UpLeft => new Vector2(-1, 1).normalized,
            StartDirection.UpRight => new Vector2(1, 1).normalized,
            StartDirection.DownLeft => new Vector2(-1, -1).normalized,
            StartDirection.DownRight => new Vector2(1, -1).normalized,
            StartDirection.Custom => new Vector2(
                                            Mathf.Cos(customAngle * Mathf.Deg2Rad),
                                            Mathf.Sin(customAngle * Mathf.Deg2Rad)),
            _ => Vector2.down
        };
    }

    // SES: player'ın ses collider'ı düşmana değerse
    private void OnTriggerStay2D(Collider2D other)
    {
        if (!canHear) return;
        if (!other.TryGetComponent<PlayerSoundEmitter>(out _)) return;

        Notice(other.transform.position);
    }

    private void FixedUpdate()
    {
        // GÖRME
        IsSeeingPlayer = canSee && CheckVision();
        if (IsSeeingPlayer)
            Notice(player.position);

        moveVelocity = Vector2.zero;
        if (HandleAttack())
        {
            rb.linearVelocity = Vector2.zero;
            UpdateAnimation();
            return;
        }


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
                    facingDirection = GetStartFacing(); // başlangıç yönüne geri dön
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

    // TEMAS: player'ın gövdesine çarparsa fark eder
    private void OnCollisionEnter2D(Collision2D collision) => TouchCheck(collision.collider);
    private void OnCollisionStay2D(Collision2D collision) => TouchCheck(collision.collider);

    private void TouchCheck(Collider2D other)
    {
        var pm = other.GetComponentInParent<PlayerMovement>();
        if (pm == null) return;

        Notice(pm.transform.position);
    }

    // Duyma, görme, temas hepsi buraya çıkıyor
    private void Notice(Vector2 position)
    {
        if (currentState != EnemyState.Chase)
            onNoticedPlayer?.Invoke();

        targetPosition = position;
        currentState = EnemyState.Chase;
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

        // Edit modunda dropdown'dan seçtiğin yönü anında gösterir
        Vector2 facing = Application.isPlaying ? facingDirection : GetStartFacing();
        float half = viewAngle * 0.5f;

        Gizmos.color = IsSeeingPlayer ? Color.red : Color.green;
        Vector3 left = Quaternion.Euler(0, 0, -half) * facing * viewDistance;
        Vector3 right = Quaternion.Euler(0, 0, half) * facing * viewDistance;

        Gizmos.DrawLine(transform.position, transform.position + left);
        Gizmos.DrawLine(transform.position, transform.position + right);
        Gizmos.DrawLine(transform.position + left, transform.position + right);
    }
    // true dönerse düşman şu an saldırıyor / saldırı pozisyonunda duruyor
    private bool HandleAttack()
    {
        if (attack == null || player == null) return false;

        if (attack.IsAttacking) return true;
        if (currentState != EnemyState.Chase) return false;
        if (!attack.InAttackPosition(player, obstacleMask)) return false;

        facingDirection = ((Vector2)player.position - rb.position).normalized;

        if (attack.IsReady)
            attack.StartAttack(player);

        return true; // cooldown'da da menzilde durur (uzak düşman geri çekilmez/üstüne yürümez)
    }








}