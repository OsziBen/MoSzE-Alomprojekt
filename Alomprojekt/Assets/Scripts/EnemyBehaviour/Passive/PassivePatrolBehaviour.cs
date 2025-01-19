using Assets.Scripts;
using Assets.Scripts.EnemyBehaviours;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts
{
    public class PassivePatrolBehaviour : PassiveEnemyBehaviour
    {
        [Header("Behaviour Settings")]
        [SerializeField]
        private int numberOfWaypoints;
        [SerializeField]
        private Vector2 patrolOffset;
        [SerializeField]
        private float patrolSpeed = 2f;
        [SerializeField]
        private float stoppingDistance = 0.2f;

        private Vector2 patrolAreaMin;  // Bottom-left corner
        private Vector2 patrolAreaMax;  // Top-right corner
        private List<Vector2> waypoints = new List<Vector2>();
        private int currentWaypointIndex = 0;

        private Rigidbody2D rb;


        public override void StartBehaviour(EnemyController enemyController)
        {
            Debug.Log("PASSIVE START");
            rb = enemyController.rigidbody2d; // Haszn�ljuk a Rigidbody2D-t a mozg�shoz
            GeneratePatrolPoints();
        }

        public override void ExecuteBehaviour(EnemyController enemyController)
        {
            if (waypoints.Count == 0)
            {
                Debug.LogWarning("No patrol points generated!");
                return;
            }

            Vector2 targetPosition = waypoints[currentWaypointIndex];

            // Kisz�m�tjuk a k�v�nt ir�nyt a c�lpont fel�
            Vector2 direction = (targetPosition - rb.position).normalized;

            // L�p�sr�l l�p�sre mozgunk, hozz�adva a sebess�get �s a fizik�t
            rb.velocity = direction * patrolSpeed;

            // Ha el�rt�k a c�lpontot, l�pj�nk a k�vetkez�re
            if (Vector2.Distance(rb.position, targetPosition) <= stoppingDistance)
            {
                currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Count;
            }

            // Forgat�s: az ellens�g mindig a mozg�s ir�ny�ba n�zzen
            if (rb.velocity != Vector2.zero)
            {
                float angle = Mathf.Atan2(rb.velocity.y, rb.velocity.x) * Mathf.Rad2Deg;
                rb.rotation = angle - 90f; // A karakter helyes elforgat�sa a mozg�s ir�nya szerint
            }
        }

        public override void StopBehaviour(EnemyController enemyController)
        {
            Debug.Log("PASSIVE END");
            rb.velocity = Vector2.zero; // Ha le�ll a viselked�s, �ll�tsuk meg a mozg�st
        }

        void GeneratePatrolPoints()
        {
            waypoints.Clear();

            Vector2 enemyPosition = rb.position;
            waypoints.Add(enemyPosition);

            patrolAreaMin = new Vector2(enemyPosition.x - patrolOffset.x / 2, enemyPosition.y - patrolOffset.y / 2);
            patrolAreaMax = new Vector2(enemyPosition.x + patrolOffset.x / 2, enemyPosition.y + patrolOffset.y / 2);

            for (int i = 0; i < numberOfWaypoints - 1; i++)
            {
                float randomX = UnityEngine.Random.Range(patrolAreaMin.x, patrolAreaMax.x);
                float randomY = UnityEngine.Random.Range(patrolAreaMin.y, patrolAreaMax.y);
                Vector2 randomPoint = new Vector2(randomX, randomY);

                if (waypoints.Count > 0 && Vector2.Distance(waypoints[waypoints.Count - 1], randomPoint) < stoppingDistance)
                {
                    i--;
                    continue;
                }

                waypoints.Add(randomPoint);
            }

            Debug.Log($"Generated {waypoints.Count} patrol points.");
        }
    }
}
