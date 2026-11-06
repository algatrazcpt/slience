using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class GrabAttack : EnemyAttack
{
    [Header("Yakalama")]
    [SerializeField] private float grabTolerance = 0.3f;
    [SerializeField] private float damagePerTick = 5f;
    [SerializeField] private float tickInterval = 1f;

    [Header("Kurtulma (tuşa bas)")]
    [SerializeField] private InputActionReference struggleAction;
    [SerializeField] private float progressPerPress = 0.15f;
    [SerializeField] private float decayPerSecond = 0.3f;
    [SerializeField] private float stunAfterEscape = 1.2f;

    public UnityEvent onGrabbed;
    public UnityEvent onReleased;

    private PlayerMovement heldPlayer;

    protected override IEnumerator Execute(Transform player)
    {
        // Hazırlıkta kaçtıysa yakalayamaz
        float dist = Vector2.Distance(transform.position, player.position);
        if (dist > attackRange + grabTolerance) yield break;

        heldPlayer = player.GetComponentInParent<PlayerMovement>();
        var health = player.GetComponentInParent<PlayerHealth>();
        if (heldPlayer == null) yield break;

        heldPlayer.CanMove = false;
        if (struggleAction != null) struggleAction.action.Enable();
        onGrabbed?.Invoke();

        float progress = 0f;
        float tickTimer = tickInterval;

        while (progress < 1f)
        {
            if (health != null && health.IsDead) break;

            progress = Mathf.Max(0f, progress - decayPerSecond * Time.deltaTime);
            if (struggleAction != null && struggleAction.action.WasPressedThisFrame())
                progress += progressPerPress;

            tickTimer -= Time.deltaTime;
            if (tickTimer <= 0f)
            {
                if (health != null) health.TakeDamage(damagePerTick);
                tickTimer = tickInterval;
            }

            yield return null;
        }

        Release();
        yield return new WaitForSeconds(stunAfterEscape); // düşman sersem
    }

    private void Release()
    {
        if (heldPlayer == null) return;

        heldPlayer.CanMove = true;
        heldPlayer = null;
        onReleased?.Invoke();
    }

    protected override void OnDisable()
    {
        Release(); // düşman ölürse/kapanırsa oyuncu kilitli kalmasın
        base.OnDisable();
    }
}