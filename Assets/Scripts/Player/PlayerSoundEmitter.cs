using UnityEngine;

[RequireComponent(typeof(CircleCollider2D))]
public class PlayerSoundEmitter : MonoBehaviour
{
    [SerializeField] private PlayerMovement movement;

    [Header("Ses Yarıçapları")]
    [SerializeField] private float crouchRadius = 1f;
    [SerializeField] private float slowWalkRadius = 2f;
    [SerializeField] private float walkRadius = 3.5f;
    [SerializeField] private float runRadius = 6f;
    [SerializeField] private float smoothSpeed = 12f;

    private CircleCollider2D col;
    private float targetRadius;

    private void Awake()
    {
        col = GetComponent<CircleCollider2D>();
        col.isTrigger = true;

        if (movement == null)
            movement = GetComponentInParent<PlayerMovement>();
    }

    private void Update()
    {
        targetRadius = movement.currentState switch
        {
            MovementState.Crouch => crouchRadius,
            MovementState.SlowWalk => slowWalkRadius,
            MovementState.Walk => walkRadius,
            MovementState.Run => runRadius,
            _ => 0f // Idle = ses yok
        };

        col.radius = Mathf.Lerp(col.radius, targetRadius, Time.deltaTime * smoothSpeed);
        col.enabled = col.radius > 0.05f; // sessizken düşman duymasın
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, runRadius);
    }
}