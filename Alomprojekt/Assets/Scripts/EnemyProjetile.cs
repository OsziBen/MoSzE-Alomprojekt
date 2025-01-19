using Assets.Scripts;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyProjetile : MonoBehaviour
{
    /// <summary>
    /// V�ltoz�k
    /// </summary>
    private float _projectileDMG;           // A l�ved�k sebz�s�rt�ke


    private bool isSubscribedForBossDeath;

    [Header("Sprites and Colliders")]
    [SerializeField]
    private List<SpriteLevelData> spriteLevelDataList;
    [SerializeField]
    private Sprite bossProjectileSprite;
    [SerializeField]
    private Collider2D bossProjectileCollider;


    /// <summary>
    /// Komponenesek
    /// </summary>
    Rigidbody2D rigidbody2d;    // L�ved�khez kapcsol�d� Rigidbody2D komponens
    private BossObjectPool bossObjectPool;    // A l�ved�keket kezel� ObjectPoolForProjectiles komponens

    private BossController boss;

    private Camera _mainCamera;

    /// <summary>
    /// Getterek �s Setterek
    /// </summary>
    public float ProjectileDMG  // L�ved�k sebz�s�rt�ke
    {
        get { return _projectileDMG; }
        set { _projectileDMG = value; }
    }

    public Camera MainCamera
    {
        get { return _mainCamera; }
        set { _mainCamera = value; }
    }


    /// <summary>
    /// Esem�nyek
    /// </summary>
    /// </summary>
    public event Action<float> OnPlayerHit;  // Esem�ny, amely akkor h�v�dik meg, amikor a l�ved�k eltal�l egy ellens�ges karaktert



    /// <summary>
    /// Inicializ�lja a Rigidbody2D komponenst �s az objektumpool-t a fizikai interakci�khoz �s l�ved�kek kezel�s�hez.
    /// A met�dus a j�t�kn�l sz�ks�ges komponenseket �ll�tja be az objektumok m�k�d�s�hez.
    /// </summary>
    void Awake()
    {
        rigidbody2d = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        bossObjectPool = FindObjectOfType<BossObjectPool>();
        boss = FindAnyObjectByType<BossController>();
        boss.OnBossDeath += DestroyProjectile;
        isSubscribedForBossDeath = true;
    }


    /// <summary>
    /// Minden friss�t�skor ellen�rzi, hogy a GameObject poz�ci�ja meghaladja-e a t�rl�shez sz�ks�ges t�vols�got.
    /// Ha a t�vols�g t�l nagy, a GameObject-et t�rli.
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
        if (boss && !isSubscribedForBossDeath)
        {
            boss.OnBossDeath += DestroyProjectile;
            isSubscribedForBossDeath = true;
        }
    }

    private void OnDisable()
    {
        if (boss && isSubscribedForBossDeath)
        {
            boss.OnBossDeath -= DestroyProjectile;
            isSubscribedForBossDeath = false;
        }
    }


    /// <summary>
    /// Elind�tja a GameObject-et egy adott ir�nyba �s er�vel a Rigidbody2D komponens seg�ts�g�vel.
    /// </summary>
    /// <param name="direction">Az ir�ny, amelybe a GameObject-et el kell ind�tani</param>
    /// <param name="force">Az er�, amellyel a GameObject-nek mozognia kell</param>
    public void Launch(Vector2 direction, float launchForce)
    {
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle - 90f);
        rigidbody2d.AddForce(direction * launchForce);
    }


    /// <summary>
    /// Esem�nykezel�, amely akkor h�v�dik meg, amikor a GameObject egy m�sik objektummal �tk�zik.
    /// Az �tk�z�sr�l debug �zenetet �r ki a konzolra, majd t�rli a GameObject-et.
    /// </summary>
    /// <param name="collision">Az �tk�z� objektum, amellyel a GameObject �tk�zik</param>
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
    /// Visszaadja a l�ved�ket az objektumpoolba, hogy �jra felhaszn�lhat� legyen.
    /// Ahelyett, hogy t�r�ln� az objektumot, visszaadja azt a poolba, hogy optimaliz�lja az er�forr�sok haszn�lat�t.
    /// </summary>
    void DestroyProjectile()
    {
        bossObjectPool.ReturnBossProjectile(gameObject);
    }
}
