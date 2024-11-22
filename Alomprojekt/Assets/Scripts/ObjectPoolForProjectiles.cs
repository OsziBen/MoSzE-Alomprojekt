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


        /// <summary>
        /// Komponenesek
        /// </summary>


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
        /// </summary>
        private void Awake()
        {
            pool = new Queue<GameObject>(poolSize);

        }


        /// <summary>
        /// Inicializ�lja a l�ved�kek pool-j�t az�ltal, hogy el��ll�tja �s deaktiv�lja a sz�ks�ges sz�m� l�ved�ket.
        /// A l�ved�kek a poolba ker�lnek, �s k�szen �llnak a haszn�latra.
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
        /// Lek�r egy l�ved�ket a poolb�l, vagy l�trehoz egy �jat, ha nincs el�rhet� inakt�v l�ved�k.
        /// Be�ll�tja a l�ved�k poz�ci�j�t, forg�s�t �s a sebz�s �rt�k�t, majd aktiv�lja azt.
        /// </summary>
        /// <param name="position">A l�ved�k k�v�nt poz�ci�ja.</param>
        /// <param name="rotation">A l�ved�k k�v�nt forgat�sa (orient�ci�ja).</param>
        /// <param name="damageValue">A l�ved�k �ltal okozott sebz�s m�rt�ke.</param>
        /// <returns>A lek�rt vagy l�trehozott l�ved�k GameObject-je.</returns>
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
        /// Friss�ti a l�ved�k tulajdons�gait, p�ld�ul a sebz�s �rt�k�t.
        /// Be�ll�tja a l�ved�k sebz�s�t a megadott 'damageValue' alapj�n.
        /// </summary>
        /// <param name="projectile">A friss�tend� l�ved�k GameObject-je.</param>
        /// <param name="damageValue">A l�ved�k �j sebz�s �rt�ke.</param>
        private void UpdateProjectileProperties(GameObject projectile, float damageValue)
        {
            Projectile proj = projectile.GetComponent<Projectile>();
            proj.ProjectileDMG = damageValue;

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
            OnProjectileDeactivated?.Invoke(projectile);
        }

    }
}
