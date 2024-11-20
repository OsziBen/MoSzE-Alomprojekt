using Assets.Scripts;
using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TextCore.Text;


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

    EnemyController[] enemies;                  // A játékban lévõ ellenségek listáját tároló tömb


    /// <summary>
    /// Komponenesek
    /// </summary>
    public InputAction MoveAction;            // A karakter mozgását vezérlõ bemeneti akció (például billentyûzet vagy kontroller mozgás)
    public InputAction launchAction;          // A lövedék indítását vezérlõ bemeneti akció (például billentyû vagy gomb lenyomás)
    public GameObject projectilePrefab;       // A lövedék prefab, amely a lövedékek létrehozásához használható (a lövedék modellje és viselkedése)


    /// <summary>
    /// Getterek és setterek
    /// </summary>


    /// <summary>
    /// Események
    /// </summary>
    public event Action OnPlayerDeath;    // Esemény, amely akkor hívódik meg, amikor a játékos meghal


    /// <summary>
    /// Inicializálja a bemeneti akciókat, összegyûjti az ellenségeket a játékban,
    /// és feliratkozik az eseményekre a játék megfelelõ mûködéséhez.
    /// </summary>
    void Start()
    {
        MoveAction.Enable();
        launchAction.Enable();

        enemies = FindObjectsOfType<EnemyController>();

        foreach (var enemy in enemies)
        {
            enemy.OnPlayerCollision += ChangeHealth;
        }

        launchAction.performed += Attack;
        OnPlayerDeath += GameOver;
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
    /// </summary>
    /// <param name="context">A bemeneti akció kontextusa, amely tartalmazza az aktuális irányt és a vezérlõt</param>
    void Attack(InputAction.CallbackContext context)
    {
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
            GameObject projectileObject = Instantiate(projectilePrefab, attackDirection, Quaternion.identity);
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
        OnPlayerDeath?.Invoke();
        Destroy(gameObject);
    }


    /// <summary>
    /// A játék végét kezeli, kiírja a "GAME OVER" üzenetet és leiratkozik az eseményekrõl.
    /// </summary>
    void GameOver()
    {
        Debug.Log("GAME OVER");
        OnPlayerDeath -= GameOver;

        foreach (var enemy in enemies)
        {
            enemy.OnPlayerCollision -= ChangeHealth;
        }
    }

}