using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : Assets.Scripts.SpawnerBase
{
    // Start is called before the first frame update

    public EnemyController enemy;
      

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    /// <summary>
    /// Mivel enemyből többet is elhelyezünk, egy külön, SpawnManager által meghívható
    /// függvényben annyiszor hívjuk meg a PlaceEnemy() függvényt, ahány enemy-t szeretnénk
    /// spawnolni.
    /// </summary>
    public void Activate()
    {
        for(int i = 1; i < numberOfSpawned; i++)
        {
            Place();
        }
        Destroy(gameObject);
    }

    /// <summary>
    /// A SpawnerBase osztály Place() függvényének kiegészítése
    /// a spawnerhez specifikus gameobject instanciálásával.
    /// </summary>
    public override void Place()
    {
        base.Place();
        EnemyController spawnedEnemy = Instantiate(enemy, spawnPosition, Quaternion.identity);
    }

    /// <summary>
    /// Az editorban való könnyű szerkesztéshez egy kört helyezünk el a
    /// spawnoló területre.
    /// </summary>
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(transform.position, spawnRadius);
    }
}
