using Assets.Scripts;
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
    public class SpawnManagerData
    {
        // Egy lista, amely az ellenségek spawnolásával kapcsolatos adatokat tárolja.
        private List<EnemyData.EnemySpawnInfo> _enemySpawnData;

        // Az ellenségek spawnolási adataihoz tartozó getter és setter.
        // Ha a setter értéke null, akkor egy új üres lista jön létre.
        public List<EnemyData.EnemySpawnInfo> EnemySpawnData
        {
            get => _enemySpawnData;
            set => _enemySpawnData = value ?? new List<EnemyData.EnemySpawnInfo>();
        }

        // A játékos prefabjának tárolása.
        public PlayerController PlayerPrefab { get; set; }
        // Az obstacle-ök prefabjainak listája.
        public List<StaticObstacleController> ObstaclePrefabs { get; set; }
        // Az aktív obstaclespawnerek száma.
        public int ActiveObstacleSpawners { get; set; }
        // Az aktív jokerspawnerek száma.
        public int ActiveJokerSpawners { get; set; }
    }

    public class GameObjectPosition
    {
        // A játékobjektum, amelynek pozícióját tároljuk.
        public GameObject gameObject;

        // A játékobjektum 2D-s pozíciója.
        public Vector2 position;

        // Konstruktor, amely beállítja a játékobjektumot és annak pozícióját.
        public GameObjectPosition(GameObject gameObject, Vector2 position)
        {
            this.gameObject = gameObject;
            this.position = position;
        }
    }

    List<LevelData> allLevels = new List<LevelData>(); // Az összes pálya adatait tároló lista.

    //LevelData currentLevel;

    //List<EnemyData.EnemySpawnInfo> spawnManagerData = new List<EnemyData.EnemySpawnInfo>();

    private bool isLoadSuccessful; // Jelzi, hogy a betöltés sikeres volt-e.

    System.Random rand = new System.Random(); // System random használata.

    // A szint vége ellenőrzéséhez szükséges listák az ellenségekről és obstacle-ökről..
    List<EnemyController> enemies;
    List<ObstacleController> obstacles;

    /// <summary>
    /// Komponensek
    /// </summary>
    GameStateManager gameStateManager; // A játék állapotának kezelője.
    GameSceneManager gameSceneManager; // A játék jeleneteinek kezelője.
    SpawnManager spawnManager; // Az objektumok spawnolásáért felelős kezelő.
    CharacterSetupManager characterSetupManager; // A karakterek beállításáért felelős kezelő.
    UIManager uiManager; // A felhasználói felület kezelője.
    SaveLoadManager saveLoadManager; // Mentés és betöltés kezelője.

    ObjectPoolForProjectiles objectPool; // Lövedékekhez tartozó objektumpool.
    BossObjectPool bossObjectPool; // Boss lövedékeihez tartozó objektumpool.
    PlayerController player; // A játékos vezérlő objektuma.
    BossController boss; // A boss vezérlő objektuma.

    [Header("Character Prefabs")]
    // A játékos prefabja, amely a Unity Inspectorban állítható be.
    [SerializeField]
    private PlayerController playerPrefab;
    // A boss prefabja.
    [SerializeField]
    private BossController bossPrefab;
    [Header("Obstacle Prefabs")]
    // A statikus obstacle prefabjainak listája.
    [SerializeField]
    private List<StaticObstacleController> staticObstaclePrefabs;
    [SerializeField]
    // A dinamikus obstacle prefabjainak listája.
    private DynamicObstacleController dynamicObstaclePrefab;
    [Header("Component Prefabs")]
    // Lövedékek objectpool-jának prefabja.
    [SerializeField]
    private ObjectPoolForProjectiles objectPoolPrefab;
    // A karakterbeállításokért felelős kezelő prefabja.
    [SerializeField]
    private BossObjectPool bossObjectPoolPrefab;
    [SerializeField]
    private CharacterSetupManager characterSetupManagerPrefab;


    /// <summary>
    /// Események
    /// </summary>
    // Esemény, amely akkor hívódik meg, ha egy szint teljesül.
    public event Action<bool, float> OnLevelCompleted; // true: success; false: failure // player HP %: 0 or ]0;100]
    // Esemény, amely akkor hívódik meg, amikor pontokat adunk a játékosnak.
    public event Action<int> OnPointsAdded;
    // Esemény, amely akkor hívódik meg, amikor a játék véget ér. True-siker, false-bukás.
    public event Action<bool> OnGameFinished;

    /// <summary>
    /// Az osztály inicializálását végző függvény.
    /// </summary>
    protected override async void Initialize()
    {
        // Az ősosztály Initialize metódusának meghívása.
        base.Initialize();

        // A GameStateManager komponens keresése a jelenetben.
        gameStateManager = FindObjectOfType<GameStateManager>();

        // A GameSceneManager komponens keresése a jelenetben.
        gameSceneManager = FindObjectOfType<GameSceneManager>();

        // Az UIManager (felhasználói felület kezelő) keresése a jelenetben.
        uiManager = FindObjectOfType<UIManager>();

        // A SaveLoadManager (mentés és betöltés kezelő) keresése a jelenetben.
        saveLoadManager = FindObjectOfType<SaveLoadManager>();

        // Feliratkozás a SaveLoadManager mentési eseményére.
        saveLoadManager.OnSaveRequested += Save;

        // Az összes szint adatainak betöltése aszinkron módon.
        isLoadSuccessful = await LoadAllLevelDataAsync();

        // Ha az adatok betöltése nem sikerült, hibát írunk a konzolra.
        if (!isLoadSuccessful)
        {
            Debug.LogError("ADATHIBA"); // Hibajelzés, ha az adatok sérültek vagy hiányosak.
        }
    }


    /// <summary>
    /// Az aktuális játékállás mentése.
    /// </summary>
    /// <param name="saveData">Az adatszerkezet, amelyben a mentés tárolódik.</param>
    void Save(SaveData saveData)
    {
        // Az összes ellenség adatainak hozzáadása a mentéshez.
        foreach (var enemy in enemies)
        {
            saveData.spawnerSaveData.prefabsData.Add(GetPrefabSaveData(enemy.gameObject));
        }

        // Ha van boss a pályán, annak adatait is mentjük.
        if (boss != null)
        {
            Debug.Log("VAN BOSS A PÁLYÁN!"); // Konzolüzenet a boss jelenlétéről.
            saveData.spawnerSaveData.prefabsData.Add(GetPrefabSaveData(boss.gameObject));
        }

        // A játékos adatainak mentése.
        saveData.spawnerSaveData.prefabsData.Add(GetPrefabSaveData(player.gameObject));

        // Az akadályok összegyűjtése és mentése.
        obstacles = new List<ObstacleController>(FindObjectsOfType<ObstacleController>());
        foreach (var obstacle in obstacles)
        {
            saveData.spawnerSaveData.prefabsData.Add(GetPrefabSaveData(obstacle.gameObject));
        }
    }


    /// <summary>
    /// Egy adott játékobjektum mentéséhez szükséges adatokat ad vissza.
    /// </summary>
    /// <param name="gameObject">A játékobjektum, amelynek adatait menteni szeretnénk.</param>
    /// <returns>A játékobjektumhoz tartozó mentési adatok.</returns>
    PrefabSaveData GetPrefabSaveData(GameObject gameObject)
    {
        // Létrehoz egy új PrefabSaveData objektumot, amelybe az adatokat mentjük.
        PrefabSaveData prefabSaveData = new PrefabSaveData();

        // Ha a játékobjektum tartalmaz EnemyController komponenst:
        if (gameObject.TryGetComponent<EnemyController>(out EnemyController ec))
        {
            // Beállítja az ellenség azonosítóját és pozícióját a mentési adatokban.
            prefabSaveData.prefabID = ec.ID;
            prefabSaveData.prefabPosition = (Vector2)ec.transform.position;
        }
        // Ha a játékobjektum tartalmaz PlayerController komponenst:
        else if (gameObject.TryGetComponent<PlayerController>(out PlayerController pc))
        {
            // Beállítja a játékos azonosítóját és pozícióját.
            prefabSaveData.prefabID = pc.ID;
            prefabSaveData.prefabPosition = (Vector2)pc.transform.position;
        }
        // Ha a játékobjektum tartalmaz ObstacleController komponenst:
        else if (gameObject.TryGetComponent<ObstacleController>(out ObstacleController oc))
        {
            // Beállítja az obstacle azonosítóját és pozícióját.
            prefabSaveData.prefabID = oc.ID;
            prefabSaveData.prefabPosition = (Vector2)oc.transform.position;
        }
        // Ha a játékobjektum tartalmaz BossController komponenst:
        else if (gameObject.TryGetComponent<BossController>(out BossController bc))
        {
            // Beállítja a boss azonosítóját és pozícióját.
            prefabSaveData.prefabID = bc.ID;
            prefabSaveData.prefabPosition = (Vector2)bc.transform.position;
        }

        // Visszatér a kitöltött PrefabSaveData objektummal.
        return prefabSaveData;
    }


    /// <summary>
    /// Akkor hívódik meg, amikor az objektum megsemmisül.
    /// </summary>
    private void OnDestroy()
    {
        // Ellenőrzi, hogy a SaveLoadManager létezik-e.
        if (saveLoadManager != null)
        {
            // Leiratkozik a mentési eseményről.
            saveLoadManager.OnSaveRequested -= Save;
        }
    }


    /// <summary>
    /// Kinyeri a jelenlegi szint adatai alapján az ellenségek spawnolására vonatkozó adatokat.
    /// </summary>
    /// <param name="currentLevel">A jelenlegi szint adatai.</param>
    /// <returns>Az ellenségek spawnolására vonatkozó információkat tartalmazó lista.</returns>
    List<EnemyData.EnemySpawnInfo> GetEnemySpawnDataFromCurrentLevelData(LevelData currentLevel)
    {
        // Létrehoz egy listát az ellenségek spawnolási adatainak tárolására.
        List<EnemyData.EnemySpawnInfo> enemySpawnData = new List<EnemyData.EnemySpawnInfo>();

        // Kinyeri a szint ellenségzónáiból az aktív zónák számát.
        int activeEnemyZoneCount = currentLevel.zoneData.spawnZoneInfos.Find(x => x.zoneType == ZoneType.Enemy).activeZoneCount;

        // Kinyeri az összes lehetséges ellenség adatát a szint adataiból.
        List<EnemyData.EnemySpawnInfo> allEnemyInfos = currentLevel.enemyData.enemyInfos;

        // Az aktív zónák számának megfelelő számú ellenség kiválasztása.
        for (int i = 0; i < activeEnemyZoneCount; i++)
        {
            // Véletlenszerű index kiválasztása a lehetséges ellenségek listájából
            int randomIndex = rand.Next(allEnemyInfos.Count);

            // Véletlenszerű elem hozzáadása a spawnManagerData listához
            enemySpawnData.Add(allEnemyInfos[randomIndex]);
        }

        // Visszaadja a kitöltött ellenség spawnolási adatokat.
        return enemySpawnData;
    }

    /// <summary>
    /// Lekéri a SpawnManager számára szükséges adatokat a megadott szint alapján.
    /// </summary>
    /// <param name="level">A kívánt szint száma.</param>
    /// <returns>A SpawnManager számára szükséges adatokat tartalmazó objektum.</returns>
    SpawnManagerData GetSpawnManagerDataByLevel(int level)
    {
        // Létrehoz egy új SpawnManagerData objektumot az adatok tárolására.
        SpawnManagerData spawnManagerData = new SpawnManagerData();
        // Kiválasztja a megadott szinthez tartozó LevelData-t az összes szint közül.
        LevelData currentLevel = allLevels.Find(x => x.levelNumber == level);

        // Beállítja az ellenségek spawnolására vonatkozó adatokat.
        spawnManagerData.EnemySpawnData = GetEnemySpawnDataFromCurrentLevelData(currentLevel);
        // Beállítja a játékos prefabját.
        spawnManagerData.PlayerPrefab = playerPrefab;
        // Beállítja a statikus obstacleök prefabjait.
        spawnManagerData.ObstaclePrefabs = staticObstaclePrefabs;
        // Beállítja az aktív obstaclespawner-ek számát.
        spawnManagerData.ActiveObstacleSpawners = currentLevel.zoneData.spawnZoneInfos.Find(x => x.zoneType == ZoneType.Obstacle).activeZoneCount;
        // Beállítja az aktív jokerspawner-ek számát.
        spawnManagerData.ActiveJokerSpawners = currentLevel.zoneData.spawnZoneInfos.Find(x => x.zoneType == ZoneType.Joker).activeZoneCount;

        // Kiírja az ellenség spawnolási adatainak számát a konzolra.
        Debug.Log(spawnManagerData.EnemySpawnData.Count);

        // Visszatér a kitöltött SpawnManagerData objektummal.
        return spawnManagerData;
    }


    /// <summary>
    /// A mentett adatokból kinyeri a SpawnManager számára szükséges objektumokat és azok pozícióit.
    /// </summary>
    /// <param name="saveData">A mentési adatokat tartalmazó objektum.</param>
    /// <returns>Az objektumokat és pozícióikat tartalmazó lista.</returns>
    List<GameObjectPosition> GetSpawnManagerLoadDataFromSaveData(SaveData saveData)
    {
        // Létrehoz egy listát az objektumok és pozícióik tárolására.
        List<GameObjectPosition> gameObjectPositions = new List<GameObjectPosition>();
        // Kinyeri a mentési adatokban szereplő szintet, és megtalálja annak LevelData-ját.
        LevelData currentLevel = allLevels.Find(x => x.levelNumber == gameStateManager.GameLevelToInt(gameStateManager.LevelNameToGameLevel(saveData.gameData.gameLevel)));

        // Végigmegy a mentett prefabok adatain, és visszaállítja azokat.
        foreach (var prefabData in saveData.spawnerSaveData.prefabsData)
        {
            // Megkeresi a prefabhoz tartozó GameObject-et a jelenlegi szint adatai alapján.
            GameObject go = GetPrefabGameObject(currentLevel, prefabData.prefabID);
            // Ha az objektum létezik, hozzáadja a listához annak pozíciójával együtt.
            if (go != null)
            {
                gameObjectPositions.Add(new GameObjectPosition(go, prefabData.prefabPosition));
            }
        }

        // Visszaadja a listát az objektumokkal és azok pozícióival.
        return gameObjectPositions;
    }

    /// <summary>
    /// Lekéri a megfelelő prefab GameObject-et a megadott szint és prefab ID alapján.
    /// </summary>
    /// <param name="currentLevel">A jelenlegi szint adatai.</param>
    /// <param name="prefabID">Az objektumhoz tartozó azonosító.</param>
    /// <returns>A megfelelő prefab GameObject-je, vagy null, ha nem található.</returns>
    private GameObject GetPrefabGameObject(LevelData currentLevel, string prefabID)
    {
        // Játékos prefab kezelése
        if (prefabID == playerPrefab.ID)
        {
            return playerPrefab.gameObject;
        }

        // StaticObstacle prefab kezelése
        var obstaclePrefab = staticObstaclePrefabs.Find(x => x.ID == prefabID);
        if (obstaclePrefab != null)
        {
            return obstaclePrefab.gameObject;
        }

        // Ellenség prefab kezelése
        var enemyPrefab = currentLevel.enemyData.enemyInfos.Find(x => x.enemyPrefab.ID == prefabID);
        if (enemyPrefab != null)
        {
            return enemyPrefab.enemyPrefab.gameObject;
        }

        // Ha nincs egyezés, visszatér null-lal
        return null;
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
    /// Aszinkron módon betölti a boss szintet, inicializálja a szükséges komponenseket és beállítja a játékos és ellenségek értékeit.
    /// </summary>
    /// <returns>True, ha a szint sikeresen betöltődött, false, ha hiba történt.</returns>
    public async Task<bool> LoadBossLevelAsync()
    {
        // Aszinkron művelet elindítása.
        await Task.Yield();
        bool asyncOperation;

        try
        {
            // SceneManager - Megfelelő pálya betöltése
            asyncOperation = await gameSceneManager.LoadUtilitySceneAsync("BossFight");
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

            asyncOperation = await InstantiateBossObjectPool(bossObjectPoolPrefab);

            // SpawnManager adatok előkészítése
            List<GameObjectPosition> bossLevelData = new List<GameObjectPosition>();
            bossLevelData.Add(new GameObjectPosition(bossPrefab.gameObject, new Vector2(0f, 0f)));
            bossLevelData.Add(new GameObjectPosition(playerPrefab.gameObject, new Vector2(0f, -10f)));

            // SpawnManager - prefabok elhelyezése a pályán
            spawnManager = FindObjectOfType<SpawnManager>();
            asyncOperation = await spawnManager.BossLevelInit(bossLevelData);
            if (!asyncOperation)
            {
                throw new Exception("Spawn Manager initialization failed.");
            }

            
            // CharacterSetupManager - játékos és ellenségek értékeinek beállítása
            asyncOperation = await characterSetupManager.SetBossLevelCharactersAsync();
            if (!asyncOperation)
            {
                throw new Exception("Character setup failed.");
            }
            
            // Eseményfeliratkozások, játékbeli objektum-referenciák összegyűjtése
            asyncOperation = await SubscribeForBossLevelCharacterEvents();
            if (!asyncOperation)
            {
                throw new Exception("Event subscription failed.");
            }
            
            // SaveLoadManager
            asyncOperation = await saveLoadManager.SaveGameAsync();
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

            // UIManager - játékos UI-jának betöltése.     
            asyncOperation = await uiManager.LoadPlayerUIAsync();
            if (!asyncOperation)
            {
                throw new Exception("UI setup failed.");
            }

            // Ha minden sikeresen végrehajtódott, visszatérünk true értékkel.
            return true;
        }
        catch (Exception ex)
        {
            // Ha hiba történt, kiírjuk az hibaüzenetet a konzolra.
            Debug.LogError($"{ex.Message}");
            return false;
        }
    }


    /// <summary>
    /// Aszinkron módon betölti a megadott számú új szintet, inicializálja a szükséges komponenseket és beállítja a játékos és ellenségek értékeit.
    /// </summary>
    /// <param name="levelNumber">A betöltendő szint száma.</param>
    /// <returns>True, ha az új szint sikeresen betöltődött, false, ha hiba történt.</returns>
    public async Task<bool> LoadNewLevelAsync(int levelNumber)
    {
        bool asyncOperation;

        try
        {
            // SceneManager - Megfelelő pálya betöltése
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
                LevelData currentLevel = allLevels.Find(level => level.levelNumber == levelNumber);
                if (currentLevel == null)
                {
                    throw new Exception($"Level {levelNumber} not found!");
                }

                // SpawnManager - prefabok elhelyezése a pályán
                spawnManager = FindObjectOfType<SpawnManager>();
                asyncOperation = await spawnManager.NewLevelInit(GetSpawnManagerDataByLevel(levelNumber));
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
            asyncOperation = await characterSetupManager.SetNormalLevelCharactersAsync(levelNumber);
            if (!asyncOperation)
            {
                throw new Exception("Character setup failed.");
            }

            // Eseményfeliratkozások, játékbeli objektum-referenciák összegyűjtése
            asyncOperation = await SubscribeForNormalLevelCharacterEvents();
            if (!asyncOperation)
            {
                throw new Exception("Event subscription failed.");
            }

            // SaveLoadManager
            asyncOperation = await saveLoadManager.SaveGameAsync();
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

            // UIManager - játékos UI-jának betöltése.   
            asyncOperation = await uiManager.LoadPlayerUIAsync();
            if (!asyncOperation)
            {
                throw new Exception("UI setup failed.");
            }

            // Ha minden sikeresen végrehajtódott, visszatérünk true értékkel.
            return true;
        }
        catch (Exception ex)
        {
            // Ha hiba történt, kiírjuk a hibaüzenetet a konzolra.
            Debug.LogError($"Error: {ex.Message}");
            return false;
        }

    }


    /// <summary>
    /// Beállítja az ObjectPool-t a játékos aktuális állapota alapján, és engedélyezi vagy letiltja a jelölést.
    /// </summary>
    /// <param name="objectPool">Az ObjectPool példány, amelyet be kell állítani.</param>
    /// <returns>True, ha a beállítás sikeres, false, ha hiba történt.</returns>
    async Task<bool> SetObjectPool(ObjectPoolForProjectiles objectPool)
    {
        // Aszinkron műveletek folytatása
        await Task.Yield();

        try
        {
            // Ellenőrizzük, hogy a player referencia létezik-e.
            if (player == null)
            {
                throw new Exception("Player reference is missing!");
            }
            // Az ObjectPool marking engedélyezése vagy letiltása a játékos aktuális támadási statja alapján.
            objectPool.EnableMarking(player.CurrentPercentageBasedDMG > 0f);
            // A jelölés állapotának kiírása a konzolra.
            Debug.Log("OBJ_POOL: " + objectPool.IsMarkingEnabled);

            // Ha minden sikeresen végrehajtódott, visszatérünk true értékkel.
            return true;
        }
        catch (Exception ex)
        {
            // Ha hiba történt, kiírjuk a hibaüzenetet a konzolra.
            Debug.LogError($"Error: {ex.Message}");
            return false;
        }
    }


    /// <summary>
    /// Aszinkron módon létrehozza a szükséges ideiglenes szint elemeket, például az ObjectPool és a CharacterSetupManager példányokat.
    /// </summary>
    /// <param name="objectPoolPrefab">Az ObjectPool prefab, amelyet instanciálni kell.</param>
    /// <param name="setupManagerPrefab">A CharacterSetupManager prefab, amelyet instanciálni kell.</param>
    /// <returns>True, ha az instanciálás sikeres, false, ha hiba történt.</returns>
    async Task<bool> InstantiateTransientLevelElementsAsync(ObjectPoolForProjectiles objectPoolPrefab, CharacterSetupManager setupManagerPrefab)
    {
        // Aszinkron műveletek folytatása
        await Task.Yield();

        try
        {
            // ObjectPool példányosítása
            ObjectPoolForProjectiles objectPoolInstance = Instantiate(objectPoolPrefab);

            // Ellenőrizzük, hogy az ObjectPool példányosítása sikeres volt-e.
            if (objectPoolInstance == null)
            {
                throw new Exception("Failed to instantiate ObjectPool.");
            }

            // Az új objektum szülőjének eltávolítása.
            objectPoolInstance.transform.SetParent(null);
            // Az ObjectPool példány lekérése.
            objectPool = objectPoolInstance.GetComponent<ObjectPoolForProjectiles>();

            // CharacterSetupManager példányosítása
            CharacterSetupManager characterSetupInstance = Instantiate(characterSetupManagerPrefab);

            // Ellenőrizzük, hogy a CharacterSetupManager példányosítása sikeres volt-e.
            if (characterSetupInstance == null)
            {
                throw new Exception("Failed to instantiate CharacterSetupManager.");
            }

            // Az új objektum szülőjének eltávolítása.
            characterSetupInstance.transform.SetParent(null);
            // A CharacterSetupManager példány lekérése.
            characterSetupManager = characterSetupInstance.GetComponent<CharacterSetupManager>();

            // Ha minden sikeresen végrehajtódott, visszatérünk true értékkel.
            return true;
        }
        catch (Exception ex)
        {
            // Ha hiba történt, kiírjuk a hibaüzenetet a konzolra.
            Debug.LogError($"Error: {ex.Message}");
            return false;
        }

    }

    async Task<bool> InstantiateBossObjectPool(BossObjectPool bossObjectPoolPrefab)
    {
        await Task.Yield();

        try
        {
            BossObjectPool bossObjectPoolInstance = Instantiate(bossObjectPoolPrefab);

            if (bossObjectPoolInstance == null)
            {
                throw new Exception("Failed to instantiate ObjectPool.");
            }

            bossObjectPoolInstance.transform.SetParent(null);
            bossObjectPool = bossObjectPoolInstance.GetComponent<BossObjectPool>();

            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Aszinkron módon feliratkozik a boss szint karaktereihez kapcsolódó eseményekre, például a játékos és a boss halálára.
    /// </summary>
    /// <returns>True, ha a feliratkozás sikeres, false, ha hiba történt.</returns>
    async Task<bool> SubscribeForBossLevelCharacterEvents()
    {
        // Aszinkron műveletek folytatása
        await Task.Yield();

        try
        {
            // Játékos és főellenség keresése a jelenetben
            player = FindObjectOfType<PlayerController>();
            boss = FindObjectOfType<BossController>();

            // Eseményekre való feliratkozás
            player.OnPlayerDeath += PlayerKilled;
            boss.OnBossDeath += BossKilled;

            // Ha minden sikeresen megtörtént, visszatérünk true értékkel.
            return true;
        }
        catch (Exception ex)
        {
            // Ha hiba történt, kiírjuk a hibaüzenetet a konzolra.
            Debug.LogError($"{ex.Message}");
            return false;
        }
    }


    /// <summary>
    /// Aszinkron módon feliratkozik a normál szint karaktereihez kapcsolódó eseményekre, beleértve a játékos és az ellenségek halálát.
    /// </summary>
    /// <returns>True, ha a feliratkozás sikeres, false, ha hiba történt.</returns>
    async Task<bool> SubscribeForNormalLevelCharacterEvents()
    {
        // Aszinkron műveletek folytatása
        await Task.Yield();

        try
        {
            // Az összes ellenség és a játékos keresése a jelenetben
            enemies = new List<EnemyController>(FindObjectsOfType<EnemyController>());
            player = FindObjectOfType<PlayerController>();

            // Ha nem találjuk a játékost, akkor hibaüzenetet írunk ki és false-t adunk vissza.
            if (player == null)
            {
                Debug.LogError("Nem található PlayerController. A játékos eseményekre való feliratkozás nem sikerült.");
                return false;
            }

            // Ha nem találunk ellenségeket, akkor figyelmeztetést írunk ki és false-t adunk vissza.
            if (enemies == null || enemies.Count == 0)
            {
                Debug.LogWarning("Nem találhatók EnemyController objektumok. Az ellenség eseményekre való feliratkozás nem sikerült.");
                return false;
            }
            else
            {
                // Ha ellenségeket találtunk, feliratkozunk az ő halálukra.
                foreach (var enemy in enemies)
                {
                    enemy.OnEnemyDeath += EnemyKilled;
                }
            }

            // A játékos halálára is feliratkozunk
            player.OnPlayerDeath += PlayerKilled;

            // Ha minden rendben van, visszatérünk true értékkel
            return true;
        }
        catch (Exception ex)
        {
            // Ha hiba történik, kiírjuk a hibát és leiratkozunk az eseményekről
            Debug.LogError($"Hiba történt a {nameof(SubscribeForNormalLevelCharacterEvents)} metódusban: {ex.Message}");

            // Leiratkozás minden eseményről, ha hiba történt
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

            // Ha hiba történt, false-t adunk vissza
            return false;
        }
    }


    /// <summary>
    /// Eseménykezelő, amely akkor hívódik meg, amikor a boss meghal.
    /// Ez a metódus leiratkozik a játékos és az ellenség halálára vonatkozó eseményekről,
    /// és jelzi a játék befejezését a `OnGameFinished` eseményen keresztül.
    /// </summary>
    void BossKilled()
    {
        // Leiratkozunk a főellenség halálára vonatkozó eseményről
        boss.OnBossDeath -= BossKilled;
        // Leiratkozunk a játékos halálára vonatkozó eseményről
        player.OnPlayerDeath -= PlayerKilled;

        // A játék befejezésének jelzése (success)
        OnGameFinished?.Invoke(true);
    }


    /// <summary>
    /// Eseménykezelő, amely akkor hívódik meg, amikor a játékos meghal.
    /// Ha vannak még ellenségek, akkor a szint befejeződését kezeli, ha pedig nincs ellenség,
    /// akkor a játék befejeződését jelezve véget vet a játéknak.
    /// </summary>
    void PlayerKilled()
    {
        // Leiratkozunk a játékos halálára vonatkozó eseményről
        player.OnPlayerDeath -= PlayerKilled;

        // Ha vannak ellenségek a pályán (ellenkező esetben a boss szinten vagyunk)
        if (enemies != null)        // ha vannak/maradtak enemy-k akkor normál pálya, amúgy Boss
        {
            // Leiratkozunk az ellenségek halálára vonatkozó eseményekről
            foreach (var enemy in enemies)
            {
                enemy.OnEnemyDeath -= EnemyKilled;
            }

            // Jelzünk, hogy a szint befejeződött (nem sikerült: false) és a játékos HP-ja 0%
            OnLevelCompleted?.Invoke(false, 0f);
        }
        else
        {
            // Ha nincs több ellenség, akkor a játék befejeződik
            OnGameFinished?.Invoke(false);
        }
    }


    /// <summary>
    /// Eseménykezelő, amely akkor hívódik meg, amikor egy ellenség meghal.
    /// Hozzáadja az ellenség által szerzett pontokat, eltávolítja az ellenséget a listából,
    /// és ha már nincs több ellenség, akkor a szint befejeződik és jelzi a játékos életének arányát.
    /// </summary>
    /// <param name="enemy">Az elpusztított ellenség vezérlője.</param>
    void EnemyKilled(EnemyController enemy)
    {
        // Leiratkozunk az ellenség halálára vonatkozó eseményről
        enemy.OnEnemyDeath -= EnemyKilled;
        // Addoljuk az ellenség pontjait
        OnPointsAdded?.Invoke(enemy.CurrentPointValue);
        // Eltávolítjuk az ellenséget az ellenségek listájából
        enemies.Remove(enemy);

        // Ha már nincs több ellenség
        if (enemies.Count == 0)
        {
            // Leiratkozunk a játékos halálára vonatkozó eseményről
            player.OnPlayerDeath -= PlayerKilled;
            // Kiszámítjuk a játékos életének jelenlegi százalékos arányát
            float playerHealthPercentage = player.CurrentHealth / player.MaxHealth;
            // Meghívjuk a szint befejeződését jelző eseményt
            OnLevelCompleted?.Invoke(true, playerHealthPercentage);
        }
    }

    /// <summary>
    /// Betölti a játékos által mentett szintet aszerint, hogy az aktuális szint egy boss harc vagy egy normál szint.
    /// A mentett adatokat használja a megfelelő szint betöltéséhez.
    /// </summary>
    /// <param name="saveData">A mentett adatok, amelyek tartalmazzák a játék aktuális állapotát és szintjét.</param>
    /// <returns>Visszatérési érték: `true`, ha a szint betöltése sikeres volt, `false`, ha hiba történt.</returns>
    public async Task<bool> LoadSavedLevelAsync(SaveData saveData)
    {
        try
        {
            // Ha a mentett szint egy boss harc, akkor a Boss szint betöltése
            if (saveData.gameData.gameLevel == GameLevel.BossBattle.ToString())
            {
                await LoadSavedBossLevelAsync(saveData); // Async metódus a boss szint betöltéséhez
            }
            else
            {
                await LoadNormalLevelAsync(saveData); // Async metódus a normál szint betöltéséhez
            }

            // Ha minden rendben, true-val tér vissza
            return true;
        }
        catch (Exception ex)
        {
            // Ha hiba történik, akkor azt logoljuk
            Debug.LogError($"Error during loading saved level! {ex.Message}");
            return false;  // Hibás betöltés esetén false-t ad vissza
        }
    }


    /// <summary>
    /// A mentett adatok alapján létrehozza a boss harc szinthez szükséges spawn pozíciókat.
    /// A mentett adatok között található prefab ID-k alapján meghatározza, hogy hol kell megjeleníteni a boss-t és a játékost.
    /// </summary>
    /// <param name="saveData">A mentett adatok, amelyek tartalmazzák a prefabokat és azok pozícióit.</param>
    /// <returns>Visszatérési érték: Egy lista a spawn pozíciókat tartalmazó `GameObjectPosition` objektumokkal.</returns>
    List<GameObjectPosition> GetBossLevelSpawnData(SaveData saveData)
    {
        // Lista, amely a boss szinthez szükséges spawn adatokat fogja tartalmazni
        List<GameObjectPosition> bossLevelSpawnData = new List<GameObjectPosition>();
        // Iterálunk a mentett prefab adatain
        foreach (var prefab in saveData.spawnerSaveData.prefabsData)
        {
            // Ha a prefab a boss prefabja, akkor a boss pozícióját (0, 0) beállítjuk
            if (prefab.prefabID == bossPrefab.ID)
            {
                bossLevelSpawnData.Add(new GameObjectPosition(bossPrefab.gameObject, new Vector2(0f, 0f)));
            }
            // Ha a prefab a játékos prefabja, akkor a játékos pozícióját (0, -10) beállítjuk
            if (prefab.prefabID == playerPrefab.ID)
            {
                bossLevelSpawnData.Add(new GameObjectPosition(playerPrefab.gameObject, new Vector2(0f, -10f)));
            }
        }

        // Visszatér a létrehozott spawn adatokat tartalmazó listával
        return bossLevelSpawnData;
    }


    /// <summary>
    /// Betölti a mentett adatokat és inicializálja a boss harc szintet.
    /// A mentett adatokat felhasználva beállítja a megfelelő pályát, objektumokat, karaktereket és eseményeket.
    /// </summary>
    /// <param name="saveData">A mentett adatok, amelyek tartalmazzák a szint és objektumok adatait.</param>
    /// <returns>Visszatérési érték: `true`, ha a betöltés sikeres volt, `false`, ha hiba történt.</returns>
    async Task<bool> LoadSavedBossLevelAsync(SaveData saveData)
    {
        bool asyncOperation;

        try
        {
            // SceneManager: megfelelő átvezető + pálya betöltése
            asyncOperation = await gameSceneManager.LoadCutsceneByLevelNameAsync(saveData.gameData.levelLayoutName);
            asyncOperation = await gameSceneManager.LoadLevelSceneByNameAsync(saveData.gameData.levelLayoutName);

            // ObjectPool és CharacterSetupManager hozzáadása a jelenethez (más komponenseknek szüksége van rá az init során)
            asyncOperation = await InstantiateTransientLevelElementsAsync(objectPoolPrefab, characterSetupManagerPrefab);
            if (!asyncOperation)
            {
                throw new Exception("Object instantiation failed.");
            }

            asyncOperation = await InstantiateBossObjectPool(bossObjectPoolPrefab);

            // SpawnManager: prefabok + pozíciók átadása
            spawnManager = FindObjectOfType<SpawnManager>();
            asyncOperation = await spawnManager.BossLevelInit(GetBossLevelSpawnData(saveData));

            // CharacterSetupManager - játékos és ellenségek értékeinek beállítása
            asyncOperation = await characterSetupManager.SetBossLevelCharactersAsync();
            if (!asyncOperation)
            {
                throw new Exception("Character setup failed.");
            }

            // Eseményfeliratkozások, játékbeli objektum-referenciák összegyűjtése
            asyncOperation = await SubscribeForBossLevelCharacterEvents();
            if (!asyncOperation)
            {
                throw new Exception("Event subscription failed.");
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

            // Ha minden lépés sikeres volt, akkor visszatérünk `true` értékkel
            return true;
        }
        catch (Exception ex)
        {
            // Hibák esetén logoljuk a hibát és visszatérünk `false` értékkel
            Debug.LogError($"{ex.Message}");
            return false;
        }

    }


    /// <summary>
    /// Betölti a normál szintet a mentett adatok alapján, beállítja az objektumokat, karaktereket és eseményeket.
    /// </summary>
    /// <param name="saveData">A mentett adatok, amelyek tartalmazzák a szint és objektumok adatait.</param>
    /// <returns>Visszatérési érték: `true`, ha a betöltés sikeres volt, `false`, ha hiba történt.</returns>
    /// <exception cref="Exception">Hibát dob, ha bármelyik lépés nem sikerül.</exception>
    async Task<bool> LoadNormalLevelAsync(SaveData saveData)
    {
        bool asyncOperation;

        // SceneManager: megfelelő átvezető + pálya betöltése
        asyncOperation = await gameSceneManager.LoadCutsceneByLevelNameAsync(saveData.gameData.levelLayoutName);
        asyncOperation = await gameSceneManager.LoadLevelSceneByNameAsync(saveData.gameData.levelLayoutName);

        // ObjectPool és CharacterSetupManager hozzáadása a jelenethez (más komponenseknek szüksége van rá az init során)
        asyncOperation = await InstantiateTransientLevelElementsAsync(objectPoolPrefab, characterSetupManagerPrefab);
        if (!asyncOperation)
        {
            throw new Exception("Object instantiation failed.");
        }

        spawnManager = FindObjectOfType<SpawnManager>();
        // SpawnManager: prefabok + pozíciók átadása
        asyncOperation = await spawnManager.LoadedLevelInit(GetSpawnManagerLoadDataFromSaveData(saveData));

        
        // CharacterSetupManager - játékos és ellenségek értékeinek beállítása
        asyncOperation = await characterSetupManager.SetNormalLevelCharactersAsync(gameStateManager.GameLevelToInt(gameStateManager.LevelNameToGameLevel(saveData.gameData.gameLevel)));
        if (!asyncOperation)
        {
            throw new Exception("Character setup failed.");
        }

        // Eseményfeliratkozások, játékbeli objektum-referenciák összegyűjtése
        asyncOperation = await SubscribeForNormalLevelCharacterEvents();
        if (!asyncOperation)
        {
            throw new Exception("Event subscription failed.");
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

        // Ha minden lépés sikeres volt, akkor visszatérünk `true` értékkel
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
