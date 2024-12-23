using Assets.Scripts;
using log4net.Core;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TextCore.Text;
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

    List<EnemyController> enemyList;

    Vector2 movementBoundsMin;
    Vector2 movementBoundsMax;

    System.Random random = new System.Random();



    /// <summary>
    /// Komponenesek
    /// </summary>
    public InputAction MoveAction;            // A karakter mozgását vezérlõ bemeneti akció (például billentyûzet vagy kontroller mozgás)
    public InputAction launchAction;          // A lövedék indítását vezérlõ bemeneti akció (például billentyû vagy gomb lenyomás)
    private ObjectPoolForProjectiles objectPool; // A lövedékeket kezelõ ObjectPoolForProjectiles komponens

    public CharacterSetupManager characterSetupManager;
    private SpriteRenderer spriteRenderer;

    /// <summary>
    /// Getterek és setterek
    /// </summary>


    /// <summary>
    /// Események
    /// </summary>
    public event Action OnPlayerDeath;    // Esemény, amely akkor hívódik meg, amikor a játékos meghal


    protected override void Awake()
    {
        base.Awake();
        // Komponensek inicializálása
        rigidbody2d = GetComponent<Rigidbody2D>();
        characterSetupManager = FindObjectOfType<CharacterSetupManager>();
        spriteRenderer = GameObject.Find("Background").GetComponent<SpriteRenderer>();
        objectPool = FindObjectOfType<ObjectPoolForProjectiles>();
        // Mozgási határok beállítása
        Vector2 spriteSize = spriteRenderer.bounds.size;
        Vector2 position = spriteRenderer.transform.position;
        Vector2 topLeft = new Vector2(position.x - spriteSize.x / 2, position.y + spriteSize.y / 2);
        Vector2 bottomRight = new Vector2(position.x + spriteSize.x / 2, position.y - spriteSize.y / 2);
        movementBoundsMin = new Vector2(topLeft.x + 0.5f, bottomRight.y + 0.5f);
        movementBoundsMax = new Vector2(bottomRight.x - 0.5f, topLeft.y - 0.5f);

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

        // Események hozzáadása
        launchAction.performed += Attack;
    }



    public void SetPlayerAttributes(int level, List<StatValuePair> statValues, float currentHealthPercentage)
    {
        //SetCurrentSpriteByLevel(level);
        if (statValues.Count == 0)
        {
            SetDefaultPlayerAttributes(currentHealthPercentage);
        }
        else
        {
            ApplyPlayerUpgradeStats(statValues, currentHealthPercentage);
        }
        characterSetupManager.OnSetPlayerAttributes -= SetPlayerAttributes;
    }

    void SetDefaultPlayerAttributes(float currentHealthPercentage)
    {
        CurrentHealth = MaxHealth * currentHealthPercentage;
        CurrentMovementSpeed = ClampStat(baseMovementSpeed, additionalMovementSpeed, minMovementSpeedValue, maxMovementSpeedValue);
        CurrentDMG = ClampStat(baseDMG, additionalDMG, minDMGValue, maxDMGValue);
        CurrentAttackCooldown = ClampStat(baseAttackCooldown, attackCooldownReduction, minAttackCooldownValue, maxAttackCooldownValue);
        CurrentCriticalHitChance = ClampStat(baseCriticalHitChance, additionalCriticalHitChance, minCriticalHitChanceValue, maxCriticalHitChanceValue);
        CurrentPercentageBasedDMG = ClampStat(basePercentageBasedDMG, additionalPercentageBasedDMG, minPercentageBasedDMGValue, maxPercentageBasedDMGValue);
    }

    void ApplyPlayerUpgradeStats(List<StatValuePair> playerUpgradeStatValues, float currentHealthPercentage)
    {
        currentHealthPercentage = Mathf.Clamp01(currentHealthPercentage);

        foreach (var statValue in playerUpgradeStatValues)
        {
            switch (statValue.Key)
            {
                case StatType.Health:
                    additionalHealth += statValue.Value;
                    MaxHealth = ClampStat(maxHealth, additionalHealth, minHealthValue, maxHealthValue);
                    CurrentHealth = MaxHealth * currentHealthPercentage;
                    break;
                case StatType.MovementSpeed:
                    additionalMovementSpeed += statValue.Value;
                    CurrentMovementSpeed = ClampStat(baseMovementSpeed, additionalMovementSpeed, minMovementSpeedValue, maxMovementSpeedValue);
                    break;
                case StatType.Damage:
                    additionalDMG += statValue.Value;
                    CurrentDMG = ClampStat(baseDMG, additionalDMG, minDMGValue, maxDMGValue);
                    break;
                case StatType.AttackCooldownReduction:
                    attackCooldownReduction += statValue.Value;
                    CurrentAttackCooldown = ClampStat(baseAttackCooldown, attackCooldownReduction, minAttackCooldownValue, maxAttackCooldownValue);
                    break;
                case StatType.CriticalHitChance:
                    additionalCriticalHitChance += statValue.Value;
                    CurrentCriticalHitChance = ClampStat(baseCriticalHitChance, additionalCriticalHitChance, minCriticalHitChanceValue, maxCriticalHitChanceValue);
                    break;
                case StatType.PercentageBasedDMG:
                    additionalPercentageBasedDMG += statValue.Value;
                    CurrentPercentageBasedDMG = ClampStat(basePercentageBasedDMG, additionalPercentageBasedDMG, minPercentageBasedDMGValue, maxPercentageBasedDMGValue);
                    break;
                default:
                    Debug.LogWarning($"Unhandled stat type: {statValue.Key}");
                    break;
            }
        }

    }

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
        move = MoveAction.ReadValue<Vector2>();

        if (isInvincible)
        {
            damageCooldown -= Time.deltaTime;
            if (damageCooldown < 0)
            {
                isInvincible = false;
            }
        }


        if (!isAbleToAttack)
        {
            remainingAttackCooldown -= Time.deltaTime;
            //Debug.Log(remainingAttackCooldown);
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
        if (isInvincible)
        {
            return;
        }
        isInvincible = true;
        damageCooldown = timeInvincible;
        base.ChangeHealth(amount);
    }



    /// <summary>
    /// A karakter támadását kezeli a bemeneti akció alapján, meghatározza a támadás irányát és elindítja a lövedéket.
    /// A támadás irányát a bemeneti vezérlõ (nyílgombok) alapján állítja be.
    /// </summary>
    /// <param name="context">A bemeneti akció kontextusa, amely tartalmazza az aktuális irányt és a vezérlõt.</param>
    void Attack(InputAction.CallbackContext context)
    {
        if (rigidbody2d == null) return;

        switch (context.control.name)
        {
            case "upArrow":
                attackDirection = rigidbody2d.position + Vector2.up * 0.5f;
                break;
            case "downArrow":
                attackDirection = rigidbody2d.position + Vector2.down * 0.5f;
                break;
            case "leftArrow":
                attackDirection = rigidbody2d.position + Vector2.left * 0.5f;
                break;
            case "rightArrow":
                attackDirection = rigidbody2d.position + Vector2.right * 0.5f;
                break;
            default:
                break;
        }

        if (isAbleToAttack)
        {
            GameObject projectileObject = objectPool
                .GetProjectile(attackDirection, Quaternion.identity, CalculateDMG(), CurrentPercentageBasedDMG);

            Projectile projectile = projectileObject.GetComponent<Projectile>();
            projectile.Launch(launchAction.ReadValue<Vector2>(), shotTravelSpeed);      // shottravespeed kiszervezése!
            remainingAttackCooldown = CurrentAttackCooldown;
            //Debug.Log(remainingAttackCooldown);
            isAbleToAttack = false;
            return;
        }

    }


    /// <summary>
    /// A játékos halálát kezeli, és az eseményt továbbítja, majd törli a játékos GameObject-jét.
    /// </summary>
    protected override void Die()
    {
        MoveAction.Disable();
        launchAction.Disable();
        launchAction.performed -= Attack;

        OnPlayerDeath?.Invoke();
        Destroy(gameObject);
    }
}