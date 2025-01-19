using UnityEngine;

namespace Assets.Scripts
{
    public interface IEnemyBehaviour
    {
        // A StartBehaviour met�dus inicializ�lja az ellens�g viselked�s�t.
        // Az EnemyController objektumot haszn�l a viselked�s elind�t�s�hoz.
        void StartBehaviour(EnemyController enemyController);

        // Az ExecuteBehaviour met�dus v�grehajtja az ellens�g viselked�s�t.
        // Az EnemyController objektumot haszn�l a viselked�s folytat�s�hoz.
        void ExecuteBehaviour(EnemyController enemyController);

        // A StopBehaviour met�dus le�ll�tja az ellens�g viselked�s�t.
        // Az EnemyController objektumot haszn�l a viselked�s megsz�ntet�s�hez.
        void StopBehaviour(EnemyController enemyController);
    }
}