using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

// FONTOS: EnemyController 330-as sor�t ki kell kommentelni, hogy a teszt lefusson.

public class EnemySpawnerTests
{
    private GameObject spawnerObject;
    private EnemySpawner enemySpawner;
    private EnemyController enemyPrefab;

    [SetUp]
    public void SetUp()
    {
        // Spawner mockol�s
        spawnerObject = new GameObject("EnemySpawner");
        enemySpawner = spawnerObject.AddComponent<EnemySpawner>();

        // Enemy mockol�s
        GameObject enemyPrefabObject = new GameObject("Enemy");
        enemyPrefab = enemyPrefabObject.AddComponent<EnemyController>();
        enemySpawner.enemy = enemyPrefab;

        // Spawner param�tereinek be�ll�t�sa
        enemySpawner.spawnRadius = 5f;
        enemySpawner.numberOfSpawned = 40;
    }

    /// <summary>
    /// Teszteli, hogy t�nyleg annyi enemy-t spawnolunk-e, ami param�terk�nt meg van adva.
    /// </summary>
    [Test]
    public void Activate_SpawnsCorrectNumberOfEnemies()
    {
        enemySpawner.Activate();

        EnemyController[] spawnedEnemies = Object.FindObjectsOfType<EnemyController>();
        Assert.AreEqual(enemySpawner.numberOfSpawned, spawnedEnemies.Length-1); // -1, mert a tesztk�rnyezetben kezdetben van m�r egy
    }

    /// <summary>
    /// Teszteli, hogy az �sszes lespawnolt ellens�g a spawnk�r sugar�n bel�l spawnol-e.
    /// </summary>
    [Test]
    public void Activate_SpawnsEnemyWithinRadius()
    {
        Vector3 spawnCenter = spawnerObject.transform.position;
        enemySpawner.Activate();

        EnemyController[] spawnedEnemies = Object.FindObjectsOfType<EnemyController>();
        foreach(EnemyController enemy in spawnedEnemies)
        {
            float distance = Vector3.Distance(spawnCenter, enemy.transform.position);
            Assert.LessOrEqual(distance, enemySpawner.spawnRadius);
        }
    }

    [TearDown]
    public void TearDown()
    {
        EnemyController[] spawnedEnemies = Object.FindObjectsOfType<EnemyController>();
        EnemySpawner[] listOfSpawners = Object.FindObjectsOfType<EnemySpawner>();

        foreach (EnemySpawner spawner in listOfSpawners)
        {
            Object.DestroyImmediate(spawner.gameObject);
        }

        foreach (EnemyController enemy in spawnedEnemies)
        {
            Object.DestroyImmediate(enemy.gameObject);
        }
    }
}
