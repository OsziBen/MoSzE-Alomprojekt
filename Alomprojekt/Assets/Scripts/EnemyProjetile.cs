using Assets.Scripts;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyProjetile : MonoBehaviour
{
    /// <summary>
    /// Változók
    /// </summary>
    private float _projectileDMG;           // A lövedék sebzésértéke


    private bool isSubscribedForBossDeath; // Jelzi, hogy feliratkozott-e a boss halál eseményére

    [Header("Sprites and Colliders")]
    [SerializeField]
    private List<SpriteLevelData> spriteLevelDataList;  // A sprite szintek adatai
    [SerializeField]
    private Sprite bossProjectileSprite;               // A boss lövedékének sprite-ja
    [SerializeField]
    private Collider2D bossProjectileCollider;         // A boss lövedékének ütközője


    /// <summary>
    /// Komponenesek
    /// </summary>
    Rigidbody2D rigidbody2d;    // Lövedékhez kapcsolódó Rigidbody2D komponens
    private BossObjectPool bossObjectPool;    // A lövedékeket kezelõ ObjectPoolForProjectiles komponens

    private BossController boss; // A boss vezérlője.

    private Camera _mainCamera; // A fő kamera referenciaja

    /// <summary>
    /// Getterek és Setterek
    /// </summary>
    public float ProjectileDMG  // Lövedék sebzésértéke
    {
        get { return _projectileDMG; }
        set { _projectileDMG = value; }
    }

    public Camera MainCamera // A fő kamerát vezérlő tulajdonság
    {
        get { return _mainCamera; }
        set { _mainCamera = value; }
    }


    /// <summary>
    /// Események
    /// </summary>
    /// </summary>
    public event Action<float> OnPlayerHit;  // Esemény, amely akkor hívódik meg, amikor a lövedék eltalál egy ellenséges karaktert



    /// <summary>
    /// Inicializálja a Rigidbody2D komponenst és az objektumpool-t a fizikai interakciókhoz és lövedékek kezeléséhez.
    /// A metódus a játéknál szükséges komponenseket állítja be az objektumok mûködéséhez.
    /// </summary>
    void Awake()
    {
        rigidbody2d = GetComponent<Rigidbody2D>();
    }

    /// <summary>
    /// Inicializálja a lövedék objektumot, beállítja a főnökhöz tartozó objektumpoolt, 
    /// és feliratkozik a főnök haláleseményére.
    /// </summary>
    private void Start()
    {
        // Keres egy BossObjectPool típusú objektumot a jelenetben, és hozzárendeli a változóhoz.
        bossObjectPool = FindObjectOfType<BossObjectPool>();

        // Keres egy BossController típusú objektumot a jelenetben, és hozzárendeli a változóhoz.
        boss = FindAnyObjectByType<BossController>();

        // Feliratkozik a főnök halálát jelző eseményre. Ha a főnök meghal, meghívja a DestroyProjectile metódust.
        boss.OnBossDeath += DestroyProjectile;

        // Jelzi, hogy a lövedék feliratkozott a főnök halálának eseményére.
        isSubscribedForBossDeath = true;
    }



    /// <summary>
    /// Minden frissítéskor ellenõrzi, hogy a GameObject pozíciója meghaladja-e a törléshez szükséges távolságot.
    /// Ha a távolság túl nagy, a GameObject-et törli.
    /// </summary>
    void Update()
    {
        Vector3 viewportPosition = MainCamera.WorldToViewportPoint(transform.position);

        if (viewportPosition.x < -0.05 || viewportPosition.x > 1.05 || viewportPosition.y < -0.05 || viewportPosition.y > 1.05 /*|| timer >= deleteTime*/)
        {
            DestroyProjectile();
        }
    }


    private void OnEnable()
    {
        // Ellenőrzi, hogy a boss (főnök) objektum létezik-e, és hogy még nincs feliratkozva a halál eseményére.
        if (boss && !isSubscribedForBossDeath)
        {
            // Feliratkozik a főnök halál eseményére, hogy a DestroyProjectile metódus fusson le, amikor a főnök meghal.
            boss.OnBossDeath += DestroyProjectile;

            // Beállítja, hogy az objektum már feliratkozott az eseményre.
            isSubscribedForBossDeath = true;
        }
    }

    private void OnDisable()
    {
        // Ellenőrzi, hogy a boss (főnök) objektum létezik-e, és hogy jelenleg fel van iratkozva a halál eseményére.
        if (boss && isSubscribedForBossDeath)
        {
            // Leiratkozik a főnök halál eseményéről.
            boss.OnBossDeath -= DestroyProjectile;

            // Beállítja, hogy az objektum már nincs feliratkozva az eseményre.
            isSubscribedForBossDeath = false;
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
        if (collision.gameObject.TryGetComponent<PlayerController>(out var player))
        {
            OnPlayerHit?.Invoke(-ProjectileDMG);
        }

        Debug.Log("Projectile collision with " + collision.gameObject);
        DestroyProjectile();
    }


    /// <summary>
    /// Visszaadja a lövedéket az objektumpoolba, hogy újra felhasználható legyen.
    /// Ahelyett, hogy törölné az objektumot, visszaadja azt a poolba, hogy optimalizálja az erõforrások használatát.
    /// </summary>
    void DestroyProjectile()
    {
        bossObjectPool.ReturnBossProjectile(gameObject);
    }
}
