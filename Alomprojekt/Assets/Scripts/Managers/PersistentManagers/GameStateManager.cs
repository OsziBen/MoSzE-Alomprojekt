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
    private int _minLevel = 1;
    private int _totalLevels = 4;
    private bool _isStateChanging = false;

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

    public int TotalLevels
    {
        get { return _totalLevels; }
        set { _totalLevels = value; }
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

        // Persistent Manager esemény-feliratkozások
        levelmanager.OnLevelCompleted += IsActualLevelCompleted;
        saveLoadManager.OnSaveRequested += Save;
        levelmanager.OnPointsAdded += AddPoints;
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


    void Save(SaveData saveData)
    {
        saveData.gameData.gameLevel = _currentLevel;
        saveData.gameData.points = PlayerPoints;
        saveData.playerSaveData.currentHealtPercentage = PlayerHealtPercenatge;
    }

    private void OnDestroy()
    {
        if (saveLoadManager != null)
        {
            saveLoadManager.OnSaveRequested -= Save;
        }

        if (levelmanager != null)
        {
            levelmanager.OnLevelCompleted -= IsActualLevelCompleted;
            levelmanager.OnPointsAdded -= AddPoints;
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
                Debug.Log("GAME OVER!!!");
                // change gameState
                bool defeatSceneLoaded = await gameSceneManager.LoadUtilitySceneAsync("Defeat");
                if (!defeatSceneLoaded)
                {
                    Debug.LogError("Level load failed!");
                }
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
                    asyncOperation = await gameSceneManager.LoadUtilitySceneAsync("MainMenu");

                    // UIManager gombokat beállító függvény hívása!
                    break;

                case GameState.LoadingNewGame:
                    this.PlayerPoints = 0;
                    this.PlayerHealtPercenatge = 1f;
                    asyncOperation = await ResetCurrentLevel();

                    asyncOperation = await playerUpgradeManager.ResetPlayerUpgradesListsAsync();
                    Time.timeScale = 1;
                    // load newGame cutscene :: sceneManager
                    asyncOperation = await gameSceneManager.LoadAnimatedCutsceneAsync("NewGame");

                    // load level 1 :: LevelManager
                    asyncOperation = await levelmanager.LoadNewLevelAsync(GameLevelToInt(CurrentLevel));
                    if (asyncOperation)
                    {
                        // After level load is complete, change state to "Playing"
                        DeferStateChange(() => SetState(GameState.Playing));
                    }

                    break;

                case GameState.LoadingNextLevel:    
                    Time.timeScale = 1;

                    asyncOperation = await IncrementCurrentLevel();

                    if (CurrentLevel == GameLevel.BossBattle)
                    {
                        // BOSS FIGHT!
                        asyncOperation = await gameSceneManager.LoadUtilitySceneAsync("BossFight");

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
                    Debug.Log("PLAYERHP: " + PlayerHealtPercenatge);
                    Debug.Log("PLAYERPOINTS: " + PlayerPoints);

                    break;

                case GameState.LoadingSavedGame:
                    Debug.Log("LOAD Game");
                    // LoadManager -> LevelManager ?

                    break;

                case GameState.Playing:
                    Time.timeScale = 1;
                    //Cursor.visible = false;  // Hides the cursor
                    //Cursor.lockState = CursorLockMode.Locked;  // Locks the cursor in the center (optional)
                    break;

                case GameState.Paused:
                    Time.timeScale = 0;
                    //Cursor.visible = true;  // Shows the cursor
                    //Cursor.lockState = CursorLockMode.None;  // Unlocks the cursor (optional)
                    break;

                case GameState.GameOver:
                    // előtte/utána valami kattintható felület?
                    // csak akkor látszódjon a kisfilm, ha boss szinten vagyunk a halálkor!
                    asyncOperation = await gameSceneManager.LoadAnimatedCutsceneAsync("Defeat");
                    asyncOperation = await gameSceneManager.LoadUtilitySceneAsync("MainMenu");
                    break;

                case GameState.Victory:
                    // előtte/utána valami kattintható felület?
                    asyncOperation = await gameSceneManager.LoadAnimatedCutsceneAsync("Victory");
                    asyncOperation = await gameSceneManager.LoadUtilitySceneAsync("MainMenu");
                    break;

                case GameState.PlayerUpgrade:
                    Cursor.visible = true;
                    Cursor.lockState = CursorLockMode.None;
                    Time.timeScale = 0;

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
        return (int)gameLevel+1;
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
