using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameSceneManager : BasePersistentManager<GameSceneManager>
{
    // Szinthez tartozó scene-ek
    private Dictionary<int, List<string>> levelScenes = new Dictionary<int, List<string>>();

    // Nem szinthez tartozó scene-ek (pl. MainMenu, Settings, stb.)
    private Dictionary<string, string> utilityScenes = new Dictionary<string, string>();

    private System.Random random = new System.Random(); // System.Random példányosítása

    // event
    //public event Action<bool> OnSceneLoaded;  // -> invoke() !

    private void Start()
    {
        LoadScenesFromBuildSettings();  // A scene-ek betöltése
        //DebugLevelScenes();             // Tesztelés: Szintek
        //DebugUtilityScenes();           // Tesztelés: Nem szint scene-ek

        // Teszt: Betöltjük az összes szintet 5 másodperces időközönként
        //StartCoroutine(LoadScenesSequentially());
    }



    // Scene-ek betöltése a Build Settings-ből
    void LoadScenesFromBuildSettings()
    {
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
            string sceneName = Path.GetFileNameWithoutExtension(scenePath);

            if (sceneName.StartsWith("Level"))
            {
                int levelNumber = ExtractLevelNumber(sceneName);

                if (levelNumber != -1)
                {
                    if (!levelScenes.ContainsKey(levelNumber))
                    {
                        levelScenes[levelNumber] = new List<string>();
                    }
                    levelScenes[levelNumber].Add(sceneName);
                }
                else
                {
                    Debug.LogWarning($"Hibás névkonvenció: {sceneName}. Kihagyva.");
                }
            }
            else
            {
                // Nem szinthez tartozó scene-ek, mint MainMenu, Settings, stb.
                utilityScenes[sceneName] = sceneName;
                Debug.Log($"Nem pálya-scene (utility): {sceneName}. Tárolva.");
            }
        }
    }

    // Szint számból szintnév kinyerése
    private int ExtractLevelNumber(string sceneName)
    {
        // Példa: "Level1_Layout1" -> 1
        string[] parts = sceneName.Split('_');
        if (parts.Length > 0 && parts[0].StartsWith("Level"))
        {
            string numberPart = parts[0].Substring(5); // Az 5. karaktertől jön a szint száma
            if (int.TryParse(numberPart, out int levelNumber))
            {
                return levelNumber;
            }
        }
        return -1; // Hibás formátum esetén
    }


    // végleges logika implementálás
    public async Task<bool> LoadRandomSceneByLevelAsync(int level)
    {
        try
        {
            SceneManager.LoadScene("LevelTestScene");
            await Task.Delay(1000);  // Szimuláljuk a betöltési időt
            Debug.Log($"Scene for level {level} loaded.");
            return true;
        }
        catch (Exception ex)
        {
            Debug.Log($"Error loading scene: {ex.Message}");
            return false;
        }
    }


    // Véletlenszerű scene betöltése egy szinthez
    public void LoadRandomSceneByLevel(int level)
    {
        if (levelScenes.ContainsKey(level))
        {
            List<string> scenes = levelScenes[level];

            // System.Random használata
            int randomIndex = random.Next(0, scenes.Count); // random.Next(0, count) -> [0, scenes.Count)
            string randomScene = scenes[randomIndex];
            Debug.Log($"Betöltendő scene: {randomScene}");
            SceneManager.LoadScene(randomScene);
        }
        else
        {
            Debug.LogError($"Nincs jelen scene az adott szinthez: {level}");
        }
    }

    // Utility scene betöltése, mint pl. MainMenu
    public void LoadUtilityScene(string sceneKey)
    {
        if (utilityScenes.ContainsKey(sceneKey))
        {
            string sceneName = utilityScenes[sceneKey];
            Debug.Log($"Betöltendő utility scene: {sceneName}");
            SceneManager.LoadScene(sceneName);
        }
        else
        {
            Debug.LogError($"Nincs ilyen utility scene: {sceneKey}");
        }
    }

    // A szinthez tartozó scene-ek tesztelése
    private void DebugLevelScenes()
    {
        Debug.Log("Szinthez tartozó scene-ek:");
        foreach (var level in levelScenes)
        {
            Debug.Log($"Szint {level.Key}:");
            foreach (var scene in level.Value)
            {
                Debug.Log($"  {scene}");
            }
        }
    }

    // A nem szinthez tartozó scene-ek tesztelése
    private void DebugUtilityScenes()
    {
        Debug.Log("Nem szinthez tartozó scene-ek:");
        foreach (var scene in utilityScenes)
        {
            Debug.Log($"{scene.Key}: {scene.Value}");
        }
    }

    private IEnumerator LoadScenesSequentially()
    {
        foreach (var level in levelScenes)
        {
            foreach (var scene in level.Value)
            {
                Debug.Log($"Betöltendő scene: {scene}");
                SceneManager.LoadScene(scene);
                yield return new WaitForSeconds(5f);  // Várakozás 5 másodpercig a következő scene betöltése előtt
            }
        }
    }
}
