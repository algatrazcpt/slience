
using UnityEngine;

public class PlayerNoiseSystem : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private CircleCollider2D noiseCollider;

    [Header("Noise Radius")]
    [SerializeField] private float crouchNoise = 1f;
    [SerializeField] private float slowWalkNoise = 2f;
    [SerializeField] private float walkNoise = 4f;
    [SerializeField] private float runNoise = 7f;

    [Header("Surface Multipliers")]
    [SerializeField] private float waterMultiplier = 2f;

    public float CurrentNoiseRadius { get; private set; }

    private void Awake()
    {
        if (playerMovement == null)
            playerMovement = GetComponent<PlayerMovement>();
    }

    private void Update()
    {
        CalculateNoise();
    }

    private void CalculateNoise()
    {
        switch (playerMovement.currentState)
        {
            case MovementState.Idle:
                CurrentNoiseRadius = 0f;
                break;

            case MovementState.Crouch:
                CurrentNoiseRadius = crouchNoise;
                break;

            case MovementState.SlowWalk:
                CurrentNoiseRadius = slowWalkNoise;
                break;

            case MovementState.Walk:
                CurrentNoiseRadius = walkNoise;
                break;

            case MovementState.Run:
                CurrentNoiseRadius = runNoise;
                break;
        }

        if (playerMovement.currentSurface == SurfaceType.Water)
        {
            CurrentNoiseRadius *= waterMultiplier;
        }

        UpdateNoiseCollider();
    }

    private void UpdateNoiseCollider()
    {
        if (noiseCollider == null) return;

        noiseCollider.enabled = CurrentNoiseRadius > 0f;
        noiseCollider.radius = CurrentNoiseRadius;
    }
    private void OnDrawGizmos()
    {
        if (noiseCollider == null)
            return;

        if (!noiseCollider.enabled)
            return;

        Gizmos.color = Color.red;

        Vector3 center = noiseCollider.transform.TransformPoint(
            noiseCollider.offset
        );

        Vector3 scale = noiseCollider.transform.lossyScale;

        float radius = noiseCollider.radius *
                       Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y));

        Gizmos.DrawWireSphere(center, radius);
    }
}
