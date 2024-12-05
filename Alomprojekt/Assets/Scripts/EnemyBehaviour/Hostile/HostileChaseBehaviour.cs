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

        public override void StartBehaviour(EnemyController enemyController)
        {
            Debug.Log("HOSTILE START");
        }
        public override void ExecuteBehaviour(EnemyController enemyController)
        {
            Debug.Log("HOSTILE");
        }

        public override void StopBehaviour(EnemyController enemyController)
        {
            Debug.Log("HOSTILE END");
        }
    }
}
