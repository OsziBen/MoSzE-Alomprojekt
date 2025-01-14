using Assets.Scripts;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.TextCore.Text;
using Assets.Scripts.EnemyBehaviours;
using static Cinemachine.DocumentationSortingAttribute;
using System.Linq;

public class EnemyController : Assets.Scripts.Character
{
    [Header("Point Value")]
    [SerializeField]
    private int basePointValue;
    private int _currentPointValue;

    private int minPointValue = 10;
    private int maxPointValue = 1000;

    [Header("Scale Factors")]
    [SerializeField, Range(1f, 2f)]
    private float maxHealthScaleFactor;
    [SerializeField, Range(1f, 2f)]
    private float baseMovementSpeedScaleFactor;
    [SerializeField, Range(1f, 2f)]
    private float baseDMGScaleFactor;
    [SerializeField, Range(1f, 2f)]
    private float baseAttackCooldownScaleFactor;
    [SerializeField, Range(1f, 2f)]
    private float baseCriticalHitChanceScaleFactor;
    [SerializeField, Range(1f, 2f)]
    private float basePercentageBasedDMGScaleFactor;
    [SerializeField, Range(1f, 2f)]
    private float pointValueScaleFactor;

    // TODO: IsMarkingOn implementálása


    [Header("Sprites and Colliders")]
    [SerializeField]
    private List<SpriteLevelData> spriteLevelDataList;



    /// <summary>
    /// Változók
    /// </summary>
    private List<Projectile> activeProjectiles = new List<Projectile>();    // Az aktívan megfigyelt lövedékek (Projectile) listája
    private bool hasDetectedPlayer = false;

    /// <summary>
    /// Komponenesek
    /// </summary>
    private ObjectPoolForProjectiles objectPool;  // A lövedékek kezelésére szolgáló ObjectPool objektum
    private PlayerController player;
    private CharacterSetupManager characterSetupManager;

    private EnemyBehaviour currentBehaviour;
    [Header("Enemy Behaviour")]
    [SerializeField]
    private float detectionRange;
    [SerializeField]
    private PassiveEnemyBehaviour passiveBehaviour;
    [SerializeField]
    private HostileEnemyBehaviour hostileBehaviour;

    /// <summary>
    /// Getterek és Setterek
    /// </summary>
    public int CurrentPointValue  // Aktuális pontérték
    {
        get { return _currentPointValue; }
        set { _currentPointValue = value; }
    }


    /// <summary>
    /// Események
    /// </summary>
    public event Action<float> OnPlayerCollision; // Játékossal való ütközés
    public event Action<EnemyController> OnEnemyDeath;   // Ellenség halála

    public event Action<EnemyController> OnBehaviourChange;
    public event Action<EnemyController> OnPlayerInRange;


    /// <summary>
    /// 
    /// </summary>
    protected override void Awake()
    {
        base.Awake();
        OnBehaviourChange += HandleBehaviourChange;
        OnPlayerInRange += HandleBehaviourChange;
        SetBehaviour(passiveBehaviour);
    }

    public void SetEnemyAttributesByLevel(int level)
    {
        SetCurrentEnemySpriteByLevel(level);
        SetCurrentEnemyValuesByLevel(level);
        characterSetupManager.OnSetEnemyAttributes -= SetEnemyAttributesByLevel;
    }

    void SetCurrentEnemyValuesByLevel(int level)
    {
        if (level <= 0)
        {
            Debug.LogWarning($"Invalid level: {level}. Defaulting to level 1.");
            level = 1;
        }

        MaxHealth = ScaleValue(maxHealth, maxHealthScaleFactor, level, minHealthValue, maxHealthValue);
        CurrentHealth = MaxHealth;
        CurrentMovementSpeed = ScaleValue(baseMovementSpeed, baseMovementSpeedScaleFactor, level, minMovementSpeedValue, maxMovementSpeedValue);
        CurrentDMG = ScaleValue(baseDMG, baseDMGScaleFactor, level, minDMGValue, maxDMGValue);
        CurrentAttackCooldown = ScaleValue(baseAttackCooldown, baseAttackCooldownScaleFactor, level, minAttackCooldownValue, maxAttackCooldownValue, inverse: true);
        CurrentCriticalHitChance = ScaleValue(baseCriticalHitChance, baseCriticalHitChanceScaleFactor, level, minCriticalHitChanceValue, maxCriticalHitChanceValue);
        CurrentPercentageBasedDMG = ScaleValue(basePercentageBasedDMG, basePercentageBasedDMGScaleFactor, level, minPercentageBasedDMGValue, maxPercentageBasedDMGValue);
        CurrentPointValue = (int)ScaleValue(basePointValue, pointValueScaleFactor, level, minPointValue, maxPointValue);
    }


    float ScaleValue(float baseValue, float scaleFactor, int level, float minValue, float maxValue, bool inverse = false)
    {
        float scaledValue = inverse
            ? baseValue / (float)Mathf.Pow(scaleFactor, level - 1)
            : baseValue * (float)Mathf.Pow(scaleFactor, level - 1);

        return Mathf.Clamp(scaledValue, minValue, maxValue);
    }


    /// <summary>
    /// Inicializálja az objektumpool-t és feliratkozik az eseményekre a lövedékek kezeléséhez.
    /// A metódus beállítja a szükséges objektumokat, és feliratkozik a lövedékek poolba helyezésére és visszaadására vonatkozó eseményekre.
    /// </summary>
    private void Start()
    {
        objectPool = FindObjectOfType<ObjectPoolForProjectiles>();
        player = FindObjectOfType<PlayerController>();
        characterSetupManager = FindObjectOfType<CharacterSetupManager>();

        // event subscriptions
        objectPool.OnProjectileActivated += StartProjectileDetection;
        objectPool.OnProjectileDeactivated += StopProjectileDetection;
        characterSetupManager.OnSetEnemyAttributes += SetEnemyAttributesByLevel;
    }


    /// <summary>
    /// 
    /// </summary>
    private void Update()
    {
        currentBehaviour.ExecuteBehaviour(this);

        if (player && !hasDetectedPlayer && IsPlayerInRange())
        {
            OnPlayerInRange?.Invoke(this);
        }
    }


    /// <summary>
    /// Eseménykezelõ, amely akkor hívódik meg, amikor egy másik collider 2D-es triggerrel ütközik.
    /// Ellenõrzi, hogy a trigger objektum a PlayerController-t tartalmazza, és ha igen, 
    /// meghívja az OnPlayerCollision eseményt, amely a játékos sebzését kezeli.
    /// </summary>
    /// <param name="trigger">Az ütközõ collider, amely a trigger eseményt váltja ki</param>
    void OnTriggerEnter2D(Collider2D trigger)
    {
        //Debug.Log(player);
        if (trigger.gameObject.TryGetComponent<PlayerController>(out var palyer))
        {
            Debug.Log(BaseDMG);
            OnPlayerCollision?.Invoke(-BaseDMG);
        }
    }


    /// <summary>
    /// 
    /// </summary>
    /// <param name="gameObject"></param>
    void HandleBehaviourChange(EnemyController targetGameObject)
    {
        if (this == targetGameObject)
        {
            hasDetectedPlayer = true;
            OnBehaviourChange -= HandleBehaviourChange;
            OnPlayerInRange -= HandleBehaviourChange;
            Debug.Log("DETECTED");
            SetBehaviour(hostileBehaviour);
        }
    }


    /// <summary>
    /// 
    /// </summary>
    /// <param name="newEnemyBehaviour"></param>
    public void SetBehaviour(EnemyBehaviour newEnemyBehaviour)
    {
        currentBehaviour?.StopBehaviour(this);
        currentBehaviour = newEnemyBehaviour;
        currentBehaviour?.StartBehaviour(this);

    }


    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    bool IsPlayerInRange()
    {
        float distance = Vector2.Distance(transform.position, player.transform.position);   // kifagy, ha a játékos meghal
        return distance <= detectionRange;
    }

    /// <summary>
    /// Elkezdi figyelni a lövedéket, amikor az aktívvá válik.
    /// Feliratkozik a lövedékek 'OnEnemyHit' eseményére, és hozzáadja õket az aktív lövedékek listájához.
    /// Biztosítja, hogy ugyanaz a lövedék ne kerüljön többször hozzáadásra.
    /// </summary>
    /// <param name="projectile">A figyelt lövedék, amelyet aktívvá vált.</param>
    void StartProjectileDetection(GameObject projectile)
    {
        Projectile proj = projectile.GetComponent<Projectile>();
        // Avoid adding the same projectile twice
        if (!activeProjectiles.Contains(proj))
        {
            proj.OnEnemyHit += HandleEnemyHit;
            activeProjectiles.Add(proj);

            Debug.Log($"Projectile detected: {proj}");
        }
    }


    /// <summary>
    /// Leállítja a lövedék figyelését, amikor az visszakerül az objektumpoolba.
    /// Eltávolítja a lövedéket az aktív lövedékek listájából, és leiratkozik az 'OnEnemyHit' eseményrõl.
    /// </summary>
    /// <param name="projectile">A lövedék, amelyet már nem kell figyelni.</param>
    void StopProjectileDetection(GameObject projectile)
    {
        Projectile proj = projectile.GetComponent<Projectile>();
        proj.OnEnemyHit -= HandleEnemyHit;

        activeProjectiles.Remove(proj);
        Debug.Log($"Projectile returned: {proj}");
    }

    /// <summary>
    /// Kezeli az ellenség találatát, amikor egy lövedék eltalálja.
    /// Ha a lövedék által eltalált objektum az aktuális ellenség, akkor alkalmazza a kapott sebzést.
    /// </summary>
    /// <param name="enemyHitByProjectile">Az ellenség, amelyet a lövedék eltalált.</param>
    /// <param name="damageAmount">A sebzés mértéke, amelyet a lövedék okozott.</param>
    void HandleEnemyHit(EnemyController enemyHitByProjectile, float damageAmount)
    {
        if (this == enemyHitByProjectile)
        {
            ChangeHealth(damageAmount);
            OnBehaviourChange?.Invoke(this);
        }

    }


    /// <summary>
    /// Kezeli az ellenség halálát.
    /// Meghívja az alap `Die` metódust, majd az `OnEnemyDeath` eseményt, hogy értesítse a rendszer többi részét az ellenség haláláról.
    /// </summary>
    protected override void Die()
    {
        base.Die();
        currentBehaviour.StopBehaviour(this);
        OnEnemyDeath?.Invoke(this);
        OnBehaviourChange?.Invoke(this);
    }


    /// <summary>
    /// Tisztítja a szükséges eseményeket és erõforrásokat az objektum megsemmisítésekor.
    /// Elõször meghívja az alap `OnDestroy` metódust, majd leiratkozik az eseményekrõl és törli az aktív lövedékek listáját.
    /// </summary>
    protected override void OnDestroy()
    {
        base.OnDestroy();
        if (objectPool != null)
        {
            objectPool.OnProjectileActivated -= StartProjectileDetection;
            objectPool.OnProjectileDeactivated -= StopProjectileDetection;
        }


        if (activeProjectiles != null)
        {
            foreach (var proj in activeProjectiles)
            {
                proj.OnEnemyHit -= HandleEnemyHit;
            }

            activeProjectiles.Clear();
        }
    }


    private void ValidateUniqueSpriteLevels()
    {
        HashSet<int> levelSet = new HashSet<int>();
        foreach (var data in spriteLevelDataList)
        {
            if (levelSet.Contains(data.level))
            {
                Debug.LogError($"Duplicate level {data.level} found in LevelSpriteDataList.");
            }
            else
            {
                levelSet.Add(data.level);
            }
        }
    }

    private void OnValidate()
    {
        ValidateUniqueSpriteLevels();
    }

    void SetCurrentEnemySpriteByLevel(int level)
    {
        var currentSpriteLevelData = spriteLevelDataList.FirstOrDefault(x => x.level == level);

        if (currentSpriteLevelData != null)
        {
            // If item is found, update the sprite and collider
            this.GetComponent<SpriteRenderer>().sprite = currentSpriteLevelData.sprite;
            currentSpriteLevelData.collider.enabled = true;
        }
        else
        {
            // Handle case where no matching level is found
            Debug.LogWarning($"No SpriteLevelData found for level {level}. Make sure the level exists in the data list.");
        }
    }

}