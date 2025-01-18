using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class FileDataHandler
{
    /// <summary>
    /// Változók
    /// </summary>
    private string _dataDirPath = "";
    private string _dataFileName = "";
    private bool _useEncryption = false;


    /// <summary>
    /// 
    /// </summary>
    /// <param name="dataDirPath"></param>
    /// <param name="dataFileName"></param>
    /// <param name="useEncryption"></param>
    public FileDataHandler(string dataDirPath, string dataFileName, bool useEncryption)
    {
        this._dataDirPath = dataDirPath;
        this._dataFileName = dataFileName;
        this._useEncryption = useEncryption;
    }


    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public async Task<SaveData> LoadGameDataAsync()
    {
        string fullPath = Path.Combine(_dataDirPath, _dataFileName);

        if (!File.Exists(fullPath))
        {
            Debug.LogWarning("Save file not found. Initializing default data.");
            return new SaveData();
        }

        try
        {
            using (FileStream stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true))
            using (StreamReader reader = new StreamReader(stream))
            {
                string dataToLoad = await reader.ReadToEndAsync();

                if (_useEncryption)
                {
                    dataToLoad = EncryptionHelper.Decrypt(dataToLoad);
                }

                return JsonUtility.FromJson<SaveData>(dataToLoad);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error while loading data: {ex.Message}");
            return null;
        }

    }


    public async Task<ScoreboardData> LoadScoreboardDataAsync()
    {
        string fullPath = Path.Combine(_dataDirPath, _dataFileName);

        if (!File.Exists(fullPath))
        {
            Debug.LogWarning("Save file not found. Initializing default data.");
            return new ScoreboardData();
        }

        try
        {
            using (FileStream stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true))
            using (StreamReader reader = new StreamReader(stream))
            {
                string dataToLoad = await reader.ReadToEndAsync();

                if (_useEncryption)
                {
                    dataToLoad = EncryptionHelper.Decrypt(dataToLoad);
                }

                return JsonUtility.FromJson<ScoreboardData>(dataToLoad);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error while loading data: {ex.Message}");
            return null;
        }

    }


    /// <summary>
    /// 
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    public async Task<bool> SaveGameDataAsync(SaveData data)
    {
        string fullPath = Path.Combine(_dataDirPath, _dataFileName);

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));

            string dataToStore = JsonUtility.ToJson(data, true);

            if (_useEncryption)
            {
                dataToStore = EncryptionHelper.Encrypt(dataToStore);
            }

            using (FileStream stream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true))
            using (StreamWriter writer = new StreamWriter(stream))
            {
                await writer.WriteAsync(dataToStore);
            }

            return true;

        }
        catch (Exception ex)
        {
            Debug.LogError($"Error occured when trying to save data to file: {fullPath}\n{ex}");
            return false;
        }
    }


    public async Task<bool> SaveScoreboardDataAsync(ScoreboardData data)
    {
        string fullPath = Path.Combine(_dataDirPath, _dataFileName);

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));

            string dataToStore = JsonUtility.ToJson(data, true);

            if (_useEncryption)
            {
                dataToStore = EncryptionHelper.Encrypt(dataToStore);
            }

            using (FileStream stream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true))
            using (StreamWriter writer = new StreamWriter(stream))
            {
                await writer.WriteAsync(dataToStore);
            }

            return true;

        }
        catch (Exception ex)
        {
            Debug.LogError($"Error occured when trying to save data to file: {fullPath}\n{ex}");
            return false;
        }
    }

}

