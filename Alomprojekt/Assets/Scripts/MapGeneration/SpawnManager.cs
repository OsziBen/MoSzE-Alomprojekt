using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using static LevelManager;
using static SpawnManager;

public class SpawnManager : MonoBehaviour
{
    private List<EnemySpawner> enemySpawners; // A manager-hez tartozó ellenség spawnerek listája.
    private List<ObstacleSpawner> obstacleSpawners; // A manager-hez tartozó obstacle spawnerek listája.
    private List<PlayerSpawner> playerSpawners; // A manager-hez tartozó player spawnerek listája.

    public int numOfObstacleSpawners; // Ennyi obstacle-t fogunk elhelyezni a pályán.

    // System.Random használata Unity.Random helyett.
    private System.Random random = new System.Random();

    // Eventek
    public event Action OnLevelGenerationFinished;

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

    public async Task<bool> newLevelInit(List<EnemyData.EnemySpawnInfo> enemies, PlayerController playerPrefab, ObstacleController obstaclePrefab)
    {
        await Task.Yield();

        try
        {
            // Begyűjtük listákba a SpawnManager gyermekeit.
            enemySpawners = new List<EnemySpawner>(GetComponentsInChildren<EnemySpawner>());
            obstacleSpawners = new List<ObstacleSpawner>(GetComponentsInChildren<ObstacleSpawner>());
            playerSpawners = new List<PlayerSpawner>(GetComponentsInChildren<PlayerSpawner>());

            SpawnEntities(enemies, playerPrefab, obstaclePrefab); // A megadott SpawnPlan lista alapján végrehajtjuk a spawnokat.
            Cleanup(); // Kitöröljük a spawnereket, mivel már nincs szükség rájuk.

            OnLevelGenerationFinished?.Invoke(); // Event a sikeres generálásról

            // Destroy(gameObject); // Magát a SpawnManager-t is töröljük. // Ezt végezheti a LevelManager?

            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Hiba történt a {nameof(newLevelInit)} metódusban: {ex.Message}");
            return false;
        }
    }

    //TryGetComponent

    public async Task<bool> loadedLevelInit(List<GameObjectPosition> loadData)
    {
        await Task.Yield();

        try
        {
            // gizmo törlése
            // tényleges működés
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Hiba történt a {nameof(loadedLevelInit)} metódusban: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> bossLevelInit(List<GameObjectPosition> bossLoadData) // kételemű lista
    {
        await Task.Yield();

        try
        {
            // gizmo törlése
            // lehelyezés fix koordinátára
            // tényleges működés
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Hiba történt a {nameof(bossLevelInit)} metódusban: {ex.Message}");
            return false;
        }
    }


    /// <summary>
    /// A spawnerek aktiválása.
    /// </summary>
    /// <param name="enemySpawnPlans">A megadott dictionary alapján spawnoljuk az ellenségeket.</param>
    private void SpawnEntities(List<EnemyData.EnemySpawnInfo> enemyGroups, PlayerController playerPrefab, ObstacleController obstaclePrefab)
    {
        // Végigiterálunk a SpawnPlan listán, ez az ellenség spawnereket kezeli.
        foreach (var enemyGroup in enemyGroups)
        {
            int enemiesToSpawn = random.Next(enemyGroup.minNum, enemyGroup.maxNum + 1); // A plan-ben megadott értékek alapján megadjuk a spawnolandó mennyiséget.
            int randomSpawnerIndex = random.Next(0, enemySpawners.Count); // Választunk egy random indexet a spawnerlistából.
            EnemySpawner selectedSpawner = enemySpawners[randomSpawnerIndex]; // Kiválasztjuk az adott indexen lévő spawnert a listából.

            selectedSpawner.enemy = enemyGroup.enemyPrefab; // Megadjuk a spawnernek a plan-ben szereplő ellenségtípust.
            selectedSpawner.numberOfSpawned = enemiesToSpawn; // Megadjuk a spawnernek a spawnolandó mennyiséget.
            selectedSpawner.Activate(); // Aktiváljuk a spawnert, elhelyezi a paraméterek alapján az ellenségeket.

            enemySpawners.RemoveAt(randomSpawnerIndex); // Használat után a spawnert töröljük a listából.
        }

        // Az obstacle spawnereket kezeli, ez egyelőre egyszerűbb.
        for (int i = 0; i < numOfObstacleSpawners; i++)
        {
            int randomObstacleIndex = random.Next(0, obstacleSpawners.Count); // Választunk egy random indexet a spawner listából.

            ObstacleSpawner selectedSpawner = obstacleSpawners[randomObstacleIndex]; // Kiválasztjuk az adott indexen lévő spawnert a listából.
            selectedSpawner.obstacle = obstaclePrefab; // Levelmanager által adott obstacle prefabot spawnoljuk.
            selectedSpawner.Place(); // Aktiváljuk a spawnert, elhelyez egy obstacle-t.

            obstacleSpawners.Remove(selectedSpawner); // Használat után a spawnert töröljük a listából.
        }

        // A játékos elhelyezése a player spawnerek egyikén.
        int randomPlayerSpawnerIndex = random.Next(0, playerSpawners.Count);
        PlayerSpawner selectedPlayerSpawner = playerSpawners[randomPlayerSpawnerIndex];
        selectedPlayerSpawner.player = playerPrefab;
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
        foreach (EnemySpawner ens in enemySpawners)
        {
            Destroy(ens.gameObject);
        }
        foreach (PlayerSpawner pls in playerSpawners)
        {
            Destroy(pls.gameObject);
        }
    }
}