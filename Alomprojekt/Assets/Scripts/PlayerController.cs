using Assets.Scripts;
using System;
using System.Collections.Generic;
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

    Vector2 movementBoundsMin;
    Vector2 movementBoundsMax;
    List<EnemyController> enemyList;


    /// <summary>
    /// Komponenesek
    /// </summary>
    public InputAction MoveAction;            // A karakter mozg�s�t vez�rl� bemeneti akci� (p�ld�ul billenty�zet vagy kontroller mozg�s)
    public InputAction launchAction;          // A l�ved�k ind�t�s�t vez�rl� bemeneti akci� (p�ld�ul billenty� vagy gomb lenyom�s)
    public ObjectPoolForProjectiles objectPool; // A l�ved�keket kezel� ObjectPoolForProjectiles komponens

    public SpriteRenderer sr;

    /// <summary>
    /// Getterek �s setterek
    /// </summary>


    /// <summary>
    /// Esem�nyek
    /// </summary>
    public event Action OnPlayerDeath;    // Esem�ny, amely akkor h�v�dik meg, amikor a j�t�kos meghal


    /// <summary>
    /// Inicializ�lja a bemeneti akci�kat, �sszegy�jti az ellens�geket a j�t�kban,
    /// �s feliratkozik a sz�ks�ges esem�nyekre a megfelel� m�k�d�s biztos�t�sa �rdek�ben.
    /// </summary>
    void Start()
    {
        MoveAction.Enable();

        sr = GameObject.Find("Background").GetComponent<SpriteRenderer>();

        // Sprite m�ret�nek kisz�m�t�sa vil�gkoordin�t�kban
        Vector2 spriteSize = sr.bounds.size; // A sprite sz�less�ge �s magass�ga vil�gkoordin�t�ban
        Vector2 position = sr.transform.position; // A sprite k�z�ppontj�nak poz�ci�ja vil�gkoordin�t�ban

        // Bal fels� sarok koordin�t�ja
        Vector2 topLeft = new Vector2(position.x - spriteSize.x / 2, position.y + spriteSize.y / 2);

        // Jobb als� sarok koordin�t�ja
        Vector2 bottomRight = new Vector2(position.x + spriteSize.x / 2, position.y - spriteSize.y / 2);

        //Debug.Log($"Bal fels� sarok: {topLeft}");
        //Debug.Log($"Jobb als� sarok: {bottomRight}");


        movementBoundsMin = new Vector2(topLeft.x + 0.5f, bottomRight.y + 0.5f);
        movementBoundsMax = new Vector2(bottomRight.x - 0.5f, topLeft.y - 0.5f);

        objectPool = FindObjectOfType<ObjectPoolForProjectiles>();
        enemyList = new List<EnemyController>(FindObjectsOfType<EnemyController>());

        foreach (var enemy in enemyList)
        {
            enemy.OnPlayerCollision += ChangeHealth;
            enemy.OnEnemyDeath += StopListeningToEnemy;
        }

        launchAction.Enable();
        launchAction.performed += Attack;
        OnPlayerDeath += GameOver;
    }


    /// <summary>
    /// Leiratkozik az adott ellens�ghez tartoz� esem�nyekr�l.
    /// Ez�ltal megsz�nteti az ellens�ggel val� interakci�t, p�ld�ul a j�t�kos eg�szs�g�nek m�dos�t�s�t
    /// �s az ellens�g hal�l�ra vonatkoz� esem�nyek kezel�st.
    /// </summary>
    /// <param name="enemy">Az ellens�g, akivel a hallgat�kat elt�vol�tjuk.</param>
    void StopListeningToEnemy(GameObject enemy)
    {
        if (enemy.TryGetComponent<EnemyController>(out var deadEnemy))
        {
            deadEnemy.OnPlayerCollision -= ChangeHealth;
            deadEnemy.OnEnemyDeath -= StopListeningToEnemy;
        }
    }


    /// <summary>
    /// Visszaadja a jelenlegi sebz�s �rt�k�t.
    /// A met�dus ki�rja a konzolra a jelenlegi sebz�st, majd visszaadja azt.
    /// Ha a l�v�s kritikus tal�lat, akkor a sebz�s �rt�ke megdupl�z�dik.
    /// </summary>
    /// <returns>Jelenlegi sebz�s (CurrentDMG) �rt�ke, esetleg megdupl�zva kritikus tal�lat eset�n.</returns>
    float CalculateDMG()
    {
        Debug.Log("CURENTDMG: " + CurrentDMG);
        return IsCriticalHit() ? CurrentDMG * 2 : CurrentDMG;
    }


    /// <summary>
    /// Ellen�rzi, hogy a l�v�s kritikus tal�latot eredm�nyezett-e.
    /// A kritikus tal�lat es�lye a 'CurrentCriticalHitChance' �rt�kt�l f�gg.
    /// </summary>
    /// <returns>True, ha a l�v�s kritikus tal�lat, false egy�bk�nt.</returns>
    bool IsCriticalHit()
    {
        System.Random random = new System.Random();
        int chance = random.Next(1, 101);
        if (chance <= CurrentCriticalHitChance * 100)
        {
            Debug.Log("*CRITICAL HIT!*");
            return true;
        }

        return false;
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

        // Ellen�rizd, hogy az �j poz�ci� k�v�l van-e a tartom�nyon
        if (position.x < movementBoundsMin.x || position.x > movementBoundsMax.x)
        {
            // Ha az X komponens k�v�l van a hat�rokon, �ll�tsd vissza az eredeti �rt�kre
            position.x = Mathf.Clamp(position.x, movementBoundsMin.x, movementBoundsMax.x);
        }

        if (position.y < movementBoundsMin.y || position.y > movementBoundsMax.y)
        {
            // Ha az Y komponens k�v�l van a hat�rokon, �ll�tsd vissza az eredeti �rt�kre
            position.y = Mathf.Clamp(position.y, movementBoundsMin.y, movementBoundsMax.y);
        }

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
    /// A t�mad�s ir�ny�t a bemeneti vez�rl� (ny�lgombok) alapj�n �ll�tja be.
    /// </summary>
    /// <param name="context">A bemeneti akci� kontextusa, amely tartalmazza az aktu�lis ir�nyt �s a vez�rl�t.</param>
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
            GameObject projectileObject = objectPool
                .GetProjectile(attackDirection, Quaternion.identity, CalculateDMG(), CurrentPercentageBasedDMG);

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
        launchAction.performed -= Attack;
        OnPlayerDeath -= GameOver;
    }


}