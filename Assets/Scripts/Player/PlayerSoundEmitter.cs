using UnityEngine;

[RequireComponent(typeof(CircleCollider2D))]
public class PlayerSoundEmitter : MonoBehaviour
{
    [SerializeField] private PlayerMovement movement;
    [SerializeField] private PlayerSurfaceDetector surfaceDetector;

    [Header("Ses Yarıçapları")]
    [SerializeField] private float crouchRadius = 1f;
    [SerializeField] private float slowWalkRadius = 2f;
    [SerializeField] private float walkRadius = 3.5f;
    [SerializeField] private float runRadius = 6f;
    [SerializeField] private float smoothSpeed = 12f;

    [Tooltip("Eğilirken zemin etkisi ne kadar geçerli (0 = hiç, 1 = tam)")]
    [SerializeField, Range(0f, 1f)] private float crouchSurfaceInfluence = 0.5f;

    public float CurrentSurfaceMultiplier { get; private set; } = 1f;

    private CircleCollider2D col;

    private void Awake()
    {
        col = GetComponent<CircleCollider2D>();
        col.isTrigger = true;

        if (movement == null)
            movement = GetComponentInParent<PlayerMovement>();

        if (surfaceDetector == null)
            surfaceDetector = GetComponentInParent<PlayerSurfaceDetector>();

        if (surfaceDetector == null)
            Debug.LogWarning("PlayerSoundEmitter: PlayerSurfaceDetector bulunamadı, zemin çarpanı 1 kullanılacak.", this);
    }

    private void Update()
    {
        float baseRadius = movement.currentState switch
        {
            MovementState.Crouch => crouchRadius,
            MovementState.SlowWalk => slowWalkRadius,
            MovementState.Walk => walkRadius,
            MovementState.Run => runRadius,
            _ => 0f // Idle = ses yok
        };

        float surfaceMult = surfaceDetector != null ? surfaceDetector.CurrentSoundMultiplier : 1f;

        // Eğilirken zemin etkisi azalır
        if (movement.currentState == MovementState.Crouch)
            surfaceMult = Mathf.Lerp(1f, surfaceMult, crouchSurfaceInfluence);

        CurrentSurfaceMultiplier = surfaceMult;

        float targetRadius = baseRadius * surfaceMult;

        col.radius = Mathf.Lerp(col.radius, targetRadius, Time.deltaTime * smoothSpeed);
        col.enabled = col.radius > 0.05f; // sessizken düşman duymasın
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        float r = Application.isPlaying && col != null ? col.radius : runRadius;
        Gizmos.DrawWireSphere(transform.position, r);
    }
}