using Assets.Scripts;
using Assets.Scripts.EnemyBehaviours;
using System;
using System.Buffers.Text;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;


namespace Assets.Scripts
{
    public class PassivePatrolBehaviour : PassiveEnemyBehaviour
    {

        public int numberOfWaypoints = 5;

        public Vector2 patrolOffset = new Vector2(3, 2);

        Vector2 patrolAreaMin;  // Bottom-left corner
        Vector2 patrolAreaMax;  // Top-right corner
        List<Vector2> waypoints = new List<Vector2>();
        int currentWaypointIndex = 0;
        public float patrolSpeed = 2f;
        public float stoppingDistance = 0.2f;

        Vector2 velocity = Vector2.zero;

        public override void StartBehaviour(EnemyController enemyController)
        {
            Debug.Log("PASSIVE START");
            GeneratePatrolPoints();
        }

        public override void ExecuteBehaviour(EnemyController enemyController)
        {
            //Debug.Log("PASSIVE");
            if (waypoints.Count == 0)
            {
                Debug.LogWarning("No patrol points generated!");
                return;
            }

            Vector2 targetPosition = waypoints[currentWaypointIndex];

            float smoothTime = 0.5f;

            enemyController.transform.position = Vector2.SmoothDamp(
                enemyController.transform.position,
                targetPosition,
                ref velocity,
                smoothTime,
                patrolSpeed,
                Time.deltaTime
                );

            /*
            float step = patrolSpeed * Time.deltaTime;
            enemyController.transform.position = Vector2.Lerp(enemyController.transform.position, targetPosition, step);
            */
            if (Vector2.Distance(enemyController.transform.position, targetPosition) <= stoppingDistance)
            {
                currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Count;
            }


        }


        public override void StopBehaviour(EnemyController enemyController)
        {
            Debug.Log("PASSIVE END");
        }


        void GeneratePatrolPoints()
        {
            waypoints.Clear();

            Vector2 enemyPosition = transform.position;
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
