using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static GameStateManager;


[System.Serializable]
public class SaveData
{
    public GameSaveData gameData;
    public SpawnerSaveData spawnerSaveData;
    public PlayerSaveData playerSaveData;

    public SaveData()
    {
        gameData = new GameSaveData();
        spawnerSaveData = new SpawnerSaveData();
        playerSaveData = new PlayerSaveData();
    }
}

[System.Serializable]
public class GameSaveData
{
    public string gameLevel;
    public string levelLayoutName;
    public int points;

    public GameSaveData()
    {
        gameLevel = string.Empty;
        levelLayoutName = string.Empty;
        points = 0;
    }
}


[System.Serializable]
public class SpawnerSaveData
{
    public List<PrefabSaveData> prefabsData;

    public SpawnerSaveData()
    {
        prefabsData = new List<PrefabSaveData>();
    }
}


[System.Serializable]
public class PrefabSaveData
{
    public string prefabID;
    public Vector2 prefabPosition;

    public PrefabSaveData()
    {
        prefabID = string.Empty;
        prefabPosition = Vector2.zero;
    }
}


[System.Serializable]
public class PlayerSaveData
{
    public List<PlayerUpgradeSaveData> upgrades;
    public float currentHealtPercentage;

    public PlayerSaveData()
    {
        upgrades = new List<PlayerUpgradeSaveData>();
        currentHealtPercentage = 100f;
    }
}

[System.Serializable]
public class PlayerUpgradeSaveData
{
    public string upgradeID;
    public int upgradeLevel;

    public PlayerUpgradeSaveData()
    {
        upgradeID = string.Empty;
        upgradeLevel = 0;
    }
}
