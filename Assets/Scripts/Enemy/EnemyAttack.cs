using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public abstract class EnemyAttack : MonoBehaviour
{
    [Header("Genel")]
    [SerializeField] protected float attackRange = 1.2f;
    [SerializeField] protected float windUpTime = 0.4f;   // Hazırlık: oyuncunun kaçma şansı
    [SerializeField] protected float recoverTime = 0.4f;
    [SerializeField] protected float cooldown = 1.5f;

    [Header("Eventler (animasyon / ses)")]
    public UnityEvent onWindUp;
    public UnityEvent onAttackExecuted;

    public bool IsAttacking { get; private set; }
    public bool IsReady => !IsAttacking && Time.time >= nextAttackTime;

    private float nextAttackTime;

    // Menzilde mi ve arada duvar yok mu?
    public bool InAttackPosition(Transform player, LayerMask obstacleMask)
    {
        Vector2 from = transform.position;
        Vector2 toPlayer = (Vector2)player.position - from;
        float dist = toPlayer.magnitude;

        if (dist > attackRange) return false;
        return !Physics2D.Raycast(from, toPlayer.normalized, dist, obstacleMask);
    }

    public void StartAttack(Transform player)
    {
        if (!IsReady) return;
        StartCoroutine(Run(player));
    }

    private IEnumerator Run(Transform player)
    {
        IsAttacking = true;

        onWindUp?.Invoke();
        yield return new WaitForSeconds(windUpTime);

        onAttackExecuted?.Invoke();
        yield return Execute(player);

        yield return new WaitForSeconds(recoverTime);

        IsAttacking = false;
        nextAttackTime = Time.time + cooldown;
    }

    // Her saldırı tipi kendi etkisini burada yapar
    protected abstract IEnumerator Execute(Transform player);

    protected virtual void OnDisable()
    {
        StopAllCoroutines();
        IsAttacking = false;
    }

    protected virtual void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}