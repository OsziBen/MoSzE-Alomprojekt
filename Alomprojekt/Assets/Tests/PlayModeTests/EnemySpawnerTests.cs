using NUnit.Framework;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class EnemySpawnerTests
{
    private GameObject spawnerObject;
    private EnemySpawner enemySpawner;
    private EnemyController enemyPrefab;
    private int level = 1;

    [SetUp]
    public void SetUp()
    {
        // Spawner mockolás
        spawnerObject = new GameObject("EnemySpawner");
        enemySpawner = spawnerObject.AddComponent<EnemySpawner>();

        // Enemy mockolás
        GameObject enemyPrefabObject = new GameObject("Enemy");
        enemyPrefab = enemyPrefabObject.AddComponent<EnemyController>();
        enemySpawner.enemy = enemyPrefab;

        // Spawner paramétereinek beállítása
        enemySpawner.spawnRadius = 5f;
        enemySpawner.numberOfSpawned = 40;
    }

    /// <summary>
    /// Teszteli, hogy tényleg annyi enemy-t spawnolunk-e, ami paraméterként meg van adva.
    /// </summary>
    [Test]
    public void Activate_SpawnsCorrectNumberOfEnemies()
    {
        enemySpawner.Activate(level);

        EnemyController[] spawnedEnemies = Object.FindObjectsOfType<EnemyController>();
        Assert.AreEqual(enemySpawner.numberOfSpawned, spawnedEnemies.Length);
    }

    /// <summary>
    /// Teszteli, hogy az összes lespawnolt ellenség a spawnkör sugarán belül spawnol-e.
    /// </summary>
    [Test]
    public void Activate_SpawnsEnemyWithinRadius()
    {
        Vector3 spawnCenter = spawnerObject.transform.position;
        enemySpawner.Activate(level);

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
