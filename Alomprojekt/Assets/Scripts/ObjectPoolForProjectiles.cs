using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts
{
    public class ObjectPoolForProjectiles : MonoBehaviour
    {
        /// <summary>
        /// Változók
        /// </summary>
        public GameObject projectilePrefab; // A lövedék prefab, amelyet a poolba helyezett objektumok alapjául használunk.
        public int poolSize = 25;   // A pool maximális mérete, amely meghatározza, hány lövedék lehet egyszerre az objektumpoolban.
        private Queue<GameObject> pool; // A lövedékek tárolására használt queue (sor), amely a rendelkezésre álló és visszaadott lövedékeket tartalmazza.


        /// <summary>
        /// Komponenesek
        /// </summary>


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
        /// </summary>
        private void Awake()
        {
            pool = new Queue<GameObject>(poolSize);

        }


        /// <summary>
        /// Inicializálja a lövedékek pool-ját azáltal, hogy elõállítja és deaktiválja a szükséges számú lövedéket.
        /// A lövedékek a poolba kerülnek, és készen állnak a használatra.
        /// </summary>
        private void Start()
        {

            for (int i = 0; i < poolSize; i++)
            {
                GameObject projectile = Instantiate(projectilePrefab);
                projectile.SetActive(false);
                pool.Enqueue(projectile);
            }
        }


        /// <summary>
        /// Lekér egy lövedéket a poolból, vagy létrehoz egy újat, ha nincs elérhetõ inaktív lövedék.
        /// Beállítja a lövedék pozícióját, forgását és a sebzés értékét, majd aktiválja azt.
        /// </summary>
        /// <param name="position">A lövedék kívánt pozíciója.</param>
        /// <param name="rotation">A lövedék kívánt forgatása (orientációja).</param>
        /// <param name="damageValue">A lövedék által okozott sebzés mértéke.</param>
        /// <returns>A lekért vagy létrehozott lövedék GameObject-je.</returns>
        public GameObject GetProjectile(Vector2 position, Quaternion rotation, float damageValue)
        {

            foreach (var projectile in pool)
            {
                if (!projectile.activeInHierarchy)
                {
                    projectile.transform.position = position;
                    projectile.transform.rotation = rotation;

                    UpdateProjectileProperties(projectile, damageValue);

                    projectile.SetActive(true);
                    OnProjectileActivated?.Invoke(projectile);
                    return projectile;
                }
            }

            GameObject newProjectile = Instantiate(projectilePrefab, position, rotation);
            pool.Enqueue(newProjectile);

            UpdateProjectileProperties(newProjectile, damageValue);

            OnProjectileActivated?.Invoke(newProjectile);

            return newProjectile;

        }


        /// <summary>
        /// Frissíti a lövedék tulajdonságait, például a sebzés értékét.
        /// Beállítja a lövedék sebzését a megadott 'damageValue' alapján.
        /// </summary>
        /// <param name="projectile">A frissítendõ lövedék GameObject-je.</param>
        /// <param name="damageValue">A lövedék új sebzés értéke.</param>
        private void UpdateProjectileProperties(GameObject projectile, float damageValue)
        {
            Projectile proj = projectile.GetComponent<Projectile>();
            proj.ProjectileDMG = damageValue;

        }


        /// <summary>
        /// Visszaad egy lövedéket a poolba: deaktiválja azt, és nullázza a sebzés értékét.
        /// Eseményt hív, hogy jelezze a lövedék deaktiválódását.
        /// </summary>
        /// <param name="projectile">A visszaadott lövedék GameObject-je.</param>
        public void ReturnProjectile(GameObject projectile)
        {
            projectile.SetActive(false);
            Projectile proj = projectile.GetComponent<Projectile>();
            proj.ProjectileDMG = 0f;
            OnProjectileDeactivated?.Invoke(projectile);
        }

    }
}
