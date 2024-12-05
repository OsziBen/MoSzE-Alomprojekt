using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static SpawnManager;

public class SpawnManager : MonoBehaviour
{
    private List<EnemySpawner> enemySpawners; // A manager-hez tartozó ellenség spawnerek listája.
    private List<ObstacleSpawner> obstacleSpawners; // A manager-hez tartozó obstacle spawnerek listája.
    private List<PlayerSpawner> playerSpawners; // A manager-hez tartozó player spawnerek listája.
    public List<EnemyController> spawnableEnemies; // A spawnolható ellenségek listája. GameStateManager-ig placeholder.

    public int numOfObstacleSpawners; // Ennyi obstacle-t fogunk elhelyezni a pályán.

    /// <summary>
    /// Itt adjuk meg a manager-nek, hogy melyik ellenségtípusból mennyit rakjon le. A GameStateManager-ig placeholder.
    /// </summary>
    public class SpawnPlan
    {
        public EnemyController enemyType;
        public int minCount;
        public int maxCount;

        // Konstruktor.
        public SpawnPlan(EnemyController enemyType, int minCount, int maxCount)
        {
            this.enemyType = enemyType;
            this.minCount = minCount;
            this.maxCount = maxCount;
        }
    }

    void Start()
    {
        // Begyűjtük listákba a SpawnManager gyermekeit.
        enemySpawners = new List<EnemySpawner>(GetComponentsInChildren<EnemySpawner>());
        obstacleSpawners = new List<ObstacleSpawner>(GetComponentsInChildren<ObstacleSpawner>());
        playerSpawners = new List<PlayerSpawner>(GetComponentsInChildren<PlayerSpawner>());

        // Megadjuk a spawnolandó ellenségeket, illetve számukat. A lista mérete egyben az aktiválandó spawnerek száma. GameStateManager-ig placeholder.
        List<SpawnPlan> enemySpawnPlans = new List<SpawnPlan>
        {
            new SpawnPlan(spawnableEnemies[0], 3, 5), // 3-5 közötti spawn az első típusból
            new SpawnPlan(spawnableEnemies[0], 2, 4), // 2-4 közötti spawn a második típusból
            new SpawnPlan(spawnableEnemies[0], 1, 3)  // 1-3 közötti spawn a harmadik típusból
        };

        SpawnEntities(enemySpawnPlans); // A megadott SpawnPlan lista alapján végrehajtjuk a spawnokat.
        Cleanup(); // Kitöröljük a spawnereket, mivel már nincs szükség rájuk.
        Destroy(gameObject); // Magát a SpawnManager-t is töröljük.
    }

    /// <summary>
    /// A spawnerek aktiválása.
    /// </summary>
    /// <param name="enemySpawnPlans">A megadott dictionary alapján spawnoljuk az ellenségeket.</param>
    private void SpawnEntities(List<SpawnPlan> enemySpawnPlans)
    {
        // Végigiterálunk a SpawnPlan listán, ez az ellenség spawnereket kezeli.
        foreach (var plan in enemySpawnPlans)
        {
            int enemiesToSpawn = Random.Range(plan.minCount, plan.maxCount + 1); // A plan-ben megadott értékek alapján megadjuk a spawnolandó mennyiséget.
            int randomSpawnerIndex = Random.Range(0, enemySpawners.Count); // Választunk egy random indexet a spawnerlistából.
            EnemySpawner selectedSpawner = enemySpawners[randomSpawnerIndex]; // Kiválasztjuk az adott indexen lévő spawnert a listából.

            selectedSpawner.enemy = plan.enemyType; // Megadjuk a spawnernek a plan-ben szereplő ellenségtípust.
            selectedSpawner.numberOfSpawned = enemiesToSpawn; // Megadjuk a spawnernek a spawnolandó mennyiséget.
            selectedSpawner.Activate(); // Aktiváljuk a spawnert, elhelyezi a paraméterek alapján az ellenségeket.

            enemySpawners.RemoveAt(randomSpawnerIndex); // Használat után a spawnert töröljük a listából.
        }

        // Az obstacle spawnereket kezeli, ez egyelőre egyszerűbb.
        for (int i = 0;i < numOfObstacleSpawners; i++)
        {
            int randomObstacleIndex = Random.Range(0, obstacleSpawners.Count); // Választunk egy random indexet a spawner listából.

            ObstacleSpawner selectedSpawner = obstacleSpawners[randomObstacleIndex]; // Kiválasztjuk az adott indexen lévő spawnert a listából.
            selectedSpawner.Place(); // Aktiváljuk a spawnert, elhelyez egy obstacle-t.

            obstacleSpawners.Remove(selectedSpawner); // Használat után a spawnert töröljük a listából.
        }

        // A játékos elhelyezése a player spawnerek egyikén.
        int randomPlayerSpawnerIndex = Random.Range(0, playerSpawners.Count);
        PlayerSpawner selectedPlayerSpawner = playerSpawners[randomPlayerSpawnerIndex];

        selectedPlayerSpawner.Activate();

        playerSpawners.Remove(selectedPlayerSpawner);
    }

    /// <summary>
    /// Töröljük a spawnereket, miután nincs rájük szükség.
    /// </summary>
    private void Cleanup()
    {
        foreach (ObstacleSpawner obs in obstacleSpawners)
        {
            Destroy(obs.gameObject);
        }
        foreach(EnemySpawner ens in enemySpawners)
        {
            Destroy(ens.gameObject);
        }
        foreach(PlayerSpawner pls in playerSpawners)
        {
            Destroy(pls.gameObject);
        }
    }
}
