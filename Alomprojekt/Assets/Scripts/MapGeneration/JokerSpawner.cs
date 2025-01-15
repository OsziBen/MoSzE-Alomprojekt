using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JokerSpawner : MonoBehaviour
{
    public EnemySpawner enemySpawner;
    public ObstacleSpawner obstacleSpawner;

    // System.Random használata Unity.Random helyett.
    private System.Random random = new System.Random();

    /// <summary>
    /// Kiválassza, melyik spawnert helyezze le a maga helyére, majd futtassa le azt.
    /// </summary>
    /// <param name="enemy"></param>
    /// <param name="obstacles"></param>
    public void SelectSpawner(EnemyData.EnemySpawnInfo enemy, List<StaticObstacleController> obstacles)
    {
        bool isHeads = UnityEngine.Random.Range(0, 2) == 0; // Random bool generálás.
        // Random bool alapján eldöntjük, hogy enemy-t, vagy obstacle-t helyezünk le.
        if (isHeads)
        {
            var selectedSpawner = Instantiate(enemySpawner,transform.position,Quaternion.identity); // lehelyezzük az enemy spawnert a jokerspawner helyére
            selectedSpawner.numberOfSpawned = random.Next(enemy.minNum, System.Math.Clamp(enemy.maxNum + 1, 0, 3)); // az enemyspawninfo-ban átadott ellenségből max 3-at spawnolunk le.
            selectedSpawner.enemy = enemy.enemyPrefab; // Megadjuk a spawnernek az enemyspawninfo-ban szereplő ellenségtípust.
            selectedSpawner.Activate(); // aktiváljuk a lehelyezett enemy spawnert.

        } else
        {
            var selectedSpawner = Instantiate(obstacleSpawner, transform.position, Quaternion.identity); // lehelyezzük az obstacle spawnert a jokerspawner helyére
            selectedSpawner.obstacles = obstacles; // megadjuk a spawnernek a 2 lespawnolható obstacle-t.
            selectedSpawner.Place(); // aktiváljuk a lehelyezett obstacle spawnert.
        }

        Destroy(gameObject); // töröljük a spawnert
    }

    /// <summary>
    /// Az editorban való könnyű szerkesztéshez egy kört helyezünk el a
    /// spawnoló területre.
    /// </summary>
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.white;
        Gizmos.DrawSphere(transform.position, 5);
    }
}
