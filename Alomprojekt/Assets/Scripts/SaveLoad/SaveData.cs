using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static GameStateManager;


[System.Serializable]
// Az osztály a játék mentett adatainak összegyűjtésére szolgál
public class SaveData
{
    // A játék adatainak mentéséhez szükséges változók
    public GameSaveData gameData;
    public SpawnerSaveData spawnerSaveData;
    public PlayerSaveData playerSaveData;

    // Az osztály konstruktora, ahol mindhárom adatot inicializáljuk
    public SaveData()
    {
        gameData = new GameSaveData();
        spawnerSaveData = new SpawnerSaveData();
        playerSaveData = new PlayerSaveData();
    }
}

[System.Serializable]
// A játék szintjeinek és pontjainak mentésére szolgáló osztály
public class GameSaveData
{
    public string gameLevel; // Az aktuális szint neve
    public string levelLayoutName; // Az adott szint elrendezésének neve
    public int points; // A játékos összegyűjtött pontjai
    public float currentRunTime; // Az aktuális játékidő

    // Az osztály konstruktora, ahol minden változót alapértelmezetten inicializálunk
    public GameSaveData()
    {
        gameLevel = string.Empty;
        levelLayoutName = string.Empty;
        points = 0;
        currentRunTime = 0f;
    }
}


[System.Serializable]
// Az osztály a spawnerek adatait tartalmazza
public class SpawnerSaveData
{
    // A listában tároljuk a prefabok mentett adatait
    public List<PrefabSaveData> prefabsData;

    // Konstruktor, ahol inicializáljuk a prefábok listáját
    public SpawnerSaveData()
    {
        prefabsData = new List<PrefabSaveData>();
    }
}


[System.Serializable]
// Az osztály egy prefab mentett adatait tárolja
public class PrefabSaveData
{
    public string prefabID; // A prefab azonosítója
    public Vector2 prefabPosition; // A prefab pozíciója a játéktér koordinátáin

    // Konstruktor, ahol alapértelmezetten inicializáljuk az adatokat
    public PrefabSaveData()
    {
        prefabID = string.Empty;
        prefabPosition = Vector2.zero;
    }
}


[System.Serializable]
// A játékos mentett adatait tartalmazza
public class PlayerSaveData
{
    // A játékos fejlesztéseinek mentett adatai
    public List<PlayerUpgradeSaveData> upgrades;
    // A játékos aktuális életerőszintje
    public float currentHealtPercentage;

    // Konstruktor, ahol inicializáljuk a fejlesztések listáját és az életerőszintet
    public PlayerSaveData()
    {
        upgrades = new List<PlayerUpgradeSaveData>();
        currentHealtPercentage = 100f;
    }
}

[System.Serializable]
// A játékos fejlesztésének mentett adatait tartalmazza
public class PlayerUpgradeSaveData
{
    public string upgradeID; // A fejlesztés azonosítója
    public int upgradeLevel; // A fejlesztés szintje

    // Konstruktor, ahol alapértelmezetten inicializáljuk az adatokat
    public PlayerUpgradeSaveData()
    {
        upgradeID = string.Empty;
        upgradeLevel = 0;
    }
}
