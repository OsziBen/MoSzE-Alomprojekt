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
    public void Activate(int level)
    {
        for(int i = 1; i < numberOfSpawned; i++)
        {
            PlaceEnemy(level);
        }
        Destroy(gameObject);
    }

    /// <summary>
    /// A SpawnerBase osztály Place() függvényének kiegészítése
    /// a spawnerhez specifikus gameobject instanciálásával.
    /// </summary>
    public void PlaceEnemy(int level)
    {
        base.Place();
        EnemyController spawnedEnemy = Instantiate(enemy, spawnPosition, Quaternion.identity);
        // spawnedEnemy.SetCurrentEnemyStatsByLevel(int level) // spawnolt enemy statjainak skálázása szint szerint
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
