using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.Pool;
using Assets.Scripts;
using TMPro;
using UnityEngine.UIElements;
using Codice.Client.BaseCommands;
using UnityEngine.InputSystem;
using static PlasticPipe.Server.MonitorStats;

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

    [Header("Bodyparts")]
    [SerializeField]
    private BossBodypartController head;
    [SerializeField]
    private BossBodypartController leftArm;
    [SerializeField]
    private BossBodypartController rightArm;

    private Phase currentPhase;

    private enum Phase
    {
        Phase1, // 75% - 100%
        Phase2, // 50% - 75%
        Phase3, // 25% - 50%
        Phase4  // 0% - 25%
    }

    private Transform player; // A játékos Transformja
    private SpriteRenderer spriteRenderer;
    [Header("Boss Behaviour Settings")]    
    
    [SerializeField]
    private float offsetDistance;  // This defines how far from the center the projectile should start
    [SerializeField]
    private float deviationRadius = 2f; // A célpont eltolásának maximális távolsága
    [SerializeField]
    private float targetUpdateInterval = 2f; // Milyen gyakran frissüljön a célpont
    [SerializeField]
    private float shotTravelSpeed = 500.0f;
    [SerializeField]
    private float intervalStart;

    private float interval;

    private Rigidbody2D rb; // A Rigidbody2D komponens
    private Vector2 movementBoundsMin; // A mozgás minimális határai
    private Vector2 movementBoundsMax; // A mozgás maximális határai
    private Vector2 currentTarget; // Az aktuális célpont
    private float targetUpdateTimer;

    // Idõ, amit várni kell


    // A következõ kiírás ideje
    private float nextTime;



    /// <summary>
    /// Komponensek
    /// </summary>
    private ObjectPoolForProjectiles objectPool;
    private List<Projectile> activeProjectiles = new List<Projectile>();

    private BossObjectPool bossObjectPool;

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


    /// <summary>
    /// Események
    /// </summary>
    public event Action OnDeath;
    public event Action<float> OnHealthChanged;
    public event Action<float> OnPlayerCollision;
    public event Action OnBossDeath;

    public event Action OnHealthBelow75;
    public event Action OnHealthBelow50;
    public event Action OnHealthBelow25;


    private void Awake()
    {
        CurrentHealth = MaxHealth;
        currentPhase = Phase.Phase1;
        interval = intervalStart;

        OnDeath += Die;
        head.OnBodypartPlayerCollision += DealDamageToPlayer;
        leftArm.OnBodypartPlayerCollision += DealDamageToPlayer;
        rightArm.OnBodypartPlayerCollision += DealDamageToPlayer;

        objectPool = FindObjectOfType<ObjectPoolForProjectiles>();
        objectPool.OnProjectileActivated += StartProjectileDetection;
        objectPool.OnProjectileDeactivated += StopProjectileDetection;

        bossObjectPool = FindObjectOfType<BossObjectPool>();

        OnHealthBelow75 += HandleHealthBelow75;
        OnHealthBelow50 += HandleHealthBelow50;
        OnHealthBelow25 += HandleHealthBelow25;

    }

    private void Start()
    {
        player = FindObjectOfType<PlayerController>().transform;
        rb = GetComponent<Rigidbody2D>(); // Hozzáférés a Rigidbody2D komponenshez
        spriteRenderer = GameObject.Find("Background").GetComponent<SpriteRenderer>();

        // Kiszámoljuk a játéktér határait
        Vector2 spriteSize = spriteRenderer.bounds.size;
        Vector2 position = spriteRenderer.transform.position;
        Vector2 topLeft = new Vector2(position.x - spriteSize.x / 2, position.y + spriteSize.y / 2);
        Vector2 bottomRight = new Vector2(position.x + spriteSize.x / 2, position.y - spriteSize.y / 2);
        movementBoundsMin = new Vector2(topLeft.x + 1.5f, bottomRight.y + 1.5f);
        movementBoundsMax = new Vector2(bottomRight.x - 1.5f, topLeft.y - 1.5f);


        // Kezdeti célpont meghatározása
        UpdateTarget();

        nextTime = Time.time + interval;
    }



    void Update()
    {
        if (player != null)
        {
            // Célpont frissítése idõszakosan
            targetUpdateTimer -= Time.deltaTime;
            if (targetUpdateTimer <= 0)
            {
                UpdateTarget();
                targetUpdateTimer = targetUpdateInterval;
            }

            // Mozgás az aktuális célpont felé
            Vector2 newPosition = Vector2.MoveTowards(transform.position, currentTarget, MovementSpeed * Time.deltaTime);

            // Pozíció határokhoz igazítása
            newPosition = ClampPositionToBounds(newPosition);
            transform.position = newPosition;
        }

        if (Time.time >= nextTime)
        {
            // Kiírjuk a szöveget
            Debug.Log("Interval: " + interval);
            Attack();

            // Beállítjuk a következõ idõpontot
            nextTime = Time.time + interval;
        }
    }

    void Attack()
    {
        if (rb == null) return; // Early exit if the rigidbody is null

        // Get the player position (ensure you have a reference to the player)
        Vector2 playerPosition = player.position;

        // Calculate direction towards the player
        Vector2 attackDirection = (playerPosition - rb.position).normalized;

        // Calculate dynamic offset based on the attack direction
        
        Vector2 offset = attackDirection * offsetDistance;  // Offset along the attack direction

        // Apply the offset to the starting position
        Vector2 startPosition = rb.position + offset;

        // Get projectile from pool
        GameObject projectileObject = GetBossProjectileFromPool();
        if (projectileObject == null) return; // Early exit if no projectile available

        LaunchProjectile(projectileObject, startPosition, attackDirection);
    }

    private GameObject GetBossProjectileFromPool()
    {
        // Retrieve a projectile from the pool (ensure that other parameters are correctly passed)
        return bossObjectPool.GetBossProjectile(rb.position, Quaternion.identity, Damage);
    }

    private void LaunchProjectile(GameObject projectileObject, Vector2 startPosition, Vector2 attackDirection)
    {
        // Get the projectile component, set its position and launch it
        EnemyProjetile projectile = projectileObject.GetComponent<EnemyProjetile>();
        projectile.transform.position = startPosition; // Set the new start position
        projectile.Launch(attackDirection, shotTravelSpeed);
    }


    void UpdateTarget()
    {
        // Véletlenszerû eltolás generálása a játékos körül
        Vector2 randomOffset = UnityEngine.Random.insideUnitCircle * deviationRadius;
        currentTarget = (Vector2)player.position + randomOffset;

        // Biztosítjuk, hogy a célpont is a határokon belül legyen
        currentTarget = ClampPositionToBounds(currentTarget);
    }

    Vector2 ClampPositionToBounds(Vector2 position)
    {
        // Biztosítjuk, hogy a pozíció a megadott határokon belül maradjon
        float clampedX = Mathf.Clamp(position.x, movementBoundsMin.x, movementBoundsMax.x);
        float clampedY = Mathf.Clamp(position.y, movementBoundsMin.y, movementBoundsMax.y);
        return new Vector2(clampedX, clampedY);
    }


    void ChangePhase(Phase newPhase)
    {
        if (currentPhase != newPhase)
        {
            currentPhase = newPhase;
            Debug.Log("Phase changed to: " + currentPhase);
            interval -= 0.125f;
        }
    }

    // TODO: ezek váltják a phase-t
    void HandleHealthBelow75()
    {
        Debug.Log("Health dropped below 75%!");
        OnHealthBelow75 -= HandleHealthBelow75; // Leiratkozás az eseményrõl
        ChangePhase(Phase.Phase2);
    }

    void HandleHealthBelow50()
    {
        Debug.Log("Health dropped below 50%!");
        OnHealthBelow50 -= HandleHealthBelow50; // Leiratkozás az eseményrõl
        ChangePhase(Phase.Phase3);
    }

    void HandleHealthBelow25()
    {
        Debug.Log("Health dropped below 25%!");
        OnHealthBelow25 -= HandleHealthBelow25; // Leiratkozás az eseményrõl
        ChangePhase(Phase.Phase4);
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
        ChangeHealth(damageAmount);
    }

    void DealDamageToPlayer()
    {
        OnPlayerCollision?.Invoke(-Damage);
    }


    private void OnTriggerStay2D(Collider2D trigger)
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
        //ValidateUniqueID();
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

        float healthPercentage = (CurrentHealth / MaxHealth) * 100;
        if (healthPercentage <= 75 && OnHealthBelow75 != null)
        {
            OnHealthBelow75.Invoke();
        }

        if (healthPercentage <= 50 && OnHealthBelow50 != null)
        {
            OnHealthBelow50.Invoke();
        }

        if (healthPercentage <= 25 && OnHealthBelow25 != null)
        {
            OnHealthBelow25.Invoke();
        }

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
        OnHealthBelow75 -= HandleHealthBelow75;
        OnHealthBelow50 -= HandleHealthBelow50;
        OnHealthBelow25 -= HandleHealthBelow25;

        if (objectPool != null)
        {
            objectPool.OnProjectileActivated -= StartProjectileDetection;
            objectPool.OnProjectileDeactivated -= StopProjectileDetection;
        }
    }
}
