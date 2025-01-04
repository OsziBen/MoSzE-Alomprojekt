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
        /// V�ltoz�k
        /// </summary>
        public GameObject projectilePrefab; // A l�ved�k prefab, amelyet a poolba helyezett objektumok alapj�ul haszn�lunk.
        public int poolSize = 25;   // A pool maxim�lis m�rete, amely meghat�rozza, h�ny l�ved�k lehet egyszerre az objektumpoolban.
        private Queue<GameObject> pool; // A l�ved�kek t�rol�s�ra haszn�lt queue (sor), amely a rendelkez�sre �ll� �s visszaadott l�ved�keket tartalmazza.

        public int projectileMarkEveryNth = 4;  // n �rt�ke, ahol minden n-edik l�ved�ket megjel�lj�k; TODO:clamp 1-x
        private bool isMarkingEnabled;   // l�ved�kjel�l�s ki- �s bekapcsol�sa
        private int fireCounter = 0;    // l�ved�ksz�ml�l� a jel�l�shez


        /// <summary>
        /// Komponenesek
        /// </summary>
        private Camera mainCamera;

        /// <summary>
        /// Getterek �s Setterek
        /// </summary>
        public bool IsMarkingEnabled {
            get { return isMarkingEnabled; }
            set { isMarkingEnabled = value; }
        }

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
        private void Awake()
        {
            pool = new Queue<GameObject>(poolSize);

            //EnableMarking(true);
        }


        /// <summary>
        /// Inicializ�lja a l�ved�kek pool-j�t az�ltal, hogy el��ll�tja �s deaktiv�lja a sz�ks�ges sz�m� l�ved�ket.
        /// A l�ved�kek a poolba ker�lnek, �s k�szen �llnak a haszn�latra.
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
        /// Lek�r egy l�ved�ket a poolb�l, vagy l�trehoz egy �jat, ha nincs el�rhet� inakt�v l�ved�k.
        /// Be�ll�tja a l�ved�k poz�ci�j�t, forgat�s�t �s a sebz�s �rt�k�t, majd aktiv�lja azt.
        /// </summary>
        /// <param name="position">A l�ved�k k�v�nt poz�ci�ja.</param>
        /// <param name="rotation">A l�ved�k k�v�nt forgat�sa (orient�ci�ja).</param>
        /// <param name="damageValue">A l�ved�k �ltal okozott sebz�s m�rt�ke.</param>
        /// <param name="percentageDMGValue">A sebz�shez tartoz� sz�zal�kos �rt�k, amely esetleg m�dos�thatja a sebz�st.</param>
        /// <returns>A lek�rt vagy l�trehozott l�ved�k GameObject-je.</returns>
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
        /// Friss�ti a l�ved�k tulajdons�gait, p�ld�ul a sebz�s �rt�k�t.
        /// Be�ll�tja a l�ved�k sebz�s�t a megadott 'damageValue' �s 'percentageDMGValue' alapj�n.
        /// </summary>
        /// <param name="projectile">A friss�tend� l�ved�k GameObject-je.</param>
        /// <param name="damageValue">A l�ved�k �j sebz�s �rt�ke.</param>
        /// <param name="percentageDMGValue">A l�ved�khez tartoz� sz�zal�kos sebz�s �rt�k.</param>
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
        /// Be�ll�tja, hogy a l�ved�k jel�lve legyen-e, a 'fireCounter' �s a 'projectileMarkEveryNth' alapj�n.
        /// Ha a l�v�sek sz�ma el�ri a be�ll�tott intervallumot, a l�ved�k jel�lve lesz.
        /// </summary>
        /// <param name="projectile">A l�ved�k, amelynek a jel�l�s�t friss�tj�k.</param>
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
        /// Enged�lyezi vagy letiltja a l�ved�kek jel�l�s�t.
        /// Ha a jel�l�s le van tiltva, a l�v�sek sz�ml�l�ja (fireCounter) vissza�ll�t�dik.
        /// </summary>
        /// <param name="enable">Ha igaz (true), enged�lyezi a jel�l�st, ha hamis (false), letiltja azt.</param>
        public void EnableMarking(bool enable)
        {
            IsMarkingEnabled = enable;
            if (!isMarkingEnabled)
            {
                fireCounter = 0;
            }
        }


        /// <summary>
        /// Visszaad egy l�ved�ket a poolba: deaktiv�lja azt, �s null�zza a sebz�s �rt�k�t.
        /// Esem�nyt h�v, hogy jelezze a l�ved�k deaktiv�l�d�s�t.
        /// </summary>
        /// <param name="projectile">A visszaadott l�ved�k GameObject-je.</param>
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
