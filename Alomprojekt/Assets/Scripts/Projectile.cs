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
    //public float deleteDistance = 25.0f;    // A l�ved�k maxim�lisan megtehet� t�vols�ga, amely ut�n t�rl�sre ker�l
    public float deleteTime = 5f;
    private float timer = 0f;
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


    /// <summary>
    /// Esem�nyek
    /// </summary>
    /// </summary>
    public event Action<GameObject, float> OnEnemyHit;  // Esem�ny, amely akkor h�v�dik meg, amikor a l�ved�k eltal�l egy ellens�ges karaktert


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
        Debug.Log("UP");
        isSubscribedForPlayerDeath = true;
    }


    /// <summary>
    /// Minden friss�t�skor ellen�rzi, hogy a GameObject poz�ci�ja meghaladja-e a t�rl�shez sz�ks�ges t�vols�got.
    /// Ha a t�vols�g t�l nagy, a GameObject-et t�rli.
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
    /// Elind�tja a GameObject-et egy adott ir�nyba �s er�vel a Rigidbody2D komponens seg�ts�g�vel.
    /// </summary>
    /// <param name="direction">Az ir�ny, amelybe a GameObject-et el kell ind�tani</param>
    /// <param name="force">Az er�, amellyel a GameObject-nek mozognia kell</param>
    public void Launch(Vector2 direction, float launchForce)
    {
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
    /// Visszaadja a l�ved�ket az objektumpoolba, hogy �jra felhaszn�lhat� legyen.
    /// Ahelyett, hogy t�r�ln� az objektumot, visszaadja azt a poolba, hogy optimaliz�lja az er�forr�sok haszn�lat�t.
    /// </summary>
    void DestroyProjectile()
    {
        objectPool.ReturnProjectile(gameObject);
        timer = 0f;
    }

}
