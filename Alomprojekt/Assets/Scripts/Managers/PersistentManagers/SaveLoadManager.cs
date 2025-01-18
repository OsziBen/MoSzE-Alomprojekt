using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

public class SaveLoadManager : BasePersistentManager<SaveLoadManager>
{
    /// <summary>
    /// Változók
    /// </summary>
    [Header("Save File Config")]
    [SerializeField]
    private string _gameStateFileName;
    [SerializeField]
    private bool _useSaveFileEncryption;

    [Header("Scoreboard File Config")]
    [SerializeField]
    private string _scoreboardFileName;
    [SerializeField]
    private bool _useScoreboardFileEncryption;

    private SaveData _saveData;

    private ScoreboardData _scoreboardData;


    /// <summary>
    /// Komponensek
    /// </summary>
    private FileDataHandler _gameStateDataHandler;
    private FileDataHandler _scoreboardDataHandler;


    /// <summary>
    /// Események
    /// </summary>
    public event Action<SaveData> OnSaveRequested;
    public event Action<ScoreboardData> OnScoreboardUpdateRequested;
    //public event Action<SaveData> OnLoadRequested;


    /// <summary>
    /// 
    /// </summary>
    protected override async void Initialize()
    {
        await Task.Yield();
        base.Initialize();
        _gameStateDataHandler = new FileDataHandler(Application.persistentDataPath, _gameStateFileName, _useSaveFileEncryption);
        _scoreboardDataHandler = new FileDataHandler(Application.persistentDataPath, _scoreboardFileName, _useScoreboardFileEncryption);

    }


    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public async Task<bool> NewGame()   // Kell-e?
    {
        await Task.Yield();
        _saveData = new SaveData();
        _scoreboardData = new ScoreboardData();

        return true;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public async Task<SaveData> LoadGameAsync()
    {
        try
        {

            _saveData = await _gameStateDataHandler.LoadGameDataAsync();

            if (_saveData == null)
            {
                Debug.Log("No data was found. Initializing data to default.");
                return null;
            }
            else
            {
                return _saveData;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Exception during loading game data: {ex.Message}");
            return null;
        }
    }


    public async Task<ScoreboardData> LoadScoreboardDataAsync()
    {
        try
        {
            _scoreboardData = await _scoreboardDataHandler.LoadScoreboardDataAsync();
            
            if (_scoreboardData == null)
            {
                Debug.Log("No scoreboard data found. Initializing data to default.");
                return null;
            }
            else
            {
                return _scoreboardData;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Exception during loading scoreboard data: {ex.Message}");
            return null;
        }
    }

    public async Task<bool> UpdateScoreboardDataAsync()
    {
        _scoreboardData = await LoadScoreboardDataAsync();
        OnScoreboardUpdateRequested?.Invoke(_scoreboardData);

        bool saved = await _scoreboardDataHandler.SaveScoreboardDataAsync(_scoreboardData);
        if (!saved)
        {
            Debug.LogError("SCOREBOARD SAVE ERROR");
        }
        Debug.Log(Application.persistentDataPath);
        return true;
    }


    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public bool SaveFileExists()
    {
        string fullPath = Path.Combine(Application.persistentDataPath, _gameStateFileName);
        return File.Exists(fullPath);
    }

    public async Task<bool> DeleteSaveFile()
    {
        await Task.Yield();

        try
        {
            if (SaveFileExists())
            {
                string fullPath = Path.Combine(Application.persistentDataPath, _gameStateFileName);
                File.Delete(fullPath);
            }

            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error during deleting existing save file! {ex.Message}");
            return false;
        }
    }


    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public async Task<bool> SaveGameAsync()
    {
        _saveData = new SaveData();
        OnSaveRequested?.Invoke(_saveData);
        bool saved = await _gameStateDataHandler.SaveGameDataAsync(_saveData);
        if (!saved)
        {
            Debug.LogError("SAVE ERROR");
        }
        Debug.Log(Application.persistentDataPath);
        return true;
    }
}

