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
    // Pont érték beállításai
    [Header("Pont érték")]
    [SerializeField]
    private int basePointValue; // Alap pont érték
    private int _currentPointValue; // Jelenlegi pont érték

    private int minPointValue = 10; // Minimum pont érték
    private int maxPointValue = 1000; // Maximum pont érték

    // Skálázási tényezők
    [Header("Skálázási tényezők")]
    [SerializeField, Range(1f, 2f)]
    private float maxHealthScaleFactor; // Maximális életerő skálázási tényező
    [SerializeField, Range(1f, 2f)]
    private float baseMovementSpeedScaleFactor; // Alap mozgási sebesség skálázási tényező
    [SerializeField, Range(1f, 2f)]
    private float baseDMGScaleFactor; // Alap sebzés skálázási tényező
    [SerializeField, Range(1f, 2f)]
    private float baseAttackCooldownScaleFactor; // Alap támadás cooldown skálázási tényező
    [SerializeField, Range(1f, 2f)]
    private float baseCriticalHitChanceScaleFactor; // Alap kritikus találat esélyének skálázása
    [SerializeField, Range(1f, 2f)]
    private float basePercentageBasedDMGScaleFactor; // Alap százalékos alapú sebzés skálázása
    [SerializeField, Range(1f, 2f)]
    private float pointValueScaleFactor; // Pont érték skálázási tényező


    // TODO: IsMarkingOn implementálása


    [Header("Sprites and Colliders")]
    [SerializeField]
    private List<SpriteLevelData> spriteLevelDataList; // A szintenként eltérő sprite-okat tároló objektum.



    /// <summary>
    /// Változók
    /// </summary>
    private List<Projectile> activeProjectiles = new List<Projectile>();    // Az aktívan megfigyelt lövedékek (Projectile) listája
    private bool hasDetectedPlayer = false; // bool, amely tárolja, hogy az enemy érzékelt-e playert.

    /// <summary>
    /// Komponenesek
    /// </summary>
    private ObjectPoolForProjectiles objectPool;  // A lövedékek kezelésére szolgáló ObjectPool objektum
    private PlayerController player; // A játékos irányításáért felelős PlayerController
    private CharacterSetupManager characterSetupManager;  // A karakter beállításokat kezelő manager

    private EnemyBehaviour currentBehaviour; // Az aktuális ellenség viselkedését meghatározó változó
    [Header("Enemy Behaviour")]
    [SerializeField]
    private float detectionRange; // Az észlelési távolság, ami meghatározza, hogy az ellenség mikor reagál
    [SerializeField]
    private PassiveEnemyBehaviour passiveBehaviour; // Az ellenség passzív viselkedését kezelő objektum
    [SerializeField]
    private HostileEnemyBehaviour hostileBehaviour; // Az ellenség agresszív viselkedését kezelő objektum

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
    public event Action<float> OnPlayerCollision; // Játékossal való ütközés eseménye
    public event Action<EnemyController> OnEnemyDeath;   // Ellenség halála eseménye

    public event Action<EnemyController> OnBehaviourChange; // Ellenség viselkedésének változása esemény
    public event Action<EnemyController> OnPlayerInRange;  // Esemény, amely akkor aktiválódik, amikor a játékos az ellenség közelébe kerül


    /// <summary>
    /// Az ébredési fázisban végzett inicializálás
    /// </summary>
    protected override void Awake()
    {
        // Az ősosztály Awake metódusának meghívása, hogy az alapértelmezett inicializálás megtörténjen
        base.Awake(); 
        // Az OnBehaviourChange eseményhez hozzáadjuk a viselkedés kezelését végző metódust
        OnBehaviourChange += HandleBehaviourChange;
        // Az OnPlayerInRange eseményhez is hozzárendeljük ugyanazt a metódust, hogy reagáljon, ha a játékos a közelben van
        OnPlayerInRange += HandleBehaviourChange;
        // Beállítjuk az alapértelmezett passzív viselkedést az ellenség számára
        SetBehaviour(passiveBehaviour);
    }

    /// <summary>
    /// Beállítja az ellenség attribútumait a megadott szint alapján. 
    /// Ez magában foglalja az ellenség sprite-ját és a különböző értékeit, 
    /// mint a sebzés, életerő stb. Az attribútumok beállítása után 
    /// leválasztja az eseményt a karakter beállításokat kezelő menedzserről.
    /// </summary>
    /// /// <param name="level">A jelenlegi szint, amely alapján a sprite beállításra kerül.</param>
    public void SetEnemyAttributesByLevel(int level)
    {
        SetCurrentEnemySpriteByLevel(level);  // Az ellenség sprite-jának beállítása a szint alapján
        SetCurrentEnemyValuesByLevel(level);  // Az ellenség értékeinek (pl. sebzés, életerő) beállítása a szint alapján
        characterSetupManager.OnSetEnemyAttributes -= SetEnemyAttributesByLevel;  // Leválasztjuk az eseményt, miután az ellenség attribútumait beállítottuk
    }

    /// <summary>
    /// Az ellenség értékeinek (pl. életerő, mozgási sebesség, sebzés) beállítása a megadott szint alapján.
    /// A szinthez kapcsolódóan skálázza az alapértékeket, és biztosítja, hogy azok a megfelelő tartományban legyenek.
    /// Ha érvénytelen szintet kap (pl. 0 vagy negatív érték), alapértelmezett szintre (1) állítja.
    /// </summary>
    /// <param name="level">A jelenlegi szint, amely alapján az értékek skálázódnak</param>
    void SetCurrentEnemyValuesByLevel(int level)
    {
        // Ha a szint érvénytelen (0 vagy kisebb), figyelmeztetést írunk ki, és alapértelmezett szintre állítjuk
        if (level <= 0)
        {
            Debug.LogWarning($"Invalid level: {level}. Defaulting to level 1.");
            level = 1;
        }
        // Az ellenség különböző értékeinek skálázása a szint alapján
        // Maximális életerő skálázása
        MaxHealth = ScaleValue(maxHealth, maxHealthScaleFactor, level, minHealthValue, maxHealthValue);
        // Jelenlegi életerő beállítása a maximális életerővel
        CurrentHealth = MaxHealth;
        // Mozgási sebesség skálázása
        CurrentMovementSpeed = ScaleValue(baseMovementSpeed, baseMovementSpeedScaleFactor, level, minMovementSpeedValue, maxMovementSpeedValue);
        // Sebzés skálázása
        CurrentDMG = ScaleValue(baseDMG, baseDMGScaleFactor, level, minDMGValue, maxDMGValue);
        // Támadási cooldown skálázása
        CurrentAttackCooldown = ScaleValue(baseAttackCooldown, baseAttackCooldownScaleFactor, level, minAttackCooldownValue, maxAttackCooldownValue, inverse: true);
        // Kritikus találat esélyének skálázása
        CurrentCriticalHitChance = ScaleValue(baseCriticalHitChance, baseCriticalHitChanceScaleFactor, level, minCriticalHitChanceValue, maxCriticalHitChanceValue);
        // Százalékos alapú sebzés skálázása
        CurrentPercentageBasedDMG = ScaleValue(basePercentageBasedDMG, basePercentageBasedDMGScaleFactor, level, minPercentageBasedDMGValue, maxPercentageBasedDMGValue);
        // Pont értékének skálázása
        CurrentPointValue = (int)ScaleValue(basePointValue, pointValueScaleFactor, level, minPointValue, maxPointValue);
    }

    /// <summary>
    /// Skálázza az alapértéket a megadott szint és skálázási tényező alapján. 
    /// Az eredményt a megadott minimum és maximum értékek között korlátozza.
    /// Az 'inverse' paraméter segítségével fordított skálázás is végezhető (pl. a skálázott érték csökkentése helyett növelése).
    /// </summary>
    /// <param name="baseValue">Az alapérték, amelyet skálázni kell.</param>
    /// <param name="scaleFactor">A skálázási tényező, amely meghatározza, hogy a szint növekedésével hogyan változik az érték.</param>
    /// <param name="level">Az aktuális szint, amely alapján az érték skálázódik.</param>
    /// <param name="minValue">A skálázott érték minimális határa.</param>
    /// <param name="maxValue">A skálázott érték maximális határa.</param>
    /// <param name="inverse">Ha true, akkor a skálázás fordítva történik (osztás helyett szorzás).</param>
    /// <returns>A skálázott érték, amely a minimum és maximum határok között van.</returns>
    float ScaleValue(float baseValue, float scaleFactor, int level, float minValue, float maxValue, bool inverse = false)
    {
        // A skálázott érték kiszámítása, figyelembe véve az 'inverse' paramétert
        float scaledValue = inverse
            ? baseValue / (float)Mathf.Pow(scaleFactor, level - 1) // Fordított skálázás
            : baseValue * (float)Mathf.Pow(scaleFactor, level - 1); // Normál skálázás

        // Az érték korlátozása a megadott minimum és maximum határok között
        return Mathf.Clamp(scaledValue, minValue, maxValue);
    }


    /// <summary>
    /// Inicializálja az objektumpool-t és feliratkozik az eseményekre a lövedékek kezeléséhez.
    /// A metódus beállítja a szükséges objektumokat, és feliratkozik a lövedékek poolba helyezésére és visszaadására vonatkozó eseményekre.
    /// </summary>
    private void Start()
    {
        // Az objectpool, a játékos és a karakter beállító menedzser inicializálása
        objectPool = FindObjectOfType<ObjectPoolForProjectiles>();
        player = FindObjectOfType<PlayerController>();
        characterSetupManager = FindObjectOfType<CharacterSetupManager>();

        // Események feliratkozása
        objectPool.OnProjectileActivated += StartProjectileDetection; // Feliratkozás a lövedék aktiválásához kapcsolódó eseményre
        objectPool.OnProjectileDeactivated += StopProjectileDetection; // Feliratkozás a lövedék deaktiválásához kapcsolódó eseményre
        characterSetupManager.OnSetEnemyAttributes += SetEnemyAttributesByLevel; // Feliratkozás az ellenség attribútumainak beállításához
    }


    /// <summary>
    /// Az egyes frissítésekben végrehajtja az aktuális viselkedést és ellenőrzi, hogy a játékos a közelben van-e.
    /// Ha a játékos észlelésére még nem került sor, és a játékos a meghatározott távolságon belül van, 
    /// akkor aktiválja az eseményt, hogy értesítse a játékot a közelgő interakcióról.
    /// </summary>
    private void FixedUpdate()
    {
        // Az aktuális viselkedés végrehajtása
        currentBehaviour.ExecuteBehaviour(this);

        // Ha a játékos elérhető, még nem észleltük, és a játékos a közelben van
        if (player && !hasDetectedPlayer && IsPlayerInRange())
        {
            // Esemény kiváltása, hogy a játékos a közelben van
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
            // A játékos sebzésének kiváltása az esemény segítségével
            OnPlayerCollision?.Invoke(-BaseDMG);
        }
    }


    /// <summary>
    /// Eseménykezelő, amely a viselkedés változását kezeli. 
    /// Amikor az ellenség észleli a játékost, és a megfelelő viselkedés váltásra kerül, 
    /// leiratkozik az eseményekről, és a viselkedést 'hostile' (ellenséges) típusúra állítja.
    /// </summary>
    /// <param name="targetGameObject">Az ellenség, akinek a viselkedése változik</param>
    void HandleBehaviourChange(EnemyController targetGameObject)
    {
        // Ellenőrizzük, hogy a viselkedésváltozást kezdeményező objektum a jelenlegi ellenség-e
        if (this == targetGameObject)
        {
            // Ha igen, akkor a játékos észlelve lett, és az eseményeket leválasztjuk
            hasDetectedPlayer = true;
            OnBehaviourChange -= HandleBehaviourChange; // Leiratkozunk a viselkedés változásáról
            OnPlayerInRange -= HandleBehaviourChange; // Leiratkozunk a játékos közelítéséről
            Debug.Log("DETECTED");
            SetBehaviour(hostileBehaviour); // Átváltjuk az ellenség viselkedését ellenségesre
        }
    }


    /// <summary>
    /// Beállítja az új ellenség viselkedést és végrehajtja az előző viselkedés leállítását, 
    /// majd elindítja az új viselkedést. Ha már létezik aktuális viselkedés, azt leállítja.
    /// </summary>
    /// <param name="newEnemyBehaviour">Az új ellenség viselkedés, amelyet be kell állítani</param>
    public void SetBehaviour(EnemyBehaviour newEnemyBehaviour)
    {
        // Ha van aktuális viselkedés, akkor leállítjuk
        currentBehaviour?.StopBehaviour(this);
        // Beállítjuk az új viselkedést
        currentBehaviour = newEnemyBehaviour;
        // Elindítjuk az új viselkedést
        currentBehaviour?.StartBehaviour(this);

    }


    /// <summary>
    /// Ellenőrzi, hogy a játékos a meghatározott távolságon belül van-e.
    /// Kiszámítja a játékos és az aktuális objektum közötti távolságot, 
    /// és visszaadja, hogy a játékos a meghatározott észlelési távolságon belül van-e.
    /// </summary>
    /// <returns>Visszaadja, hogy a játékos a meghatározott távolságon belül van-e (true), vagy nincs (false).</returns>
    bool IsPlayerInRange()
    {
        // Kiszámítjuk az aktuális objektum és a játékos közötti távolságot
        float distance = Vector2.Distance(transform.position, player.transform.position);   // kifagy, ha a játékos meghal
        // Visszaadjuk, hogy a távolság kisebb-e vagy egyenlő a meghatározott észlelési távolsággal
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
        // A lövedék komponensének lekérése
        Projectile proj = projectile.GetComponent<Projectile>();
        // Elkerüljük, hogy ugyanaz a lövedék többször kerüljön hozzáadásra
        if (!activeProjectiles.Contains(proj))
        {
            // Feliratkozunk a lövedék 'OnEnemyHit' eseményére
            proj.OnEnemyHit += HandleEnemyHit;
            // Hozzáadjuk a lövedéket az aktív lövedékek listájához
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
        // A lövedék komponensének lekérése
        Projectile proj = projectile.GetComponent<Projectile>();
        // Leiratkozunk a lövedék 'OnEnemyHit' eseményéről
        proj.OnEnemyHit -= HandleEnemyHit;

        // Eltávolítjuk a lövedéket az aktív lövedékek listájából
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
        // Ellenőrizzük, hogy az ellenség, akit eltaláltak, megegyezik-e az aktuális ellenséggel
        if (this == enemyHitByProjectile)
        {
            // Ha igen, akkor alkalmazzuk a sebzést az aktuális ellenségen
            ChangeHealth(damageAmount);
            // Ha a viselkedés változott, meghívjuk a viselkedés-változást kezelő eseményt
            OnBehaviourChange?.Invoke(this);
        }

    }


    /// <summary>
    /// Kezeli az ellenség halálát.
    /// Meghívja az alap `Die` metódust, majd az `OnEnemyDeath` eseményt, hogy értesítse a rendszer többi részét az ellenség haláláról.
    /// </summary>
    protected override void Die()
    {
        // Meghívjuk az alap 'Die' metódust
        base.Die();
        // Leállítjuk az aktuális viselkedést
        currentBehaviour.StopBehaviour(this);
        // Meghívjuk az 'OnEnemyDeath' eseményt, hogy értesítsük a többi rendszert
        OnEnemyDeath?.Invoke(this);
        // Meghívjuk az 'OnBehaviourChange' eseményt, hogy értesítsük a viselkedésváltozásról
        OnBehaviourChange?.Invoke(this);
    }


    /// <summary>
    /// Tisztítja a szükséges eseményeket és erõforrásokat az objektum megsemmisítésekor.
    /// Elõször meghívja az alap `OnDestroy` metódust, majd leiratkozik az eseményekrõl és törli az aktív lövedékek listáját.
    /// </summary>
    protected override void OnDestroy()
    {
        // Meghívjuk az alap 'OnDestroy' metódust
        base.OnDestroy();

        // Ha létezik az objektumpool, leiratkozunk az eseményekről
        if (objectPool != null)
        {
            objectPool.OnProjectileActivated -= StartProjectileDetection;
            objectPool.OnProjectileDeactivated -= StopProjectileDetection;
        }

        // Ha az aktív lövedékek lista nem null, eltávolítjuk a lövedékek eseményeit és töröljük a listát
        if (activeProjectiles != null)
        {
            foreach (var proj in activeProjectiles)
            {
                proj.OnEnemyHit -= HandleEnemyHit;
            }

            // Kiürítjük az aktív lövedékek listáját
            activeProjectiles.Clear();
        }
    }

    /// <summary>
    /// Ellenőrzi, hogy a `spriteLevelDataList` lista tartalmazza-e a duplikált szinteket.
    /// Ha duplikált szintet talál, hibát jelez a logban.
    /// </summary>
    private void ValidateUniqueSpriteLevels()
    {
        // Létrehozunk egy hashsetet a szintek tárolására
        HashSet<int> levelSet = new HashSet<int>();
        // Végigiterálunk a sprite szint adatokat tartalmazó listán
        foreach (var data in spriteLevelDataList)
        {
            // Ha a szint már szerepel a hashsetben, duplikált szintet találtunk
            if (levelSet.Contains(data.level))
            {
                Debug.LogError($"Duplicate level {data.level} found in LevelSpriteDataList.");
            }
            else
            {
                // Ha nincs duplikált szint, hozzáadjuk a szintet a szetthez
                levelSet.Add(data.level);
            }
        }
    }

    /// <summary>
    /// A Unity Editor által hívott metódus, amely akkor fut le, amikor a komponens vagy az objektum tulajdonságait módosítják a szerkesztőben.
    /// </summary>
    private void OnValidate()
    {
        // ValidateUniqueSpriteLevels(); // tesztek ezzel nem futnak le, kikommentelve
    }

    /// <summary>
    /// Beállítja az aktuális ellenség sprite-ját és kollidálóját az adott szint alapján.
    /// A `spriteLevelDataList` listában keres egy olyan adatot, amely megfelel a megadott szintnek,
    /// majd frissíti a sprite-ot és engedélyezi a collidert.
    /// Ha nem találja a megfelelő szintet, figyelmeztetést ad.
    /// </summary>
    /// <param name="level">A szint, amely alapján a sprite és a collider beállításra kerül.</param>
    void SetCurrentEnemySpriteByLevel(int level)
    {
        // Megkeressük a megfelelő szinthez tartozó adatot a lista első elemében
        var currentSpriteLevelData = spriteLevelDataList.FirstOrDefault(x => x.level == level);

        if (currentSpriteLevelData != null)
        {
            // Ha találunk megfelelő adatot, frissítjük a sprite-ot és engedélyezzük a collidert.
            this.GetComponent<SpriteRenderer>().sprite = currentSpriteLevelData.sprite;
            currentSpriteLevelData.collider.enabled = true;
        }
        else
        {
            // Ha nem találunk megfelelő adatot, figyelmeztetést adunk
            Debug.LogWarning($"No SpriteLevelData found for level {level}. Make sure the level exists in the data list.");
        }
    }

}