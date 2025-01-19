using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewLevelData", menuName = "Game/Level Data")]
public class LevelData : ScriptableObject
// Egy ScriptableObject osztály, amely egy adott szint adatait tárolja.
{
    [Range(1, 4)]
    // Ez a mező az 1 és 4 közötti értékeket engedélyezi a szerkesztőben.
    public int levelNumber; // A szint sorszáma.

    // Az adott szinthez tartozó ellenféladatokat tároló objektum.
    public EnemyData enemyData;

    // Az adott szinthez tartozó spawn zóna adatait tároló objektum.
    public SpawnZoneData zoneData;
}