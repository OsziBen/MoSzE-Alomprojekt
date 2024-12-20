using System.Collections;
using System.Collections.Generic;
using UnityEngine;


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
    public int gameLevel;
    public string levelLayoutName;
    public int points;

    public GameSaveData()
    {
        gameLevel = 0;
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
    public List<string> upgradeIDs;
    public float currentHealtPercentage;

    public PlayerSaveData()
    {
        upgradeIDs = new List<string>();
        currentHealtPercentage = 100f;
    }
}
