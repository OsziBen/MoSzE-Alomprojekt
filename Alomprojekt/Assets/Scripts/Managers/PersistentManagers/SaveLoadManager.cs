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
    private string _gameStateFileName; // A játék állapotának fájlneve
    [SerializeField]
    private bool _useSaveFileEncryption; // Ha igaz, akkor titkosítjuk a mentési fájlt

    [Header("Scoreboard File Config")]
    [SerializeField]
    private string _scoreboardFileName; // A pontszámlista fájlneve
    [SerializeField]
    private bool _useScoreboardFileEncryption; // Ha igaz, akkor titkosítjuk a pontszám fájlt

    private SaveData _saveData; // A játék mentési adatainak tárolására szolgáló változó

    private ScoreboardData _scoreboardData; // A pontszám adatait tároló változó


    /// <summary>
    /// Komponensek
    /// </summary>
    private FileDataHandler _gameStateDataHandler; // A játék állapotának kezeléséért felelős komponens
    private FileDataHandler _scoreboardDataHandler; // A pontszámadatok kezeléséért felelős komponens


    /// <summary>
    /// Események
    /// </summary>
    public event Action<SaveData> OnSaveRequested; // Esemény, amely akkor lép életbe, ha a mentés kérés történik
    public event Action<ScoreboardData> OnScoreboardUpdateRequested; // Esemény, amely akkor lép életbe, ha a pontszám frissítése történik
    //public event Action<SaveData> OnLoadRequested;


    /// <summary>
    /// Inicializálás
    /// </summary>
    protected override async void Initialize()
    {
        await Task.Yield(); // Várakozás, hogy a feladat (task) befejeződjön, mielőtt folytatná az inicializálást
        base.Initialize(); // A szülő osztály Initialize metódusának meghívása
        // A játékállapot adatkezelőjének inicializálása, meghatározva a mentési fájl helyét, nevét és titkosítást
        _gameStateDataHandler = new FileDataHandler(Application.persistentDataPath, _gameStateFileName, _useSaveFileEncryption);
        // A pontszám adatkezelőjének inicializálása, meghatározva a fájl helyét, nevét és titkosítást
        _scoreboardDataHandler = new FileDataHandler(Application.persistentDataPath, _scoreboardFileName, _useScoreboardFileEncryption);
    }


    /// <summary>
    /// Új játék indítása
    /// </summary>
    /// <returns>Visszaadja, hogy sikeres volt-e az új játék létrehozása</returns>
    public async Task<bool> NewGame()
    {
        await Task.Yield(); // Várakozás, hogy a feladat befejeződjön, mielőtt folytatná a kódot
        _saveData = new SaveData(); // Új mentési adatok objektum létrehozása
        _scoreboardData = new ScoreboardData(); // Új pontszám adatok objektum létrehozása

        return true; // Visszatérési érték: igaz, mert a játék újraindítása sikeres volt
    }

    /// <summary>
    /// Játék betöltése aszinkron módon
    /// </summary>
    /// <returns>Betöltött mentési adatokat ad vissza, vagy null-t, ha nem sikerült betölteni</returns>
    public async Task<SaveData> LoadGameAsync()
    {
        try
        {
            // A játék adatainak betöltése aszinkron módon a _gameStateDataHandler segítségével
            _saveData = await _gameStateDataHandler.LoadGameDataAsync();

            if (_saveData == null)
            {
                // Ha nincs mentett adat, akkor logolunk egy üzenetet, és null-t adunk vissza
                Debug.Log("No data was found. Initializing data to default.");
                return null;
            }
            else
            {
                // Ha a mentett adat sikeresen betöltődött, akkor azt visszaadjuk
                return _saveData;
            }
        }
        catch (Exception ex)
        {
            // Ha hiba történik a betöltés során, akkor azt logoljuk és null-t adunk vissza
            Debug.LogError($"Exception during loading game data: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Pontszámadatok betöltése aszinkron módon
    /// </summary>
    /// <returns>Betöltött pontszámadatokat ad vissza, vagy null-t, ha nem sikerült betölteni</returns>
    public async Task<ScoreboardData> LoadScoreboardAsync()
    {
        try
        {
            // A pontszámadatok betöltése aszinkron módon a _scoreboardDataHandler segítségével
            _scoreboardData = await _scoreboardDataHandler.LoadScoreboardDataAsync();

            if (_scoreboardData == null)
            {
                // Ha nincs pontszámadat, akkor logolunk egy üzenetet, és null-t adunk vissza
                Debug.Log("No scoreboard data found. Initializing data to default.");
                return null;
            }
            else
            {
                // Ha a pontszámadat sikeresen betöltődött, akkor azt visszaadjuk
                return _scoreboardData;
            }
        }
        catch (Exception ex)
        {
            // Ha hiba történik a betöltés során, akkor azt logoljuk és null-t adunk vissza
            Debug.LogError($"Exception during loading scoreboard data: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// A pontszámadatok frissítése aszinkron módon
    /// </summary>
    /// <returns>Visszaadja, hogy a pontszámadatok frissítése sikeres volt-e</returns>
    public async Task<bool> UpdateScoreboardDataAsync()
    {
        // Betöltjük a legfrissebb pontszámadatokat
        _scoreboardData = await LoadScoreboardAsync();

        // Ha a pontszámadatok betöltődtek, akkor meghívjuk a pontszám frissítési eseményt
        OnScoreboardUpdateRequested?.Invoke(_scoreboardData);

        // A pontszámadatok mentése aszinkron módon
        bool saved = await _scoreboardDataHandler.SaveScoreboardDataAsync(_scoreboardData);
        if (!saved)
        {
            // Ha a mentés nem sikerült, akkor hibát logolunk
            Debug.LogError("SCOREBOARD SAVE ERROR");
        }

        // Kiírjuk a mentési fájl helyét a logba (nyomkövetéshez)
        Debug.Log(Application.persistentDataPath);

        return true; // A frissítés sikeres
    }



    /// <summary>
    /// Ellenőrzi, hogy létezik-e mentési fájl
    /// </summary>
    /// <returns>Visszaadja, hogy létezik-e mentési fájl</returns>
    public bool SaveFileExists()
    {
        string fullPath = Path.Combine(Application.persistentDataPath, _gameStateFileName);
        // A mentési fájl teljes elérési útjának összeállítása a persistentDataPath és a fájlnevéből

        return File.Exists(fullPath); // Ellenőrzi, hogy létezik-e a fájl az adott elérési úton
    }

    /// <summary>
    /// A mentési fájl törlése aszinkron módon
    /// </summary>
    /// <returns>Visszaadja, hogy a fájl törlése sikeres volt-e</returns>
    public async Task<bool> DeleteSaveFile()
    {
        await Task.Yield(); // Várakozik, hogy a feladat befejeződjön

        try
        {
            if (SaveFileExists()) // Ha létezik mentési fájl
            {
                string fullPath = Path.Combine(Application.persistentDataPath, _gameStateFileName);
                // A fájl teljes elérési útja

                File.Delete(fullPath); // A fájl törlése
            }

            return true; // A fájl törlése sikeres
        }
        catch (Exception ex)
        {
            // Ha hiba történik a törlés során, akkor azt logoljuk és hamis értéket adunk vissza
            Debug.LogError($"Error during deleting existing save file! {ex.Message}");
            return false;
        }
    }



    /// <summary>
    /// Játék mentése aszinkron módon
    /// </summary>
    /// <returns>Visszaadja, hogy a mentés sikeres volt-e</returns>
    public async Task<bool> SaveGameAsync()
    {
        _saveData = new SaveData(); // Új mentési adatok létrehozása

        OnSaveRequested?.Invoke(_saveData); // Ha van mentési kérés esemény, akkor azt meghívjuk

        // A mentési adatok aszinkron mentése
        bool saved = await _gameStateDataHandler.SaveGameDataAsync(_saveData);
        if (!saved)
        {
            // Ha a mentés nem sikerült, akkor hibát logolunk
            Debug.LogError("SAVE ERROR");
        }

        // Kiírjuk a mentési fájl helyét a logba (nyomkövetéshez)
        Debug.Log(Application.persistentDataPath);

        return true; // A mentés sikeres.
    }

}

