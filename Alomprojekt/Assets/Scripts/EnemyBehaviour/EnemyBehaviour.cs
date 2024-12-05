using Assets.Scripts;
using System;
using System.Buffers.Text;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

namespace Assets.Scripts.EnemyBehaviours
{
    public abstract class EnemyBehaviour : MonoBehaviour, IEnemyBehaviour
    {
        public abstract void ExecuteBehaviour(EnemyController enemyController);

        public abstract void StartBehaviour(EnemyController enemyController);

        public abstract void StopBehaviour(EnemyController enemyController);
    }
}
