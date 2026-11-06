using UnityEngine;
using UnityEngine.Events;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private float maxHealth = 100f;

    public UnityEvent<float> onDamaged;
    public UnityEvent onDied;
    [SerializeField] public float current;
    public bool IsDead => current <= 0f;

    private void Awake() => current = maxHealth;

    public void TakeDamage(float amount)
    {
        Debug.Log("PlayerTakeDamage");
        if (IsDead) return;

        current = Mathf.Max(0f, current - amount);
        onDamaged?.Invoke(amount);

        if (IsDead)
            onDied?.Invoke();
    }
}