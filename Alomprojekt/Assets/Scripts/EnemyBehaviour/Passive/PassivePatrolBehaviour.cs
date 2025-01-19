using Assets.Scripts;
using Assets.Scripts.EnemyBehaviours;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts
{
    // A PassivePatrolBehaviour osztály örökli a PassiveEnemyBehaviour-t, 
    // tehát egy passzív ellenséges viselkedést képvisel, amely a térképen végez járőrözést.
    public class PassivePatrolBehaviour : PassiveEnemyBehaviour
    {
        [Header("Behaviour Settings")]
        // A "Behaviour Settings" fejléccel ellátott beállítások a viselkedéshez tartoznak.
        [SerializeField]
        // A szám, hogy hány különböző célpontot (waypointot) kell bejárnia az ellenségnek.
        private int numberOfWaypoints;
        [SerializeField]
        // A járőrözési terület eltolása, amely meghatározza, hogy a célpontok hol helyezkednek el a játékterületen.
        private Vector2 patrolOffset;
        [SerializeField]
        // A járőrözés sebessége, meghatározza, hogy milyen gyorsan mozogjon az ellenség a célpontok között.
        private float patrolSpeed = 2f;
        [SerializeField]
        // A távolság, amelynél az ellenség megáll, miután elérte a célpontot.
        private float stoppingDistance = 0.2f;

        // A járőrözési terület minimális és maximális koordinátái, amelyek a pályát határozzák meg.
        private Vector2 patrolAreaMin;  // Bal alsó sarok
        private Vector2 patrolAreaMax;  // Jobb felső sarok
        // A waypointek listája, amely tartalmazza a járőrözéshez szükséges célpontokat.
        private List<Vector2> waypoints = new List<Vector2>();
        // Az aktuális célpont indexe, amelyet az ellenség követ.
        private int currentWaypointIndex = 0;

        // Az ellenség 2D fizikáját kezelő Rigidbody2D komponens, amely szükséges a mozgás vezérléséhez.
        private Rigidbody2D rb;

        /// <summary>
        /// A PassivePatrolBehaviour viselkedés indítása, amikor az ellenség viselkedése elindul.
        /// </summary>
        /// <param name="enemyController">Az ellenséget vezérlő EnemyController referencia.</param>
        public override void StartBehaviour(EnemyController enemyController)
        {
            Debug.Log("PASSIVE START");
            // Kiírja a logba, hogy a passzív viselkedés elindult.

            rb = enemyController.rigidbody2d;
            // A Rigidbody2D referencia mentése az EnemyController-ből, hogy mozgassuk az ellenséget.

            GeneratePatrolPoints();
            // Meghívja a GeneratePatrolPoints() metódust, hogy előállítsa a járőrözési pontokat a járőrözési területen.
        }

        /// <summary>
        /// A PassivePatrolBehaviour végrehajtja a járőrözési viselkedést minden egyes frissítéskor.
        /// </summary>
        /// <param name="enemyController">Az ellenséget vezérlő EnemyController referencia.</param>
        public override void ExecuteBehaviour(EnemyController enemyController)
        {
            if (waypoints.Count == 0)
            {
                Debug.LogWarning("No patrol points generated!");
                // Ha nincs generálva járőrözési pont, figyelmeztető üzenet kerül a logba.
                return;
            }

            Vector2 targetPosition = waypoints[currentWaypointIndex];
            // Az aktuális célpont pozíciójának meghatározása a waypoints listából.

            // Kiszámítjuk a kívánt irányt a célpont felé
            Vector2 direction = (targetPosition - rb.position).normalized;
            // Az irányvektor kiszámítása a célpont és az aktuális pozíció közötti különbség alapján.

            // Lépésrõl lépésre mozgunk, hozzáadva a sebességet és a fizikát
            rb.velocity = direction * patrolSpeed;
            // A Rigidbody2D sebességét a kívánt irány és a járőrözési sebesség szorzataként állítjuk be.

            // Ha elértük a célpontot, lépjünk a következõre
            if (Vector2.Distance(rb.position, targetPosition) <= stoppingDistance)
            {
                currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Count;
                // Ha a távolság a célponttól kisebb vagy egyenlő, mint a stoppingDistance, 
                // akkor váltunk a következő célpontra.
                // A mod operátor biztosítja, hogy a célpontok ciklikusan ismétlődjenek.
            }

            // Forgatás: az ellenség mindig a mozgás irányába nézzen
            if (rb.velocity != Vector2.zero)
            {
                float angle = Mathf.Atan2(rb.velocity.y, rb.velocity.x) * Mathf.Rad2Deg;
                // Az irányból szöget számolunk a mozgás alapján.

                rb.rotation = angle - 90f;
                // Az ellenség forgatásának beállítása, hogy a mozgás irányába nézzen. 
                // A -90f korrekció a sprite orientációjához szükséges.
            }
        }

        /// <summary>
        /// A PassivePatrolBehaviour leállítása, amikor az ellenség viselkedését meg kell szakítani.
        /// </summary>
        /// <param name="enemyController">Az ellenséget vezérlő EnemyController referencia.</param>
        public override void StopBehaviour(EnemyController enemyController)
        {
            Debug.Log("PASSIVE END");
            // Kiírja a logba, hogy a passzív viselkedés leállt.

            rb.velocity = Vector2.zero;
            // Ha leáll a viselkedés, a mozgás leállítása. A Rigidbody2D sebességét nullára állítjuk.
        }

        /// <summary>
        /// A járőrözési pontok generálása, amelyek a passzív ellenség számára meghatározzák az útvonalat.
        /// </summary>
        void GeneratePatrolPoints()
        {
            waypoints.Clear();
            // Az előzőleg generált waypointokat töröljük.

            Vector2 enemyPosition = rb.position;
            waypoints.Add(enemyPosition);
            // Az ellenség aktuális pozícióját hozzáadjuk a waypoints listához, mint első célpont.

            patrolAreaMin = new Vector2(enemyPosition.x - patrolOffset.x / 2, enemyPosition.y - patrolOffset.y / 2);
            patrolAreaMax = new Vector2(enemyPosition.x + patrolOffset.x / 2, enemyPosition.y + patrolOffset.y / 2);
            // A járőrözési terület bal alsó és jobb felső sarkainak meghatározása az eltolás figyelembevételével.

            for (int i = 0; i < numberOfWaypoints - 1; i++)
            {
                float randomX = UnityEngine.Random.Range(patrolAreaMin.x, patrolAreaMax.x);
                float randomY = UnityEngine.Random.Range(patrolAreaMin.y, patrolAreaMax.y);
                Vector2 randomPoint = new Vector2(randomX, randomY);
                // Véletlenszerűen generált pontok a járőrözési területen belül.

                if (waypoints.Count > 0 && Vector2.Distance(waypoints[waypoints.Count - 1], randomPoint) < stoppingDistance)
                {
                    i--;
                    continue;
                }
                // Ha az új véletlenszerűen generált pont túl közel van az előzőhöz, akkor új pontot generálunk.
                // Az i-- biztosítja, hogy a ciklus nem lépjen előre, hanem próbáljon új véletlen pontot találni.

                waypoints.Add(randomPoint);
                // Hozzáadjuk az új pontot a waypoints listához.
            }

            Debug.Log($"Generated {waypoints.Count} patrol points.");
            // A logba kiírjuk, hogy hány járőrözési pontot generáltunk.
        }

    }
}
