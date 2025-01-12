using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JokerSpawner : Assets.Scripts.SpawnerBase
{
    public EnemySpawner enemySpawner;
    public ObstacleSpawner obstacleSpawner;

    // System.Random használata Unity.Random helyett.
    private System.Random random = new System.Random();

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SelectSpawner(bool isHeads, EnemyData.EnemySpawnInfo enemy, ObstacleController obstacle)
    {
        if(isHeads)
        {
            var selectedSpawner = Instantiate(enemySpawner,transform.position,Quaternion.identity);
            selectedSpawner.numberOfSpawned = random.Next(enemy.minNum, System.Math.Clamp(enemy.maxNum + 1, 0, 3));
            selectedSpawner.enemy = enemy.enemyPrefab;
            selectedSpawner.Activate();

        } else
        {
            var selectedSpawner = Instantiate(obstacleSpawner, transform.position, Quaternion.identity);
            selectedSpawner.obstacle = obstacle;
            selectedSpawner.Place();
        }

        Destroy(gameObject);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.black;
        Gizmos.DrawSphere(transform.position, spawnRadius);
    }
}
