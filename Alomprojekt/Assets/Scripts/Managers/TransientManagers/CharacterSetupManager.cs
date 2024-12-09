using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class CharacterSetupManager : BaseTransientManager<CharacterSetupManager>
{
    List<EnemyController> enemyList;

    PlayerController player;


    public async Task<bool> LoadCharactersAsync(int level)
    {
        try
        {
            enemyList = new List<EnemyController>(FindObjectsOfType<EnemyController>());
            player = FindObjectOfType<PlayerController>();
            foreach (var enemy in enemyList)
            {
                enemy.SetCurrentEnemyStatsByLevel(level);
            }

            player.InitPlayerStats(level);
            await Task.Delay(1000);  // Szimuláljuk a betöltési időt
            Debug.Log($"Characters of Level {level} have been loaded.");
            return true;
        }
        catch (Exception ex)
        {
            Debug.Log($"Error loading characters: {ex.Message}");
            return false;
        }
    }

}
