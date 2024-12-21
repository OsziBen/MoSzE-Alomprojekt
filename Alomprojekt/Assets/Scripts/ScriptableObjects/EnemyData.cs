using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewEnemyData", menuName = "Game/Enemy Data")]
public class EnemyData : ScriptableObject
{
    [System.Serializable]
    public class EnemySpawnInfo
    {
        public EnemyController enemyPrefab;

        [Range(0, 10)]
        public int minNum;

        [Range(0, 10)]
        public int maxNum;
    }

    public List<EnemySpawnInfo> enemyInfos = new List<EnemySpawnInfo>();


    private void OnValidate()
    {
        foreach (var enemyInfo in enemyInfos)
        {
            if (enemyInfo.maxNum < enemyInfo.minNum)
            {
                Debug.LogWarning($"[EnemyData] MaxNum ({enemyInfo.maxNum}) cannot be less than MinNum ({enemyInfo.minNum}). Adjusting...");
                enemyInfo.maxNum = enemyInfo.minNum;
            }

            if (enemyInfo.minNum < 0 || enemyInfo.maxNum < 0)
            {
                Debug.LogWarning($"[EnemyData] Numbers must be non-negative. Clamping values.");
                enemyInfo.minNum = Mathf.Max(0, enemyInfo.minNum);
                enemyInfo.maxNum = Mathf.Max(0, enemyInfo.maxNum);
            }

            if (enemyInfo.minNum > 10 || enemyInfo.maxNum > 10)
            {
                Debug.LogWarning($"[EnemyData] Values must be within [0, 10]. Clamping values.");
                enemyInfo.minNum = Mathf.Clamp(enemyInfo.minNum, 0, 10);
                enemyInfo.maxNum = Mathf.Clamp(enemyInfo.maxNum, 0, 10);
            }
        }
    }
    public EnemyController GetEnemyPrefabByID(string enemyID)
    {
        foreach (var info in enemyInfos)
        {
            if (info.enemyPrefab != null && info.enemyPrefab.ID == enemyID)
            {
                return info.enemyPrefab;
            }
        }
        Debug.LogError($"Enemy prefab with name '{enemyID}' not found!");
        return null;
    }
}
