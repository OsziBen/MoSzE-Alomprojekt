using System.Collections;
using UnityEngine;

namespace Assets.Scripts
{
    /// <summary>
    /// A spawner objektumokt közös részeit foglalja magába.
    /// </summary>
    public abstract class SpawnerBase : MonoBehaviour
    {
        public float spawnRadius; // A kör, amelyben a spawner objektumokat helyezhet le.
        public int numberOfSpawned; // A spawnolandó objektumok száma.

        protected Vector2 randomPosition; // A spawn körön belül felvett random pozíció.
        protected Vector2 spawnPosition; // Az előző random pozíció offsetelése a spawner pozíciójával.

        /// <summary>
        /// Inicializálja a randomPosition és spawnPosition változókat.
        /// Leszármazott osztályokban található az objektum instanciálás.
        /// </summary>
        public virtual void Place()
        {
            randomPosition = Random.insideUnitCircle * spawnRadius; // spawnRadius sugarú körben kiválaszt egy random koordinátát

            spawnPosition = new Vector2(randomPosition.x, randomPosition.y) + (Vector2)transform.position; // a random koordinátához hozzáadjuk a spawner koordinátáit
        }
    }
}