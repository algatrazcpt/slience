using System.Collections;
using UnityEngine;

public class MeleeAttack : EnemyAttack
{
    [Header("Yakın Saldırı")]
    [SerializeField] private float damage = 15f;
    [SerializeField] private float hitTolerance = 0.3f; // Menzile küçük pay

    protected override IEnumerator Execute(Transform player)
    {
        float dist = Vector2.Distance(transform.position, player.position);
        var health = player.GetComponentInParent<PlayerHealth>();

        if (health != null && dist <= attackRange + hitTolerance)
            health.TakeDamage(damage);

        yield break;
    }
}