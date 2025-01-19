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
        private float stopDistance;
        [SerializeField]
        private float overshootDistance;
        [SerializeField]
        private float reorientTime;
        [SerializeField]
        private float overshootTimer;
        [SerializeField]



        private Rigidbody2D rb;
        private Transform playerTransform;
        private float movementSpeed;

        private BehaviourState currentState = BehaviourState.Chasing;
        private Vector2 lastDirection;

        private enum BehaviourState
        {
            Chasing,
            Overshooting,
            Reorienting
        }


        public override void StartBehaviour(EnemyController enemyController)
        {
            Debug.Log("HOSTILE START");
            rb = enemyController.rigidbody2d;
            playerTransform = FindObjectOfType<PlayerController>().GetComponent<Transform>();
            movementSpeed = enemyController.CurrentMovementSpeed;
        }
        public override void ExecuteBehaviour(EnemyController enemyController)
        {
            if (playerTransform == null || rb == null)
                return;

            switch (currentState)
            {
                case BehaviourState.Chasing:
                    ChasePlayer(enemyController);
                    break;
                case BehaviourState.Overshooting:
                    Overshoot(enemyController);
                    break;
                case BehaviourState.Reorienting:
                    Reorient();
                    break;
            }

        }

        public override void StopBehaviour(EnemyController enemyController)
        {
            Debug.Log("HOSTILE END");
        }



        private void ChasePlayer(EnemyController enemyController)
        {
            float distanceToPlayer = Vector2.Distance(rb.position, playerTransform.position);

            if (distanceToPlayer <= stopDistance)
            {
                currentState = BehaviourState.Overshooting;
                lastDirection = ((Vector2)playerTransform.position - rb.position).normalized;
                overshootTimer = overshootDistance / movementSpeed;
            }
            else
            {
                Vector2 direction = ((Vector2)playerTransform.position - rb.position).normalized;

                // Mozgás és forgatás
                MoveAndRotate(enemyController, direction);
            }
        }


        private void Overshoot(EnemyController enemyController)
        {
            if (overshootTimer > 0)
            {
                MoveAndRotate(enemyController, lastDirection);
                overshootTimer -= Time.fixedDeltaTime;
            }
            else
            {
                currentState = BehaviourState.Reorienting;
                overshootTimer = reorientTime;
            }
        }


        private void Reorient()
        {
            if (overshootTimer > 0)
            {
                overshootTimer -= Time.fixedDeltaTime;
            }
            else
            {
                currentState = BehaviourState.Chasing;
            }
        }


        private void MoveAndRotate(EnemyController enemyController, Vector2 direction)
        {
            if (direction != Vector2.zero)
            {
                rb.MovePosition(rb.position + direction * movementSpeed * Time.fixedDeltaTime);

                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                enemyController.transform.rotation = Quaternion.Euler(0f, 0f, angle - 90f);
            }
            else
            {
                enemyController.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
            }
        }
    }
}
