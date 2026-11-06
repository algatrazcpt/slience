using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyAI : MonoBehaviour
{
    public enum EnemyState { Idle, Suspicious, Chase, Search, Return }

    public enum StartDirection
    {
        Up, Down, Left, Right,
        UpLeft, UpRight, DownLeft, DownRight,
        Custom
    }

    [Header("Algı Tipi")]
    [SerializeField] private bool canHear = true;
    [SerializeField] private bool canSee = true;

    [Header("Görüş")]
    [SerializeField] private Transform player;
    [SerializeField] private float viewDistance = 6f;
    [SerializeField, Range(10f, 360f)] private float viewAngle = 90f;
    [SerializeField] private LayerMask obstacleMask; // Duvarlar (oyuncu layer'ı EKLEME)

    [Header("Fark Etme Süresi")]
    [Tooltip("Sesi tam fark etmesi için gereken süre (sn)")]
    [SerializeField] private float hearNoticeTime = 0.8f;
    [Tooltip("Seni görüp tam fark etmesi için gereken süre (sn)")]
    [SerializeField] private float seeNoticeTime = 0.5f;
    [Tooltip("Yakındayken fark etme süresi kısalır")]
    [SerializeField] private bool closerIsFaster = true;
    [Tooltip("En yakındayken süre çarpanı (0.3 = süre %30'a düşer)")]
    [SerializeField, Range(0.1f, 1f)] private float minTimeMultiplier = 0.3f;
    [Tooltip("Algı kesilince barın sıfırlanma süresi (sn)")]
    [SerializeField] private float awarenessDecayTime = 2f;

    [Header("Başlangıç Yönü")]
    [SerializeField] private StartDirection startDirection = StartDirection.Down;
    [Tooltip("Sadece Custom iken. 0 = sağ, 90 = yukarı, 180 = sol, 270 = aşağı")]
    [SerializeField, Range(0f, 360f)] private float customAngle = 0f;

    [Header("Hız")]
    [SerializeField] private float chaseSpeed = 3.5f;
    [SerializeField] private float returnSpeed = 2f;

    [Header("Davranış")]
    [SerializeField] private float stopDistance = 0.2f;
    [SerializeField] private float searchDuration = 3f;

    [Header("Saldırı")]
    [SerializeField] private EnemyAttack attack;

    [Header("Components")]
    [SerializeField] private Animator animator;

    [Header("Eventler")]
    public UnityEvent onSuspicious;   // Şüphelenmeye başladı (?)
    public UnityEvent onNoticedPlayer; // Tam fark etti (!)

    public EnemyState currentState = EnemyState.Idle;
    public bool IsSeeingPlayer { get; private set; }
    public float Awareness { get; private set; } // 0..1

    private Rigidbody2D rb;
    private Vector2 startPosition;
    private Vector2 targetPosition;
    private Vector2 suspicionPosition;
    private Vector2 moveVelocity;
    private Vector2 facingDirection;
    private float searchTimer;

    // Duyma bilgisi (OnTriggerStay2D -> FixedUpdate)
    private bool heardThisStep;
    private Vector2 heardPosition;
    private float heardCloseness;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        if (animator == null)
            animator = GetComponent<Animator>();

        if (attack == null)
            attack = GetComponent<EnemyAttack>();

        if (player == null)
        {
            var pm = FindFirstObjectByType<PlayerMovement>();
            if (pm != null) player = pm.transform;
        }

        startPosition = rb.position;
        facingDirection = GetStartFacing();
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

    // ───────────── ALGI ─────────────

    // SES: sadece bilgiyi kaydeder, işi FixedUpdate yapar
    private void OnTriggerStay2D(Collider2D other)
    {
        if (!canHear) return;
        if (!other.TryGetComponent<PlayerSoundEmitter>(out _)) return;

        heardThisStep = true;
        heardPosition = other.transform.position;

        // Ses ne kadar yakınsa (yarıçapa oranla) 1'e yaklaşır
        float closeness = 0f;
        if (other is CircleCollider2D circle)
        {
            float dist = Vector2.Distance(rb.position, heardPosition);
            closeness = Mathf.Clamp01(1f - dist / Mathf.Max(circle.radius, 0.01f));
        }
        heardCloseness = closeness;
    }

    // TEMAS: gecikme yok, anında fark eder
    private void OnCollisionEnter2D(Collision2D collision) => TouchCheck(collision.collider);
    private void OnCollisionStay2D(Collision2D collision) => TouchCheck(collision.collider);

    private void TouchCheck(Collider2D other)
    {
        var pm = other.GetComponentInParent<PlayerMovement>();
        if (pm == null) return;

        Notice(pm.transform.position);
    }

    private void UpdateAwareness(bool seeing)
    {
        bool hearing = heardThisStep;
        heardThisStep = false;

        // Zaten fark etti: algıladıkça hedefi anında güncelle
        if (currentState == EnemyState.Chase)
        {
            if (seeing) targetPosition = player.position;
            else if (hearing) targetPosition = heardPosition;

            Awareness = 1f;
            return;
        }

        if (seeing || hearing)
        {
            Vector2 source;
            float noticeTime;
            float closeness;

            if (seeing)
            {
                source = player.position;
                noticeTime = seeNoticeTime;
                float dist = Vector2.Distance(rb.position, source);
                closeness = Mathf.Clamp01(1f - dist / viewDistance);
            }
            else
            {
                source = heardPosition;
                noticeTime = hearNoticeTime;
                closeness = heardCloseness;
            }

            if (closerIsFaster)
                noticeTime *= Mathf.Lerp(1f, minTimeMultiplier, closeness);

            // Şüphelenmeye başladı
            if (currentState != EnemyState.Suspicious)
            {
                currentState = EnemyState.Suspicious;
                onSuspicious?.Invoke();
            }

            suspicionPosition = source;
            facingDirection = (suspicionPosition - rb.position).normalized; // sesin/görüşün geldiği yöne bak

            Awareness += Time.fixedDeltaTime / Mathf.Max(noticeTime, 0.01f);

            if (Awareness >= 1f)
                Notice(source);
        }
        else
        {
            Awareness -= Time.fixedDeltaTime / Mathf.Max(awarenessDecayTime, 0.01f);

            if (Awareness <= 0f)
            {
                Awareness = 0f;

                if (currentState == EnemyState.Suspicious)
                    currentState = EnemyState.Return; // yanılmışım deyip yerine döner
            }
        }
    }

    // Duyma, görme, temas sonunda buraya çıkar
    private void Notice(Vector2 position)
    {
        if (currentState != EnemyState.Chase)
            onNoticedPlayer?.Invoke();

        Awareness = 1f;
        targetPosition = position;
        currentState = EnemyState.Chase;
    }

    // ───────────── ANA DÖNGÜ ─────────────

    private void FixedUpdate()
    {
        IsSeeingPlayer = canSee && CheckVision();
        UpdateAwareness(IsSeeingPlayer);

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
            case EnemyState.Suspicious: // durup bakınır
                break;

            case EnemyState.Chase:
                if (MoveTo(targetPosition, chaseSpeed))
                {
                    Awareness = 0f;
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
                    facingDirection = GetStartFacing();
                    currentState = EnemyState.Idle;
                }
                break;
        }

        rb.linearVelocity = moveVelocity;

        if (moveVelocity.sqrMagnitude > 0.01f)
            facingDirection = moveVelocity.normalized;

        UpdateAnimation();
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

        return true;
    }

    private bool CheckVision()
    {
        if (player == null) return false;

        Vector2 origin = rb.position;
        Vector2 toPlayer = (Vector2)player.position - origin;
        float distance = toPlayer.magnitude;

        if (distance > viewDistance) return false;
        if (Vector2.Angle(facingDirection, toPlayer) > viewAngle * 0.5f) return false;
        if (Physics2D.Raycast(origin, toPlayer.normalized, distance, obstacleMask)) return false;

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

        Vector2 facing = Application.isPlaying ? facingDirection : GetStartFacing();
        float half = viewAngle * 0.5f;

        // Yeşil: sakin, Sarı: şüpheli, Kırmızı: fark etti
        Gizmos.color = currentState == EnemyState.Chase ? Color.red
                     : Awareness > 0f ? Color.yellow
                     : Color.green;

        Vector3 left = Quaternion.Euler(0, 0, -half) * facing * viewDistance;
        Vector3 right = Quaternion.Euler(0, 0, half) * facing * viewDistance;

        Gizmos.DrawLine(transform.position, transform.position + left);
        Gizmos.DrawLine(transform.position, transform.position + right);
        Gizmos.DrawLine(transform.position + left, transform.position + right);
    }
}