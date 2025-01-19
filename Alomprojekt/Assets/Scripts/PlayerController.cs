using Assets.Scripts;
using log4net.Core;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TextCore.Text;
using UnityEngine.UIElements;
using static Cinemachine.DocumentationSortingAttribute;
using static PlayerUpgradeData;
using StatValuePair = System.Collections.Generic.KeyValuePair<PlayerUpgradeData.StatType, float>;

public class PlayerController : Assets.Scripts.Character
{
    /// <summary>
    /// Változók
    /// </summary>
    Vector2 move;                               // A karakter mozgásirányát tároló változó (2D vektor)

    public float timeInvincible = 2.0f;         // Az idõ, ameddig a karakter sebezhetetlen (másodpercekben)
    bool isInvincible;                          // A karakter sebezhetetlenségének állapotát tároló változó (true, ha sebezhetetlen)
    float damageCooldown;                       // A sebzés utáni visszatöltõdési idõ, amely megakadályozza a túl gyors újratámadást

    public float shotTravelSpeed = 300.0f;      // A lövedékek utazási sebessége

    bool isAbleToAttack = true;                 // A karakter támadási képességét tároló változó (true, ha támadhat)
    float remainingAttackCooldown;              // Az aktív támadási visszatöltõdési idõ hátralévõ ideje (másodpercekben)
    Vector2 attackDirection;                    // A támadás irányát tároló változó (2D vektor)

    List<EnemyController> enemyList;            // A pályán található ellenségek listája.

    Vector2 movementBoundsMin;                  // A pálya széleinek
    Vector2 movementBoundsMax;                  // megadása.

    System.Random random = new System.Random(); // System random használata.

    BossController boss;                        // Boss objektum.

    BossObjectPool bossObjectPool;
    private List<EnemyProjetile> activeProjectiles = new List<EnemyProjetile>();    // Az aktívan megfigyelt lövedékek (Projectile) listája


    /// <summary>
    /// Komponenesek
    /// </summary>
    public InputAction MoveAction;              // A karakter mozgását vezérlõ bemeneti akció (például billentyûzet vagy kontroller mozgás)
    public InputAction launchAction;            // A lövedék indítását vezérlõ bemeneti akció (például billentyû vagy gomb lenyomás)
    private ObjectPoolForProjectiles objectPool; // A lövedékeket kezelõ ObjectPoolForProjectiles komponens

    public CharacterSetupManager characterSetupManager; // A karakterobjektumok paramétereinek beállítását végző manager.
    private SpriteRenderer spriteRenderer;      // Az player objektum spriterendererje.

    /// <summary>
    /// Getterek és setterek
    /// </summary>


    /// <summary>
    /// Események
    /// </summary>
    public event Action OnPlayerDeath;    // Esemény, amely akkor hívódik meg, amikor a játékos meghal


    protected override void Awake()
    {
        // Meghívja a bázisosztály Awake() metódusát
        base.Awake();
        // Komponensek inicializálása
        //rigidbody2d = GetComponent<Rigidbody2D>();
        // A karakter beállításokat kezelő manager objektum keresése a jelenlegi jelenetben.
        characterSetupManager = FindObjectOfType<CharacterSetupManager>();
        // A player objektum SpriteRendererének lekérése
        spriteRenderer = GameObject.Find("Background").GetComponent<SpriteRenderer>();
        // Az objektumpool (projektilok újrahasznosításáért felelős pool-rendszer) referenciájának lekérése.
        objectPool = FindObjectOfType<ObjectPoolForProjectiles>();
        bossObjectPool = FindObjectOfType<BossObjectPool>();

        // Mozgási határok beállítása
        // A háttér sprite méreteinek meghatározása
        Vector2 spriteSize = spriteRenderer.bounds.size;
        // A háttér sprite pozíciójának meghatározása.
        Vector2 position = spriteRenderer.transform.position;
        // A sprite bal felső sarka (world space koordináta).
        Vector2 topLeft = new Vector2(position.x - spriteSize.x / 2, position.y + spriteSize.y / 2);
        // A sprite jobb alsó sarka (world space koordináta).
        Vector2 bottomRight = new Vector2(position.x + spriteSize.x / 2, position.y - spriteSize.y / 2);
        // A mozgási határok minimális és maximális értékeinek meghatározása.
        movementBoundsMin = new Vector2(topLeft.x + 0.5f, bottomRight.y + 0.5f);
        movementBoundsMax = new Vector2(bottomRight.x - 0.5f, topLeft.y - 0.5f);

        // Feliratkozás az OnSetPlayerAttributes eseményre.
        // Ez az esemény akkor aktiválódik, amikor a játékos attribútumait be kell állítani.
        characterSetupManager.OnSetPlayerAttributes += SetPlayerAttributes;
    }



    /// <summary>
    /// Inicializálja a bemeneti akciókat, összegyûjti az ellenségeket a játékban,
    /// és feliratkozik a szükséges eseményekre a megfelelõ mûködés biztosítása érdekében.
    /// </summary>
    void Start()
    {
        // Bemeneti akciók engedélyezése
        MoveAction.Enable();
        launchAction.Enable();

        // Ellenségek összegyűjtése és eseményekhez való csatlakozás
        enemyList = new List<EnemyController>(FindObjectsOfType<EnemyController>());
        foreach (var enemy in enemyList)
        {
            enemy.OnPlayerCollision += ChangeHealth;
            enemy.OnEnemyDeath += StopListeningToEnemy;
        }

        // Boss begyűjtése és eseményekhez való csatlakozás
        boss = FindObjectOfType<BossController>();
        if (boss != null)
        {
            boss.OnPlayerCollision += ChangeHealth;
        }

        bossObjectPool.OnProjectileActivated += StartProjectileDetection;
        bossObjectPool.OnProjectileDeactivated += StopProjectileDetection;

        // Események hozzáadása
        launchAction.performed += Attack;
    }



    public void SetPlayerAttributes(List<StatValuePair> statValues, float currentHealthPercentage)
    {
        // Ellenőrzés: Ha a statValues lista üres, akkor az alapértelmezett játékos attribútumokat állítja be.
        if (statValues.Count == 0)
        {
            SetDefaultPlayerAttributes(currentHealthPercentage);
        }
        else
        {
            // Az alapértelmezett attribútumok beállítása, függetlenül attól, hogy vannak-e upgradek.
            SetDefaultPlayerAttributes(currentHealthPercentage);
            // Upgrade-ek beállítása a playerre.
            ApplyPlayerUpgradeStats(statValues, currentHealthPercentage);
        }
        // Eltávolítja ezt a metódust az OnSetPlayerAttributes eseményből.
        // Ez biztosítja, hogy a metódus csak egyszer fusson le az esemény aktiválásakor.
        characterSetupManager.OnSetPlayerAttributes -= SetPlayerAttributes;
    }

    void SetDefaultPlayerAttributes(float currentHealthPercentage)
    {
        // Jelenlegi HP beállítása.
        CurrentHealth = MaxHealth * currentHealthPercentage;
        // A mozgási sebesség beállítása.
        CurrentMovementSpeed = ClampStat(baseMovementSpeed, additionalMovementSpeed, minMovementSpeedValue, maxMovementSpeedValue);
        // A sebzés értékének beállítása.
        CurrentDMG = ClampStat(baseDMG, additionalDMG, minDMGValue, maxDMGValue);
        // A támadási késleltetés (cooldown) beállítása.
        CurrentAttackCooldown = ClampStat(baseAttackCooldown, attackCooldownReduction, minAttackCooldownValue, maxAttackCooldownValue);
        // A kritikus találati esély beállítása.
        CurrentCriticalHitChance = ClampStat(baseCriticalHitChance, additionalCriticalHitChance, minCriticalHitChanceValue, maxCriticalHitChanceValue);
        // A százalékos alapú sebzés beállítása.
        CurrentPercentageBasedDMG = ClampStat(basePercentageBasedDMG, additionalPercentageBasedDMG, minPercentageBasedDMGValue, maxPercentageBasedDMGValue);
    }

    /// <summary>
    /// Alkalmazza a paraméterben megadott upgrade-eket a playerre.
    /// </summary>
    /// <param name="playerUpgradeStatValues">Lista, melyben az alkalmazandó upgrade-ek vannak.</param>
    /// <param name="currentHealthPercentage">A jelenlegi HP százalék.</param>
    void ApplyPlayerUpgradeStats(List<StatValuePair> playerUpgradeStatValues, float currentHealthPercentage)
    {
        // Biztosítja, hogy a currentHealthPercentage értéke 0 és 1 közé legyen korlátozva.
        currentHealthPercentage = Mathf.Clamp01(currentHealthPercentage);

        // Végigiterál minden egyes staton az upgrade listában.
        foreach (var statValue in playerUpgradeStatValues)
        {
            // Ellenőrzés az adott upgrade típus alapján.
            switch (statValue.Key)
            {
                case StatType.Health:
                    Debug.Log(statValue.Key + " :: " + statValue.Value);
                    // Növeli az additionalHealth értékét az adott frissítés alapján.
                    additionalHealth += statValue.Value;
                    // Az új maximális életerő értékének kiszámítása.
                    MaxHealth = ClampStat(maxHealth, additionalHealth, minHealthValue, maxHealthValue);
                    // Az aktuális életerő frissítése a százalékos egészség alapján.
                    CurrentHealth = MaxHealth * currentHealthPercentage;
                    break;
                case StatType.MovementSpeed:
                    Debug.Log(statValue.Key + " :: " + statValue.Value);
                    additionalMovementSpeed += statValue.Value;
                    // A mozgási sebesség frissítése az új bónusz értékkel.
                    CurrentMovementSpeed = ClampStat(baseMovementSpeed, additionalMovementSpeed, minMovementSpeedValue, maxMovementSpeedValue);
                    break;
                case StatType.Damage:
                    Debug.Log(statValue.Key + " :: " + statValue.Value);
                    additionalDMG += statValue.Value;
                    // A sebzés frissítése az új értékekkel.
                    CurrentDMG = ClampStat(baseDMG, additionalDMG, minDMGValue, maxDMGValue);
                    break;
                case StatType.AttackCooldownReduction:
                    Debug.Log(statValue.Key + " :: " + statValue.Value);
                    attackCooldownReduction += statValue.Value;
                    // A támadás delay frissítése.
                    CurrentAttackCooldown = ClampStat(baseAttackCooldown, attackCooldownReduction, minAttackCooldownValue, maxAttackCooldownValue);
                    break;
                case StatType.CriticalHitChance:
                    Debug.Log(statValue.Key + " :: " + statValue.Value);
                    additionalCriticalHitChance += statValue.Value;
                    // A kritikus találati esély frissítése.
                    CurrentCriticalHitChance = ClampStat(baseCriticalHitChance, additionalCriticalHitChance, minCriticalHitChanceValue, maxCriticalHitChanceValue);
                    break;
                case StatType.PercentageBasedDMG:
                    Debug.Log(statValue.Key + " :: " + statValue.Value);
                    additionalPercentageBasedDMG += statValue.Value;
                    // A százalékos sebzés frissítése.
                    CurrentPercentageBasedDMG = ClampStat(basePercentageBasedDMG, additionalPercentageBasedDMG, minPercentageBasedDMGValue, maxPercentageBasedDMGValue);
                    break;
                default:
                    // Ha egy nem kezelt stat típus érkezik, figyelmeztetést küld.
                    Debug.LogWarning($"Unhandled stat type: {statValue.Key}");
                    break;
            }
        }

    }

    /// <summary>
    /// Statok beállítására használt függvény.
    /// Biztosítja, hogy nem a statok nem lépnek túl megadott értékeken.
    /// </summary>
    /// <param name="baseValue">Az alap érték.</param>
    /// <param name="additionalValue">Az alap értékhez hozzáadott upgrade érték.</param>
    /// <param name="minValue">A stat minimum értéke.</param>
    /// <param name="maxValue">A stat maximum értéke.</param>
    /// <returns></returns>
    private float ClampStat(float baseValue, float additionalValue, float minValue, float maxValue)
    {
        return Mathf.Clamp(baseValue + additionalValue, minValue, maxValue);
    }


    /// <summary>
    /// Leiratkozik az adott ellenséghez tartozó eseményekrõl.
    /// Ezáltal megszünteti az ellenséggel való interakciót, például a játékos egészségének módosítását
    /// és az ellenség halálára vonatkozó események kezelést.
    /// </summary>
    /// <param name="enemy">Az ellenség, akivel a hallgatókat eltávolítjuk.</param>
    void StopListeningToEnemy(EnemyController enemy)
    {
        if (enemy.TryGetComponent<EnemyController>(out var deadEnemy))
        {
            deadEnemy.OnPlayerCollision -= ChangeHealth;
            deadEnemy.OnEnemyDeath -= StopListeningToEnemy;
        }
    }


    /// <summary>
    /// Visszaadja a jelenlegi sebzés értékét.
    /// A metódus kiírja a konzolra a jelenlegi sebzést, majd visszaadja azt.
    /// Ha a lövés kritikus találat, akkor a sebzés értéke megduplázódik.
    /// </summary>
    /// <returns>Jelenlegi sebzés (CurrentDMG) értéke, esetleg megduplázva kritikus találat esetén.</returns>
    float CalculateDMG()
    {
        Debug.Log("CURENTDMG: " + CurrentDMG);
        return IsCriticalHit() ? CurrentDMG * 2 : CurrentDMG;
    }


    /// <summary>
    /// Ellenõrzi, hogy a lövés kritikus találatot eredményezett-e.
    /// A kritikus találat esélye a 'CurrentCriticalHitChance' értéktõl függ.
    /// </summary>
    /// <returns>True, ha a lövés kritikus találat, false egyébként.</returns>
    bool IsCriticalHit()
    {
        int chance = random.Next(1, 101);
        if (chance <= CurrentCriticalHitChance * 100)
        {
            Debug.Log("*CRITICAL HIT!*");
            return true;
        }

        return false;
    }


    /// <summary>
    /// Minden frissítéskor végrehajtódik, kezeli a karakter mozgását, sebezhetetlenségi idõt, 
    /// és támadási visszatöltõdési idõt. A bemeneti akciók értékeit frissíti, és csökkenti a visszatöltõdési idõket.
    /// </summary>
    void Update()
    {
        // A mozgási bemenet lekérése.
        move = MoveAction.ReadValue<Vector2>();
        // Ha van érvényes mozgási bemenet.
        if (move != Vector2.zero)
        {
            // A játékos forgatása a mozgás irányának megfelelően.
            // Az Atan2 kiszámítja az irány szögét radiánban, amit aztán fokokra konvertálunk.
            // Az "-90f" érték azért van, hogy a sprite megfelelően igazodjon az irányhoz.
            float angle = Mathf.Atan2(move.y, move.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle -90f);
        }
        else
        {
            // Ha nincs mozgási bemenet, a játékos visszatér az alaphelyzetbe.
            transform.rotation = Quaternion.Euler(0f, 0f, 0f);
        }

        // Ha a játékos sebezhetetlen, csökkenti a hátralévő időt.
        if (isInvincible)
        {
            damageCooldown -= Time.deltaTime;
            // Ha a cooldown idő lejárt, a sebezhetetlenség véget ér.
            if (damageCooldown < 0)
            {
                isInvincible = false;
            }
        }

        // Ha a játékos nem képes támadni, csökkenti a támadási cooldown időt.
        if (!isAbleToAttack)
        {
            remainingAttackCooldown -= Time.deltaTime;
            //Debug.Log(remainingAttackCooldown);

            // Ha a cooldown idő lejárt, a játékos ismét támadhat.
            if (remainingAttackCooldown <= 0)
            {
                isAbleToAttack = true;
            }
        }

    }


    /// <summary>
    /// A fizikai frissítéshez tartozó metódus, amely minden fix idõintervallumban végrehajtódik.
    /// A karakter mozgását kezeli a Rigidbody2D komponens segítségével a meghatározott sebesség és irány alapján.
    /// </summary>
    void FixedUpdate()
    {
        Vector2 position = (Vector2)rigidbody2d.position + move * CurrentMovementSpeed * Time.deltaTime;

        // Ellenõrizd, hogy az új pozíció kívül van-e a tartományon
        if (position.x < movementBoundsMin.x || position.x > movementBoundsMax.x)
        {
            // Ha az X komponens kívül van a határokon, állítsd vissza az eredeti értékre
            position.x = Mathf.Clamp(position.x, movementBoundsMin.x, movementBoundsMax.x);
        }

        if (position.y < movementBoundsMin.y || position.y > movementBoundsMax.y)
        {
            // Ha az Y komponens kívül van a határokon, állítsd vissza az eredeti értékre
            position.y = Mathf.Clamp(position.y, movementBoundsMin.y, movementBoundsMax.y);
        }

        rigidbody2d.MovePosition(position);
    }



    /// <summary>
    /// A karakter életerõ-változását kezeli, figyelembe véve a sebezhetetlenségi idõt.
    /// Ha a karakter éppen sebezhetetlen, a változás nem történik meg.
    /// </summary>
    /// <param name="amount">Az életerõ változása; pozitív érték gyógyulást, negatív érték sebzést jelent</param>
    public override void ChangeHealth(float amount)
    {
        // Ellenőrzi, hogy a karakter sebezhetetlen-e.
        // Ha igen, az életerő nem változik, és a metódus visszatér.
        if (isInvincible)
        {
            return;
        }
        // Aktiválja a sebezhetetlenségi állapotot, így a karakter egy ideig nem sérülhet.
        isInvincible = true;
        // Beállítja a sebezhetetlenségi időszak hosszát.
        damageCooldown = timeInvincible;
        // Meghívja az ősosztály ChangeHealth metódusát, hogy az életerő ténylegesen frissüljön.
        base.ChangeHealth(amount);
    }



    /// <summary>
    /// Kezeli a játékos támadási akcióját, figyelembe véve a támadási irányt és a támadási cooldown-t.
    /// </summary>
    /// <param name="context">Kezeli a játékos támadási akcióját, figyelembe véve a támadási irányt és a támadási cooldown-t.</param>
    void Attack(InputAction.CallbackContext context)
    {
        // Ellenőrzi, hogy a rigidbody vagy a támadás le van-e tiltva. Ha igen, azonnal visszatér.
        if (rigidbody2d == null || !isAbleToAttack) return;
        // A támadás helyi eltolásának meghatározása a bemenet alapján.
        Vector2 localOffset = GetLocalOffset(context.control.name);
        // Ha a bemenet érvénytelen (nulla eltolás), a metódus visszatér.
        if (localOffset == Vector2.zero) return;

        // Kiszámítja a támadási irányt egy szorzó segítségével, amely az eltolást méretezi.
        float multiplier = GetMultiplier(localOffset);
        attackDirection = rigidbody2d.position + localOffset * multiplier;

        // Projectile objektum lekérése az objectpoolból.
        GameObject projectileObject = GetProjectileFromPool();
        // Ha nincs elérhető projektil, a metódus visszatér.
        if (projectileObject == null) return;

        // A projectile elindítása a megadott irányba.
        LaunchProjectile(projectileObject);
        // A támadási cooldown visszaállítása.
        ResetAttackCooldown();
        // A támadási képesség letiltása a cooldown idejére.
        isAbleToAttack = false;
    }


    /// <summary>
    /// Meghatározza a támadás irányát a megadott bemeneti vezérlő neve alapján.
    /// </summary>
    /// <param name="controlName">A bemeneti vezérlő neve, például "upArrow", "downArrow", "leftArrow", vagy "rightArrow".</param>
    /// <returns>
    /// A megfelelő irányvektor (Vector2) a vezérlőnek megfelelően.
    /// Ha a bemenet érvénytelen, Vector2.zero értéket ad vissza.
    /// </returns>
    private Vector2 GetLocalOffset(string controlName)
    {
        // Ellenőrzi a vezérlő nevét, és visszaadja a hozzá tartozó irányvektort.
        switch (controlName)
        {
            case "upArrow":
                return Vector2.up;
            case "downArrow":
                return Vector2.down;
            case "leftArrow":
                return Vector2.left;
            case "rightArrow":
                return Vector2.right;
            default:
                return Vector2.zero; // Érvénytelen bemenet esetén nulla vektor
        }
    }


    /// <summary>
    /// Meghatározza a támadás irányától függő szorzót, amely figyelembe veszi a karakter irányát és a támadás helyét.
    /// </summary>
    /// <param name="localOffset">A támadás irányát reprezentáló vektor (pl. Vector2.up, Vector2.left, stb.).</param>
    /// <returns>A támadás irányától függő szorzó, amely befolyásolja a támadás hatótávolságát vagy intenzitását.</returns>
    private float GetMultiplier(Vector2 localOffset)
    {
        // A karakter pillanatnyi iránya, amit a "transform.up" ad vissza
        Vector2 facingDirection = transform.up;

        // Kezeli a diagonális irányokat (pl. balra-fel vagy jobbra-le)
        if (Mathf.Abs(facingDirection.x) > 0.1f && Mathf.Abs(facingDirection.y) > 0.1f)
        {
            // Ha a karakter diagonálisan néz, akkor az iránytól függően kombinált szorzót ad vissza
            return (2f + 1f) / 2f; // A diagonális irányokhoz egy átlagos szorzót (1.5f) ad vissza
        }

        // Kezeli a vízszintes és függőleges irányokat
        bool isHorizontal = Mathf.Abs(facingDirection.x) > Mathf.Abs(facingDirection.y);

        // Ha a karakter vízszintes irányba néz
        if (isHorizontal)
        {
            // A balra vagy jobbra irányban végzett támadásokhoz nagyobb szorzót (2f) ad, míg a függőleges irányú támadásokhoz 1f-et
            return (localOffset == Vector2.left || localOffset == Vector2.right) ? 2f : 1f; // 2f for horizontal, 1f for vertical
        }
        else
        {
            // Ha a karakter függőleges irányba néz
            // A fel és le irányú támadásokhoz nagyobb szorzót (2f) ad, míg a vízszintes irányú támadásokhoz 1f-et
            return (localOffset == Vector2.up || localOffset == Vector2.down) ? 2f : 1f; // 2f for vertical, 1f for horizontal
        }
    }


    /// <summary>
    /// A projectileokat szerzi meg az object poolból, figyelembe véve a támadás irányát és a karakter statisztikáit.
    /// </summary>
    /// <returns>A megfelelő projectile objektumot adja vissza, vagy null értéket, ha nem található elérhető projectile.</returns>
    private GameObject GetProjectileFromPool()
    {
        // Kéri az objektumpoolból a következő projectile-t, amely megfelel a támadás irányának
        return objectPool.GetProjectile(attackDirection, Quaternion.identity, CalculateDMG(), CurrentPercentageBasedDMG);
    }


    /// <summary>
    /// A projectile indítását kezeli. A projektile az objektumból kiolvassa a szükséges információkat és elindul.
    /// </summary>
    /// <param name="projectileObject">Az elindítandó projectile objektum.</param>
    private void LaunchProjectile(GameObject projectileObject)
    {
        // Kinyerjük a projectile komponensét az objektumból
        Projectile projectile = projectileObject.GetComponent<Projectile>();
        // Kiolvassuk az indítás irányát a launchAction inputból
        Vector2 launchDirection = launchAction.ReadValue<Vector2>();
        // A projectile indítása a megadott irányba és sebességgel
        projectile.Launch(launchDirection, shotTravelSpeed);
    }


    /// <summary>
    /// A támadás cooldownját alaphelyzetbe állítja, hogy a következő támadás előtt elteljen a megfelelő idő.
    /// </summary>
    private void ResetAttackCooldown()
    {
        // A maradék cooldown beállítása a karakter aktuális támadási cooldown-jára
        remainingAttackCooldown = CurrentAttackCooldown;
    }

    void StartProjectileDetection(GameObject projectile)
    {
        EnemyProjetile proj = projectile.GetComponent<EnemyProjetile>();
        // Avoid adding the same projectile twice
        if (!activeProjectiles.Contains(proj))
        {
            proj.OnPlayerHit += HandlePlayerHit;
            activeProjectiles.Add(proj);

            Debug.Log($"Projectile detected: {proj}");
        }
    }


    /// <summary>
    /// Leállítja a lövedék figyelését, amikor az visszakerül az objektumpoolba.
    /// Eltávolítja a lövedéket az aktív lövedékek listájából, és leiratkozik az 'OnEnemyHit' eseményről.
    /// </summary>
    /// <param name="projectile">A lövedék, amelyet már nem kell figyelni.</param>
    void StopProjectileDetection(GameObject projectile)
    {
        EnemyProjetile proj = projectile.GetComponent<EnemyProjetile>();
        proj.OnPlayerHit -= HandlePlayerHit;

        activeProjectiles.Remove(proj);
        Debug.Log($"Projectile returned: {proj}");
    }

    void HandlePlayerHit(float damageAmount)
    {
        ChangeHealth(damageAmount);
    }


    /// <summary>
    /// A játékos halálát kezeli, és az eseményt továbbítja, majd törli a játékos GameObject-jét.
    /// </summary>
    protected override void Die()
    {
        // Letiltja a mozgási akciót, hogy a játékos ne tudjon mozogni a halál után
        MoveAction.Disable();
        // Letiltja a támadási akciót, hogy a játékos ne tudjon támadni a halál után
        launchAction.Disable();
        // Eltávolítja a támadás eseménykezelőjét, hogy ne reagáljon többé a támadás akciókra
        launchAction.performed -= Attack;

        // Ha a játékos egy boss ellenséggel találkozott, akkor leiratkozik a boss eseménykezelőjéről
        if (boss != null)
        {
            boss.OnPlayerCollision -= ChangeHealth;
        }

        bossObjectPool.OnProjectileActivated -= StartProjectileDetection;
        bossObjectPool.OnProjectileDeactivated -= StopProjectileDetection;
        
        // A halál eseményt továbbítja a rendszer felé
        OnPlayerDeath?.Invoke();
        // A játékos GameObject-jét törli, így a karakter eltűnik a játékból
        Destroy(gameObject);
    }
}