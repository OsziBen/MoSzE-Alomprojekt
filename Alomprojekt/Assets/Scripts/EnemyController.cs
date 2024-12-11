using Assets.Scripts;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.TextCore.Text;
using Assets.Scripts.EnemyBehaviours;
using static Cinemachine.DocumentationSortingAttribute;

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

    public void InitEnemyByLevel(int level)
    {
        SetCurrentSpriteByLevel(level);
        SetCurrentEnemyStatsByLevel(level);
    }

    public void SetCurrentEnemyStatsByLevel(int level)
    {
        // ellens�g maxim�lis �leterej�nek be�ll�t�sa szint alapj�n
        MaxHealth = Math.Clamp(maxHealth * (float)Math.Pow(maxHealthScaleFactor, level - 1), minHealthValue, maxHealthValue);
        CurrentHealth = maxHealth;
        // ellens�g mozg�si sebess�g�nek be�ll�t�sa szint alapj�n
        CurrentMovementSpeed = Math.Clamp(baseMovementSpeed * (float)Math.Pow(baseMovementSpeedScaleFactor, level - 1), minMovementSpeedValue, maxMovementSpeedValue);
        // ellens�g sebz�s�rt�k�nek be�ll�t�sa szint alapj�n
        CurrentDMG = Math.Clamp(baseDMG * (float)Math.Pow(baseDMGScaleFactor, level - 1), minDMGValue, maxDMGValue);
        // ellens�g sebz�s-visszat�lt�d�si idej�nek be�ll�t�sa szint alapj�n
        CurrentAttackCooldown = Math.Clamp(baseAttackCooldown / (float)Math.Pow(baseAttackCooldownScaleFactor, level - 1), minAttackCooldownValue, maxAttackCooldownValue);
        // ellens�g kritikus sebz�s es�ly�nek be�ll�t�sa szint alapj�n
        CurrentCriticalHitChance = Math.Clamp(baseCriticalHitChance * (float)Math.Pow(baseCriticalHitChanceScaleFactor, level - 1), minCriticalHitChanceValue, maxCriticalHitChanceValue);
        // ellens�g sz�zal�kos sebz�s�rt�k�nek be�l�t�sa szint alapj�n
        CurrentPercentageBasedDMG = Math.Clamp(basePercentageBasedDMG * (float)Math.Pow(basePercentageBasedDMGScaleFactor, level - 1), minPercentageBasedDMGValue, maxPercentageBasedDMGValue);

        // ellens�g pont�rt�k�nek be�ll�t�sa szint alapj�n
        CurrentPointValue = (int)Math.Clamp(basePointValue * Math.Pow(pointValueScaleFactor, level - 1), minPointValue, maxPointValue);
    }


    /// <summary>
    /// Inicializ�lja az objektumpool-t �s feliratkozik az esem�nyekre a l�ved�kek kezel�s�hez.
    /// A met�dus be�ll�tja a sz�ks�ges objektumokat, �s feliratkozik a l�ved�kek poolba helyez�s�re �s visszaad�s�ra vonatkoz� esem�nyekre.
    /// </summary>
    private void Start()
    {
        objectPool = FindObjectOfType<ObjectPoolForProjectiles>();
        player = FindObjectOfType<PlayerController>();

        // event subscriptions
        objectPool.OnProjectileActivated += StartProjectileDetection;
        objectPool.OnProjectileDeactivated += StopProjectileDetection;
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


}