﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : Assets.Scripts.SpawnerBase
{
    public EnemyController enemy;

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
