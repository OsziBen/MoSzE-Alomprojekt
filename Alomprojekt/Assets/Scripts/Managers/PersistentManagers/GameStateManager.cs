using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameStateManager : BasePersistentManager<GameStateManager>
{
    /// <summary>
    /// Változók
    /// </summary>
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

    //private int currentLevel = 1;
    //private int totalLevels = 4;


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
        levelmanager.OnLevelCompleted += IsActualLevelCompleted;
    }

    async void IsActualLevelCompleted(bool isCompleted)
    {
        levelmanager.OnLevelCompleted -= IsActualLevelCompleted;

        try
        {
            if (isCompleted)
            {
                Debug.Log("UPGRADES!!!");
                // change gameState
                bool victorySceneLoaded = await gameSceneManager.LoadUtilityScene("Victory");
                if (!victorySceneLoaded)
                {
                    Debug.LogError("Level load failed!");
                }
            }
            else
            {
                Debug.Log("GAME OVER!!!");
                // change gameState
                bool defeatSceneLoaded = await gameSceneManager.LoadUtilityScene("Defeat");
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
