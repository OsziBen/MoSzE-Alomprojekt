using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewEnemyData", menuName = "Game/Enemy Data")]
public class EnemyData : ScriptableObject
{
    // Egy belső osztály, amely információkat tartalmaz az ellenségek spawnolásáról
    [System.Serializable]
    public class EnemySpawnInfo
    {
        // Az ellenség prefab.
        public EnemyController enemyPrefab;

        // Az ellenség minimum száma egy spawn során (1 és 10 között)
        [Range(1, 10)]
        public int minNum;

        // Az ellenség maximum száma egy spawn során (1 és 10 között)
        [Range(1, 10)]
        public int maxNum;
    }

    // Lista, amely az összes ellenség spawn információit tárolja
    public List<EnemySpawnInfo> enemyInfos = new List<EnemySpawnInfo>();

    // Ellenőrzések és érvényesítések, amikor az adatokat módosítjuk az inspectorban
    private void OnValidate()
    {
        foreach (var enemyInfo in enemyInfos)
        {
            // Ha a maximum szám kisebb, mint a minimum, korrigáljuk
            if (enemyInfo.maxNum < enemyInfo.minNum)
            {
                Debug.LogWarning($"[EnemyData] MaxNum ({enemyInfo.maxNum}) cannot be less than MinNum ({enemyInfo.minNum}). Adjusting...");
                enemyInfo.maxNum = enemyInfo.minNum;
            }

            // Ha bármelyik érték negatív, nullára korrigáljuk
            if (enemyInfo.minNum < 0 || enemyInfo.maxNum < 0)
            {
                Debug.LogWarning($"[EnemyData] Numbers must be non-negative. Clamping values.");
                enemyInfo.minNum = Mathf.Max(0, enemyInfo.minNum);
                enemyInfo.maxNum = Mathf.Max(0, enemyInfo.maxNum);
            }

            // Ha az értékek kívül esnek a [0, 10] tartományon, azt korrigáljuk
            if (enemyInfo.minNum > 10 || enemyInfo.maxNum > 10)
            {
                Debug.LogWarning($"[EnemyData] Values must be within [0, 10]. Clamping values.");
                enemyInfo.minNum = Mathf.Clamp(enemyInfo.minNum, 0, 10);
                enemyInfo.maxNum = Mathf.Clamp(enemyInfo.maxNum, 0, 10);
            }
        }
    }

    // Egy adott azonosítójú ellenség prefabjának visszaadása
    public EnemyController GetEnemyPrefabByID(string enemyID)
    {
        // Végigmegyünk az összes spawn információn
        foreach (var info in enemyInfos)
        {
            // Ha megtaláljuk az azonosítónak megfelelő prefab-et, visszaadjuk
            if (info.enemyPrefab != null && info.enemyPrefab.ID == enemyID)
            {
                return info.enemyPrefab;
            }
        }
        // Ha nem található az azonosító, hibát jelezünk
        Debug.LogError($"Enemy prefab with name '{enemyID}' not found!");
        return null;
    }
}
