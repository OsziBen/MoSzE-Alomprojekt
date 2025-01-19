using Assets.Scripts;
using Assets.Scripts.EnemyBehaviours;
using System;
using System.Buffers.Text;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

namespace Assets.Scripts
{
    public class HostileChaseBehaviour : HostileEnemyBehaviour
    {
        [Header("Behaviour Settings")]
        [SerializeField]
        // A távolság, amelynél az ellenség megáll, ha eléri a játékost
        private float stopDistance;
        [SerializeField]
        // A távolság, amelynél az ellenség túlhalad a játékoson, mielőtt megállna
        private float overshootDistance;
        [SerializeField]
        // Az idő, ameddig az ellenség újraorientálódik, miután túlhaladt
        private float reorientTime;
        [SerializeField]
        // Az időzítő, amely a túlhaladás után beállítja, mennyi ideig kell az ellenségnek reorientálódni
        private float overshootTimer;
        [SerializeField]
        // Az objektumok, amikkel az ellenség kölcsönhatásba lép
        private Rigidbody2D rb; // Az ellenség fizikai komponense (rigidbody), ami az irányításához szükséges
        private Transform playerTransform; // A játékos pozíciója, amit az ellenség követ
        private float movementSpeed; // Az ellenség mozgási sebessége

        // Az aktuális állapot, amely meghatározza, hogy az ellenség épp milyen viselkedést követ
        private BehaviourState currentState = BehaviourState.Chasing;
        private Vector2 lastDirection; // Az utolsó irány, amerre az ellenség mozgott

        // Az állapotok enumerációja, hogy az ellenség milyen fázisban van
        private enum BehaviourState
        {
            Chasing, // Az ellenség üldözi a játékost
            Overshooting, // Az ellenség túlhaladja a játékost egy előre beállított távolságig
            Reorienting // Az ellenség újraorientálódik, miután túlhaladt
        }

        /// <summary>
        /// Az ellenség viselkedésének elindításáért felelős metódus.
        /// </summary>
        /// <param name="enemyController">Az ellenség vezérlője, amely a fizikai és mozgási adatokat tartalmazza.</param>
        public override void StartBehaviour(EnemyController enemyController)
        {
            // A kezdő üzenet a viselkedés indításakor
            Debug.Log("HOSTILE START");

            // Az ellenség fizikai komponensének (rigidbody2d) beállítása
            rb = enemyController.rigidbody2d;

            // A játékos pozíciójának beállítása (először megtalálja a játékos vezérlőjét)
            playerTransform = FindObjectOfType<PlayerController>().GetComponent<Transform>();

            // Az ellenség mozgási sebességének beállítása
            movementSpeed = enemyController.CurrentMovementSpeed;
        }

        /// <summary>
        /// Az ellenség viselkedésének végrehajtása, attól függően, hogy éppen milyen állapotban van.
        /// </summary>
        /// <param name="enemyController">Az ellenség vezérlője, amely a fizikai és mozgási adatokat tartalmazza.</param>
        public override void ExecuteBehaviour(EnemyController enemyController)
        {
            // Ha a játékos pozíciója vagy az ellenség rigidbody-ja nem található, akkor kilép a függvényből
            if (playerTransform == null || rb == null)
                return;

            // Az aktuális viselkedési állapot ellenőrzése és a megfelelő metódus hívása
            switch (currentState)
            {
                case BehaviourState.Chasing:
                    // Ha az ellenség üldözi a játékost, meghívja az "ChasePlayer" metódust
                    ChasePlayer(enemyController);
                    break;
                case BehaviourState.Overshooting:
                    // Ha az ellenség túlhaladja a játékost, meghívja az "Overshoot" metódust
                    Overshoot(enemyController);
                    break;
                case BehaviourState.Reorienting:
                    // Ha az ellenség újraorientálódik, meghívja a "Reorient" metódust
                    Reorient();
                    break;
            }
        }

        /// <summary>
        /// Az ellenség viselkedésének leállítása.
        /// </summary>
        /// <param name="enemyController">Az ellenség vezérlője, amely a fizikai és mozgási adatokat tartalmazza.</param>
        public override void StopBehaviour(EnemyController enemyController)
        {
            // A viselkedés leállításakor egy üzenet kiírása a naplóba
            Debug.Log("HOSTILE END");
        }


        /// <summary>
        /// Az ellenség üldözési viselkedését végrehajtó metódus.
        /// </summary>
        /// <param name="enemyController">Az ellenség vezérlője, amely a fizikai és mozgási adatokat tartalmazza.</param>
        private void ChasePlayer(EnemyController enemyController)
        {
            // A távolság számítása az ellenség és a játékos között
            float distanceToPlayer = Vector2.Distance(rb.position, playerTransform.position);

            // Ha az ellenség elérte a játékost (a távolság kisebb vagy egyenlő, mint a stopDistance)
            if (distanceToPlayer <= stopDistance)
            {
                // Állapotváltás: Az ellenség mostantól túl fog haladni a játékoson
                currentState = BehaviourState.Overshooting;

                // Az irány beállítása, amerre az ellenség a játékos felé mozog
                lastDirection = ((Vector2)playerTransform.position - rb.position).normalized;

                // A túlhaladáshoz szükséges idő kiszámítása a távolság és a sebesség alapján
                overshootTimer = overshootDistance / movementSpeed;
            }
            else
            {
                // A játékos felé mutató irány kiszámítása
                Vector2 direction = ((Vector2)playerTransform.position - rb.position).normalized;

                // Mozgás és forgatás: Az ellenség a megfelelő irányba mozog és forog
                MoveAndRotate(enemyController, direction);
            }
        }


        /// <summary>
        /// Az ellenség túlhaladási viselkedését végrehajtó metódus.
        /// </summary>
        /// <param name="enemyController">Az ellenség vezérlője, amely a fizikai és mozgási adatokat tartalmazza.</param>
        private void Overshoot(EnemyController enemyController)
        {
            // Ha a túlhaladási időzítő még nem ért véget
            if (overshootTimer > 0)
            {
                // Az ellenség továbbra is a korábban kiszámított irányba mozog és forog
                MoveAndRotate(enemyController, lastDirection);

                // A túlhaladási idő csökkentése
                overshootTimer -= Time.fixedDeltaTime;
            }
            else
            {
                // Ha a túlhaladási idő lejárt, az állapotot átállítjuk "Reorienting"-ra (újraorientálódás)
                currentState = BehaviourState.Reorienting;

                // Az újraorientálódáshoz szükséges idő beállítása
                overshootTimer = reorientTime;
            }
        }


        /// <summary>
        /// Az ellenség újraorientálódási viselkedését végrehajtó metódus.
        /// </summary>
        private void Reorient()
        {
            // Ha az újraorientálódáshoz szükséges idő még nem telt le
            if (overshootTimer > 0)
            {
                // Csökkenti az időzítőt az időeltolódás függvényében
                overshootTimer -= Time.fixedDeltaTime;
            }
            else
            {
                // Ha az időzítő lejárt, visszaállítja az állapotot "Chasing"-ra (üldözés)
                currentState = BehaviourState.Chasing;
            }
        }


        /// <summary>
        /// Az ellenség mozgását és forgatását végrehajtó metódus.
        /// </summary>
        /// <param name="enemyController">Az ellenség vezérlője, amely a fizikai és mozgási adatokat tartalmazza.</param>
        /// <param name="direction">A kívánt mozgási irány, amely a játékos irányába mutat.</param>
        private void MoveAndRotate(EnemyController enemyController, Vector2 direction)
        {
            // Ha a mozgási irány nem nulla vektor (vagyis van irány, amerre mozogni kell)
            if (direction != Vector2.zero)
            {
                // Az ellenség pozíciójának frissítése a mozgási sebesség és az irány alapján
                rb.MovePosition(rb.position + direction * movementSpeed * Time.fixedDeltaTime);

                // Az ellenség forgatása az irány alapján
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg; // Az irány szögének kiszámítása
                enemyController.transform.rotation = Quaternion.Euler(0f, 0f, angle - 90f); // Az ellenség forgatása, hogy a megfelelő irányba nézzen
            }
            else
            {
                // Ha nincs mozgás (az irány nulla), akkor az ellenség nem forog, és alaphelyzetbe áll a forgatás
                enemyController.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
            }
        }
    }
}
