﻿using Assets.Scripts;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Pool;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.TextCore.Text;
using static GameStateManager;
using static SpawnZoneData;
using static UnityEngine.EventSystems.EventTrigger;

public class LevelManager : BasePersistentManager<LevelManager>
{
    /// <summary>
    /// Változók
    /// </summary>
    public class GameObjectPosition
    {
        public GameObject gameObject;
        public Vector2 position;

        public GameObjectPosition(GameObject gameObject, Vector2 position)
        {
            this.gameObject = gameObject;
            this.position = position;
        }
    }

    List<LevelData> allLevels = new List<LevelData>();

    LevelData currentLevel;

    List<EnemyData.EnemySpawnInfo> spawnManagerData = new List<EnemyData.EnemySpawnInfo>();

    private bool isLoadSuccessful;

    System.Random rand = new System.Random();

    // szint vége ellenőrzés
    List<EnemyController> enemies;
    List<ObstacleController> obstacles;

    /// <summary>
    /// Komponensek
    /// </summary>
    GameSceneManager gameSceneManager;
    SpawnManager spawnManager;
    CharacterSetupManager characterSetupManager;
    UIManager uiManager;
    SaveLoadManager saveLoadManager;

    ObjectPoolForProjectiles objectPool;
    PlayerController player;

    [Header("Prefabs")]
    [SerializeField]
    private PlayerController playerPrefab;
    [SerializeField]
    private ObstacleController obstaclePrefab;
    [SerializeField]
    private ObjectPoolForProjectiles objectPoolPrefab;
    [SerializeField]
    private CharacterSetupManager characterSetupManagerPrefab;


    /// <summary>
    /// Események
    /// </summary>
    public event Action<bool, float> OnLevelCompleted; // true: success; false: failure // player HP %: 0 or ]0;100]
    public event Action<int> OnPointsAdded;


    /// <summary>
    /// 
    /// </summary>
    protected override async void Initialize()
    {
        base.Initialize();
        gameSceneManager = FindObjectOfType<GameSceneManager>();
        
        uiManager = FindObjectOfType<UIManager>();
        //characterSetupManager = FindObjectOfType<CharacterSetupManager>();    // EZT IS KÖZBEN KERESSÜK MEG!
        saveLoadManager = FindObjectOfType<SaveLoadManager>();

        saveLoadManager.OnSaveRequested += Save;

        isLoadSuccessful = await LoadAllLevelDataAsync();
        if (!isLoadSuccessful)
        {
            Debug.LogError("ADATHIBA");
        }
    }


    /// <summary>
    /// 
    /// </summary>
    /// <param name="saveData"></param>
    void Save(SaveData saveData)
    {
        foreach (var enemy in enemies)
        {
            saveData.spawnerSaveData.prefabsData.Add(GetPrefabSaveData(enemy.gameObject));
        }

        saveData.spawnerSaveData.prefabsData.Add(GetPrefabSaveData(player.gameObject));

        obstacles = new List<ObstacleController>(FindObjectsOfType<ObstacleController>());
        foreach (var obstacle in obstacles)
        {
            saveData.spawnerSaveData.prefabsData.Add(GetPrefabSaveData(obstacle.gameObject));
        }
    }


    /// <summary>
    /// 
    /// </summary>
    /// <param name="gameObject"></param>
    /// <returns></returns>
    PrefabSaveData GetPrefabSaveData(GameObject gameObject)
    {
        PrefabSaveData prefabSaveData = new PrefabSaveData();

        if (gameObject.TryGetComponent<EnemyController>(out EnemyController ec))
        {
            prefabSaveData.prefabID = ec.ID;
            prefabSaveData.prefabPosition = (Vector2)ec.transform.position;
        }
        else if (gameObject.TryGetComponent<PlayerController>(out PlayerController pc))
        {
            prefabSaveData.prefabID = pc.ID;
            prefabSaveData.prefabPosition = (Vector2)pc.transform.position;
        }
        else if (gameObject.TryGetComponent<ObstacleController>(out ObstacleController oc))
        {
            prefabSaveData.prefabID = oc.ID;
            prefabSaveData.prefabPosition = (Vector2)oc.transform.position;
        }

        return prefabSaveData;
    }


    /// <summary>
    /// 
    /// </summary>
    private void OnDestroy()
    {
        if (saveLoadManager != null)
        {
            saveLoadManager.OnSaveRequested -= Save;            
        }
    }


    /// <summary>
    /// 
    /// </summary>
    /// <param name="level"></param>
    /// <returns></returns>
    List<EnemyData.EnemySpawnInfo> GetSpawnManagerDataByLevel(int level)
    {
        spawnManagerData.Clear();

        currentLevel = allLevels.Find(x => x.levelNumber == level);

        int activeZoneCount = currentLevel.zoneData.spawnZoneInfos.Find(x => x.zoneType == ZoneType.Enemy).activeZoneCount;

        List<EnemyData.EnemySpawnInfo> allEnemyInfos = currentLevel.enemyData.enemyInfos;  // List of all possible enemy data

        for (int i = 0; i < activeZoneCount; i++)
        {
            // Véletlenszerű index kiválasztása a lehetséges ellenségek listájából
            int randomIndex = rand.Next(allEnemyInfos.Count);

            // Véletlenszerű elem hozzáadása a spawnManagerData listához
            spawnManagerData.Add(allEnemyInfos[randomIndex]);
        }

        return spawnManagerData;
    }


    /// <summary>
    /// 
    /// </summary>
    /// <param name="saveData"></param>
    /// <returns></returns>
    List<GameObjectPosition> GetSpawnManagerLoadDataFromSaveData(SaveData saveData)
    {
        List<GameObjectPosition> gameObjectPositions = new List<GameObjectPosition>();
        currentLevel = allLevels.Find(x => x.levelNumber == saveData.gameData.gameLevel);

        foreach (var prefabData in saveData.spawnerSaveData.prefabsData)
        {
            // PLAYER
            if (prefabData.prefabID == playerPrefab.ID)
            {
                gameObjectPositions.Add(new GameObjectPosition(playerPrefab.gameObject, prefabData.prefabPosition));
            }
            // OBSTACLE
            else if (prefabData.prefabID == obstaclePrefab.ID)
            {
                gameObjectPositions.Add(new GameObjectPosition(obstaclePrefab.gameObject, prefabData.prefabPosition));
            }
            // ENEMY
            else
            {
                GameObject go = currentLevel.enemyData.enemyInfos.Find(x => x.enemyPrefab.ID == prefabData.prefabID).enemyPrefab.gameObject;
                gameObjectPositions.Add(new GameObjectPosition(go, prefabData.prefabPosition));  
            }
        }

        return gameObjectPositions;
    }


    /// <summary>
    /// Aszinkron metódus, amely betölti az összes szint adatot az Addressables rendszerből.
    /// A metódus a 'Levels' címkét tartalmazó LevelData objektumokat tölti be.
    /// A betöltés befejezése után egy TaskCompletionSource segítségével visszaadja a sikeresség eredményét.
    /// </summary>
    /// <returns>Visszatérési érték: bool - true, ha a betöltés sikeres volt, különben false.</returns>
    async Task<bool> LoadAllLevelDataAsync()
    {
        // TaskCompletionSource létrehozása a betöltési folyamat aszinkron befejezésének kezelésére.
        var taskCompletionSource = new TaskCompletionSource<bool>();

        // Az Addressables rendszer segítségével betölti a 'Levels' címkét tartalmazó LevelData objektumokat.
        // Az OnLevelDataLoaded metódus hívódik meg minden egyes betöltött adat objektum esetén.
        var handle = Addressables.LoadAssetsAsync<LevelData>("Levels", (levelData) =>
        {
            // Minden betöltött adatot az OnLevelDataLoaded metódus dolgoz fel.
            OnLevelDataLoaded(levelData);
        });

        // A betöltési folyamat befejeződése után a 'Completed' eseménykezelő hívódik meg.
        handle.Completed += (operation) =>
        {
            // Ellenőrzi, hogy a betöltési folyamat sikeresen befejeződött-e.
            if (operation.Status == AsyncOperationStatus.Succeeded)
            {
                // Ha a betöltés sikeres volt, akkor a megfelelő üzenetet jeleníti meg, és a TaskCompletionSource-t sikeresen befejezi.
                Debug.Log($"All levels loaded successfully ({allLevels.Count})");
                taskCompletionSource.SetResult(true); // Sikeres betöltés
            }
            else
            {
                // Ha a betöltés nem sikerült, hibaüzenetet ír ki és a TaskCompletionSource-t hibás eredménnyel zárja le.
                Debug.LogError("Failed to load levels");
                taskCompletionSource.SetResult(false); // Hibás betöltés
            }
        };

        // Várakozik a TaskCompletionSource eredményére (visszaadja a végső sikerességi értéket: true vagy false).
        return await taskCompletionSource.Task;
    }


    /// <summary>
    /// Az OnLevelDataLoaded metódus feldolgozza a betöltött LevelData objektumokat.
    /// A betöltött adatokat hozzáadja az allLevels listához.
    /// </summary>
    /// <param name="levelData">A betöltött LevelData objektum, amely tartalmazza az aktuális szint adatokat.</param>
    private void OnLevelDataLoaded(LevelData levelData)
    {
        // Hozzáadjuk a betöltött szintet az allLevels listához.
        allLevels.Add(levelData);

        // Debug üzenet, amely tájékoztatja, hogy egy új szint adatot sikerült betölteni.
        Debug.Log($"Level {levelData.levelNumber} loaded.");
    }


    /// <summary>
    /// 
    /// </summary>
    /// <param name="levelNumber"></param>
    /// <returns></returns>
    public async Task<bool> LoadNewLevelAsync(int levelNumber)
    {
        bool asyncOperation;

        try {
            // SceneManager - Megfelelő pálya betöltése (TEMP: random pályaválasztás kikapcsolva)
            asyncOperation = await gameSceneManager.LoadRandomSceneByLevelAsync(levelNumber);
            if (!asyncOperation)
            {
                throw new Exception("Scene loading failed.");
            }

            // ObjectPool és CharacterSetupManager hozzáadása a jelenethez (más komponenseknek szüksége van rá az init során)
            asyncOperation = await InstantiateTransientLevelElementsAsync(objectPoolPrefab, characterSetupManagerPrefab);
            if (!asyncOperation)
            {
                throw new Exception("Object instantiation failed.");
            }

            // SpawnManager adatok előkészítése
            if (isLoadSuccessful)
            {
                currentLevel = allLevels.Find(level => level.levelNumber == levelNumber);
                if (currentLevel == null)
                {
                    throw new Exception($"Level {levelNumber} not found!");
                }

                // SpawnManager - prefabok elhelyezése a pályán
                spawnManager = FindObjectOfType<SpawnManager>();
                asyncOperation = await spawnManager.NewLevelInit(GetSpawnManagerDataByLevel(levelNumber), playerPrefab, obstaclePrefab, 3, 2);
                if (!asyncOperation)
                {
                    throw new Exception("Spawn Manager initialization failed.");
                }

            }
            else
            {
                throw new Exception("Level load failed! Cannot load new level.");
            }


            // CharacterSetupManager - játékos és ellenségek értékeinek beállítása
            asyncOperation = await characterSetupManager.SetCharactersAsync(levelNumber);
            if (!asyncOperation)
            {
                throw new Exception("Character setup failed.");
            }

            // Eseményfeliratkozások, játékbeli objektum-referenciák összegyűjtése
            asyncOperation = await SubscribeForCharacterEvents();
            if (!asyncOperation)
            {
                throw new Exception("Event subscription failed.");
            }

            // SaveLoadManager
            asyncOperation = await saveLoadManager.SaveGame();
            if (!asyncOperation)
            {
                throw new Exception("Saving level state failed.");
            }

            // ObjectPool - beállítások (pl.: marking based on Player stats)        
            asyncOperation = await SetObjectPool(objectPool);
            if (!asyncOperation)
            {
                throw new Exception("ObjectPool setup failed.");
            }

            // UIManager        
            asyncOperation = await uiManager.LoadPlayerUIAsync();
            if (!asyncOperation)
            {
                throw new Exception("UI setup failed.");
            }

            return true;
        }
        catch (Exception ex) {
            Debug.LogError($"Error: {ex.Message}");
            return false;
        }
        
    }


    /// <summary>
    /// 
    /// </summary>
    /// <param name="objectPool"></param>
    /// <returns></returns>
    async Task<bool> SetObjectPool(ObjectPoolForProjectiles objectPool)
    {
        await Task.Yield();

        try {
            if (player == null)
            {
                throw new Exception("Player reference is missing!");
            }
            objectPool.EnableMarking(player.CurrentPercentageBasedDMG > 0f);
            Debug.Log("OBJ_POOL: " + objectPool.IsMarkingEnabled);

            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error: {ex.Message}");
            return false;
        }
    }


    /// <summary>
    /// 
    /// </summary>
    /// <param name="objectPoolPrefab"></param>
    /// <param name="setupManagerPrefab"></param>
    /// <returns></returns>
    async Task<bool> InstantiateTransientLevelElementsAsync(ObjectPoolForProjectiles objectPoolPrefab, CharacterSetupManager setupManagerPrefab)
    {
        await Task.Yield();

        try {
            // ObjectPool
            ObjectPoolForProjectiles objectPoolInstance = Instantiate(objectPoolPrefab);

            if (objectPoolInstance == null)
            {
                throw new Exception("Failed to instantiate ObjectPool.");
            }

            objectPoolInstance.transform.SetParent(null);
            objectPool = objectPoolInstance.GetComponent<ObjectPoolForProjectiles>();

            // CharacterSetupManager
            CharacterSetupManager characterSetupInstance = Instantiate(characterSetupManagerPrefab);

            if (characterSetupInstance == null)
            {
                throw new Exception("Failed to instantiate CharacterSetupManager.");
            }

            characterSetupInstance.transform.SetParent(null);
            characterSetupManager = characterSetupInstance.GetComponent<CharacterSetupManager>();

            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error: {ex.Message}");
            return false;
        }

    }


    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    async Task<bool> SubscribeForCharacterEvents()
    {
        await Task.Yield();

        try
        {
            enemies = new List<EnemyController>(FindObjectsOfType<EnemyController>());
            player = FindAnyObjectByType<PlayerController>();

            if (player == null)
            {
                Debug.LogError("Nem található PlayerController. A játékos eseményekre való feliratkozás nem sikerült.");
                return false;
            }

            if (enemies == null || enemies.Count == 0)
            {
                Debug.LogWarning("Nem találhatók EnemyController objektumok. Az ellenség eseményekre való feliratkozás nem sikerült.");
                return false;
            }
            else
            {
                foreach (var enemy in enemies)
                {
                    enemy.OnEnemyDeath += EnemyKilled;
                }
            }

            player.OnPlayerDeath += PlayerKilled;

            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Hiba történt a {nameof(SubscribeForCharacterEvents)} metódusban: {ex.Message}");

            // Leiratkozás hibák esetén
            if (enemies != null)
            {
                foreach (var enemy in enemies)
                {
                    enemy.OnEnemyDeath -= EnemyKilled;
                }
            }

            if (player != null)
            {
                player.OnPlayerDeath -= PlayerKilled;
            }

            return false;
        }
    }


    /// <summary>
    /// 
    /// </summary>
    void PlayerKilled()
    {
        player.OnPlayerDeath -= PlayerKilled;

        foreach (var enemy in enemies)
        {
            enemy.OnEnemyDeath -= EnemyKilled;
        }

        OnLevelCompleted?.Invoke(false, 0f);
    }

    void EnemyKilled(EnemyController enemy)
    {
        enemy.OnEnemyDeath -= EnemyKilled;
        OnPointsAdded?.Invoke(enemy.CurrentPointValue);
        enemies.Remove(enemy);
        if (enemies.Count == 0)
        {
            player.OnPlayerDeath -= PlayerKilled;
            float playerHealthPercentage = player.CurrentHealth / player.MaxHealth;
            OnLevelCompleted?.Invoke(true, playerHealthPercentage);
        }
    }


    public async Task<bool> LoadSavedLevelAsync()
    {
        await Task.Yield();
        // SceneManager: megfelelő pálya betöltése
        // SpawnManager: prefabok+pozíciók átadása
            // ID alapján prefab a dictionaryból
        // CharacterSetupManager: hp% alapján értékek beállítása + egyéb beállítások
        // játék figyelése: eseményekre való feliratkozás

        return true;
    }

    /*
    public void PrintAllLevels()
    {
        if (allLevels == null || allLevels.Count == 0)
        {
            Debug.LogWarning("No levels available.");
            return;
        }

        // Iterálunk az összes szinten és kiírjuk az adatokat
        foreach (var level in allLevels)
        {
            PrintLevelData(level);
        }
    }

    private void PrintLevelData(LevelData level)
    {
        Debug.Log($"Level {level.levelNumber}:");
        Debug.Log($"  Spawn Zone Data:");

        // Kiírjuk a spawn zónákat (feltételezve, hogy az adatokat hasonló szerkezetben tároljuk)
        foreach (var spawnZoneInfo in level.zoneData.spawnZoneInfos)
        {
            Debug.Log($"    Zone Type: {spawnZoneInfo.zoneType}, Active Zones: {spawnZoneInfo.activeZoneCount}");
        }

        Debug.Log($"  Enemy Spawn Data:");

        // Kiírjuk az ellenségeket (feltételezve, hogy az adatokat hasonló szerkezetben tároljuk)
        foreach (var enemyInfo in level.enemyData.enemyInfos)
        {
            Debug.Log($"    Enemy Prefab: {enemyInfo.enemyPrefab.name}, Min: {enemyInfo.minNum}, Max: {enemyInfo.maxNum}");
        }
    }
    */


}
