using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public EnemyController enemy; // A spawnolandó enemy object.

    public float spawnRadius; // A kör, amelyben a spawner objektumokat helyezhet le.
    public int numberOfSpawned; // A spawnolandó objektumok száma.
    protected Vector2 randomPosition; // A spawn körön belül felvett random pozíció.
    protected Vector2 spawnPosition; // Az előző random pozíció offsetelése a spawner pozíciójával.

    /// <summary>
    /// Mivel enemyből többet is elhelyezünk, egy külön, SpawnManager által meghívható
    /// függvényben annyiszor hívjuk meg a Place() függvényt, ahány enemy-t szeretnénk
    /// spawnolni.
    /// </summary>
    public void Activate()
    {
        for(int i = 0; i < numberOfSpawned; i++)
        {
            Place();
        }
        Destroy(gameObject);
    }

    /// <summary>
    /// Enemy elhelyezése a spawnradius sugarú körön belüli random pontra.
    /// </summary>
    public void Place()
    {
        randomPosition = Random.insideUnitCircle * spawnRadius; // spawnRadius sugarú körben kiválaszt egy random koordinátát
        spawnPosition = new Vector2(randomPosition.x, randomPosition.y) + (Vector2)transform.position; // a random koordinátához hozzáadjuk a spawner koordinátáit
        
        Instantiate(enemy, spawnPosition, Quaternion.identity);
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
