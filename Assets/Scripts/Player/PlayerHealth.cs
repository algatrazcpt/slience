using UnityEngine;
using UnityEngine.Events;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private float maxHealth = 100f;

    public UnityEvent<float> onDamaged;
    public UnityEvent onDied;

    public float Current { get; private set; }
    public bool IsDead => Current <= 0f;

    private void Awake() => Current = maxHealth;

    public void TakeDamage(float amount)
    {
        if (IsDead) return;

        Current = Mathf.Max(0f, Current - amount);
        onDamaged?.Invoke(amount);

        if (IsDead)
            onDied?.Invoke();
    }
}