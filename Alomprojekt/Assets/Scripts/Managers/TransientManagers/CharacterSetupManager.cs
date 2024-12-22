using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using static PlayerUpgradeData;
using StatValuePair = System.Collections.Generic.KeyValuePair<PlayerUpgradeData.StatType, float>;

public class CharacterSetupManager : BaseTransientManager<CharacterSetupManager>
{
    List<EnemyController> enemyList;

    public PlayerController player;

    PlayerUpgradeManager playerUpgradeManager;
    GameStateManager gameStateManager;


    public event Action<int> OnSetEnemyAttributes;
    public event Action<int, List<StatValuePair>, float> OnSetPlayerAttributes;


    public async Task<bool> SetCharactersAsync(int level)
    {
        //var taskCompletionSource = new TaskCompletionSource<bool>();

        await Task.Yield();

        try
        {
            SetEnemyCharacters(level);
            SetPlayerCharacter(level);
            //taskCompletionSource.SetResult(true);
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError("ERROR DURING CHARACTER SETUP" + ex);
            //taskCompletionSource.SetResult(false);
            return false;
        }
        
        //return await taskCompletionSource.Task;
    }


    void SetEnemyCharacters(int level)
    {
        enemyList = new List<EnemyController>(FindObjectsOfType<EnemyController>());
        if (enemyList.Count == 0)
        {
            Debug.LogWarning("No enemies found in the scene.");
        }

        OnSetEnemyAttributes?.Invoke(level);
    }


    void SetPlayerCharacter(int level)
    {
        player = FindObjectOfType<PlayerController>();
        if (player == null)
        {
            Debug.LogError("PlayerController not found in the scene.");
            return;
        }

        gameStateManager = FindObjectOfType<GameStateManager>();
        if (gameStateManager == null)
        {
            Debug.LogError("GameStateManager not found in the scene.");
            return;
        }

        playerUpgradeManager = FindObjectOfType<PlayerUpgradeManager>();
        if (playerUpgradeManager == null)
        {
            Debug.LogError("PlayerUpgradeManager not found in the scene.");
            return;
        }

        if (player == null || gameStateManager == null || playerUpgradeManager == null) return;

        List<PlayerUpgrade> upgrades = playerUpgradeManager.PurchasedPlayerUpgrades;

        List<StatValuePair> statValues = upgrades
            .SelectMany(upgrade => upgrade.GetCurrentValues())
            .ToList();

        OnSetPlayerAttributes?.Invoke(level, statValues, gameStateManager.PlayerHealtPercenatge);
        //player.SetPlayerAttributes(level, statValues, gameStateManager.PlayerHealtPercenatge);
    }
}
