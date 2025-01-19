using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class BossObjectPool : BaseTransientManager<BossObjectPool>
{
    /// <summary>
    /// Változók
    /// </summary>
    [Header("Object Pool Settings")]
    [SerializeField]
    private GameObject projectilePrefab; // A lövedék prefab, amelyet a poolba helyezett objektumok alapjául használunk.
    [SerializeField]
    private int poolSize;   // A pool maximális mérete, amely meghatározza, hány lövedék lehet egyszerre az objektumpoolban.

    private Queue<GameObject> pool; // A lövedékek tárolására használt queue (sor), amely a rendelkezésre álló és visszaadott lövedékeket tartalmazza.


    /// <summary>
    /// Komponenesek
    /// </summary>
    private Camera mainCamera;

    /// <summary>
    /// Getterek és Setterek
    /// </summary>
    

    /// <summary>
    /// Események
    /// </summary>
    public event Action<GameObject> OnProjectileActivated;  // Esemény, amely akkor aktiválódik, amikor egy lövedék elérhetõvé válik a poolban és használatba kerül.
    public event Action<GameObject> OnProjectileDeactivated;    // Esemény, amely akkor aktiválódik, amikor egy lövedék deaktiválódik, miután visszakerült a poolba.


    /// <summary>
    /// Inicializálja a lövedékek pool-ját az elõre meghatározott méret alapján.
    /// A 'poolSize' változó határozza meg, hogy hány lövedék fér el a poolban.
    /// A 'pool' változó egy Queue típusú adatstruktúra, amely a lövedékek kezelésére szolgál.
    /// A 'EnableMarking' metódus itt kerül meghívásra, hogy engedélyezze a jelölést.
    /// </summary>
    protected override async void Initialize()
    {
        await Task.Yield();
        base.Initialize();
        mainCamera = Camera.main;
        pool = new Queue<GameObject>(poolSize);

        for (int i = 0; i < poolSize; i++)
        {
            GameObject projectile = Instantiate(projectilePrefab);
            projectile.SetActive(false);
            projectile.GetComponent<EnemyProjetile>().MainCamera = mainCamera;
            pool.Enqueue(projectile);
        }
    }


    /// <summary>
    /// Lekér egy lövedéket a poolból, vagy létrehoz egy újat, ha nincs elérhetõ inaktív lövedék.
    /// Beállítja a lövedék pozícióját, forgatását és a sebzés értékét, majd aktiválja azt.
    /// </summary>
    /// <param name="position">A lövedék kívánt pozíciója.</param>
    /// <param name="rotation">A lövedék kívánt forgatása (orientációja).</param>
    /// <param name="damageValue">A lövedék által okozott sebzés mértéke.</param>
    /// <returns>A lekért vagy létrehozott lövedék GameObject-je.</returns>
    public GameObject GetBossProjectile(Vector2 position, Quaternion rotation, float damageValue)
    {

        foreach (var projectile in pool)
        {
            if (!projectile.activeInHierarchy)
            {
                projectile.transform.position = position;
                projectile.transform.rotation = rotation;

                UpdateBossProjectileProperties(projectile, damageValue);

                projectile.SetActive(true);
                OnProjectileActivated?.Invoke(projectile);
                return projectile;
            }
        }

        GameObject newProjectile = Instantiate(projectilePrefab, position, rotation);
        pool.Enqueue(newProjectile);

        UpdateBossProjectileProperties(newProjectile, damageValue);

        OnProjectileActivated?.Invoke(newProjectile);

        return newProjectile;

    }


    /// <summary>
    /// Frissíti a lövedék tulajdonságait, például a sebzés értékét.
    /// Beállítja a lövedék sebzését a megadott 'damageValue' és 'percentageDMGValue' alapján.
    /// </summary>
    /// <param name="projectile">A frissítendõ lövedék GameObject-je.</param>
    /// <param name="damageValue">A lövedék új sebzés értéke.</param>
    private void UpdateBossProjectileProperties(GameObject projectile, float damageValue)
    {
        EnemyProjetile proj = projectile.GetComponent<EnemyProjetile>();
        proj.ProjectileDMG = damageValue;
    }


    /// <summary>
    /// Visszaad egy lövedéket a poolba: deaktiválja azt, és nullázza a sebzés értékét.
    /// Eseményt hív, hogy jelezze a lövedék deaktiválódását.
    /// </summary>
    /// <param name="projectile">A visszaadott lövedék GameObject-je.</param>
    public void ReturnBossProjectile(GameObject projectile)
    {
        projectile.SetActive(false);
        EnemyProjetile proj = projectile.GetComponent<EnemyProjetile>();
        proj.ProjectileDMG = 0f;
        OnProjectileDeactivated?.Invoke(projectile);
    }

}
