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
    private List<JokerSpawner> jokerSpawners; // A manager-hez tartozó joker spawnerek listája.

    // public int numOfObstacleSpawners; // Ennyi obstacle-t fogunk elhelyezni a pályán.

    // System.Random használata Unity.Random helyett.
    private System.Random random = new System.Random();

    // Eventek
    public event Action OnLevelGenerationFinished;

    /// <summary>
    /// Új pálya generálása.
    /// </summary>
    /// <param name="spawnManagerData"></param>
    /// <returns></returns>
    public async Task<bool> NewLevelInit(SpawnManagerData spawnManagerData)
    {
        await Task.Yield();

        try
        {
            // Begyűjtük listákba a SpawnManager gyermekeit.
            enemySpawners = new List<EnemySpawner>(GetComponentsInChildren<EnemySpawner>());
            obstacleSpawners = new List<ObstacleSpawner>(GetComponentsInChildren<ObstacleSpawner>());
            playerSpawners = new List<PlayerSpawner>(GetComponentsInChildren<PlayerSpawner>());
            jokerSpawners = new List<JokerSpawner>(GetComponentsInChildren<JokerSpawner>());

            SpawnEntities(spawnManagerData.EnemySpawnData, spawnManagerData.PlayerPrefab, spawnManagerData.ObstaclePrefab, spawnManagerData.ActiveObstacleSpawners, 0); // A megadott paraméterek alapján végrehajtjuk a spawnokat.
            Cleanup(); // Kitöröljük a spawnereket, mivel már nincs szükség rájuk.

            OnLevelGenerationFinished?.Invoke(); // Event a sikeres generálásról

            // Destroy(gameObject); // Magát a SpawnManager-t is töröljük. // Ezt végezheti a LevelManager?

            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Hiba történt a {nameof(NewLevelInit)} metódusban: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Mentett pálya betöltése.
    /// </summary>
    /// <param name="loadData"></param>
    /// <returns></returns>
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

    /// <summary>
    /// Boss pálya inicializálása.
    /// </summary>
    /// <param name="bossLoadData"></param>
    /// <returns></returns>
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
    private void SpawnEntities(List<EnemyData.EnemySpawnInfo> enemyGroups, PlayerController playerPrefab, ObstacleController obstaclePrefab, int activeObstacleSpawners, int activeJokerSpawners)
    {
        // Végigiterálunk az enemyGroup listán, ez az ellenség spawnereket kezeli.
        foreach (var enemyGroup in enemyGroups)
        {
            int enemiesToSpawn = random.Next(enemyGroup.minNum, enemyGroup.maxNum) + 1; // Az enemyGroup objektumban megadott értékek alapján megadjuk a spawnolandó mennyiséget.
            int randomSpawnerIndex = random.Next(0, enemySpawners.Count); // Választunk egy random indexet a spawnerlistából.
            EnemySpawner selectedSpawner = enemySpawners[randomSpawnerIndex]; // Kiválasztjuk az adott indexen lévő spawnert a listából.

            selectedSpawner.enemy = enemyGroup.enemyPrefab; // Megadjuk a spawnernek a plan-ben szereplő ellenségtípust.
            selectedSpawner.numberOfSpawned = enemiesToSpawn; // Megadjuk a spawnernek a spawnolandó mennyiséget.
            selectedSpawner.Activate(); // Aktiváljuk a spawnert, elhelyezi a paraméterek alapján az ellenségeket.

            enemySpawners.RemoveAt(randomSpawnerIndex); // Használat után a spawnert töröljük a listából.
        }        

        // Az obstacle spawnereket kezeli, ez egyelőre egyszerűbb.
        for (int i = 0; i < activeObstacleSpawners; i++)
        {
            int randomObstacleIndex = random.Next(0, obstacleSpawners.Count); // Választunk egy random indexet a spawner listából.

            ObstacleSpawner selectedSpawner = obstacleSpawners[randomObstacleIndex]; // Kiválasztjuk az adott indexen lévő spawnert a listából.
            selectedSpawner.obstacle = obstaclePrefab; // Levelmanager által adott obstacle prefabot spawnoljuk.
            selectedSpawner.Place(); // Aktiváljuk a spawnert, elhelyez egy obstacle-t.

            obstacleSpawners.Remove(selectedSpawner); // Használat után a spawnert töröljük a listából.
        }

        // Joker spawnerek kezelése
        for (int i = 0; i < activeJokerSpawners; i++)
        {
            int randomJokerIndex = random.Next(0, jokerSpawners.Count); // Választunk egy random indexet a spawner listából.

            JokerSpawner selectedSpawner = jokerSpawners[randomJokerIndex]; // Kiválasztjuk az adott indexen lévő spawnert a listából.

            EnemyData.EnemySpawnInfo selectedEnemies = enemyGroups[random.Next(0, enemyGroups.Count)]; // Kiválasztunk egy random enemyGroup objektumot, amit átadunk a jokernek.
            bool isHeads = UnityEngine.Random.Range(0, 2) == 0; // Random bool generálás.
            // A random bool alapján eldől, hogy a joker obstacle-t, vagy enemy-t fog elhelyezni.
            if (isHeads)
            {
                selectedSpawner.SelectSpawner(isHeads, selectedEnemies, obstaclePrefab);
            }
            else
            {
                selectedSpawner.SelectSpawner(isHeads, selectedEnemies, obstaclePrefab);
            }

            jokerSpawners.Remove(selectedSpawner); // Használat után a spawnert töröljük a listából.
        }

        // A játékos elhelyezése a player spawnerek egyikén.
        int randomPlayerSpawnerIndex = random.Next(0, playerSpawners.Count); // Választunk egy random indexet a spawner listából.
        PlayerSpawner selectedPlayerSpawner = playerSpawners[randomPlayerSpawnerIndex]; // Kiválasztjuk az adott indexen lévő spawnert a listából.
        selectedPlayerSpawner.player = playerPrefab; // Inicializáljuk a spawnert a játékos prefabunkkal.
        selectedPlayerSpawner.Activate(); // Elhelyezzük a játékos karaktert a pályán.

        playerSpawners.Remove(selectedPlayerSpawner); // Használat után a spawnert töröljük a listából.
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
        foreach (JokerSpawner js in jokerSpawners)
        {
            Destroy(js.gameObject);
        }
    }
}