using Assets.Scripts;
using System;
using UnityEngine;
using UnityEngine.InputSystem;


public class Projectile : MonoBehaviour
{
    /// <summary>
    /// Változók
    /// </summary>
    private float _projectileDMG;           // A lövedék sebzésértéke
    public float deleteTime = 5f;
    //private float timer = 0f;
    public int force = 3;                   // Az alkalmazott erõ, amely meghatározza a lövedék indításának intenzitását
    private float _percentageDMGValue;      // Százalékos sebzésérték
    private bool _isMarked;     // lövedék megjelöltsége [igen/nem]

    private bool isSubscribedForPlayerDeath;


    /// <summary>
    /// Komponenesek
    /// </summary>
    Rigidbody2D rigidbody2d;    // Lövedékhez kapcsolódó Rigidbody2D komponens
    private ObjectPoolForProjectiles objectPool;    // A lövedékeket kezelõ ObjectPoolForProjectiles komponens

    private PlayerController player;

    private Camera _mainCamera;

    /// <summary>
    /// Getterek és Setterek
    /// </summary>
    public float ProjectileDMG  // Lövedék sebzésértéke
    {
        get { return _projectileDMG; }
        set { _projectileDMG = value; }
    }

    public float PercentageDMGValue // Lövedék százalékos sebzésértéke
    {
        get { return _percentageDMGValue; }
        set { _percentageDMGValue = value; }
    }

    public bool IsMarked    // Lövedék megjelöltsége
    {
        get { return _isMarked; }
        set { _isMarked = value; }
    }

    public Camera MainCamera
    {
        get { return _mainCamera; }
        set { _mainCamera = value; }
    }


    /// <summary>
    /// Események
    /// </summary>
    /// </summary>
    public event Action<EnemyController, float> OnEnemyHit;  // Esemény, amely akkor hívódik meg, amikor a lövedék eltalál egy ellenséges karaktert
    public event Action<float> OnBossHit;
 

    /// <summary>
    /// Inicializálja a Rigidbody2D komponenst és az objektumpool-t a fizikai interakciókhoz és lövedékek kezeléséhez.
    /// A metódus a játéknál szükséges komponenseket állítja be az objektumok mûködéséhez.
    /// </summary>
    void Awake()
    {
        rigidbody2d = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        // Megkeressük az ObjectPoolForProjectiles típusú objektumot a jelenetben, és hozzárendeljük a 'objectPool' változóhoz.
        objectPool = FindObjectOfType<ObjectPoolForProjectiles>();
        // Megkeressük a PlayerController típusú objektumot a jelenetben, és hozzárendeljük a 'player' változóhoz.
        player = FindAnyObjectByType<PlayerController>();
        // Feliratkozunk a 'player' halál eseményére, hogy a 'DestroyProjectile' metódust meghívjuk, ha a játékos meghal.
        player.OnPlayerDeath += DestroyProjectile;
        // Jelezzük, hogy feliratkoztunk a játékos halál eseményére.
        isSubscribedForPlayerDeath = true;
    }


    /// <summary>
    /// Minden frissítéskor ellenõrzi, hogy a GameObject pozíciója meghaladja-e a törléshez szükséges távolságot.
    /// Ha a távolság túl nagy, a GameObject-et törli.
    /// </summary>
    void Update()
    {
        Vector3 viewportPosition = MainCamera.WorldToViewportPoint(transform.position);

        //timer += Time.deltaTime;

        if (viewportPosition.x < -0.05 || viewportPosition.x > 1.05 || viewportPosition.y < -0.05 || viewportPosition.y > 1.05 /*|| timer >= deleteTime*/)
        {
            DestroyProjectile();
        }
    }


    private void OnEnable()
    {
        // Ellenőrizzük, hogy a 'player' objektum létezik-e, és hogy még nem történt meg a feliratkozás a halál eseményére.
        if (player && !isSubscribedForPlayerDeath)
        {
            // Feliratkozunk a 'player' halál eseményére, hogy a 'DestroyProjectile' metódust meghívjuk, ha a játékos meghal.
            player.OnPlayerDeath += DestroyProjectile;
            // Jelezzük, hogy a feliratkozás megtörtént.
            isSubscribedForPlayerDeath = true;
        }
    }

    private void OnDisable()
    {
        // Ellenőrizzük, hogy a 'player' objektum létezik-e, és hogy korábban feliratkoztunk-e a halál eseményére.
        if (player && isSubscribedForPlayerDeath)
        {
            // Leiratkozunk a 'player' halál eseményéről, hogy elkerüljük a felesleges eseményhívásokat.
            player.OnPlayerDeath -= DestroyProjectile;
            // Jelezzük, hogy a leiratkozás megtörtént.
            isSubscribedForPlayerDeath = false;
        }
    }


    /// <summary>
    /// Elindítja a GameObject-et egy adott irányba és erõvel a Rigidbody2D komponens segítségével.
    /// </summary>
    /// <param name="direction">Az irány, amelybe a GameObject-et el kell indítani</param>
    /// <param name="force">Az erõ, amellyel a GameObject-nek mozognia kell</param>
    public void Launch(Vector2 direction, float launchForce)
    {
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle - 90f);
        rigidbody2d.AddForce(direction * launchForce);
    }


    /// <summary>
    /// Eseménykezelõ, amely akkor hívódik meg, amikor a GameObject egy másik objektummal ütközik.
    /// Az ütközésrõl debug üzenetet ír ki a konzolra, majd törli a GameObject-et.
    /// </summary>
    /// <param name="collision">Az ütközõ objektum, amellyel a GameObject ütközik</param>
    void OnCollisionEnter2D(Collision2D collision)
    {
        Debug.Log("Projectile collision with " + collision.gameObject);
        DestroyProjectile();
    }


    /// <summary>
    /// Kezeli az ütközést, amikor a lövedék találkozik egy ellenséggel.
    /// Ha az ütközés egy ellenséggel történik, meghívja az eseményt, amely csökkenti az ellenség életerejét.
    /// Ezután törli a lövedéket.
    /// </summary>
    /// <param name="trigger">A collider, amely aktiválta az ütközést, valószínûleg egy ellenség.</param>
    private void OnTriggerEnter2D(Collider2D trigger)
    {
        // Ellenőrizzük, hogy a trigger objektum rendelkezik-e EnemyController komponenssel.
        if (trigger.gameObject.TryGetComponent<EnemyController>(out var enemy))
        {
            // Ha a lövedék meg van jelölve, extra sebzést oszt ki az ellenség maximális életerejének egy százalékos értéke alapján.
            // Ha nincs megjelölve, csak az alap sebzést osztja ki.
            float damage = IsMarked
                ? -ProjectileDMG - enemy.MaxHealth * PercentageDMGValue
                : -ProjectileDMG;

            Debug.Log(IsMarked ? "MARKED!" : "UNMARKED!");

            // Meghívjuk az OnEnemyHit eseményt, hogy értesítsük a rendszert az ellenség eltalálásáról.
            OnEnemyHit?.Invoke(enemy, damage);
        }

        // Ellenőrizzük, hogy a trigger objektum rendelkezik-e BossBodypartController komponenssel.
        if (trigger.gameObject.TryGetComponent<BossBodypartController>(out var bodypart))
        {
            Debug.Log($"{bodypart.name}");

            // Ha a lövedék meg van jelölve, extra sebzést oszt ki a főellenfél maximális életerejének egy százalékos értéke alapján.
            // Ha nincs megjelölve, csak az alap sebzést osztja ki.
            float damage = IsMarked
                ? -ProjectileDMG - bodypart.GetComponentInParent<BossController>().MaxHealth * PercentageDMGValue
                : -ProjectileDMG;

            // Meghívjuk az OnBossHit eseményt, hogy értesítsük a rendszert a főellenfél testrészének eltalálásáról.
            OnBossHit?.Invoke(damage);
        }

        // Ellenőrizzük, hogy a trigger objektum rendelkezik-e BossController komponenssel.
        if (trigger.gameObject.TryGetComponent<BossController>(out var boss))
        {
            Debug.Log($"{boss.name}");

            // Ha a lövedék meg van jelölve, extra sebzést oszt ki a főellenfél maximális életerejének egy százalékos értéke alapján.
            // Ha nincs megjelölve, csak az alap sebzést osztja ki.
            float damage = IsMarked
                ? -ProjectileDMG - boss.MaxHealth * PercentageDMGValue
                : -ProjectileDMG;

            // Meghívjuk az OnBossHit eseményt, hogy értesítsük a rendszert a főellenfél eltalálásáról.
            OnBossHit?.Invoke(damage);
        }

        // A lövedék megsemmisítése a találkozás után.
        DestroyProjectile();
    }


    /// <summary>
    /// Visszaadja a lövedéket az objektumpoolba, hogy újra felhasználható legyen.
    /// Ahelyett, hogy törölné az objektumot, visszaadja azt a poolba, hogy optimalizálja az erõforrások használatát.
    /// </summary>
    void DestroyProjectile()
    {
        objectPool.ReturnProjectile(gameObject);
        //timer = 0f;
    }

}
