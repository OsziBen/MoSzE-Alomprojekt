using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class SaveLoadManager : BasePersistentManager<SaveLoadManager>
{
    /// <summary>
    /// Változók
    /// </summary>
    [Header("File Storage Config")]
    [SerializeField]
    private string _fileName;
    [SerializeField]
    private bool _useEncryption;

    private SaveData _saveData;


    /// <summary>
    /// Komponensek
    /// </summary>
    private FileDataHandler _dataHandler;


    /// <summary>
    /// Események
    /// </summary>
    public event Action<SaveData> OnSaveRequested;
    public event Action<SaveData> OnLoadRequested;


    /// <summary>
    /// 
    /// </summary>
    protected override void Initialize()
    {
        base.Initialize();
        _dataHandler = new FileDataHandler(Application.persistentDataPath, _fileName, _useEncryption);
    }


    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public async Task<bool> NewGame()   // async + eventekhez kapcsolódni
    {
        await Task.Yield();
        _saveData = new SaveData();

        return true;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public async Task<bool> LoadGameAsync() // async + eventekhez kapcsolódni
    {
        try
        {
            
            _saveData = await _dataHandler.LoadAsync();

            if (_saveData == null)
            {
                Debug.Log("No data was found. Initializing data to default.");
                return false; // No data found
            }
            else
            {
                // Data loaded successfully
                OnLoadRequested?.Invoke(_saveData);
                return true;
            }
        }
        catch (Exception ex)
        {
            // Handle the exception, such as logging it and returning false
            Debug.LogError($"Exception during loading game data: {ex.Message}");
            return false;
        }
    }


    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public async Task<bool> SaveGame()  // async + eventekhez kapcsolódni
    {
        _saveData = new SaveData();
        OnSaveRequested?.Invoke(_saveData);
        bool saved = await _dataHandler.SaveAsync(_saveData);
        if (!saved)
        {
            Debug.LogError("SAVE ERROR");
        }
        Debug.Log(Application.persistentDataPath);
        return true;
    }
}

