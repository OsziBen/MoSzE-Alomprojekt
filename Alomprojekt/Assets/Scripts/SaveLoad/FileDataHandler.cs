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
    /// Változók, amelyek tárolják az adatfájl elérési útját, nevét és az esetleges titkosítást.
    /// </summary>
    private string _dataDirPath = ""; // Az adatokat tartalmazó mappa elérési útja
    private string _dataFileName = ""; // Az adatfájl neve
    private bool _useEncryption = false; // Meghatározza, hogy a fájl titkosítva legyen-e

    /// <summary>
    /// Konstruktor, amely beállítja az adatfájl elérési útját, nevét és a titkosítás használatát.
    /// </summary>
    /// <param name="dataDirPath">Az adatokat tároló mappa elérési útja.</param>
    /// <param name="dataFileName">Az adatfájl neve.</param>
    /// <param name="useEncryption">A titkosítás használatának engedélyezése.</param>
    public FileDataHandler(string dataDirPath, string dataFileName, bool useEncryption)
    {
        this._dataDirPath = dataDirPath; // Beállítja az adatfájl elérési útját
        this._dataFileName = dataFileName; // Beállítja az adatfájl nevét
        this._useEncryption = useEncryption; // Beállítja, hogy használjunk-e titkosítást
    }


    /// <summary>
    /// Aszinkron módon betölti a játék adatokat egy fájlból.
    /// Ha a fájl nem található, alapértelmezett adatokat hoz létre.
    /// Titkosítás használatakor dekódolja a fájl tartalmát, mielőtt JSON formátumban deszerializálja.
    /// </summary>
    /// <returns>Betöltött játékadatokat tartalmazó `SaveData` objektum vagy null, ha hiba történt.</returns>
    public async Task<SaveData> LoadGameDataAsync()
    {
        // A fájl teljes elérési útjának létrehozása a mappa és a fájlnév alapján
        string fullPath = Path.Combine(_dataDirPath, _dataFileName);

        // Ellenőrzi, hogy a fájl létezik-e
        if (!File.Exists(fullPath))
        {
            // Figyelmeztet, ha a fájl nem található
            Debug.LogWarning("Save file not found. Initializing default data."); 
            return new SaveData(); // Visszaadja az alapértelmezett adatokat
        }

        try
        {
            // Fájl megnyitása olvasásra
            using (FileStream stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true))            
            using (StreamReader reader = new StreamReader(stream))
            {
                // Beolvassa a fájl teljes tartalmát
                string dataToLoad = await reader.ReadToEndAsync();

                // Ha titkosítás van használatban, akkor dekódoljuk az adatokat
                if (_useEncryption)
                {
                    dataToLoad = EncryptionHelper.Decrypt(dataToLoad);
                }

                // JSON formátumból deszerializáljuk a betöltött adatokat `SaveData` típusú objektummá
                return JsonUtility.FromJson<SaveData>(dataToLoad);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error while loading data: {ex.Message}");
            return null; // Ha hiba történik, visszaad null-t
        }

    }

    /// <summary>
    /// Aszinkron módon betölti a ranglista adatokat egy fájlból.
    /// Ha a fájl nem található, alapértelmezett ranglistát hoz létre.
    /// Titkosítás használatakor dekódolja a fájl tartalmát, mielőtt JSON formátumban deszerializálja.
    /// </summary>
    /// <returns>Betöltött ranglistát tartalmazó `ScoreboardData` objektum vagy null, ha hiba történt.</returns>
    public async Task<ScoreboardData> LoadScoreboardDataAsync()
    {
        // A fájl teljes elérési útjának létrehozása a mappa és a fájlnév alapján
        string fullPath = Path.Combine(_dataDirPath, _dataFileName);

        // Ellenőrzi, hogy a fájl létezik-e
        if (!File.Exists(fullPath))
        {
            Debug.LogWarning("Save file not found. Initializing default data."); // Figyelmeztet, ha a fájl nem található
            return new ScoreboardData(); // Visszaadja az alapértelmezett ranglistát
        }

        try
        {
            // Fájl megnyitása olvasásra
            using (FileStream stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true))
            using (StreamReader reader = new StreamReader(stream))
            {
                // Beolvassa a fájl teljes tartalmát
                string dataToLoad = await reader.ReadToEndAsync();

                // Ha titkosítás van használatban, akkor dekódoljuk az adatokat
                if (_useEncryption)
                {
                    dataToLoad = EncryptionHelper.Decrypt(dataToLoad);
                }

                // JSON formátumból deszerializáljuk a betöltött adatokat `ScoreboardData` típusú objektummá
                return JsonUtility.FromJson<ScoreboardData>(dataToLoad);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error while loading data: {ex.Message}");
            return null; // Ha hiba történik, visszaad null-t
        }

    }


    /// <summary>
    /// Aszinkron módon elmenti a játékadatokat egy fájlba.
    /// Titkosítás használatakor először titkosítja az adatokat, majd JSON formátumban menti el őket.
    /// Ha bármilyen hiba történik a fájl mentése közben, azt naplózza.
    /// </summary>
    /// <param name="data">A menteni kívánt játékadatokat tartalmazó `SaveData` objektum.</param>
    /// <returns>Visszaadja, hogy a mentés sikeres volt-e (`true`) vagy hibát észleltek (`false`).</returns>
    public async Task<bool> SaveGameDataAsync(SaveData data)
    {
        // A fájl teljes elérési útjának létrehozása a mappa és a fájlnév alapján
        string fullPath = Path.Combine(_dataDirPath, _dataFileName);

        try
        {
            // Létrehozza a mappát, ha még nem létezik
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));

            // A játékadatokat JSON formátumba konvertálja
            string dataToStore = JsonUtility.ToJson(data, true);

            // Ha titkosítás van használatban, akkor titkosítja az adatokat
            if (_useEncryption)
            {
                dataToStore = EncryptionHelper.Encrypt(dataToStore);
            }

            // Fájl megnyitása írásra
            using (FileStream stream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true))
            using (StreamWriter writer = new StreamWriter(stream))
            {
                // A titkosított vagy sima adatokat a fájlba írja.
                await writer.WriteAsync(dataToStore);
            }

            // Visszaadja, hogy a mentés sikeres volt
            return true;

        }
        catch (Exception ex)
        {
            Debug.LogError($"Error occured when trying to save data to file: {fullPath}\n{ex}");
            return false; // Ha hiba történik, visszaadja, hogy a mentés nem sikerült
        }
    }

    /// <summary>
    /// Aszinkron módon elmenti a ranglista adatokat egy fájlba.
    /// Titkosítás használatakor először titkosítja az adatokat, majd JSON formátumban menti el őket.
    /// Ha bármilyen hiba történik a fájl mentése közben, azt naplózza.
    /// </summary>
    /// <param name="data">A menteni kívánt ranglistát tartalmazó `ScoreboardData` objektum.</param>
    /// <returns>Visszaadja, hogy a mentés sikeres volt-e (`true`) vagy hibát észleltek (`false`).</returns>
    public async Task<bool> SaveScoreboardDataAsync(ScoreboardData data)
    {
        // A fájl teljes elérési útjának létrehozása a mappa és a fájlnév alapján
        string fullPath = Path.Combine(_dataDirPath, _dataFileName);

        try
        {
            // Létrehozza a mappát, ha még nem létezik
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));

            // A ranglista adatokat JSON formátumba konvertálja
            string dataToStore = JsonUtility.ToJson(data, true);

            // Ha titkosítás van használatban, akkor titkosítja az adatokat
            if (_useEncryption)
            {
                dataToStore = EncryptionHelper.Encrypt(dataToStore);
            }
            // Fájl megnyitása írásra
            using (FileStream stream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true))
            using (StreamWriter writer = new StreamWriter(stream))
            {
                // A titkosított vagy sima adatokat a fájlba írja.
                await writer.WriteAsync(dataToStore);
            }

            // Visszaadja, hogy a mentés sikeres volt
            return true;

        }
        catch (Exception ex)
        {
            Debug.LogError($"Error occured when trying to save data to file: {fullPath}\n{ex}");
            return false; // Ha hiba történik, visszaadja, hogy a mentés nem sikerült
        }
    }

}

