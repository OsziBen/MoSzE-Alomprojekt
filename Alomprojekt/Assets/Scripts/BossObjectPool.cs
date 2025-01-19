using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class BossObjectPool : BaseTransientManager<BossObjectPool>
{
    /// <summary>
    /// V�ltoz�k
    /// </summary>
    [Header("Object Pool Settings")]
    [SerializeField]
    private GameObject projectilePrefab; // A l�ved�k prefab, amelyet a poolba helyezett objektumok alapj�ul haszn�lunk.
    [SerializeField]
    private int poolSize;   // A pool maxim�lis m�rete, amely meghat�rozza, h�ny l�ved�k lehet egyszerre az objektumpoolban.

    private Queue<GameObject> pool; // A l�ved�kek t�rol�s�ra haszn�lt queue (sor), amely a rendelkez�sre �ll� �s visszaadott l�ved�keket tartalmazza.


    /// <summary>
    /// Komponenesek
    /// </summary>
    private Camera mainCamera;

    /// <summary>
    /// Getterek �s Setterek
    /// </summary>
    

    /// <summary>
    /// Esem�nyek
    /// </summary>
    public event Action<GameObject> OnProjectileActivated;  // Esem�ny, amely akkor aktiv�l�dik, amikor egy l�ved�k el�rhet�v� v�lik a poolban �s haszn�latba ker�l.
    public event Action<GameObject> OnProjectileDeactivated;    // Esem�ny, amely akkor aktiv�l�dik, amikor egy l�ved�k deaktiv�l�dik, miut�n visszaker�lt a poolba.


    /// <summary>
    /// Inicializ�lja a l�ved�kek pool-j�t az el�re meghat�rozott m�ret alapj�n.
    /// A 'poolSize' v�ltoz� hat�rozza meg, hogy h�ny l�ved�k f�r el a poolban.
    /// A 'pool' v�ltoz� egy Queue t�pus� adatstrukt�ra, amely a l�ved�kek kezel�s�re szolg�l.
    /// A 'EnableMarking' met�dus itt ker�l megh�v�sra, hogy enged�lyezze a jel�l�st.
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
    /// Lek�r egy l�ved�ket a poolb�l, vagy l�trehoz egy �jat, ha nincs el�rhet� inakt�v l�ved�k.
    /// Be�ll�tja a l�ved�k poz�ci�j�t, forgat�s�t �s a sebz�s �rt�k�t, majd aktiv�lja azt.
    /// </summary>
    /// <param name="position">A l�ved�k k�v�nt poz�ci�ja.</param>
    /// <param name="rotation">A l�ved�k k�v�nt forgat�sa (orient�ci�ja).</param>
    /// <param name="damageValue">A l�ved�k �ltal okozott sebz�s m�rt�ke.</param>
    /// <returns>A lek�rt vagy l�trehozott l�ved�k GameObject-je.</returns>
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
    /// Friss�ti a l�ved�k tulajdons�gait, p�ld�ul a sebz�s �rt�k�t.
    /// Be�ll�tja a l�ved�k sebz�s�t a megadott 'damageValue' �s 'percentageDMGValue' alapj�n.
    /// </summary>
    /// <param name="projectile">A friss�tend� l�ved�k GameObject-je.</param>
    /// <param name="damageValue">A l�ved�k �j sebz�s �rt�ke.</param>
    private void UpdateBossProjectileProperties(GameObject projectile, float damageValue)
    {
        EnemyProjetile proj = projectile.GetComponent<EnemyProjetile>();
        proj.ProjectileDMG = damageValue;
    }


    /// <summary>
    /// Visszaad egy l�ved�ket a poolba: deaktiv�lja azt, �s null�zza a sebz�s �rt�k�t.
    /// Esem�nyt h�v, hogy jelezze a l�ved�k deaktiv�l�d�s�t.
    /// </summary>
    /// <param name="projectile">A visszaadott l�ved�k GameObject-je.</param>
    public void ReturnBossProjectile(GameObject projectile)
    {
        projectile.SetActive(false);
        EnemyProjetile proj = projectile.GetComponent<EnemyProjetile>();
        proj.ProjectileDMG = 0f;
        OnProjectileDeactivated?.Invoke(projectile);
    }

}
