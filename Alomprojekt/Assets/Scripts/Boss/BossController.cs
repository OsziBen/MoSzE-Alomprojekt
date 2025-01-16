using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.Pool;
using Assets.Scripts;

public class BossController : MonoBehaviour
{
    /// <summary>
    /// Változók
    /// </summary>
    [Header("Prefab ID")]
    [SerializeField]
    protected string prefabID;

    [ContextMenu("Generate guid for ID")]
    private void GenerateGuid()
    {
        prefabID = System.Guid.NewGuid().ToString();
    }

    [Header("Base Stats")]
    [SerializeField]
    private float _maxHealth;
    private float _currentHealth;
    [SerializeField]
    private float _movementSpeed;
    [SerializeField]
    private float _damage;
    [SerializeField]
    private float _attackCooldown;

    [Header("Bodyparts")]
    [SerializeField]
    private BossBodypartController head;
    [SerializeField]
    private BossBodypartController leftArm;
    [SerializeField]
    private BossBodypartController rightArm;


    /// <summary>
    /// Komponensek
    /// </summary>
    ObjectPoolForProjectiles objectPool;
    private List<Projectile> activeProjectiles = new List<Projectile>();

    /// <summary>
    /// Getterek és Setterek
    /// </summary>
    /// 
    public string ID
    {
        get { return prefabID; }
    }

    public float CurrentHealth
    {
        get { return _currentHealth; }
        set { _currentHealth = value; }
    }

    public float MaxHealth
    {
        get { return _maxHealth; }
        set { _maxHealth = value; }
    }

    public float MovementSpeed
    {
        get { return _movementSpeed; }
        set { _movementSpeed = value; }
    }

    public float Damage
    {
        get { return _damage; }
        set { _damage = value; }
    }

    public float AttackCooldown
    {
        get { return _attackCooldown; }
        set { _attackCooldown = value; }
    }

    /// <summary>
    /// Események
    /// </summary>
    public event Action OnDeath;
    public event Action<float> OnHealthChanged;
    public event Action<float> OnPlayerCollision;
    public event Action OnBossDeath;


    private void Awake()
    {
        CurrentHealth = MaxHealth;

        OnDeath += Die;
        head.OnBodypartPlayerCollision += DealDamageToPlayer;
        leftArm.OnBodypartPlayerCollision += DealDamageToPlayer;
        rightArm.OnBodypartPlayerCollision += DealDamageToPlayer;

        objectPool = FindObjectOfType<ObjectPoolForProjectiles>();
        objectPool.OnProjectileActivated += StartProjectileDetection;
        objectPool.OnProjectileDeactivated += StopProjectileDetection;

    }


    void StartProjectileDetection(GameObject projectile)
    {
        Projectile proj = projectile.GetComponent<Projectile>();
        // Avoid adding the same projectile twice
        if (!activeProjectiles.Contains(proj))
        {
            proj.OnBossHit += HandleEnemyHit;
            activeProjectiles.Add(proj);

            Debug.Log($"Projectile detected: {proj}");
        }
    }


    void StopProjectileDetection(GameObject projectile)
    {
        Projectile proj = projectile.GetComponent<Projectile>();
        proj.OnBossHit -= HandleEnemyHit;

        activeProjectiles.Remove(proj);
        Debug.Log($"Projectile returned: {proj}");
    }


    void HandleEnemyHit(float damageAmount)
    {
        ChangeHealth(damageAmount);   //damageAmount!
                            //OnBehaviourChange?.Invoke(this);


    }

    void DealDamageToPlayer()
    {
        OnPlayerCollision?.Invoke(-Damage);
    }


    void OnTriggerEnter2D(Collider2D trigger)
    {
        if (trigger.gameObject.TryGetComponent<PlayerController>(out var player))
        {
            Debug.Log(player);
            Debug.Log(Damage);
            DealDamageToPlayer();
        }
    }


    private void OnValidate()
    {
        ValidateUniqueID();
    }


    private void ValidateUniqueID()
    {
        if (string.IsNullOrEmpty(prefabID))
        {
            Debug.LogError("Prefab ID is empty! Please generate or assign a unique ID.", this);
        }
    }

    public void ChangeHealth(float amount)
    {
        CurrentHealth = Mathf.Clamp(CurrentHealth + amount, 0, MaxHealth);
        OnHealthChanged?.Invoke(CurrentHealth);

        Debug.Log(CurrentHealth + " / " + MaxHealth);
        if (CurrentHealth == 0f)
        {
            OnDeath?.Invoke();
        }
    }


    protected virtual void Die()
    {
        Debug.Log("Entity " + gameObject.name + " has died");
        OnBossDeath?.Invoke();
        Destroy(gameObject);
    }


    protected virtual void OnDestroy()
    {
        OnDeath -= Die;

        if (objectPool != null)
        {
            objectPool.OnProjectileActivated -= StartProjectileDetection;
            objectPool.OnProjectileDeactivated -= StopProjectileDetection;
        }
    }
}
