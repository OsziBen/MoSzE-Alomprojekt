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
    private int _currentLevel = 1;
    //private int _totalLevels = 4;
    private bool _isStateChanging = false;

    private Queue<Func<Task>> deferredStateChanges = new Queue<Func<Task>>();


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

    public int CurrentLevel
    {
        get { return _currentLevel; }
        set { _currentLevel = value; }
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
        levelmanager.OnLevelCompleted += IsActualLevelCompleted;    // ezt újra aktiválni kell majd!!!, feliratkozás máshol
        saveLoadManager.OnSaveRequested += Save;
        levelmanager.OnPointsAdded += AddPoints;
        uiManager.OnStartNewGame += HandleStateChanged;
        uiManager.OnLoadGame += HandleStateChanged;
        uiManager.OnExitGame += HandleStateChanged;
        uiManager.OnGamePaused += HandleStateChanged;
        uiManager.OnGameResumed += HandleStateChanged;
        uiManager.OnBackToMainMenu += HandleStateChanged;

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

        }

        if (uiManager != null)
        {
            uiManager.OnStartNewGame -= HandleStateChanged;
            uiManager.OnLoadGame -= HandleStateChanged;
            uiManager.OnExitGame -= HandleStateChanged;
            uiManager.OnGamePaused -= HandleStateChanged;
            uiManager.OnGameResumed -= HandleStateChanged;
            uiManager.OnBackToMainMenu -= HandleStateChanged;

        }
    }

    public void AddPoints(int points)
    {
        this.PlayerPoints += points;
        OnPointsChanged?.Invoke(PlayerPoints);
        Debug.Log("PONTOK, LACIKÁM: " + this.PlayerPoints);
    }

    // TODO: SetState!
    async void IsActualLevelCompleted(bool isCompleted, float playerHealthPercentage)
    {
        levelmanager.OnLevelCompleted -= IsActualLevelCompleted;
        levelmanager.OnPointsAdded -= AddPoints;

        this.PlayerHealtPercenatge = playerHealthPercentage;
        Debug.Log(PlayerHealtPercenatge);

        try
        {
            if (isCompleted)
            {
                Debug.Log("UPGRADES!!!");
                // change gameState
                bool victorySceneLoaded = await gameSceneManager.LoadUtilitySceneAsync("Victory");
                if (!victorySceneLoaded)
                {
                    Debug.LogError("Level load failed!");
                }
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
            /*
            bool sceneLoaded = await gameSceneManager.LoadUtilityScene("ManagersTestScene");
            if (!sceneLoaded)
            {
                Debug.LogError("Level load failed!");
            }
            */
        }
        catch (Exception ex)
        {
            Debug.LogError($"Hiba történt az IsActualLevelCompleted metódusban: {ex.Message}");
        }
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
                    Debug.Log("NEW Game");
                    // load newGame cutscene :: sceneManager
                    //asyncOperation = await gameSceneManager.LoadAnimatedCutsceneAsync("NewGame");
                    
                    // load level 1 :: LevelManager
                    asyncOperation = await levelmanager.LoadNewLevelAsync(CurrentLevel);
                    if (asyncOperation)
                    {
                        // After level load is complete, change state to "Playing"
                        DeferStateChange(() => SetState(GameState.Playing));
                    }
                    // event feliratkozás IsActualLevelCompleted
                    break;

                case GameState.LoadingNextLevel:
                    string cutsceneRefName = "LevelTransition" + (CurrentLevel - 1).ToString() + CurrentLevel.ToString();
                    asyncOperation = await gameSceneManager.LoadAnimatedCutsceneAsync(cutsceneRefName);
                    // event feliratkozás IsActualLevelCompleted
                    break;

                case GameState.LoadingSavedGame:
                    Debug.Log("LOAD Game");
                    // LoadManager -> LevelManager ?
                    // event feliratkozás IsActualLevelCompleted
                    break;

                case GameState.Playing:
                    //Cursor.visible = false;  // Hides the cursor
                    //Cursor.lockState = CursorLockMode.Locked;  // Locks the cursor in the center (optional)
                    Time.timeScale = 1;
                    break;

                case GameState.Paused:
                    //Cursor.visible = true;  // Shows the cursor
                    //Cursor.lockState = CursorLockMode.None;  // Unlocks the cursor (optional)
                    Time.timeScale = 0;
                    break;

                case GameState.GameOver:
                    // előtte/utána valami kattintható felület?
                    asyncOperation = await gameSceneManager.LoadAnimatedCutsceneAsync("Defeat");
                    asyncOperation = await gameSceneManager.LoadUtilitySceneAsync("MainMenu");
                    break;

                case GameState.Victory:
                    // előtte/utána valami kattintható felület?
                    asyncOperation = await gameSceneManager.LoadAnimatedCutsceneAsync("Victory");
                    asyncOperation = await gameSceneManager.LoadUtilitySceneAsync("MainMenu");
                    break;

                case GameState.PlayerUpgrade:
                    //Cursor.visible = true;  // Shows the cursor
                    //Cursor.lockState = CursorLockMode.None;  // Unlocks the cursor (optional)
                    Time.timeScale = 0;
                    // ui elem megjelenítése
                    // gombnyomást követően/ event hatására állapotváltás
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
}
