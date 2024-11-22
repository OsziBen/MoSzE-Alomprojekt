using Assets.Scripts;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.TextCore.Text;

public class EnemyController : Assets.Scripts.Character
{
    /// <summary>
    /// V�ltoz�k
    /// </summary>
    private List<Projectile> activeProjectiles = new List<Projectile>();    // Az akt�van megfigyelt l�ved�kek (Projectile) list�ja


    /// <summary>
    /// Komponenesek
    /// </summary>
    private ObjectPoolForProjectiles objectPool;  // A l�ved�kek kezel�s�re szolg�l� ObjectPool objektum


    /// <summary>
    /// Getterek �s Setterek
    /// </summary>


    /// <summary>
    /// Esem�nyek
    /// </summary>
    public event Action<float> OnPlayerCollision; // J�t�kossal val� �tk�z�s
    public event Action<GameObject> OnEnemyDeath;   // Ellens�g hal�la


    /// <summary>
    /// Inicializ�lja az objektumpool-t �s feliratkozik az esem�nyekre a l�ved�kek kezel�s�hez.
    /// A met�dus be�ll�tja a sz�ks�ges objektumokat, �s feliratkozik a l�ved�kek poolba helyez�s�re �s visszaad�s�ra vonatkoz� esem�nyekre.
    /// </summary>
    private void Start()
    {
        objectPool = FindObjectOfType<ObjectPoolForProjectiles>();

        // event subscriptions
        objectPool.OnProjectileActivated += StartProjectileDetection;
        objectPool.OnProjectileDeactivated += StopProjectileDetection;
    }


    /// <summary>
    /// Esem�nykezel�, amely akkor h�v�dik meg, amikor egy m�sik collider 2D-es triggerrel �tk�zik.
    /// Ellen�rzi, hogy a trigger objektum a PlayerController-t tartalmazza, �s ha igen, 
    /// megh�vja az OnPlayerCollision esem�nyt, amely a j�t�kos sebz�s�t kezeli.
    /// </summary>
    /// <param name="trigger">Az �tk�z� collider, amely a trigger esem�nyt v�ltja ki</param>
    void OnTriggerEnter2D(Collider2D trigger)
    {
        //Debug.Log(player);
        if (trigger.gameObject.TryGetComponent<PlayerController>(out var palyer))
        {
            Debug.Log(BaseDMG);
            OnPlayerCollision?.Invoke(-BaseDMG);
        }
    }

    /// <summary>
    /// Elkezdi figyelni a l�ved�ket, amikor az akt�vv� v�lik.
    /// Feliratkozik a l�ved�kek 'OnEnemyHit' esem�ny�re, �s hozz�adja �ket az akt�v l�ved�kek list�j�hoz.
    /// Biztos�tja, hogy ugyanaz a l�ved�k ne ker�lj�n t�bbsz�r hozz�ad�sra.
    /// </summary>
    /// <param name="projectile">A figyelt l�ved�k, amelyet akt�vv� v�lt.</param>
    void StartProjectileDetection(GameObject projectile)
    {
        Projectile proj = projectile.GetComponent<Projectile>();
        // Avoid adding the same projectile twice
        if (!activeProjectiles.Contains(proj))
        {
            proj.OnEnemyHit += HandleEnemyHit;
            activeProjectiles.Add(proj);

            Debug.Log($"Projectile detected: {proj}");
        }
    }


    /// <summary>
    /// Le�ll�tja a l�ved�k figyel�s�t, amikor az visszaker�l az objektumpoolba.
    /// Elt�vol�tja a l�ved�ket az akt�v l�ved�kek list�j�b�l, �s leiratkozik az 'OnEnemyHit' esem�nyr�l.
    /// </summary>
    /// <param name="projectile">A l�ved�k, amelyet m�r nem kell figyelni.</param>
    void StopProjectileDetection(GameObject projectile)
    {
        Projectile proj = projectile.GetComponent<Projectile>();
        proj.OnEnemyHit -= HandleEnemyHit;

        activeProjectiles.Remove(proj);
        Debug.Log($"Projectile returned: {proj}");
    }

    /// <summary>
    /// Kezeli az ellens�g tal�lat�t, amikor egy l�ved�k eltal�lja.
    /// Ha a l�ved�k �ltal eltal�lt objektum az aktu�lis ellens�g, akkor alkalmazza a kapott sebz�st.
    /// </summary>
    /// <param name="enemyHitByProjectile">Az ellens�g, amelyet a l�ved�k eltal�lt.</param>
    /// <param name="damageAmount">A sebz�s m�rt�ke, amelyet a l�ved�k okozott.</param>
    void HandleEnemyHit(GameObject enemyHitByProjectile, float damageAmount)
    {

        if (gameObject == enemyHitByProjectile)
        {
            ChangeHealth(damageAmount);
        }

    }


    /// <summary>
    /// Kezeli az ellens�g hal�l�t.
    /// Megh�vja az alap `Die` met�dust, majd az `OnEnemyDeath` esem�nyt, hogy �rtes�tse a rendszer t�bbi r�sz�t az ellens�g hal�l�r�l.
    /// </summary>
    protected override void Die()
    {
        base.Die();
        OnEnemyDeath?.Invoke(gameObject);
    }


    /// <summary>
    /// Tiszt�tja a sz�ks�ges esem�nyeket �s er�forr�sokat az objektum megsemmis�t�sekor.
    /// El�sz�r megh�vja az alap `OnDestroy` met�dust, majd leiratkozik az esem�nyekr�l �s t�rli az akt�v l�ved�kek list�j�t.
    /// </summary>
    protected override void OnDestroy()
    {
        base.OnDestroy();
        if (objectPool != null)
        {
            objectPool.OnProjectileActivated -= StartProjectileDetection;
            objectPool.OnProjectileDeactivated -= StopProjectileDetection;
        }


        if (activeProjectiles != null)
        {
            foreach (var proj in activeProjectiles)
            {
                proj.OnEnemyHit -= HandleEnemyHit;
            }

            activeProjectiles.Clear();
        }
    }


}