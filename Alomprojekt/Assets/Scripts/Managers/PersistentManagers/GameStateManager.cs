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

    void IsActualLevelCompleted(bool isCompleted)
    {
        levelmanager.OnLevelCompleted -= IsActualLevelCompleted;

        if (isCompleted)
        {
            Debug.Log("UPGRADES!!!");
            // change gameState
        }
        else
        {
            Debug.Log("GAME OVER!!!");
            // change gameState
        }

        gameSceneManager.LoadUtilityScene("ManagersTestScene");
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
