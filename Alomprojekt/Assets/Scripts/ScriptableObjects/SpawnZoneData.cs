using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewSpawnZoneData", menuName = "Game/Spawn Zone Data")]
public class SpawnZoneData : ScriptableObject
{
    public enum ZoneType
    {
        Enemy,
        Obstacle,
        Joker
    }

    [System.Serializable]
    public class SpawnZoneInfo
    {
        public ZoneType zoneType;

        [Range(0, 5)]
        public int activeZoneCount;
    }

    public List<SpawnZoneInfo> spawnZoneInfos = new List<SpawnZoneInfo>();


    private void OnValidate()
    {
        HashSet<ZoneType> zoneTypes = new HashSet<ZoneType>();
        foreach (var zoneInfo in spawnZoneInfos)
        {
            if (zoneTypes.Contains(zoneInfo.zoneType))
            {
                Debug.LogWarning($"[SpawnZoneData] Duplicate zone type found: {zoneInfo.zoneType}. This will be removed.");
                zoneInfo.activeZoneCount = 0;
            }
            else
            {
                zoneTypes.Add(zoneInfo.zoneType);
            }
        }

        foreach (var zoneInfo in spawnZoneInfos)
        {
            if (zoneInfo.activeZoneCount < 0 || zoneInfo.activeZoneCount > 5)
            {
                Debug.LogWarning($"[SpawnZoneData] ActiveZoneCount ({zoneInfo.activeZoneCount}) must be between 0 and 5. Clamping to valid range.");
                zoneInfo.activeZoneCount = Mathf.Clamp(zoneInfo.activeZoneCount, 0, 5);
            }
        }
    }
}
