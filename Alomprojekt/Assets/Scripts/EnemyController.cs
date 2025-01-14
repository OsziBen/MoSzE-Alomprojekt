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

    // TODO: IsMarkingOn implement�l�sa


    [Header("Sprites and Colliders")]
    [SerializeField]
    private List<SpriteLevelData> spriteLevelDataList;



    /// <summary>
    /// V�ltoz�k
    /// </summary>
    private List<Projectile> activeProjectiles = new List<Projectile>();    // Az akt�van megfigyelt l�ved�kek (Projectile) list�ja
    private bool hasDetectedPlayer = false;

    /// <summary>
    /// Komponenesek
    /// </summary>
    private ObjectPoolForProjectiles objectPool;  // A l�ved�kek kezel�s�re szolg�l� ObjectPool objektum
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
    /// Getterek �s Setterek
    /// </summary>
    public int CurrentPointValue  // Aktu�lis pont�rt�k
    {
        get { return _currentPointValue; }
        set { _currentPointValue = value; }
    }


    /// <summary>
    /// Esem�nyek
    /// </summary>
    public event Action<float> OnPlayerCollision; // J�t�kossal val� �tk�z�s
    public event Action<EnemyController> OnEnemyDeath;   // Ellens�g hal�la

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
    /// Inicializ�lja az objektumpool-t �s feliratkozik az esem�nyekre a l�ved�kek kezel�s�hez.
    /// A met�dus be�ll�tja a sz�ks�ges objektumokat, �s feliratkozik a l�ved�kek poolba helyez�s�re �s visszaad�s�ra vonatkoz� esem�nyekre.
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
    /// Esem�nykezel�, amely akkor h�v�dik meg, amikor egy m�sik collider 2D-es triggerrel �tk�zik.
    /// Ellen�rzi, hogy a trigger objektum a PlayerController-t tartalmazza, �s ha igen, 
    /// megh�vja az OnPlayerCollision esem�nyt, amely a j�t�kos sebz�s�t kezeli.
    /// </summary>
    /// <param name="trigger">Az �tk�z� collider, amely a trigger esem�nyt v�ltja ki</param>
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
        float distance = Vector2.Distance(transform.position, player.transform.position);   // kifagy, ha a j�t�kos meghal
        return distance <= detectionRange;
    }

    /// <summary>
    /// Elkezdi figyelni a l�ved�ket, amikor az akt�vv� v�lik.
    /// Feliratkozik a l�ved�kek 'OnEnemyHit' esem�ny�re, �s hozz�adja �ket az akt�v l�ved�kek list�j�hoz.
    /// Biztos�tja, hogy ugyanaz a l�ved�k ne ker�lj�n t�bbsz�r hozz�ad�sra.
    /// </summary>
    /// <param name="projectile">A figyelt l�ved�k, amelyet akt�vv� v�lt.</param>
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
    /// Le�ll�tja a l�ved�k figyel�s�t, amikor az visszaker�l az objektumpoolba.
    /// Elt�vol�tja a l�ved�ket az akt�v l�ved�kek list�j�b�l, �s leiratkozik az 'OnEnemyHit' esem�nyr�l.
    /// </summary>
    /// <param name="projectile">A l�ved�k, amelyet m�r nem kell figyelni.</param>
    void StopProjectileDetection(GameObject projectile)
    {
        Projectile proj = projectile.GetComponent<Projectile>();
        proj.OnEnemyHit -= HandleEnemyHit;

        activeProjectiles.Remove(proj);
        Debug.Log($"Projectile returned: {proj}");
    }

    /// <summary>
    /// Kezeli az ellens�g tal�lat�t, amikor egy l�ved�k eltal�lja.
    /// Ha a l�ved�k �ltal eltal�lt objektum az aktu�lis ellens�g, akkor alkalmazza a kapott sebz�st.
    /// </summary>
    /// <param name="enemyHitByProjectile">Az ellens�g, amelyet a l�ved�k eltal�lt.</param>
    /// <param name="damageAmount">A sebz�s m�rt�ke, amelyet a l�ved�k okozott.</param>
    void HandleEnemyHit(EnemyController enemyHitByProjectile, float damageAmount)
    {
        if (this == enemyHitByProjectile)
        {
            ChangeHealth(damageAmount);
            OnBehaviourChange?.Invoke(this);
        }

    }


    /// <summary>
    /// Kezeli az ellens�g hal�l�t.
    /// Megh�vja az alap `Die` met�dust, majd az `OnEnemyDeath` esem�nyt, hogy �rtes�tse a rendszer t�bbi r�sz�t az ellens�g hal�l�r�l.
    /// </summary>
    protected override void Die()
    {
        base.Die();
        currentBehaviour.StopBehaviour(this);
        OnEnemyDeath?.Invoke(this);
        OnBehaviourChange?.Invoke(this);
    }


    /// <summary>
    /// Tiszt�tja a sz�ks�ges esem�nyeket �s er�forr�sokat az objektum megsemmis�t�sekor.
    /// El�sz�r megh�vja az alap `OnDestroy` met�dust, majd leiratkozik az esem�nyekr�l �s t�rli az akt�v l�ved�kek list�j�t.
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