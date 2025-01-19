using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts
{
    public class ObjectPoolForProjectiles : BaseTransientManager<ObjectPoolForProjectiles>
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
        protected override async void Initialize()
        {
            // Várakozás a következő frissítésig.
            await Task.Yield();
            // Az alapértelmezett inicializálás meghívása a szülő osztályban
            base.Initialize();
            // A fő kamera hozzárendelése
            mainCamera = Camera.main;
            // A pool inicializálása egy Queue típusú adatstruktúrával, amely a lövedékeket fogja tárolni
            pool = new Queue<GameObject>(poolSize);

            // Létrehozza és deaktiválja a lövedékeket a pool méretének megfelelően
            for (int i = 0; i < poolSize; i++)
            {
                // A lövedék példányosítása a prefab alapján
                GameObject projectile = Instantiate(projectilePrefab);
                // A lövedék deaktiválása, hogy ne jelenjen meg a játékban kezdetben
                projectile.SetActive(false);
                // A lövedékhez hozzárendeljük a fő kamerát
                projectile.GetComponent<Projectile>().MainCamera = mainCamera;
                // A lövedék hozzáadása a pool-hoz
                pool.Enqueue(projectile);
            }
        }


        /// <summary>
        /// Inicializálja a lövedékek pool-ját azáltal, hogy elõállítja és deaktiválja a szükséges számú lövedéket.
        /// A lövedékek a poolba kerülnek, és készen állnak a használatra.
        /// </summary>



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
            // Loop, hogy megnézze van-e inaktív lövedék a pool-ban
            foreach (var projectile in pool)
            {
                if (!projectile.activeInHierarchy)
                {
                    // Ha találunk inaktív lövedéket, beállítjuk annak pozícióját és forgatását
                    projectile.transform.position = position;
                    projectile.transform.rotation = rotation;

                    // A lövedék tulajdonságainak frissítése (sebzés érték, módosító)
                    UpdateProjectileProperties(projectile, damageValue, percentageDMGValue);

                    // A lövedék aktiválása a pool-ból
                    projectile.SetActive(true);
                    // Az esemény triggerelése, hogy a lövedék aktiválva lett
                    OnProjectileActivated?.Invoke(projectile);
                    // Visszaadjuk a pool-ból lekért lövedéket
                    return projectile;
                }
            }

            // Ha nincs elérhető inaktív lövedék, létrehozunk egy újat
            GameObject newProjectile = Instantiate(projectilePrefab, position, rotation);
            // Az új lövedék hozzáadása a pool-hoz
            pool.Enqueue(newProjectile);

            // Az új lövedék tulajdonságainak beállítása
            UpdateProjectileProperties(newProjectile, damageValue, percentageDMGValue);

            // Az esemény triggerelése az új lövedék aktiválásához
            OnProjectileActivated?.Invoke(newProjectile);

            // Visszaadjuk az új lövedéket
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
            // A tüzelés számláló növelése minden lövedék frissítéskor
            fireCounter++;

            // A lövedék komponensének lekérése a GameObject-ről
            Projectile proj = projectile.GetComponent<Projectile>();
            // A lövedék sebzésének és százalékos sebzésének beállítása
            proj.ProjectileDMG = damageValue;
            proj.PercentageDMGValue = percentageDMGValue;

            // Ha a jelölés engedélyezve van, jelöljük meg a lövedéket
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
            // Ellenőrzi, hogy a lövések száma elérte-e a beállított intervallumot (projectileMarkEveryNth)
            if (fireCounter % projectileMarkEveryNth == 0)
            {
                // Ha elérte az intervallumot, a lövedék jelölve lesz
                projectile.IsMarked = true;
                // A tüzelés számláló visszaállítása nullára, hogy újrainduljon az intervallum
                fireCounter = 0;
            }
            else
            {
                // Ha nem érte el az intervallumot, a lövedék nem lesz jelölve
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
            // A jelölés engedélyezésének beállítása
            IsMarkingEnabled = enable;
            // Ha a jelölés le van tiltva, a tüzelés számláló visszaállítódik nullára
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
            // A lövedék deaktiválása, hogy visszakerüljön a pool-ba
            projectile.SetActive(false);
            // A lövedék komponensének lekérése
            Projectile proj = projectile.GetComponent<Projectile>();
            // A lövedék sebzésének nullázása, hogy ne tartalmazzon érvényes sebzést a következő használat előtt
            proj.ProjectileDMG = 0f;
            // A lövedék jelölésének törlése
            proj.IsMarked = false;
            // Az esemény triggerelése, hogy jelezze a lövedék deaktiválódását
            OnProjectileDeactivated?.Invoke(projectile);
        }

    }
}
