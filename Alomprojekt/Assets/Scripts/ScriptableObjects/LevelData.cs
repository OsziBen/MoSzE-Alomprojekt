using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewLevelData", menuName = "Game/Level Data")]
public class LevelData : ScriptableObject
{
    [Range(1, 4)]
    public int levelNumber;
    public EnemyData enemyData;
    public SpawnZoneData zoneData;
}
