using System.Collections;
using UnityEngine;

public class RangedAttack : EnemyAttack
{
    [Header("Uzak Saldırı")]
    [SerializeField] private EnemyProjectile projectilePrefab;
    [SerializeField] private Transform firePoint; // boşsa düşmanın merkezi
    [SerializeField] private float projectileSpeed = 8f;
    [SerializeField] private float damage = 10f;

    protected override IEnumerator Execute(Transform player)
    {
        Vector2 origin = firePoint != null ? firePoint.position : transform.position;
        Vector2 dir = ((Vector2)player.position - origin).normalized;

        var projectile = Instantiate(projectilePrefab, origin, Quaternion.identity);
        projectile.Launch(dir, projectileSpeed, damage);

        yield break;
    }
}