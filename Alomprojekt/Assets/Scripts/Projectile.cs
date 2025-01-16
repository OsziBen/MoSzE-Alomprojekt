using Assets.Scripts;
using System;
using UnityEngine;
using UnityEngine.InputSystem;


public class Projectile : MonoBehaviour
{
    /// <summary>
    /// V�ltoz�k
    /// </summary>
    private float _projectileDMG;           // A l�ved�k sebz�s�rt�ke
    public float deleteTime = 5f;
    //private float timer = 0f;
    public int force = 3;                   // Az alkalmazott er�, amely meghat�rozza a l�ved�k ind�t�s�nak intenzit�s�t
    private float _percentageDMGValue;      // Sz�zal�kos sebz�s�rt�k
    private bool _isMarked;     // l�ved�k megjel�lts�ge [igen/nem]

    private bool isSubscribedForPlayerDeath;


    /// <summary>
    /// Komponenesek
    /// </summary>
    Rigidbody2D rigidbody2d;    // L�ved�khez kapcsol�d� Rigidbody2D komponens
    private ObjectPoolForProjectiles objectPool;    // A l�ved�keket kezel� ObjectPoolForProjectiles komponens

    private PlayerController player;

    private Camera _mainCamera;

    /// <summary>
    /// Getterek �s Setterek
    /// </summary>
    public float ProjectileDMG  // L�ved�k sebz�s�rt�ke
    {
        get { return _projectileDMG; }
        set { _projectileDMG = value; }
    }

    public float PercentageDMGValue // L�ved�k sz�zal�kos sebz�s�rt�ke
    {
        get { return _percentageDMGValue; }
        set { _percentageDMGValue = value; }
    }

    public bool IsMarked    // L�ved�k megjel�lts�ge
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
    /// Esem�nyek
    /// </summary>
    /// </summary>
    public event Action<EnemyController, float> OnEnemyHit;  // Esem�ny, amely akkor h�v�dik meg, amikor a l�ved�k eltal�l egy ellens�ges karaktert
    public event Action<float> OnBossHit;
 

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
        objectPool = FindObjectOfType<ObjectPoolForProjectiles>();
        player = FindAnyObjectByType<PlayerController>();
        player.OnPlayerDeath += DestroyProjectile;
        isSubscribedForPlayerDeath = true;
    }


    /// <summary>
    /// Minden friss�t�skor ellen�rzi, hogy a GameObject poz�ci�ja meghaladja-e a t�rl�shez sz�ks�ges t�vols�got.
    /// Ha a t�vols�g t�l nagy, a GameObject-et t�rli.
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
        if (player && !isSubscribedForPlayerDeath)
        {
            player.OnPlayerDeath += DestroyProjectile;
            isSubscribedForPlayerDeath = true;
        }
    }

    private void OnDisable()
    {
        if (player && isSubscribedForPlayerDeath)
        {
            player.OnPlayerDeath -= DestroyProjectile;
            isSubscribedForPlayerDeath = false;
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
        Debug.Log("Projectile collision with " + collision.gameObject);
        DestroyProjectile();
    }


    /// <summary>
    /// Kezeli az �tk�z�st, amikor a l�ved�k tal�lkozik egy ellens�ggel.
    /// Ha az �tk�z�s egy ellens�ggel t�rt�nik, megh�vja az esem�nyt, amely cs�kkenti az ellens�g �leterej�t.
    /// Ezut�n t�rli a l�ved�ket.
    /// </summary>
    /// <param name="trigger">A collider, amely aktiv�lta az �tk�z�st, val�sz�n�leg egy ellens�g.</param>
    private void OnTriggerEnter2D(Collider2D trigger)
    {
        if (trigger.gameObject.TryGetComponent<EnemyController>(out var enemy))
        {
            float damage = IsMarked
                ? -ProjectileDMG - enemy.MaxHealth * PercentageDMGValue
                : -ProjectileDMG;

            Debug.Log(IsMarked ? "MARKED!" : "UNMARKED!");

            OnEnemyHit?.Invoke(enemy, damage);
        }

        if (trigger.gameObject.TryGetComponent<BossBodypartController>(out var bodypart))
        {
            Debug.Log($"{bodypart.name}");

            float damage = IsMarked
                ? -ProjectileDMG - bodypart.GetComponentInParent<BossController>().MaxHealth * PercentageDMGValue
                : -ProjectileDMG;
            
            OnBossHit?.Invoke(damage);
        }

        if (trigger.gameObject.TryGetComponent<BossController>(out var boss))
        {
            Debug.Log($"{boss.name}");

            float damage = IsMarked
                ? -ProjectileDMG - boss.MaxHealth * PercentageDMGValue
                : -ProjectileDMG;

            OnBossHit?.Invoke(damage);
        }

        DestroyProjectile();
    }


    /// <summary>
    /// Visszaadja a l�ved�ket az objektumpoolba, hogy �jra felhaszn�lhat� legyen.
    /// Ahelyett, hogy t�r�ln� az objektumot, visszaadja azt a poolba, hogy optimaliz�lja az er�forr�sok haszn�lat�t.
    /// </summary>
    void DestroyProjectile()
    {
        objectPool.ReturnProjectile(gameObject);
        //timer = 0f;
    }

}
