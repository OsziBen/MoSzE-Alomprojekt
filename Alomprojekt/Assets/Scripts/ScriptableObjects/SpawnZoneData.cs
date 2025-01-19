using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// ScriptableObject osztály, amely adatokat tartalmaz a spawn zónák típusairól és azok tulajdonságairól.
[CreateAssetMenu(fileName = "NewSpawnZoneData", menuName = "Game/Spawn Zone Data")]
public class SpawnZoneData : ScriptableObject
{
    // A zónák típusainak meghatározása.
    public enum ZoneType
    {
        Enemy,      // EnemySpawner zóna
        Obstacle,   // ObstacleSpawner zóna
        Joker       // JokerSpawner zóna
    }

    // Egy osztály, amely a zóna típusát és aktív zónák számát definiálja.
    [System.Serializable]
    public class SpawnZoneInfo
    {
        public ZoneType zoneType; // A zóna típusa.

        [Range(0, 5)]
        public int activeZoneCount; // Az aktív zónák száma (0 és 5 között).
    }

    // A spawn zónák információit tartalmazó lista.
    public List<SpawnZoneInfo> spawnZoneInfos = new List<SpawnZoneInfo>();

    // Automatikusan meghívott metódus, amikor az adatokat módosítják az inspectorban.
    private void OnValidate()
    {
        // Ellenőrző logika az egyedi zóna típusok biztosítására.
        HashSet<ZoneType> zoneTypes = new HashSet<ZoneType>();
        foreach (var zoneInfo in spawnZoneInfos)
        {
            // Ha egy zóna típusa többször szerepel, figyelmeztetést ad és nullázza az aktív zónák számát.
            if (zoneTypes.Contains(zoneInfo.zoneType))
            {
                Debug.LogWarning($"[SpawnZoneData] Duplicate zone type found: {zoneInfo.zoneType}. This will be removed.");
                zoneInfo.activeZoneCount = 0; // Az érvénytelen adat nullázása.
            }
            else
            {
                zoneTypes.Add(zoneInfo.zoneType); // Új típus hozzáadása az egyedi halmazhoz.
            }
        }

        // Minden zónára ellenőrzi az aktív zónák számát, hogy az érvényes tartományban legyen (0-5).
        foreach (var zoneInfo in spawnZoneInfos)
        {
            if (zoneInfo.activeZoneCount < 0 || zoneInfo.activeZoneCount > 5)
            {
                Debug.LogWarning($"[SpawnZoneData] ActiveZoneCount ({zoneInfo.activeZoneCount}) must be between 0 and 5. Clamping to valid range.");
                zoneInfo.activeZoneCount = Mathf.Clamp(zoneInfo.activeZoneCount, 0, 5); // Érték korrigálása a megadott tartományra.
            }
        }
    }
}
