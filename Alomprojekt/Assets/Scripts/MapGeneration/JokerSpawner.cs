using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JokerSpawner : Assets.Scripts.SpawnerBase
{
    public EnemySpawner enemySpawner;
    public ObstacleSpawner obstacleSpawner;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SelectSpawner()
    {
        bool isHeads = Random.Range(0, 2) == 0;
        if(isHeads)
        {
            Instantiate(enemySpawner,transform.position,Quaternion.identity);
        } else
        {
            Instantiate(obstacleSpawner, transform.position, Quaternion.identity);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.black;
        Gizmos.DrawSphere(transform.position, spawnRadius);
    }
}
