using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyProjectile : MonoBehaviour
{
    [SerializeField] private LayerMask obstacleMask; // Duvar layer'ı
    [SerializeField] private float lifetime = 4f;

    private float damage;

    public void Launch(Vector2 direction, float speed, float dmg)
    {
        var rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.linearVelocity = direction * speed;

        damage = dmg;
        transform.right = direction; // sprite yöne baksın

        Destroy(gameObject, lifetime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Player'ın ses collider'ına ve düşmanlara çarpmasın
        if (other.TryGetComponent<PlayerSoundEmitter>(out _)) return;
        if (other.GetComponentInParent<EnemyAI>() != null) return;

        var health = other.GetComponentInParent<PlayerHealth>();
        if (health != null)
        {
            health.TakeDamage(damage);
            Destroy(gameObject);
            return;
        }

        if (((1 << other.gameObject.layer) & obstacleMask) != 0)
            Destroy(gameObject);
    }
}