using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using static SpawnZoneData;

public class LevelManager : BasePersistentManager<LevelManager>
{
    public List<LevelData> allLevels = new List<LevelData>();

    public LevelData currentLevel;

    List<EnemyData.EnemySpawnInfo> spawnManagerData = new List<EnemyData.EnemySpawnInfo>();

    public event Action<bool> OnLoadCompleted;

    private bool isLoadSuccessful = false;



    GameSceneManager gameSceneManager;
    SpawnManager spawnManager;  // létrehozás
    CharacterSetupManager characterSetupManager;    // létrehozás
    SaveLoadManager saveLoadManager;

    // szint vége ellenőrzés
    List<EnemyController> enemies;
    public PlayerController player;


    public event Action<bool> OnLevelCompleted; // true: success; flase: faliure


    private async void Start()
    {
        // manager-ek összeszedése változókba!
        gameSceneManager = FindObjectOfType<GameSceneManager>();
        spawnManager = FindObjectOfType<SpawnManager>();
        characterSetupManager = FindObjectOfType<CharacterSetupManager>();
        saveLoadManager = FindObjectOfType<SaveLoadManager>();


        bool dataLoaded = await LoadAllLevelData();
        if (!dataLoaded)
        {
            Debug.LogError("ADATHIBA");
        }
        bool success = await LoadNewLevel(1);
        if (!success)
        {
            Debug.LogError("FAIL");
        }


    }



    List<EnemyData.EnemySpawnInfo> GetSpawnManagerDataByLevel(int level)
    {
        currentLevel = allLevels.Find(x => x.levelNumber == level);

        int activeZoneCount = currentLevel.zoneData.spawnZoneInfos.Find(x => x.zoneType == ZoneType.Enemy).activeZoneCount;

        List<EnemyData.EnemySpawnInfo> allEnemyInfos = currentLevel.enemyData.enemyInfos;  // List of all possible enemy data

        System.Random rand = new System.Random();

        for (int i = 0; i < activeZoneCount; i++)
        {
            // Véletlenszerű index kiválasztása a lehetséges ellenségek listájából
            int randomIndex = rand.Next(allEnemyInfos.Count);

            // Véletlenszerű elem hozzáadása a spawnManagerData listához
            spawnManagerData.Add(allEnemyInfos[randomIndex]);
        }

        return spawnManagerData;
    }


    async Task<bool> LoadAllLevelData()
    {
        await Task.Delay(1000);
        Addressables.LoadAssetsAsync<LevelData>("Levels", OnLevelDataLoaded).Completed += OnLoadComplete;
        return true;
    }

    private void OnLevelDataLoaded(LevelData levelData)
    {
        allLevels.Add(levelData);
        Debug.Log($"Level {levelData.levelNumber} loaded.");
    }
    
    private void OnLoadComplete(AsyncOperationHandle<IList<LevelData>> handle)
    {
        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            Debug.Log("All levels loaded successfully!");
            OnLoadCompleted?.Invoke(true);
            isLoadSuccessful = true;
        }
        else
        {
            Debug.LogError("Failed to load levels!");
            OnLoadCompleted?.Invoke(false);
        }

        foreach (var level in allLevels)
        {
            Debug.Log($"Level in list: {level.levelNumber}");
        }
    }
    
    // ASYNC !!!
    public async Task<bool> LoadNewLevel(int levelNumber)
    {
        // SCENEMANAGER hívás
        bool sceneLoaded = await gameSceneManager.LoadRandomSceneByLevelAsync(1);
        if (!sceneLoaded)
        {
            Debug.Log("Scene loading failed.");
            return false;
        }

        if (isLoadSuccessful)
        {
            currentLevel = allLevels.Find(level => level.levelNumber == levelNumber);
            if (currentLevel == null)
            {
                Debug.LogError($"Level {levelNumber} not found!");
                return false;
            }
            Debug.Log("READY FOR SPAWN MANAGER TASKS");
            // SPAWNMANAGER ADATOK generálása és SpawnManager meghívása
            Debug.Log("LEVELDATA: " + GetSpawnManagerDataByLevel(levelNumber).Count);
            Debug.Log("SPAWN MANAGER TASKS...");
        }
        else
        {
            Debug.LogError("Level load failed! Cannot load new level.");
        }


        // CharacterSetupManager meghívása
        bool charactersSetup = await characterSetupManager.LoadCharactersAsync(1);
        if (!charactersSetup)
        {
            Debug.Log("Character setup failed.");
            return false;
        }

        // SaveLoadManager
        bool saved = await saveLoadManager.SaveStateAsync(1);
        if (!saved)
        {
            Debug.Log("Saving level state failed.");
            return false;
        }

        Debug.Log("CHECKING LEVEL CONDITIONS...");
        bool eventSubscription = await SubscribeForCharacterEvents();
        if (!eventSubscription)
        {
            Debug.Log("Event subscription has failed.");
            return false;
        }
        Debug.Log("PLAY TIME!");

        return true;
    }

    async Task<bool> SubscribeForCharacterEvents()
    {
        enemies = new List<EnemyController>(FindObjectsOfType<EnemyController>());
        foreach (var enemy in enemies)
        {
            enemy.OnEnemyDeath += EnemyKilled;
        }

        player = FindAnyObjectByType<PlayerController>();
        player.OnPlayerDeath += PlayerKilled;

        await Task.Delay(1000);  // Szimuláljuk a betöltési időt
        return true;
    }

    void PlayerKilled()
    {
        player.OnPlayerDeath -= PlayerKilled;

        foreach (var enemy in enemies)
        {
            enemy.OnEnemyDeath -= EnemyKilled;
        }

        OnLevelCompleted?.Invoke(false);
    }

    void EnemyKilled(EnemyController enemy)
    {
        enemy.OnEnemyDeath -= EnemyKilled;
        enemies.Remove(enemy);
        if (enemies.Count == 0)
        {
            player.OnPlayerDeath -= PlayerKilled;
            OnLevelCompleted?.Invoke(true);
        }
    }


    public void LoadSavedLevel()
    {
        Debug.Log("LOAD LEVEL BY SAVE DATA...");
    }

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

}
