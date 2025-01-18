using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

// FONTOS: ObstacleController 34-es sora kikommentelése szükséges a teszt lefutásához.

public class ObstacleSpawnerTests
{
    private GameObject spawnerObject;
    private ObstacleSpawner obstacleSpawner;
    private StaticObstacleController obstaclePrefab1;
    private StaticObstacleController obstaclePrefab2;
    private List<StaticObstacleController> obstacles;
    private GameObject characterSetupManager;
    private string Obstacle1ID;
    private string Obstacle2ID;

    [SetUp]
    public void SetUp()
    {
        // CharacterSetupManager mockolás, szükséges a StaticObstacleController komponens működéséhez.
        characterSetupManager = new GameObject("CharacterSetupManager");
        characterSetupManager.AddComponent<CharacterSetupManager>();

        // Spawner mockolás
        spawnerObject = new GameObject("ObstacleSpawner");
        obstacleSpawner = spawnerObject.AddComponent<ObstacleSpawner>();

        // Obstacle mockolás
        GameObject obstaclePrefab1Object = new GameObject("Obstacle1");
        obstaclePrefab1 = obstaclePrefab1Object.AddComponent<StaticObstacleController>();
        Obstacle1ID = obstaclePrefab1.ID;
        obstaclePrefab1.tag = "Respawn"; // Default tagek használata az amúgy megegyező obstacle objektumok megkülönböztetéséhez.
        GameObject obstaclePrefab2Object = new GameObject("Obstacle2");
        obstaclePrefab2 = obstaclePrefab2Object.AddComponent<StaticObstacleController>();
        Obstacle2ID = obstaclePrefab2.ID;
        obstaclePrefab2.tag = "Finish"; // Default tagek használata az amúgy megegyező obstacle objektumok megkülönböztetéséhez.

        // Obstaclespawner lista mockolása, amiből a spawner választ.
        obstacles = new List<StaticObstacleController> { obstaclePrefab1, obstaclePrefab2 };
        obstacleSpawner.obstacles = obstacles;
    }

    /// <summary>
    /// Teszteli, hogy igaz bool esetén a megfelelő obstacle-t válassza a listából.
    /// </summary>
    [Test]
    public void Place_CorrectOnTrue()
    {
        bool isHeads = true;
        obstacleSpawner.Place(isHeads);

        // A lista első eleme Respawn taggel van ellátva. Mivel mockolásnál már létrejön mindkét objektum a teszttérben, ezért azt ellenőrizzük, a megfelelő típusból 2 van-e.
        var spawnedObstacle = GameObject.FindGameObjectsWithTag("Respawn");
        Assert.IsTrue(spawnedObstacle.Count() == 2);        
    }

    /// <summary>
    /// Teszteli, hogy hamis bool esetén a megfelelő obstacle-t válassza a listából.
    /// </summary>
    [Test]
    public void Place_CorrectOnFalse()
    {
        bool isHeads = false;
        obstacleSpawner.Place(isHeads);

        // A lista második eleme Finish taggel van ellátva. Mivel mockolásnál már létrejön mindkét objektum a teszttérben, ezért azt ellenőrizzük, a megfelelő típusból 2 van-e.
        var spawnedObstacle = GameObject.FindGameObjectsWithTag("Finish");
        Assert.IsTrue(spawnedObstacle.Count() == 2);
    }



    [TearDown]
    public void TearDown()
    {
        StaticObstacleController[] spawnedObstacles = Object.FindObjectsOfType<StaticObstacleController>();
        ObstacleSpawner[] listOfSpawners = Object.FindObjectsOfType<ObstacleSpawner>();

        foreach (ObstacleSpawner spawner in listOfSpawners)
        {
            Object.DestroyImmediate(spawner.gameObject);
        }

        foreach (StaticObstacleController obstacle in spawnedObstacles)
        {
            Object.DestroyImmediate(obstacle.gameObject);
        }

        Object.Destroy(characterSetupManager);
    }
}
