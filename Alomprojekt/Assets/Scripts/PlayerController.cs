using Assets.Scripts;
using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TextCore.Text;


public class PlayerController : Assets.Scripts.Character
{
    /// <summary>
    /// V�ltoz�k
    /// </summary>
    Vector2 move;                               // A karakter mozg�sir�ny�t t�rol� v�ltoz� (2D vektor)

    public float timeInvincible = 2.0f;         // Az id�, ameddig a karakter sebezhetetlen (m�sodpercekben)
    bool isInvincible;                          // A karakter sebezhetetlens�g�nek �llapot�t t�rol� v�ltoz� (true, ha sebezhetetlen)
    float damageCooldown;                       // A sebz�s ut�ni visszat�lt�d�si id�, amely megakad�lyozza a t�l gyors �jrat�mad�st

    public float shotTravelSpeed = 300.0f;      // A l�ved�kek utaz�si sebess�ge

    bool isAbleToAttack = true;                 // A karakter t�mad�si k�pess�g�t t�rol� v�ltoz� (true, ha t�madhat)
    float remainingAttackCooldown;              // Az akt�v t�mad�si visszat�lt�d�si id� h�tral�v� ideje (m�sodpercekben)
    Vector2 attackDirection;                    // A t�mad�s ir�ny�t t�rol� v�ltoz� (2D vektor)

    EnemyController[] enemies;                  // A j�t�kban l�v� ellens�gek list�j�t t�rol� t�mb


    /// <summary>
    /// Komponenesek
    /// </summary>
    public InputAction MoveAction;            // A karakter mozg�s�t vez�rl� bemeneti akci� (p�ld�ul billenty�zet vagy kontroller mozg�s)
    public InputAction launchAction;          // A l�ved�k ind�t�s�t vez�rl� bemeneti akci� (p�ld�ul billenty� vagy gomb lenyom�s)
    public GameObject projectilePrefab;       // A l�ved�k prefab, amely a l�ved�kek l�trehoz�s�hoz haszn�lhat� (a l�ved�k modellje �s viselked�se)


    /// <summary>
    /// Getterek �s setterek
    /// </summary>


    /// <summary>
    /// Esem�nyek
    /// </summary>
    public event Action OnPlayerDeath;    // Esem�ny, amely akkor h�v�dik meg, amikor a j�t�kos meghal


    /// <summary>
    /// Inicializ�lja a bemeneti akci�kat, �sszegy�jti az ellens�geket a j�t�kban,
    /// �s feliratkozik az esem�nyekre a j�t�k megfelel� m�k�d�s�hez.
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
    /// Minden friss�t�skor v�grehajt�dik, kezeli a karakter mozg�s�t, sebezhetetlens�gi id�t, 
    /// �s t�mad�si visszat�lt�d�si id�t. A bemeneti akci�k �rt�keit friss�ti, �s cs�kkenti a visszat�lt�d�si id�ket.
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
    /// A fizikai friss�t�shez tartoz� met�dus, amely minden fix id�intervallumban v�grehajt�dik.
    /// A karakter mozg�s�t kezeli a Rigidbody2D komponens seg�ts�g�vel a meghat�rozott sebess�g �s ir�ny alapj�n.
    /// </summary>
    void FixedUpdate()
    {
        Vector2 position = (Vector2)rigidbody2d.position + move * CurrentMovementSpeed * Time.deltaTime;
        rigidbody2d.MovePosition(position);
    }


    /// <summary>
    /// A karakter �leter�-v�ltoz�s�t kezeli, figyelembe v�ve a sebezhetetlens�gi id�t.
    /// Ha a karakter �ppen sebezhetetlen, a v�ltoz�s nem t�rt�nik meg.
    /// </summary>
    /// <param name="amount">Az �leter� v�ltoz�sa; pozit�v �rt�k gy�gyul�st, negat�v �rt�k sebz�st jelent</param>
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
    /// A karakter t�mad�s�t kezeli a bemeneti akci� alapj�n, meghat�rozza a t�mad�s ir�ny�t �s elind�tja a l�ved�ket.
    /// </summary>
    /// <param name="context">A bemeneti akci� kontextusa, amely tartalmazza az aktu�lis ir�nyt �s a vez�rl�t</param>
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
            projectile.Launch(launchAction.ReadValue<Vector2>(), shotTravelSpeed);      // shottravespeed kiszervez�se!
            remainingAttackCooldown = CurrentAttackCooldown;
            //Debug.Log(remainingAttackCooldown);
            isAbleToAttack = false;
            return;
        }

    }


    /// <summary>
    /// A j�t�kos hal�l�t kezeli, �s az esem�nyt tov�bb�tja, majd t�rli a j�t�kos GameObject-j�t.
    /// </summary>
    protected override void Die()
    {
        OnPlayerDeath?.Invoke();
        Destroy(gameObject);
    }


    /// <summary>
    /// A j�t�k v�g�t kezeli, ki�rja a "GAME OVER" �zenetet �s leiratkozik az esem�nyekr�l.
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