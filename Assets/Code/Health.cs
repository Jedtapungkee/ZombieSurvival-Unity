using System;
using UnityEngine;

public class Health : MonoBehaviour
{
    [Min(1)] public int maxHealth = 100;
    [SerializeField] private bool destroyOnDeath = false;
    [Header("Debug")] public bool debugLog;

    public int CurrentHealth { get; private set; }
    public bool IsDead => CurrentHealth <= 0;

    // Events
    public event Action<int, int> Damaged;   // (damage, currentHealth)
    public event Action Healed;              // Fired on heal
    public event Action Died;

    void Awake()
    {
        CurrentHealth = Mathf.Max(1, maxHealth);
        if (debugLog) Debug.Log($"[Health] Awake {name} HP={CurrentHealth}/{maxHealth}", this);
    }

    public void ResetHealth()
    {
        CurrentHealth = Mathf.Max(1, maxHealth);
    }

    public void TakeDamage(int damage)
    {
        if (IsDead || damage <= 0) return;
        int before = CurrentHealth;
        CurrentHealth = Mathf.Max(0, CurrentHealth - damage);
        if (debugLog) Debug.Log($"[Health] {name} took {damage}. {before}->{CurrentHealth}", this);
        Damaged?.Invoke(damage, CurrentHealth);

        if (CurrentHealth <= 0)
        {
            if (debugLog) Debug.Log($"[Health] {name} died.", this);
            Died?.Invoke();
            if (destroyOnDeath)
            {
                Destroy(gameObject);
            }
        }
    }

    public void Heal(int amount)
    {
        if (amount <= 0 || IsDead) return;
        int before = CurrentHealth;
        CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + amount);
        if (debugLog) Debug.Log($"[Health] {name} healed {amount}. {before}->{CurrentHealth}", this);
        Healed?.Invoke();
    }
}
