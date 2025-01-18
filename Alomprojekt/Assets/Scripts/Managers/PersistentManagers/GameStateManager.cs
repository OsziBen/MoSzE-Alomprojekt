using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using UnityEngine;

// TODO: ellenség halálakor eventre feliratkozni, pontok számolása (-> mentés)
public class GameStateManager : BasePersistentManager<GameStateManager>
{
    /// <summary>
    /// Változók
    /// </summary>
    private int _points = 0;
    private float _playerHealthPercentage = 1f;
    private GameLevel _currentLevel;
    private bool _isStateChanging = false;

    private float _currentRunTime = 0f;
    private string _currentRunDate = string.Empty;
    private string _currentRunPlayerName = string.Empty;

    private Queue<Func<Task>> deferredStateChanges = new Queue<Func<Task>>();

    public enum GameLevel
    {
        Level1,
        Level2,
        Level3,
        Level4,
        BossBattle
    }


    public enum GameState
    {
        MainMenu,
        LoadingNewGame,
        LoadingNextLevel,
        LoadingSavedGame,
        Playing,
        Paused,
        GameOver,
        Victory,
        PlayerUpgrade,
        Quitting
    }


    /// <summary>
    /// Komponensek
    /// </summary>
    LevelManager levelmanager;
    GameSceneManager gameSceneManager;
    SaveLoadManager saveLoadManager;
    PlayerUpgradeManager playerUpgradeManager;
    UIManager uiManager;
    Timer timer;


    /// <summary>
    /// Getterek és Setterek
    /// </summary>
    public bool IsStateChanging
    {
        get { return _isStateChanging; }
        set { _isStateChanging = value; }
    }
    public float PlayerHealtPercenatge
    {
        get { return _playerHealthPercentage; }
        set { _playerHealthPercentage = value; }
    }

    public int PlayerPoints
    {
        get { return _points; }
        set { _points = value; }
    }

    public GameLevel CurrentLevel
    {
        get { return _currentLevel; }
        set { _currentLevel = value; }
    }

    public float CurrentRunTime
    {
        get { return _currentRunTime; }
        set { _currentRunTime = value; }
    }

    public string CurrentRunDate
    {
        get { return _currentRunDate; }
        set { _currentRunDate = value; }
    }

    public string CurrentRunPlayerName
    {
        get { return _currentRunPlayerName; }
        set { _currentRunPlayerName = value; }
    }


    public GameState CurrentState { get; private set; } = GameState.MainMenu;


    /// <summary>
    /// Események
    /// </summary>
    //public event Action<GameState> OnStateChanged;
    public event Action<int> OnPointsChanged;


    protected override async void Initialize()
    {
        base.Initialize();
        // Persistent Manager referenciák összegűjtése
        levelmanager = FindObjectOfType<LevelManager>();
        gameSceneManager = FindObjectOfType<GameSceneManager>();
        saveLoadManager = FindObjectOfType<SaveLoadManager>();
        playerUpgradeManager = FindObjectOfType<PlayerUpgradeManager>();
        uiManager = FindAnyObjectByType<UIManager>();
        timer = FindObjectOfType<Timer>();

        // Persistent Manager esemény-feliratkozások
        levelmanager.OnLevelCompleted += IsActualLevelCompleted;
        levelmanager.OnGameFinished += IsGameFinished;
        levelmanager.OnPointsAdded += AddPoints;
        saveLoadManager.OnSaveRequested += SaveGameData;
        saveLoadManager.OnScoreboardUpdateRequested += UpdateScoreboardData;
        uiManager.OnStartNewGame += HandleStateChanged;
        uiManager.OnLoadGame += HandleStateChanged;
        uiManager.OnExitGame += HandleStateChanged;
        uiManager.OnGamePaused += HandleStateChanged;
        uiManager.OnGameResumed += HandleStateChanged;
        uiManager.OnBackToMainMenu += HandleStateChanged;
        uiManager.OnPurchaseOptionChosen += IsPurchaseOptionChosen;

        //await Task.Yield();
    }

    private async void HandleStateChanged(GameState newState)
    {
        Debug.Log("State is changing to: " + newState);

        // Await SetState to ensure state transition completes before continuing
        await SetState(newState);
    }

    void UpdateScoreboardData(ScoreboardData scoreboardData)
    {
        ScoreboardEntry newScoreboardEntry = new ScoreboardEntry(CurrentRunDate, CurrentRunPlayerName, PlayerPoints, timer.FormatTime(CurrentRunTime));
        if (scoreboardData == null)
        {
            Debug.Log("ZVVVVVVVVVVVVVVVVVVV");
        }
        scoreboardData.scoreboardEntries.Add(newScoreboardEntry);
    }


    void SaveGameData(SaveData saveData)
    {
        saveData.gameData.gameLevel = _currentLevel.ToString();
        saveData.gameData.points = PlayerPoints;
        saveData.playerSaveData.currentHealtPercentage = PlayerHealtPercenatge;
        saveData.gameData.currentRunTime = CurrentRunTime;
    }

    async Task<bool> SetLoadDataAsync(SaveData loadData)
    {
        try
        {
            await SetCurrentLevel(LevelNameToGameLevel(loadData.gameData.gameLevel));
            PlayerPoints = loadData.gameData.points;
            PlayerHealtPercenatge = loadData.playerSaveData.currentHealtPercentage;
            CurrentRunTime = loadData.gameData.currentRunTime;
            await playerUpgradeManager.SetLoadDataAsync(loadData);

            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error during load setup in GameStateManager! {ex.Message}");
            return false;
        }

    }

    public GameLevel LevelNameToGameLevel(string levelName)
    {
        if (Enum.TryParse(levelName, out GameLevel level))
        {
            return level;
        }
        else
        {
            throw new ArgumentException($"Invalid level value: {levelName}");
        }
    }

    private void OnDestroy()
    {
        if (saveLoadManager != null)
        {
            saveLoadManager.OnSaveRequested -= SaveGameData;
            saveLoadManager.OnScoreboardUpdateRequested -= UpdateScoreboardData;
        }

        if (levelmanager != null)
        {
            levelmanager.OnLevelCompleted -= IsActualLevelCompleted;
            levelmanager.OnPointsAdded -= AddPoints;
            levelmanager.OnGameFinished -= IsGameFinished;
        }

        if (uiManager != null)
        {
            uiManager.OnStartNewGame -= HandleStateChanged;
            uiManager.OnLoadGame -= HandleStateChanged;
            uiManager.OnExitGame -= HandleStateChanged;
            uiManager.OnGamePaused -= HandleStateChanged;
            uiManager.OnGameResumed -= HandleStateChanged;
            uiManager.OnBackToMainMenu -= HandleStateChanged;
            uiManager.OnPurchaseOptionChosen -= IsPurchaseOptionChosen;
        }
    }

    public void AddPoints(int points)
    {
        this.PlayerPoints += points;
        OnPointsChanged?.Invoke(PlayerPoints);
    }

    async void IsGameFinished(bool isFinished)
    {
        if (isFinished)
        {
            // itt jelenítjük meg a UI elemet
            await SetState(GameState.Victory);
        }
        else
        {
            // itt jelenítjük meg a UI elemet
            await SetState(GameState.GameOver);
        }
    }


    async void IsActualLevelCompleted(bool isCompleted, float playerHealthPercentage)
    {
        this.PlayerHealtPercenatge = playerHealthPercentage;
        Debug.Log(PlayerHealtPercenatge);

        try
        {
            if (isCompleted)
            {
                await SetState(GameState.PlayerUpgrade);
            }
            else
            {
                await SetState(GameState.GameOver);
            }

        }
        catch (Exception ex)
        {
            Debug.LogError($"Hiba történt az IsActualLevelCompleted metódusban: {ex.Message}");
        }
    }

    async void IsPurchaseOptionChosen(string upgradeID)
    {
        bool asyncOperation;
        asyncOperation = await playerUpgradeManager.PurchasePlayerUpgrade(upgradeID);
        if (!asyncOperation)
        {
            Debug.LogError("Hiba fejlesztés-vásárlása során!");
        }

        await SetState(GameState.LoadingNextLevel);
    }


    // TODO: cursor setter method
    public async Task SetState(GameState newState)
    {
        if (IsStateChanging)
        {
            Debug.LogWarning("State change already in progress! " + newState);
            return;
        }

        IsStateChanging = true;

        try
        {
            if (CurrentState == newState)
            {
                return;
            }

            CurrentState = newState;
            //OnStateChanged?.Invoke(CurrentState);

            bool asyncOperation;
            switch (CurrentState)
            {
                case GameState.MainMenu:
                    timer.StopTimer();
                    asyncOperation = await gameSceneManager.LoadUtilitySceneAsync("MainMenu");

                    asyncOperation = await playerUpgradeManager.ResetPlayerUpgradesListsAsync();
                    // UIManager gombokat beállító függvény hívása!
                    break;

                case GameState.LoadingNewGame:
                    this.PlayerPoints = 0;
                    this.PlayerHealtPercenatge = 1f;
                    this.CurrentRunTime = 0f;
                    asyncOperation = await ResetCurrentLevel();

                    // mentés törlése új játék esetén
                    //asyncOperation = await saveLoadManager.DeleteSaveFile();

                    Time.timeScale = 1;
                    // load newGame cutscene :: sceneManager
                    asyncOperation = await gameSceneManager.LoadAnimatedCutsceneAsync("NewGame");

                    // load level 1 :: LevelManager
                    asyncOperation = await levelmanager.LoadNewLevelAsync(GameLevelToInt(CurrentLevel));
                    if (asyncOperation)
                    {
                        // After level load is complete, change state to "Playing"
                        DeferStateChange(() => SetState(GameState.Playing));
                        timer.RestartTimer();
                    }

                    break;

                case GameState.LoadingNextLevel:
                    Time.timeScale = 1;
                    CurrentRunTime = timer.GetElapsedTime();

                    asyncOperation = await IncrementCurrentLevel();

                    if (CurrentLevel == GameLevel.BossBattle)
                    {
                        // BOSS FIGHT!
                        asyncOperation = await levelmanager.LoadBossLevelAsync();
                        if (asyncOperation)
                        {
                            // After level load is complete, change state to "Playing"
                            DeferStateChange(() => SetState(GameState.Playing));
                        }

                    }
                    else
                    {
                        string cutsceneRefName = "LevelTransition" + (GameLevelToInt(CurrentLevel) - 1).ToString() + GameLevelToInt(CurrentLevel).ToString();
                        asyncOperation = await gameSceneManager.LoadAnimatedCutsceneAsync(cutsceneRefName);

                        asyncOperation = await levelmanager.LoadNewLevelAsync(GameLevelToInt(CurrentLevel));
                        if (asyncOperation)
                        {
                            // After level load is complete, change state to "Playing"
                            DeferStateChange(() => SetState(GameState.Playing));
                        }
                    }

                    break;

                case GameState.LoadingSavedGame:
                    Time.timeScale = 1;

                    SaveData loadData = await saveLoadManager.LoadGameAsync();
                    if (loadData != null)
                    {
                        // GameStateManager adatok beállítása
                        asyncOperation = await SetLoadDataAsync(loadData);

                        // LevelManager hívása loadData-val
                        asyncOperation = await levelmanager.LoadSavedLevelAsync(loadData);
                        if (asyncOperation)
                        {
                            // After level load is complete, change state to "Playing"
                            DeferStateChange(() => SetState(GameState.Playing));
                            timer.SetTimer(CurrentRunTime);
                        }
                    }
                    else
                    {
                        Debug.LogError("LOAD DATA IS NULL!");
                    }

                    break;

                case GameState.Playing:
                    Time.timeScale = 1;
                    timer.ResumeTimer();
                    //Cursor.visible = false;  // Hides the cursor
                    //Cursor.lockState = CursorLockMode.Locked;  // Locks the cursor in the center (optional)
                    break;

                case GameState.Paused:
                    Time.timeScale = 0;
                    timer.StopTimer();
                    //Cursor.visible = true;  // Shows the cursor
                    //Cursor.lockState = CursorLockMode.None;  // Unlocks the cursor (optional)
                    break;

                case GameState.GameOver:
                    asyncOperation = await saveLoadManager.DeleteSaveFile();

                    // csak akkor látszódjon a kisfilm, ha boss szinten vagyunk a halálkor!
                    if (CurrentLevel == GameLevel.BossBattle)
                    {
                        asyncOperation = await gameSceneManager.LoadAnimatedCutsceneAsync("Defeat");
                    }
                    Time.timeScale = 0;
                    asyncOperation = await uiManager.DisplayDefeatPanelAsync();

                    break;

                case GameState.Victory:
                    timer.StopTimer();
                    CurrentRunTime = timer.GetElapsedTime();
                    Debug.Log(timer.FormatTime(CurrentRunTime));

                    // végső idő
                    CurrentRunTime = timer.GetElapsedTime();    // formázás stringként
                    // aktuális dátum
                    CurrentRunDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                    
                    asyncOperation = await saveLoadManager.DeleteSaveFile();

                    asyncOperation = await gameSceneManager.LoadAnimatedCutsceneAsync("Victory");
                    Time.timeScale = 0;
                    asyncOperation = await uiManager.DisplayVictoryPanelAsync();

                    break;

                case GameState.PlayerUpgrade:
                    Cursor.visible = true;
                    Cursor.lockState = CursorLockMode.None;
                    Time.timeScale = 0;
                    timer.StopTimer();

                    asyncOperation = await playerUpgradeManager.GenerateCurrentShopUpgradesAsync(GameLevelToInt(CurrentLevel), PlayerHealtPercenatge);
                    asyncOperation = await uiManager.LoadUpgradesShopUIAsync(playerUpgradeManager.CurrentShopUpgrades);

                    break;

                case GameState.Quitting:
                    Debug.Log("EXIT Game");
                    Application.Quit();
                    break;
            }

            Debug.Log(CurrentState);
        }
        finally
        {
            IsStateChanging = false;

            while (deferredStateChanges.Count > 0)
            {
                var deferredAction = deferredStateChanges.Dequeue();
                await deferredAction(); // Ensure the deferred action is awaited
            }
        }


    }

    private void DeferStateChange(Func<Task> action)
    {
        deferredStateChanges.Enqueue(action);
    }

    public int GameLevelToInt(GameLevel gameLevel)
    {
        return (int)gameLevel + 1;
    }

    public GameLevel IntToGameLevel(int levelValue)
    {
        if (Enum.IsDefined(typeof(GameLevel), levelValue))
        {
            return (GameLevel)levelValue;
        }
        else
        {
            throw new ArgumentException($"Invalid level value: {levelValue}");
        }
    }


    async Task<bool> SetCurrentLevel(GameLevel level)
    {
        await Task.Yield();

        try
        {
            CurrentLevel = level;
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"ERROR DURING INCREMIENTING LEVEL! {ex.Message}");
            return false;
        }
    }


    async Task<bool> IncrementCurrentLevel()
    {
        await Task.Yield();

        try
        {
            CurrentLevel++;
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"ERROR DURING INCREMIENTING LEVEL! {ex.Message}");
            return false;
        }

    }


    async Task<bool> ResetCurrentLevel()
    {
        await Task.Yield();

        try
        {
            CurrentLevel = GameLevel.Level1;
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"ERROR DURING INCREMIENTING LEVEL! {ex.Message}");
            return false;
        }
    }
}
