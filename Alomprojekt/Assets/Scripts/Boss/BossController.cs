using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.Pool;
using Assets.Scripts;
using TMPro;
using UnityEngine.UIElements;
using UnityEngine.InputSystem;

public class BossController : MonoBehaviour
{
    /// <summary>
    /// Változók
    /// </summary>
    [Header("Prefab ID")]
    [SerializeField]
    protected string prefabID; // A prefab azonosítója, amely egyedi módon azonosítja a boss karaktert

    [ContextMenu("Generate guid for ID")]
    private void GenerateGuid()
    {
        prefabID = System.Guid.NewGuid().ToString(); // Új egyedi azonosítót generál a prefabID számára
    }

    [Header("Base Stats")]
    [SerializeField]
    private float _maxHealth; // A boss maximális életereje
    private float _currentHealth; // A boss aktuális életereje
    [SerializeField]
    private float _movementSpeed; // A boss mozgási sebessége
    [SerializeField]
    private float _damage; // A boss támadási sebzése

    [Header("Bodyparts")]
    [SerializeField]
    private BossBodypartController head; // A boss feje, amelyet a BossBodypartController irányít
    [SerializeField]
    private BossBodypartController leftArm; // A boss bal keze, amelyet a BossBodypartController irányít
    [SerializeField]
    private BossBodypartController rightArm; // A boss jobb keze, amelyet a BossBodypartController irányít

    private Phase currentPhase; // A boss jelenlegi fázisa, amely meghatározza a harc szakaszát

    private enum Phase
    {
        Phase1, // 75% - 100% életerő között
        Phase2, // 50% - 75% életerő között
        Phase3, // 25% - 50% életerő között
        Phase4  // 0% - 25% életerő között
    }

    private Transform player; // A játékos Transformja, amely lehetővé teszi a boss számára, hogy nyomon kövesse a játékos mozgását
    private SpriteRenderer spriteRenderer; // A boss sprite renderer-je, amely a boss megjelenéséért felelős

    [Header("Boss Behaviour Settings")]
    [SerializeField]
    private float offsetDistance;  // Meghatározza, hogy a lövedék milyen távolságról induljon el a központtól
    [SerializeField]
    private float deviationRadius; // A célpont eltolásának maximális távolsága, amely a lövedékek indításakor alkalmazott szórás mértékét határozza meg
    [SerializeField]
    private float targetUpdateInterval; // Milyen gyakran frissüljön a boss célpontja
    [SerializeField]
    private float shotTravelSpeed; // A lövedék sebessége, amely meghatározza, milyen gyorsan utoléri a célpontot
    [SerializeField]
    private float intervalStart; // A lövések közötti kezdeti időintervallum, amely befolyásolja a boss támadásainak sebességét

    private float interval; // Az intervallum, amely meghatározza, milyen gyakran történjenek a lövések, támadások

    private Rigidbody2D rb; // A Rigidbody2D komponens, amely lehetővé teszi a fizikai alapú mozgást és interakciókat a 2D-s térben
    private Vector2 movementBoundsMin; // A mozgás minimális határai, amely meghatározza a boss mozgásának alsó korlátját
    private Vector2 movementBoundsMax; // A mozgás maximális határai, amely meghatározza a boss mozgásának felső korlátját
    private Vector2 currentTarget; // Az aktuális célpont, amelyet a boss követ vagy amelyhez közelít
    private float targetUpdateTimer; // Időzítő, amely meghatározza, milyen gyakran frissüljön az aktuális célpont

    // Idő, amit várni kell (például támadások vagy fázisváltás közben)
    private float nextTime; // A következő esemény, mint például a lövés indításának ideje

    /// <summary>
    /// Komponensek
    /// </summary>
    private ObjectPoolForProjectiles objectPool; // A lövedékek objektumpoolja, amely a lövedékek újrafelhasználását kezeli
    private List<Projectile> activeProjectiles = new List<Projectile>(); // A jelenleg aktív lövedékek listája, amelyek a pályán vannak

    private BossObjectPool bossObjectPool; // A boss objektumpoolja, amely a bosshoz tartozó objektumokat (pl. testrészek) kezeli


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
    public event Action OnDeath; // Esemény, amely akkor aktiválódik, amikor a boss meghal
    public event Action<float> OnHealthChanged; // Esemény, amely akkor aktiválódik, amikor a boss életereje változik (az új életerő értéke is átadásra kerül)
    public event Action<float> OnPlayerCollision; // Esemény, amely akkor aktiválódik, amikor a boss ütközik a játékossal (a játékos pozícióját is átadhatjuk)
    public event Action OnBossDeath; // Esemény, amely akkor aktiválódik, amikor a boss halála befejeződik

    public event Action OnHealthBelow75; // Esemény, amely akkor aktiválódik, amikor a boss életereje 75% alá csökken
    public event Action OnHealthBelow50; // Esemény, amely akkor aktiválódik, amikor a boss életereje 50% alá csökken
    public event Action OnHealthBelow25; // Esemény, amely akkor aktiválódik, amikor a boss életereje 25% alá csökken

    /// <summary>
    /// Inicializálás és események előkészítése
    /// </summary>
    private void Awake()
    {
        CurrentHealth = MaxHealth; // A boss életerejét a maximális értékre állítja be a kezdeti állapotban
        currentPhase = Phase.Phase1; // Kezdetben a boss az első fázisban van (100%-75% életerő)
        interval = intervalStart; // A támadások közötti kezdő időintervallum beállítása

        // Események hozzáadása a megfelelő metódusokhoz
        OnDeath += Die; // A boss halála esetén a Die metódus fut le
        head.OnBodypartPlayerCollision += DealDamageToPlayer; // A fej ütközése a játékossal sebzést okoz
        leftArm.OnBodypartPlayerCollision += DealDamageToPlayer; // A bal kar ütközése a játékossal sebzést okoz
        rightArm.OnBodypartPlayerCollision += DealDamageToPlayer; // A jobb kar ütközése a játékossal sebzést okoz

        // Objektumpoolok és események inicializálása
        objectPool = FindObjectOfType<ObjectPoolForProjectiles>(); // Megkeresi az objektumpoolt a lövedékekhez
        objectPool.OnProjectileActivated += StartProjectileDetection; // Amikor egy lövedék aktiválódik, elindítja a lövedékek észlelését
        objectPool.OnProjectileDeactivated += StopProjectileDetection; // Amikor egy lövedék deaktiválódik, leállítja a lövedékek észlelését

        bossObjectPool = FindObjectOfType<BossObjectPool>(); // Megkeresi a boss objektumpoolt

        // A boss életerejének megfelelő események hozzáadása
        OnHealthBelow75 += HandleHealthBelow75; // Ha az életerő 75% alá csökken, a HandleHealthBelow75 metódus fut le
        OnHealthBelow50 += HandleHealthBelow50; // Ha az életerő 50% alá csökken, a HandleHealthBelow50 metódus fut le
        OnHealthBelow25 += HandleHealthBelow25; // Ha az életerő 25% alá csökken, a HandleHealthBelow25 metódus fut le
    }

    /// <summary>
    /// Inicializálás és kezdő beállítások
    /// </summary>
    private void Start()
    {
        player = FindObjectOfType<PlayerController>().transform; // A játékos pozícióját tartalmazó Transform komponens keresése
        rb = GetComponent<Rigidbody2D>(); // A Rigidbody2D komponens lekérése, amely a boss fizikai mozgásáért felel
        spriteRenderer = GameObject.Find("Background").GetComponent<SpriteRenderer>(); // A háttér SpriteRenderer komponensének lekérése

        // Kiszámoljuk a játéktér határait a háttér sprite alapján
        Vector2 spriteSize = spriteRenderer.bounds.size; // A háttér sprite méretének lekérése
        Vector2 position = spriteRenderer.transform.position; // A háttér pozíciójának lekérése
        Vector2 topLeft = new Vector2(position.x - spriteSize.x / 2, position.y + spriteSize.y / 2); // A játéktér bal felső sarka
        Vector2 bottomRight = new Vector2(position.x + spriteSize.x / 2, position.y - spriteSize.y / 2); // A játéktér jobb alsó sarka
        movementBoundsMin = new Vector2(topLeft.x + 1.5f, bottomRight.y + 1.5f); // A mozgás minimális határa
        movementBoundsMax = new Vector2(bottomRight.x - 1.5f, topLeft.y - 1.5f); // A mozgás maximális határa

        // Kezdeti célpont meghatározása
        UpdateTarget(); // A boss célpontjának frissítése

        nextTime = Time.time + interval; // A következő támadás idejének kiszámolása
    }


    /// <summary>
    /// A boss viselkedésének frissítése minden egyes frame-ben
    /// </summary>
    void Update()
    {
        if (player != null)
        {
            // Célpont frissítése időszakosan
            targetUpdateTimer -= Time.deltaTime; // Csökkentjük az időzítőt minden frame-ben
            if (targetUpdateTimer <= 0) // Ha az időzítő lejár
            {
                UpdateTarget(); // Frissítjük a boss célpontját
                targetUpdateTimer = targetUpdateInterval; // Újraindítjuk az időzítőt a beállított intervallumra
            }

            // Mozgás az aktuális célpont felé
            Vector2 newPosition = Vector2.MoveTowards(transform.position, currentTarget, MovementSpeed * Time.deltaTime); // A boss új pozíciója a célpont felé

            // Pozíció határokhoz igazítása
            newPosition = ClampPositionToBounds(newPosition); // A pozíciót a mozgás határaihoz igazítjuk
            transform.position = newPosition; // A boss tényleges pozíciója frissül
        }

        if (Time.time >= nextTime) // Ha elérkezett a következő támadási időpont
        {
            // Kiírjuk a szöveget a konzolra
            Debug.Log("Interval: " + interval); // A támadási intervallum kiírása

            Attack(); // A boss támadása

            // Beállítjuk a következő időpontot a támadáshoz
            nextTime = Time.time + interval; // A következő támadás időpontjának kiszámítása
        }
    }

    /// <summary>
    /// A boss támadásának végrehajtása
    /// </summary>
    void Attack()
    {
        if (rb == null || player == null) return; // Ha a Rigidbody2D vagy a játékos hivatkozás null, kilépünk a metódusból

        // A játékos pozíciójának lekérése (győződjünk meg róla, hogy van hivatkozás a játékosra)
        Vector2 playerPosition = player.position;

        // A támadás irányának kiszámítása a játékos pozíciója felé
        Vector2 attackDirection = (playerPosition - rb.position).normalized; // Normalizált vektor, amely a boss és a játékos közötti irányt adja meg

        // Dinamikus eltolás kiszámítása a támadás iránya alapján
        Vector2 offset = attackDirection * offsetDistance;  // Az eltolás a támadás irányában

        // Az eltolás alkalmazása a kezdő pozícióra
        Vector2 startPosition = rb.position + offset; // A kezdő pozíció a boss jelenlegi helye plusz az eltolás

        // Lövedék lekérése az objektumpoolból
        GameObject projectileObject = GetBossProjectileFromPool();
        if (projectileObject == null) return; // Ha nincs elérhető lövedék, kilépünk

        // A lövedék indítása a megfelelő pozícióból és irányba
        LaunchProjectile(projectileObject, startPosition, attackDirection);
    }

    /// <summary>
    /// A boss lövedékének lekérése az objektumpoolból
    /// </summary>
    /// <returns>Visszaadja a boss lövedékét, amelyet a poolból kapunk</returns>
    private GameObject GetBossProjectileFromPool()
    {
        // Lövedék lekérése az objektumpoolból (biztosítjuk, hogy a többi paraméter helyesen legyen átadva)
        return bossObjectPool.GetBossProjectile(rb.position, Quaternion.identity, Damage);
    }

    /// <summary>
    /// A lövedék indítása a megfelelő irányba és sebességgel
    /// </summary>
    /// <param name="projectileObject">A lövedék GameObject-je</param>
    /// <param name="startPosition">A lövedék kiinduló pozíciója</param>
    /// <param name="attackDirection">A lövedék támadási iránya</param>
    private void LaunchProjectile(GameObject projectileObject, Vector2 startPosition, Vector2 attackDirection)
    {
        // A lövedék komponensének lekérése, pozíciójának beállítása és elindítása
        EnemyProjetile projectile = projectileObject.GetComponent<EnemyProjetile>();
        projectile.transform.position = startPosition; // A lövedék új kiinduló pozíciójának beállítása
        projectile.Launch(attackDirection, shotTravelSpeed); // A lövedék elindítása a támadási irányban és sebességgel
    }

    /// <summary>
    /// A célpont frissítése, amely a játékos pozíciójából indul ki, és véletlenszerű eltolást tartalmaz
    /// </summary>
    void UpdateTarget()
    {
        // Véletlenszerű eltolás generálása a játékos körül
        Vector2 randomOffset = UnityEngine.Random.insideUnitCircle * deviationRadius; // A célpont véletlenszerű eltolása a játékos pozíciójától
        currentTarget = (Vector2)player.position + randomOffset; // Az új célpont meghatározása a játékos pozíciójából és az eltolás hozzáadásával

        // Biztosítjuk, hogy a célpont is a határokon belül legyen
        currentTarget = ClampPositionToBounds(currentTarget); // A célpontot a mozgás határaihoz igazítjuk, ha szükséges
    }

    /// <summary>
    /// A pozíció korlátozása a megadott határok között
    /// </summary>
    /// <param name="position">A pozíció, amelyet korlátozni szeretnénk</param>
    /// <returns>A korlátozott pozíció, amely biztosítja, hogy az a megadott határokon belül maradjon</returns>
    Vector2 ClampPositionToBounds(Vector2 position)
    {
        // Biztosítjuk, hogy a pozíció a megadott határokon belül maradjon
        float clampedX = Mathf.Clamp(position.x, movementBoundsMin.x, movementBoundsMax.x); // A pozíció X koordinátájának korlátozása
        float clampedY = Mathf.Clamp(position.y, movementBoundsMin.y, movementBoundsMax.y); // A pozíció Y koordinátájának korlátozása
        return new Vector2(clampedX, clampedY); // A korlátozott pozíció visszaadása
    }

    /// <summary>
    /// A boss fázisának váltása
    /// </summary>
    /// <param name="newPhase">Az új fázis, amire váltani szeretnénk</param>
    void ChangePhase(Phase newPhase)
    {
        if (currentPhase != newPhase) // Ha az új fázis eltér a jelenlegitől
        {
            currentPhase = newPhase; // Beállítjuk az új fázist
            Debug.Log("Phase changed to: " + currentPhase); // Kiírjuk a fázisváltást a konzolra
            interval -= 0.125f; // Csökkentjük az intervallumot a fázis váltásával
        }
    }


    // TODO: ezek váltják a phase-t
    /// <summary>
    /// A kezelés, amikor a boss életereje 75% alá csökken
    /// </summary>
    void HandleHealthBelow75()
    {
        Debug.Log("Health dropped below 75%!"); // Kiírjuk a konzolra, hogy az életerő 75% alá csökkent
        OnHealthBelow75 -= HandleHealthBelow75; // Leiratkozunk az eseményről, hogy ne hívd meg többször
        ChangePhase(Phase.Phase2); // Váltunk a 2. fázisra
    }

    /// <summary>
    /// A kezelés, amikor a boss életereje 50% alá csökken
    /// </summary>
    void HandleHealthBelow50()
    {
        Debug.Log("Health dropped below 50%!"); // Kiírjuk a konzolra, hogy az életerő 50% alá csökkent
        OnHealthBelow50 -= HandleHealthBelow50; // Leiratkozunk az eseményről
        ChangePhase(Phase.Phase3); // Váltunk a 3. fázisra
    }

    /// <summary>
    /// A kezelés, amikor a boss életereje 25% alá csökken
    /// </summary>
    void HandleHealthBelow25()
    {
        Debug.Log("Health dropped below 25%!"); // Kiírjuk a konzolra, hogy az életerő 25% alá csökkent
        OnHealthBelow25 -= HandleHealthBelow25; // Leiratkozunk az eseményről
        ChangePhase(Phase.Phase4); // Váltunk a 4. fázisra
    }


    /// <summary>
    /// A lövedék észlelése és hozzáadása az aktív lövedékek listájához
    /// </summary>
    /// <param name="projectile">A detektált lövedék GameObject-je</param>
    void StartProjectileDetection(GameObject projectile)
    {
        Projectile proj = projectile.GetComponent<Projectile>(); // A lövedék komponensének lekérése
                                                                 // Elkerüljük, hogy ugyanazt a lövedéket kétszer adjuk hozzá
        if (!activeProjectiles.Contains(proj))
        {
            proj.OnBossHit += HandleEnemyHit; // Feliratkozunk a lövedék eseményére, ha eléri a boss-t
            activeProjectiles.Add(proj); // Hozzáadjuk az aktív lövedékek listájához

            Debug.Log($"Projectile detected: {proj}"); // Kiírjuk, hogy a lövedék észlelve lett
        }
    }

    /// <summary>
    /// A lövedék eltávolítása az aktív lövedékek listájából, amikor visszatér a poolba
    /// </summary>
    /// <param name="projectile">A lövedék GameObject-je</param>
    void StopProjectileDetection(GameObject projectile)
    {
        Projectile proj = projectile.GetComponent<Projectile>(); // A lövedék komponensének lekérése
        proj.OnBossHit -= HandleEnemyHit; // Leiratkozunk a lövedék eseményéről

        activeProjectiles.Remove(proj); // Eltávolítjuk az aktív lövedékek listájából
        Debug.Log($"Projectile returned: {proj}"); // Kiírjuk, hogy a lövedék visszatért a poolba
    }


    /// <summary>
    /// A boss sebzésének kezelése, amikor eltalálja a lövedék
    /// </summary>
    /// <param name="damageAmount">A sebzés mennyisége</param>
    void HandleEnemyHit(float damageAmount)
    {
        ChangeHealth(damageAmount); // A boss életerejének csökkentése a lövedéktől kapott sebzéssel
    }

    /// <summary>
    /// A játékosnak okozott sebzés kezelése, amikor a boss-t eltalálja
    /// </summary>
    void DealDamageToPlayer()
    {
        OnPlayerCollision?.Invoke(-Damage); // A játékos sebzése, a damage negatív értékként (mivel a játékos kapja a sebzést)
    }


    /// <summary>
    /// A trigger zónán belépő objektumok figyelése, amikor a játékos a boss közelében van
    /// </summary>
    /// <param name="trigger">A 2D trigger zóna ütközésének kezelője</param>
    private void OnTriggerStay2D(Collider2D trigger)
    {
        // Ellenőrizzük, hogy az ütköző objektum a játékos
        if (trigger.gameObject.TryGetComponent<PlayerController>(out var player))
        {
            Debug.Log(player); // Kiírjuk a játékost a konzolra
            Debug.Log(Damage); // Kiírjuk a boss sebzését a konzolra
            DealDamageToPlayer(); // Sebzést okozunk a játékosnak
        }
    }


    private void OnValidate()
    {
        //ValidateUniqueID();
    }

    /// <summary>
    /// Ellenőrzi, hogy a prefabID mező ki van-e töltve érvényes értékkel.
    /// Ha a prefabID üres vagy null, akkor hibaüzenetet jelenít meg az Unity konzolban,
    /// figyelmeztetve a fejlesztőt, hogy generáljon vagy adjon meg egy egyedi azonosítót.
    /// </summary>
    private void ValidateUniqueID()
    {
        if (string.IsNullOrEmpty(prefabID))
        {
            Debug.LogError("Prefab ID is empty! Please generate or assign a unique ID.", this);
        }
    }

    /// <summary>
    /// A karakter életerejének módosítására szolgáló függvény. Kezeli az életerő változásával
    /// kapcsolatos eseményeket és ellenőrzi a különböző életerő küszöbértékeket.
    /// </summary>
    /// <param name="amount">Az életerő változás mértéke (pozitív: gyógyulás, negatív: sebződés)</param>
    public void ChangeHealth(float amount)
    {
        // Az életerőt a megadott értékkel módosítjuk, de a 0 és MaxHealth közötti tartományban tartjuk
        CurrentHealth = Mathf.Clamp(CurrentHealth + amount, 0, MaxHealth);

        // Értesítjük a feliratkozókat az életerő változásáról
        OnHealthChanged?.Invoke(CurrentHealth);

        // Kiszámoljuk az életerő százalékos értékét
        float healthPercentage = (CurrentHealth / MaxHealth) * 100;

        // Ellenőrizzük a különböző életerő küszöböket és meghívjuk a megfelelő eseményeket
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

        // Kiírjuk az aktuális/maximum életerőt a debug konzolra
        Debug.Log(CurrentHealth + " / " + MaxHealth);

        // Ha az életerő 0-ra csökkent, meghívjuk a halál eseményt
        if (CurrentHealth == 0f)
        {
            OnDeath?.Invoke();
        }
    }

    /// <summary>
    /// Az entitás halálát kezelő virtuális függvény. Meghívja a főellenség halálához kapcsolódó eseményt,
    /// naplózza a halál tényét, majd megsemmisíti az objektumot a játéktérben.
    /// A 'virtual' kulcsszó lehetővé teszi, hogy a leszármazott osztályok felülírják ezt a viselkedést.
    /// </summary>
    protected virtual void Die()
    {
        // Kiírjuk a debug konzolra, hogy melyik entitás halt meg
        Debug.Log("Entity " + gameObject.name + " has died");

        // Meghívjuk a főellenség halálához kapcsolódó eseményt
        OnBossDeath?.Invoke();

        // Eltávolítjuk az objektumot a játéktérből
        Destroy(gameObject);
    }

    /// <summary>
    /// Destroy esetén leiratkozás függvényekről.
    /// </summary>
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
