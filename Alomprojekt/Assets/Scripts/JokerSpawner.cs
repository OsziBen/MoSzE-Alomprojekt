using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JokerSpawner : Assets.Scripts.SpawnerBase
{
    public GameObject obstacle;
    public EnemyController enemy;

    // Start is called before the first frame update
    void Start()
    {
        //for (int i = 0; i < numberOfSpawned; i++)
        //{
        //    Place();
        //}
        //Destroy(gameObject);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public override void Place()
    {
        bool isHeads = Random.Range(0, 2) == 0;
        base.Place();
        if(isHeads)
        {
            Instantiate(obstacle, spawnPosition, Quaternion.identity);
        } 
        else
        {
            Instantiate(enemy, spawnPosition, Quaternion.identity);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.black;
        Gizmos.DrawSphere(transform.position, spawnRadius);
    }
}
