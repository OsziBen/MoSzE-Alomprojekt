using UnityEngine;

namespace Assets.Scripts
{
    public interface IEnemyBehaviour
    {
        // A StartBehaviour metódus inicializálja az ellenség viselkedését.
        // Az EnemyController objektumot használ a viselkedés elindításához.
        void StartBehaviour(EnemyController enemyController);

        // Az ExecuteBehaviour metódus végrehajtja az ellenség viselkedését.
        // Az EnemyController objektumot használ a viselkedés folytatásához.
        void ExecuteBehaviour(EnemyController enemyController);

        // A StopBehaviour metódus leállítja az ellenség viselkedését.
        // Az EnemyController objektumot használ a viselkedés megszüntetéséhez.
        void StopBehaviour(EnemyController enemyController);
    }
}