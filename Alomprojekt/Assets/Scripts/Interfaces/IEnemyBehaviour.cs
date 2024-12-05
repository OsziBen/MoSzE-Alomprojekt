using UnityEngine;

namespace Assets.Scripts
{
    public interface IEnemyBehaviour
    {
        void StartBehaviour(EnemyController enemyController);
        void ExecuteBehaviour(EnemyController enemyController);
        void StopBehaviour(EnemyController enemyController);
    }
}