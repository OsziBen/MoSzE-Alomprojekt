using GluonGui.WorkspaceWindow.Views.WorkspaceExplorer;
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
    //public event Action<SaveData> OnLoadRequested;


    /// <summary>
    /// 
    /// </summary>
    protected override async void Initialize()
    {
        await Task.Yield();
        base.Initialize();
        _dataHandler = new FileDataHandler(Application.persistentDataPath, _fileName, _useEncryption);
    }


    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public async Task<bool> NewGame()
    {
        await Task.Yield();
        _saveData = new SaveData();

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

            _saveData = await _dataHandler.LoadAsync();

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


    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public bool SaveFileExists()
    {
        string fullPath = Path.Combine(Application.persistentDataPath, _fileName);
        return File.Exists(fullPath);
    }

    public async Task<bool> DeleteSaveFile()
    {
        await Task.Yield();

        try
        {
            if (SaveFileExists())
            {
                string fullPath = Path.Combine(Application.persistentDataPath, _fileName);
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
        bool saved = await _dataHandler.SaveAsync(_saveData);
        if (!saved)
        {
            Debug.LogError("SAVE ERROR");
        }
        Debug.Log(Application.persistentDataPath);
        return true;
    }
}

