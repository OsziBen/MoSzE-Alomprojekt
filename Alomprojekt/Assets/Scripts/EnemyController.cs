using Assets.Scripts;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.TextCore.Text;

public class EnemyController : Assets.Scripts.Character
{
    /// <summary>
    /// Változók
    /// </summary>
    private List<Projectile> activeProjectiles = new List<Projectile>();    // Az aktívan megfigyelt lövedékek (Projectile) listája


    /// <summary>
    /// Komponenesek
    /// </summary>
    private ObjectPoolForProjectiles objectPool;  // A lövedékek kezelésére szolgáló ObjectPool objektum


    /// <summary>
    /// Getterek és Setterek
    /// </summary>


    /// <summary>
    /// Események
    /// </summary>
    public event Action<float> OnPlayerCollision; // Játékossal való ütközés
    public event Action<GameObject> OnEnemyDeath;   // Ellenség halála


    /// <summary>
    /// Inicializálja az objektumpool-t és feliratkozik az eseményekre a lövedékek kezeléséhez.
    /// A metódus beállítja a szükséges objektumokat, és feliratkozik a lövedékek poolba helyezésére és visszaadására vonatkozó eseményekre.
    /// </summary>
    private void Start()
    {
        objectPool = FindObjectOfType<ObjectPoolForProjectiles>();

        // event subscriptions
        objectPool.OnProjectileActivated += StartProjectileDetection;
        objectPool.OnProjectileDeactivated += StopProjectileDetection;
    }


    /// <summary>
    /// Eseménykezelõ, amely akkor hívódik meg, amikor egy másik collider 2D-es triggerrel ütközik.
    /// Ellenõrzi, hogy a trigger objektum a PlayerController-t tartalmazza, és ha igen, 
    /// meghívja az OnPlayerCollision eseményt, amely a játékos sebzését kezeli.
    /// </summary>
    /// <param name="trigger">Az ütközõ collider, amely a trigger eseményt váltja ki</param>
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
    /// Elkezdi figyelni a lövedéket, amikor az aktívvá válik.
    /// Feliratkozik a lövedékek 'OnEnemyHit' eseményére, és hozzáadja õket az aktív lövedékek listájához.
    /// Biztosítja, hogy ugyanaz a lövedék ne kerüljön többször hozzáadásra.
    /// </summary>
    /// <param name="projectile">A figyelt lövedék, amelyet aktívvá vált.</param>
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
    /// Leállítja a lövedék figyelését, amikor az visszakerül az objektumpoolba.
    /// Eltávolítja a lövedéket az aktív lövedékek listájából, és leiratkozik az 'OnEnemyHit' eseményrõl.
    /// </summary>
    /// <param name="projectile">A lövedék, amelyet már nem kell figyelni.</param>
    void StopProjectileDetection(GameObject projectile)
    {
        Projectile proj = projectile.GetComponent<Projectile>();
        proj.OnEnemyHit -= HandleEnemyHit;

        activeProjectiles.Remove(proj);
        Debug.Log($"Projectile returned: {proj}");
    }

    /// <summary>
    /// Kezeli az ellenség találatát, amikor egy lövedék eltalálja.
    /// Ha a lövedék által eltalált objektum az aktuális ellenség, akkor alkalmazza a kapott sebzést.
    /// </summary>
    /// <param name="enemyHitByProjectile">Az ellenség, amelyet a lövedék eltalált.</param>
    /// <param name="damageAmount">A sebzés mértéke, amelyet a lövedék okozott.</param>
    void HandleEnemyHit(GameObject enemyHitByProjectile, float damageAmount)
    {

        if (gameObject == enemyHitByProjectile)
        {
            ChangeHealth(damageAmount);
        }

    }


    /// <summary>
    /// Kezeli az ellenség halálát.
    /// Meghívja az alap `Die` metódust, majd az `OnEnemyDeath` eseményt, hogy értesítse a rendszer többi részét az ellenség haláláról.
    /// </summary>
    protected override void Die()
    {
        base.Die();
        OnEnemyDeath?.Invoke(gameObject);
    }


    /// <summary>
    /// Tisztítja a szükséges eseményeket és erõforrásokat az objektum megsemmisítésekor.
    /// Elõször meghívja az alap `OnDestroy` metódust, majd leiratkozik az eseményekrõl és törli az aktív lövedékek listáját.
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