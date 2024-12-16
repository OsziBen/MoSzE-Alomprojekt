using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameSceneManager : BasePersistentManager<GameSceneManager>
{
    /// <summary>
    /// Változók
    /// </summary>
    // Szinthez tartozó scene-ek; Dictionary<szint száma, List<scene neve>>
    private readonly Dictionary<int, List<string>> levelScenes = new Dictionary<int, List<string>>();

    // Nem szinthez tartozó scene-ek (pl. MainMenu, Settings, stb.); Dictionary<kulcs, szint neve>
    // TODO: első paraméter konkretizálása, ha minden scene-t ismerünk (~enum)
    private readonly Dictionary<string, string> utilityScenes = new Dictionary<string, string>();

    // TODO: átvezető képernyő scene-ek

    private readonly System.Random random = new System.Random();


    /// <summary>
    /// Események
    /// </summary>
    //public event Action<bool> OnSceneLoaded;  // -> invoke() !


    /// <summary>
    /// A játék elindulásakor végrehajtott kezdeti műveletek.
    /// Betölti a szinteket a Build Settings-ből.
    /// </summary>
    private void Start()
    {
        LoadScenesFromBuildSettings();  // A szintek betöltése a Build Settings-ből

        //DebugLevelScenes();             // Tesztelés: Szintek
        //DebugUtilityScenes();           // Tesztelés: Nem szint scene-ek

        // Teszt: Betöltjük az összes szintet 5 másodperces időközönként
        //StartCoroutine(LoadScenesSequentially());
    }


    /// <summary>
    /// A szintek betöltése a Build Settings-ben található scene-ekből.
    /// A scene-ek nevét és típusát ellenőrzi, és a szinteket különböző listákban tárolja.
    /// A nem szint típusú scene-eket utility scene-eként menti el.
    /// </summary>
    void LoadScenesFromBuildSettings()
    {
        // Végigiterálunk a Build Settings-ben lévő összes scene-en
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            // A scene elérési útjának lekérése a build index alapján
            string scenePath = SceneUtility.GetScenePathByBuildIndex(i);

            // A scene nevének kinyerése a fájl elérési útjából (kiterjesztés nélkül)
            string sceneName = Path.GetFileNameWithoutExtension(scenePath);

            // Ha a scene neve "Level"-lel kezdődik, akkor azt szintként kezeljük
            if (sceneName.StartsWith("Level"))
            {
                // A szint számának kinyerése a névből
                int levelNumber = ExtractLevelNumber(sceneName);

                // Ha érvényes a szint szám, hozzáadjuk a levelScenes Dictionary-hez
                if (levelNumber != -1)
                {
                    // Ha még nem létezik a szint, új listát hozunk létre
                    if (!levelScenes.ContainsKey(levelNumber))
                    {
                        levelScenes[levelNumber] = new List<string>();
                    }
                    // A scene hozzáadása az adott szinthez
                    levelScenes[levelNumber].Add(sceneName);
                }
                else
                {
                    // Hibás névkonvenció, figyelmeztető üzenet
                    Debug.LogWarning($"Hibás névkonvenció: {sceneName}. Kihagyva.");
                }
            }
            else
            {
                // Ha nem "Level" típusú scene, akkor utility scene-ként kezeljük (pl. MainMenu, Settings, stb.)
                utilityScenes[sceneName] = sceneName;
                Debug.Log($"Nem pálya-scene (utility): {sceneName}. Tárolva.");
            }
            // TODO: átvezetők gyűjtése
            // TODO: boss pályák esetén több scene? -> rendezés?
        }
    }


    /// <summary>
    /// Kinyeri a szint számát a scene nevéből, ha az "Level" kezdetű.
    /// A név formátuma "LevelX_..." kell legyen, ahol X egy szám.
    /// </summary>
    /// <param name="sceneName">A scene neve, például "Level1_Layout1"</param>
    /// <returns>Ha sikerül kinyerni a szint számot, visszaadja azt; ha a formátum hibás, -1-et ad vissza.</returns>
    private int ExtractLevelNumber(string sceneName)
    {
        // Példa: "Level1_Layout1" -> 1
        // A név részekre bontása az "_" karakter mentén
        string[] parts = sceneName.Split('_');

        // Ellenőrizzük, hogy az első rész "Level" kezdetű-e
        if (parts.Length > 0 && parts[0].StartsWith("Level"))
        {
            // Az első rész 5. karakterétől kezdve található a szint száma
            string numberPart = parts[0].Substring(5);

            // Megpróbáljuk integer típussá konvertálni a kinyert részt
            if (int.TryParse(numberPart, out int levelNumber))
            {
                return levelNumber; // Ha sikerült a konverzió, visszaadjuk a szint számot
            }
        }

        // Ha a formátum nem megfelelő vagy nem sikerült konvertálni, -1-et adunk vissza
        return -1;
    }


    /// <summary>
    /// Véletlenszerű szintet tölt be a megadott szint alapján.
    /// Ha létezik a megadott szint a `levelScenes` Dictionary-ben, akkor egy véletlenszerűen választott scene-t tölt be.
    /// </summary>
    /// <param name="levelNum">A szint száma, amely alapján véletlenszerűen választunk egy scene-t</param>
    /// <returns>Visszaadja, hogy a scene sikeresen betöltődött-e (true, ha betöltődött, false, ha nem)</returns>
    public async Task<bool> LoadRandomSceneByLevelAsync(int levelNum)
    {
        // Ellenőrizzük, hogy létezik-e scene a megadott szinten
        if (levelScenes.ContainsKey(levelNum))
        {
            // A kiválasztott szinthez tartozó scene-ek listájának lekérése
            List<string> scenes = levelScenes[levelNum];

            // Véletlenszerű index kiválasztása a scene-ek listájában
            int randomIndex = random.Next(0, scenes.Count);

            // A kiválasztott véletlenszerű scene neve
            string randomScene = scenes[randomIndex];
            Debug.Log($"Betöltendő scene: {randomScene}");

            // Aszinkron scene betöltés (jelenleg tesztelési célból fixált "LevelTestScene" betöltése)
            // AsyncOperation asyncOperation = SceneManager.LoadSceneAsync(randomScene);

            // TEMP! - A teszteléshez ideiglenesen a "LevelTestScene" betöltése
            AsyncOperation asyncOperation = SceneManager.LoadSceneAsync("LevelTestScene");

            // Várunk, amíg a scene teljesen betöltődik
            while (!asyncOperation.isDone)
            {
                await Task.Yield(); // Aszinkron várakozás, hogy ne blokkoljuk a fő szálat
            }

            // Visszaadjuk, hogy a scene betöltődött-e
            return asyncOperation.isDone;
        }
        else
        {
            // Hibás szint, ha nem található scene a megadott szinthez
            Debug.LogError($"ERROR: Missing scene for Level {levelNum}!");
            return false; // Ha nem található a szint, nem sikerült betölteni
        }
    }


    /// <summary>
    /// Betölt egy utility scene-t a megadott kulcs alapján.
    /// Ha létezik a `utilityScenes` Dictionary-ben a kulcs, akkor a hozzá tartozó scene-t aszinkron módon betölti.
    /// </summary>
    /// <param name="sceneKey">A keresett utility scene kulcsa (pl. "MainMenu", "Settings", stb.)</param>
    /// <returns>Visszaadja, hogy a scene sikeresen betöltődött-e (true, ha betöltődött, false, ha nem)</returns>
    public async Task<bool> LoadUtilitySceneAsync(string sceneKey)
    {
        // Ellenőrizzük, hogy létezik-e a megadott kulcs a utilityScenes Dictionary-ben
        if (utilityScenes.ContainsKey(sceneKey))
        {
            // A megfelelő scene név lekérése a kulcs alapján
            string sceneName = utilityScenes[sceneKey];
            Debug.Log($"Betöltendő utility scene: {sceneName}");

            // Aszinkron módon betöltjük a scene-t
            AsyncOperation asyncOperation = SceneManager.LoadSceneAsync(sceneName);

            // Várunk, amíg a scene teljesen betöltődik
            while (!asyncOperation.isDone)
            {
                await Task.Yield(); // Aszinkron várakozás, hogy a fő szál ne blokkolódjon
            }

            // Visszaadjuk, hogy a scene betöltődött-e
            return asyncOperation.isDone;
        }
        else
        {
            // Hibás kulcs, ha nem található a megadott utility scene
            Debug.LogError($"Nincs ilyen utility scene: {sceneKey}");
            return false; // Ha nem található a scene, nem sikerült betölteni
        }
    }


    /// <summary>
    /// Kiírja a szinthez tartozó scene-eket a logba, amelyek a `levelScenes` Dictionary-ben vannak tárolva.
    /// Ez a metódus segít debugolni és ellenőrizni a szintekhez rendelt scene-eket.
    /// </summary>
    private void DebugLevelScenes()
    {
        // Kiíratjuk a szinthez tartozó scene-ek kezdetét a logba
        Debug.Log("Szinthez tartozó scene-ek:");

        // Végigiterálunk a levelScenes Dictionary szintjein
        foreach (var level in levelScenes)
        {
            // Kiíratjuk a szint számát
            Debug.Log($"Szint {level.Key}:");

            // Végigiterálunk a szinthez tartozó scene-eken és kiírjuk őket
            foreach (var scene in level.Value)
            {
                Debug.Log($"  {scene}");
            }
        }
    }


    /// <summary>
    /// Kiírja a nem szinthez tartozó scene-eket a logba, amelyek a `utilityScenes` Dictionary-ben vannak tárolva.
    /// Ez a metódus segít debugolni és ellenőrizni a utility típusú scene-eket, mint például a MainMenu vagy Settings.
    /// </summary>
    private void DebugUtilityScenes()
    {
        // Kiíratjuk a nem szinthez tartozó scene-ek kezdetét a logba
        Debug.Log("Nem szinthez tartozó scene-ek:");

        // Végigiterálunk az utilityScenes Dictionary elemein
        foreach (var scene in utilityScenes)
        {
            // Minden egyes utility scene kulcs-érték párját kiírjuk a logba
            Debug.Log($"{scene.Key}: {scene.Value}");
        }
    }


    /// <summary>
    /// Szintek és azok scene-jeinek betöltése sorban, minden scene betöltése között 5 másodperces várakozással.
    /// A szintek és scene-ek betöltése a `levelScenes` Dictionary-ből történik, és minden scene betöltése után várakozik, mielőtt a következő betöltődne.
    /// Ez a metódus segít debugolni és ellenőrizni a scene-adatok meglétét és helyes tárolását a megjelenítésen keresztül
    /// </summary>
    /// <returns>Visszaadja az IEnumerator-t, amely lehetővé teszi a szintenkénti betöltést az időzítés figyelembevételével</returns>
    private IEnumerator LoadScenesSequentially()
    {
        // Végigiterálunk a szinteken, amelyek a levelScenes Dictionary-ben vannak tárolva
        foreach (var level in levelScenes)
        {
            // Végigiterálunk a szinthez tartozó scene-eken
            foreach (var scene in level.Value)
            {
                // Kiíratjuk a betöltendő scene nevét a logba
                Debug.Log($"Betöltendő scene: {scene}");

                // Aszinkron módon betöltjük a scene-t
                SceneManager.LoadScene(scene);

                // Várakozunk 5 másodpercig a következő scene betöltése előtt
                yield return new WaitForSeconds(5f);  // Várakozás 5 másodpercig
            }
        }
    }

}
