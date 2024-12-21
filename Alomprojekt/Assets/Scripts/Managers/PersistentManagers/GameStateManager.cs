using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEngine;

// TODO: ellenség halálakor eventre feliratkozni, pontok számolása (-> mentés)
public class GameStateManager : BasePersistentManager<GameStateManager>
{
    /// <summary>
    /// Változók
    /// </summary>
    private int _points = 0;
    private float _playerHealthPercentage = 100f;
    private int _currentLevel = 1;
    private int _totalLevels = 4;


    public enum GameState
    {
        MainMenu,
        Playing,
        Paused,
        GameOver,
        Victory,
        PlayerUpgrade
    }


    /// <summary>
    /// Komponensek
    /// </summary>
    LevelManager levelmanager;
    GameSceneManager gameSceneManager;
    SaveLoadManager saveLoadManager;


    /// <summary>
    /// Getterek és Setterek
    /// </summary>
    public GameState CurrentState { get; private set; } = GameState.MainMenu;


    /// <summary>
    /// Események
    /// </summary>
    public event Action<GameState> OnStateChanged;


    protected override void Initialize()
    {
        base.Initialize();
        levelmanager = FindObjectOfType<LevelManager>();
        gameSceneManager = FindObjectOfType<GameSceneManager>();
        saveLoadManager = FindObjectOfType<SaveLoadManager>();
        levelmanager.OnLevelCompleted += IsActualLevelCompleted;    // ezt újra aktiválni kell majd!!!
        saveLoadManager.OnSaveRequested += Save;
        levelmanager.OnPointsAdded += AddPoints;
    }


    void Save(SaveData saveData)
    {
        saveData.gameData.gameLevel = _currentLevel;
        saveData.gameData.points = _points;
        saveData.playerSaveData.currentHealtPercentage = _playerHealthPercentage;
    }

    private void OnDestroy()
    {
        saveLoadManager.OnSaveRequested -= Save;
    }

    public void AddPoints(int points)
    {
        this._points += points;
        Debug.Log("PONTOK, LACIKÁM: " + this._points);
    }

    async void IsActualLevelCompleted(bool isCompleted, float playerHealthPercentage)
    {
        levelmanager.OnLevelCompleted -= IsActualLevelCompleted;
        levelmanager.OnPointsAdded -= AddPoints;

        this._playerHealthPercentage = playerHealthPercentage;
        Debug.Log(this._playerHealthPercentage);

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


    public void SetState(GameState newState)
    {
        if (CurrentState == newState)
        {
            return;
        }

        CurrentState = newState;
        OnStateChanged?.Invoke(CurrentState);


        switch (CurrentState)
        {
            case GameState.MainMenu:
                break;
            case GameState.Playing:
                Time.timeScale = 1;
                break;
            case GameState.Paused:
                Time.timeScale = 0;
                break;
            case GameState.GameOver:
                break;
            case GameState.Victory:
                break;
            case GameState.PlayerUpgrade:
                break;
        }
    }
}
