using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static GameStateManager;

public class GameSceneManager : BasePersistentManager<GameSceneManager>
{
    public class CutsceneData
    {
        public string CutsceneRefName { get; set; }
        public string CutsceneFullName { get; set; }

        public CutsceneData(string refName, string fullName)
        {
            CutsceneRefName = refName;
            CutsceneFullName = fullName;
        }
    }

    /// <summary>
    /// Változók
    /// </summary>
    // Szinthez tartozó scene-ek; Dictionary<szint száma, List<scene neve>>
    private readonly Dictionary<int, List<string>> levelScenes = new Dictionary<int, List<string>>();

    // Nem szinthez tartozó scene-ek (pl. MainMenu, Settings, stb.); Dictionary<kulcs, szint neve>
    // TODO: első paraméter konkretizálása, ha minden scene-t ismerünk (~enum)
    private readonly Dictionary<string, string> utilityScenes = new Dictionary<string, string>();

    // Cutscene-ek, kategóriatípusokra lebontva; Dictionary<CutsceneType; List<CutsceneData>>
    private readonly Dictionary<string, List<CutsceneData>> cutscenes = new Dictionary<string, List<CutsceneData>>();

    // Jelenlegi cutscene-hez tartozó képek, ezeket fogjuk később animálni
    private List<Image> currentAnimatedCutsceneImages;

    [Header("Animation Settings")]
    [SerializeField]
    private float bufferTimeBeforeFirstFade; // New variable for the buffer time
    [SerializeField]
    private float fadeDuration; // Duration for each image fade effect in seconds
    [SerializeField]
    private float waitDuration; // Duration to wait before starting next image fade
    private float targetAlpha = 1f; // Set the target alpha to max (fully opaque)

    // System.Random
    private readonly System.Random random = new System.Random();


    /// <summary>
    /// Komponensek
    /// </summary>
    SaveLoadManager saveLoadManager;

    /// <summary>
    /// Események
    /// </summary>
    //public event Action<bool> OnSceneLoaded;  // -> invoke() !


    protected override async void Initialize()
    {
        await Task.Yield();
        base.Initialize();
        LoadScenesFromBuildSettings();
        saveLoadManager = FindObjectOfType<SaveLoadManager>();
        saveLoadManager.OnSaveRequested += Save;
        //LogCutscenes(cutscenes);
    }


    void Save(SaveData saveData)
    {
        saveData.gameData.levelLayoutName = GetCurrentSceneName();
    }


    string GetCurrentSceneName()
    {
        return SceneManager.GetActiveScene().name;
    }

    private void OnDestroy()
    {
        if (saveLoadManager != null)
        {
            saveLoadManager.OnSaveRequested -= Save;
        }
    }


    /// <summary>
    /// A játék elindulásakor végrehajtott kezdeti műveletek.
    /// Betölti a szinteket a Build Settings-ből.
    /// </summary>
    private void Start()
    {
        //LoadScenesFromBuildSettings();  // A szintek betöltése a Build Settings-ből

        //DebugLevelScenes();             // Tesztelés: Szintek
        //DebugUtilityScenes();           // Tesztelés: Nem szint scene-ek

        // Teszt: Betöltjük az összes szintet 5 másodperces időközönként
        //StartCoroutine(LoadScenesSequentially());
    }


    /// <summary>
    /// A scene-ek betöltése a Build Settings-ben található jelenetekből, kategorizálva azokat típusuk szerint.
    /// A scene-ek nevét elemezve három kategóriába soroljuk őket:
    /// - Szintek: "Level" kezdetű nevek alapján csoportosítva.
    /// - Átvezetők: "Cutscene" kezdetű nevek alapján típus szerint rendezve.
    /// - Utility jelenetek: Minden egyéb típusú scene külön kategóriába kerül, nem szint funkciókra.
    /// </summary>
    void LoadScenesFromBuildSettings()
    {
        // Végigmegyünk az összes jeleneten, amelyek a Build Settings-ben szerepelnek
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            // A jelenet elérési útjának lekérése a build index alapján
            string scenePath = SceneUtility.GetScenePathByBuildIndex(i);

            // A jelenet nevének kinyerése a fájl elérési útjából (kiterjesztés nélkül)
            string sceneName = Path.GetFileNameWithoutExtension(scenePath);

            // Ellenőrizzük, hogy a jelenet neve "Level"-lel kezdődik-e
            if (sceneName.StartsWith("Level"))
            {
                // A szint számának kinyerése a jelenet nevéből
                int levelNumber = ExtractLevelNumber(sceneName);

                // Ha érvényes szintszámot találtunk, hozzáadjuk a levelScenes szótárhoz
                if (levelNumber != -1)
                {
                    // Ha a szint még nem létezik, létrehozunk egy új listát hozzá
                    if (!levelScenes.ContainsKey(levelNumber))
                    {
                        levelScenes[levelNumber] = new List<string>();
                    }
                    // Hozzáadjuk a jelenetet a megfelelő szint listájához
                    levelScenes[levelNumber].Add(sceneName);
                }
                else
                {
                    // Figyelmeztetést írunk ki, ha a névkonvenció nem megfelelő
                    Debug.LogWarning($"Hibás névkonvenció: {sceneName}. Kihagyva.");
                }
            }
            else if (sceneName.StartsWith("Cutscene")) // Ellenőrizzük, hogy a jelenet átvezető-e
            {
                // Kinyerjük az átvezető típusát és referencia nevét
                string cutsceneTypeName = ExtractCutsceneTypeName(sceneName);
                string cutsceneRefName = ExtractCutsceneRefName(sceneName);

                // Ha az adott átvezető típus még nem létezik, létrehozunk egy új listát hozzá
                if (!cutscenes.ContainsKey(cutsceneTypeName))
                {
                    cutscenes[cutsceneTypeName] = new List<CutsceneData>();
                }
                // Hozzáadjuk az átvezetőt az adott típus listájához
                cutscenes[cutsceneTypeName].Add(new CutsceneData(cutsceneRefName, sceneName));
            }
            else
            {
                // Ha a jelenet nem szint és nem átvezető, akkor utility jelenetként kezeljük
                utilityScenes[sceneName] = sceneName;
                Debug.Log($"Utility jelenet észlelve: {sceneName}. Tárolva.");
            }

            // TODO: boss pályák esetén több scene? -> rendezés?
        }
    }


    /// <summary>
    /// Egy átvezető (Cutscene) típusának kinyerése a jelenet nevéből.
    /// </summary>
    /// <param name="cutsceneName">Az átvezető jelenet neve.</param>
    /// <returns>Az átvezető típusa.</returns>
    private string ExtractCutsceneTypeName(string cutsceneName)
    {
        return ExtractCutscenePart(cutsceneName, 1);
    }


    /// <summary>
    /// Egy átvezető (Cutscene) referencia nevének kinyerése a jelenet nevéből.
    /// </summary>
    /// <param name="cutsceneName">Az átvezető jelenet neve.</param>
    /// <returns>Az átvezető referencia neve.</returns>
    private string ExtractCutsceneRefName(string cutsceneName)
    {
        return ExtractCutscenePart(cutsceneName, 2);
    }


    /// <summary>
    /// Egy átvezető (Cutscene) nevének egy adott részét kinyerő metódus.
    /// A név egyes részeit az alulvonás ('_') karakter választja el egymástól.
    /// </summary>
    /// <param name="cutsceneName">Az átvezető jelenet teljes neve.</param>
    /// <param name="index">A kívánt rész indexe (0-alapú).</param>
    /// <returns>Az átvezető nevének kinyert része, vagy null, ha az index érvénytelen.</returns>
    private string ExtractCutscenePart(string cutsceneName, int index)
    {
        if (string.IsNullOrWhiteSpace(cutsceneName))
        {
            return null;
        }

        string[] parts = cutsceneName.Split('_');
        return parts.Length > index ? parts[index] : null;
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
            UnityEngine.AsyncOperation asyncOperation = SceneManager.LoadSceneAsync(randomScene);

            // TEMP! - A teszteléshez ideiglenesen a "LevelTestScene" betöltése
            //AsyncOperation asyncOperation = SceneManager.LoadSceneAsync("Level1_Layout1");

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
    /// Animált átvezető betöltése aszinkron módon a megadott referencia név alapján.
    /// </summary>
    /// <param name="cutsceneRefName">Az átvezető referencia neve, amely alapján betöltjük a jelenetet.</param>
    /// <returns>Igaz, ha a betöltés sikeres, egyébként hamis.</returns>
    public async Task<bool> LoadAnimatedCutsceneAsync(string cutsceneRefName)
    {
        // Kikeressük az átvezetőt az összes elérhető kategóriából
        var foundCutscene = cutscenes
        .SelectMany(category => category.Value)
        .FirstOrDefault(cutscene => cutscene.CutsceneRefName == cutsceneRefName);

        if (foundCutscene != null)
        {
            // Megkapjuk az átvezető teljes nevét
            string cutsceneName = foundCutscene.CutsceneFullName;
            Debug.Log($"Betöltendő utility scene: {cutsceneName}");

            // Aszinkron módon betöltjük a jelenetet
            UnityEngine.AsyncOperation asyncOperation = SceneManager.LoadSceneAsync(cutsceneName);

            // Várakozunk, amíg a betöltés be nem fejeződik
            while (!asyncOperation.isDone)
            {
                await Task.Yield();
            }

            // Animációk lejátszása
            bool animations = await PlayAnimationAsync();
            if (!animations)
            {
                Debug.LogError("ANIMATION ERROR!");
            }

            return asyncOperation.isDone;
        }
        else
        {
            // Hibát jelezünk, ha a jelenet nem található
            Debug.LogError($"Nincs ilyen utility scene: {cutsceneRefName}");
            return false; // Ha nem található a scene, nem sikerült betölteni
        }
    }


    /// <summary>
    /// Az animált átvezető animációinak lejátszása aszinkron módon.
    /// </summary>
    /// <returns>
    /// Igaz, ha az animáció sikeresen lejátszódott, vagy hamis, ha a folyamat megszakadt.
    /// </returns>
    public async Task<bool> PlayAnimationAsync()
    {
        // Kiválasztjuk az összes jelenlegi Image komponensű objektumot a jelenetben
        currentAnimatedCutsceneImages = FindObjectsOfType<Image>().ToList();

        // Az Image komponenseket a hierarchiában elfoglalt pozíciójuk (sibling index) alapján rendezzük
        currentAnimatedCutsceneImages.Sort((x, y) => x.transform.GetSiblingIndex().CompareTo(y.transform.GetSiblingIndex()));

        // Létrehozunk egy TaskCompletionSource-t, amely lehetővé teszi az animáció befejezésének várakozását
        var taskCompletionSource = new TaskCompletionSource<bool>();

        // Elindítjuk a képek fade animációját végrehajtó coroutine-t
        StartCoroutine(FadeImagesOverTime(taskCompletionSource));

        // Várakozunk, amíg a TaskCompletionSource jelez, hogy a coroutine befejeződött
        return await taskCompletionSource.Task;
    }


    /// <summary>
    /// Coroutine, amely a képeket fokozatosan átlátszóvá vagy teljesen láthatóvá teszi (fade animáció),
    /// majd jelzi a TaskCompletionSource-nek, hogy az animációk befejeződtek.
    /// </summary>
    /// <param name="taskCompletionSource">
    /// A TaskCompletionSource objektum, amely a coroutine futásának végén jelez az aszinkron metódusnak.
    /// </param>
    /// <returns>Az animáció lépései között eltelt időt kezelő IEnumerator.</returns>
    private IEnumerator FadeImagesOverTime(TaskCompletionSource<bool> taskCompletionSource)
    {
        // Az első fade animáció megkezdése előtti pufferidő (várakozás)
        yield return new WaitForSeconds(bufferTimeBeforeFirstFade);

        // Végigmegyünk az animált átvezetőhöz tartozó képek listáján
        foreach (var image in currentAnimatedCutsceneImages)
        {
            // Elindítjuk az adott kép átlátszóságának (alpha) változtatását
            StartCoroutine(FadeImageAlpha(image, image.color.a, targetAlpha, fadeDuration));

            // Megvárjuk, hogy az aktuális fade animáció időtartama és a képek közötti szünet leteljen
            yield return new WaitForSeconds(fadeDuration + waitDuration);
        }

        // Minden animáció befejezése után jelezzük a TaskCompletionSource-nek, hogy a folyamat véget ért
        taskCompletionSource.SetResult(true);
    }


    /// <summary>
    /// Coroutine, amely egy adott kép (Image) átlátszósági értékét (alpha) fokozatosan változtatja.
    /// </summary>
    /// <param name="image">A módosítandó kép (Image) objektum.</param>
    /// <param name="startAlpha">A kezdő alpha érték (0 = teljesen átlátszó, 1 = teljesen látható).</param>
    /// <param name="targetAlpha">A cél alpha érték (0 = teljesen átlátszó, 1 = teljesen látható).</param>
    /// <param name="duration">Az animáció időtartama másodpercekben.</param>
    /// <returns>Az animáció időbeli lefolyását kezelő IEnumerator.</returns>
    private IEnumerator FadeImageAlpha(Image image, float startAlpha, float targetAlpha, float duration)
    {
        // Az animációhoz eltelt idő nyilvántartása
        float timeElapsed = 0f;

        // Amíg az eltelt idő kevesebb, mint az animáció időtartama
        while (timeElapsed < duration)
        {
            // Növeljük az eltelt időt a frame-enként eltelt idővel
            timeElapsed += Time.deltaTime;

            // Az eltelt idő normalizált értéke (0-tól 1-ig)
            float normalizedTime = timeElapsed / duration;

            // Az alpha érték sima átmenetének számítása
            float newAlpha = Mathf.Lerp(startAlpha, targetAlpha, normalizedTime);

            // Az aktuális szín módosítása az új alpha értékkel
            Color currentColor = image.color;
            image.color = new Color(currentColor.r, currentColor.g, currentColor.b, newAlpha);

            // Egy frame-et várakozunk, mielőtt folytatjuk az animációt
            yield return null;
        }

        // Az animáció végén az alpha érték pontosan a célértékre állítása
        Color finalColor = image.color;
        image.color = new Color(finalColor.r, finalColor.g, finalColor.b, targetAlpha);
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
            UnityEngine.AsyncOperation asyncOperation = SceneManager.LoadSceneAsync(sceneName);

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

    public async Task<bool> LoadCutsceneByLevelNameAsync(string levelName)
    {
        bool asyncOperation;

        try
        {
            int levelNum = ExtractLevelNumber(levelName);

            if (levelNum == -1) // Boss Level
            {
                asyncOperation = await LoadAnimatedCutsceneAsync("LevelTransition34");
            }
            else if (levelNum == 1) // Első pálya
            {
                asyncOperation = await LoadAnimatedCutsceneAsync("NewGame");
            }
            else
            {
                string cutsceneRefName = "LevelTransition" + (levelNum - 1).ToString() + levelNum.ToString();
                asyncOperation = await LoadAnimatedCutsceneAsync(cutsceneRefName);
            }

            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error during loading cutscene by level name! {ex.Message}");
            return false;
        }
    }


    public async Task<bool> LoadLevelSceneByNameAsync(string levelName)
    {
        try
        {
            if (levelName == GameLevel.BossBattle.ToString())
            {
                bool asyncOp = await LoadUtilitySceneAsync("BossFight");
            }
            else
            {
                UnityEngine.AsyncOperation asyncOperation = SceneManager.LoadSceneAsync(levelName);
                // Várunk, amíg a scene teljesen betöltődik
                while (!asyncOperation.isDone)
                {
                    await Task.Yield(); // Aszinkron várakozás, hogy ne blokkoljuk a fő szálat
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error during loading saved scene data! {ex.Message}");
            return false;
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


    private void LogCutscenes(Dictionary<string, List<CutsceneData>> cutscenes)
    {
        foreach (var category in cutscenes)
        {
            string categoryName = category.Key;
            Debug.Log($"Category: {categoryName}");

            foreach (var cutscene in category.Value)
            {
                Debug.Log($"    Cutscene Ref Name: {cutscene.CutsceneRefName}, Full Name: {cutscene.CutsceneFullName}");
            }
        }
    }


}
