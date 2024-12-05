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
    //public float deleteDistance = 25.0f;    // A lövedék maximálisan megtehetõ távolsága, amely után törlésre kerül
    public float deleteTime = 5f;
    private float timer = 0f;
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


    /// <summary>
    /// Események
    /// </summary>
    /// </summary>
    public event Action<GameObject, float> OnEnemyHit;  // Esemény, amely akkor hívódik meg, amikor a lövedék eltalál egy ellenséges karaktert


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
        objectPool = FindObjectOfType<ObjectPoolForProjectiles>();
        player = FindAnyObjectByType<PlayerController>();
        player.OnPlayerDeath += DestroyProjectile;
        Debug.Log("UP");
        isSubscribedForPlayerDeath = true;
    }


    /// <summary>
    /// Minden frissítéskor ellenõrzi, hogy a GameObject pozíciója meghaladja-e a törléshez szükséges távolságot.
    /// Ha a távolság túl nagy, a GameObject-et törli.
    /// </summary>
    void Update()
    {
        timer += Time.deltaTime;

        if (timer >= deleteTime)
        {
            DestroyProjectile();
        }
    }

    private void OnEnable()
    {
        if (player && !isSubscribedForPlayerDeath)
        {
            Debug.Log("UP");
            player.OnPlayerDeath += DestroyProjectile;
            isSubscribedForPlayerDeath = true;
        }
    }

    private void OnDisable()
    {
        if (player && isSubscribedForPlayerDeath)
        {
            Debug.Log("DOWN");
            player.OnPlayerDeath -= DestroyProjectile;
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
        if (trigger.gameObject.TryGetComponent<EnemyController>(out var enemy))
        {
            if (IsMarked)
            {
                Debug.Log("MARKED!");
                OnEnemyHit?.Invoke(trigger.gameObject, -ProjectileDMG
                    - enemy.GetComponent<EnemyController>().MaxHealth * PercentageDMGValue);
            }
            else
            {
                OnEnemyHit?.Invoke(trigger.gameObject, -ProjectileDMG);
            }

        }

        DestroyProjectile();
    }


    /// <summary>
    /// Visszaadja a lövedéket az objektumpoolba, hogy újra felhasználható legyen.
    /// Ahelyett, hogy törölné az objektumot, visszaadja azt a poolba, hogy optimalizálja az erõforrások használatát.
    /// </summary>
    void DestroyProjectile()
    {
        objectPool.ReturnProjectile(gameObject);
        timer = 0f;
    }

}
