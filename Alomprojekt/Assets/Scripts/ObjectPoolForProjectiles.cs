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

        public int projectileMarkEveryNth = 4;  // n értéke, ahol minden n-edik lövedéket megjelöljük; TODO:clamp 1-x
        private bool isMarkingEnabled;   // lövedékjelölés ki- és bekapcsolása
        private int fireCounter = 0;    // lövedékszámláló a jelöléshez


        /// <summary>
        /// Komponenesek
        /// </summary>
        private Camera mainCamera;

        /// <summary>
        /// Getterek és Setterek
        /// </summary>
        public bool IsMarkingEnabled {
            get { return isMarkingEnabled; }
            set { isMarkingEnabled = value; }
        }

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
        private void Awake()
        {
            pool = new Queue<GameObject>(poolSize);

            //EnableMarking(true);
        }


        /// <summary>
        /// Inicializálja a lövedékek pool-ját azáltal, hogy elõállítja és deaktiválja a szükséges számú lövedéket.
        /// A lövedékek a poolba kerülnek, és készen állnak a használatra.
        /// </summary>
        private void Start()
        {
            mainCamera = Camera.main;
            for (int i = 0; i < poolSize; i++)
            {
                GameObject projectile = Instantiate(projectilePrefab);
                projectile.SetActive(false);
                projectile.GetComponent<Projectile>().MainCamera = mainCamera;
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
        /// <param name="percentageDMGValue">A sebzéshez tartozó százalékos érték, amely esetleg módosíthatja a sebzést.</param>
        /// <returns>A lekért vagy létrehozott lövedék GameObject-je.</returns>
        public GameObject GetProjectile(Vector2 position, Quaternion rotation, float damageValue, float percentageDMGValue)
        {

            foreach (var projectile in pool)
            {
                if (!projectile.activeInHierarchy)
                {
                    projectile.transform.position = position;
                    projectile.transform.rotation = rotation;

                    UpdateProjectileProperties(projectile, damageValue, percentageDMGValue);

                    projectile.SetActive(true);
                    OnProjectileActivated?.Invoke(projectile);
                    return projectile;
                }
            }

            GameObject newProjectile = Instantiate(projectilePrefab, position, rotation);
            pool.Enqueue(newProjectile);

            UpdateProjectileProperties(newProjectile, damageValue, percentageDMGValue);

            OnProjectileActivated?.Invoke(newProjectile);

            return newProjectile;

        }


        /// <summary>
        /// Frissíti a lövedék tulajdonságait, például a sebzés értékét.
        /// Beállítja a lövedék sebzését a megadott 'damageValue' és 'percentageDMGValue' alapján.
        /// </summary>
        /// <param name="projectile">A frissítendõ lövedék GameObject-je.</param>
        /// <param name="damageValue">A lövedék új sebzés értéke.</param>
        /// <param name="percentageDMGValue">A lövedékhez tartozó százalékos sebzés érték.</param>
        private void UpdateProjectileProperties(GameObject projectile, float damageValue, float percentageDMGValue)
        {
            fireCounter++;

            Projectile proj = projectile.GetComponent<Projectile>();
            proj.ProjectileDMG = damageValue;
            proj.PercentageDMGValue = percentageDMGValue;

            if (isMarkingEnabled)
            {
                MarkProjectile(proj);
            }

        }


        /// <summary>
        /// Beállítja, hogy a lövedék jelölve legyen-e, a 'fireCounter' és a 'projectileMarkEveryNth' alapján.
        /// Ha a lövések száma eléri a beállított intervallumot, a lövedék jelölve lesz.
        /// </summary>
        /// <param name="projectile">A lövedék, amelynek a jelölését frissítjük.</param>
        void MarkProjectile(Projectile projectile)
        {
            if (fireCounter % projectileMarkEveryNth == 0)
            {
                projectile.IsMarked = true;
                fireCounter = 0;
            }
            else
            {
                projectile.IsMarked = false;
            }
        }


        /// <summary>
        /// Engedélyezi vagy letiltja a lövedékek jelölését.
        /// Ha a jelölés le van tiltva, a lövések számlálója (fireCounter) visszaállítódik.
        /// </summary>
        /// <param name="enable">Ha igaz (true), engedélyezi a jelölést, ha hamis (false), letiltja azt.</param>
        public void EnableMarking(bool enable)
        {
            IsMarkingEnabled = enable;
            if (!isMarkingEnabled)
            {
                fireCounter = 0;
            }
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
            proj.IsMarked = false;
            OnProjectileDeactivated?.Invoke(projectile);
        }

    }
}
