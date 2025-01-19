using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using static PlayerUpgradeData;
using StatValuePair = System.Collections.Generic.KeyValuePair<PlayerUpgradeData.StatType, float>;

// A CharacterSetupManager osztály a karakterek (pl. játékos, ellenség, akadályok) beállításainak kezeléséért felelős manager osztály.
public class CharacterSetupManager : BaseTransientManager<CharacterSetupManager>
{
    // Lista, amely az EnemyControllerek objektumait tárolja.
    List<EnemyController> enemyList;

    // A PlayerController-t tároló változó.
    PlayerController player;

    // A játékos fejlesztéseit kezelő manager objektum.
    PlayerUpgradeManager playerUpgradeManager;
    // A játékállapotok kezelését végző manager objektum.
    GameStateManager gameStateManager;

    // Esemény, amelyet akkor hívnak meg, amikor az ellenség attribútumait be kell állítani.
    public event Action<int> OnSetEnemyAttributes;
    // Esemény, amelyet akkor hívnak meg, amikor a játékos attribútumait és azok értékeit be kell állítani.
    public event Action<List<StatValuePair>, float> OnSetPlayerAttributes;
    // Esemény, amelyet akkor hívnak meg, amikor az akadályok attribútumait be kell állítani.
    public event Action<int> OnSetObstacleAttributes;

    /// <summary>
    /// Aszinkron metódus, amely beállítja a főellenség (BOSS) szintjéhez tartozó karaktereket.
    /// A metódus beállítja a játékos karakterét és végrehajtja a szükséges műveleteket a BOSS szintre vonatkozóan.
    /// </summary>
    /// <returns>True, ha sikeresen beállította a karaktereket, egyébként false, ha hiba történt.</returns>
    public async Task<bool> SetBossLevelCharactersAsync()
    {
        // Aszinkron feladat végrehajtása, a Task.Yield biztosítja, hogy a végrehajtás ne blokkolja a fő szálat.
        await Task.Yield();

        try
        {
            // A játékos karakterének beállítása.
            SetPlayerCharacter();
            // BOSS?

            return true; // Ha minden jól ment, igaz értéket adunk vissza.
        }
        catch (Exception ex)
        {
            // Hiba esetén a hibaüzenetet naplózzuk.
            Debug.LogError($"{ex.Message}");
            return false; // Hiba esetén hamis értéket adunk vissza.
        }
    }

    /// <summary>
    /// Aszinkron metódus, amely beállítja a normál szinthez tartozó karaktereket, beleértve az ellenségeket, a játékost és az akadályokat.
    /// A metódus a szint paramétert használja a karakterek megfelelő beállításához.
    /// </summary>
    /// <param name="level">A szint, amelyhez a karaktereket be kell állítani.</param>
    /// <returns>True, ha sikeresen beállította a karaktereket, egyébként false, ha hiba történt.</returns>
    public async Task<bool> SetNormalLevelCharactersAsync(int level)
    {
        //var taskCompletionSource = new TaskCompletionSource<bool>();

        // Az aszinkron feladat végrehajtása, a Task.Yield biztosítja, hogy a végrehajtás ne blokkolja a fő szálat.
        await Task.Yield();

        try
        {
            // Az ellenség karakterek beállítása a megadott szintnek megfelelően.
            SetEnemyCharacters(level);
            // A játékos karakterének beállítása.
            SetPlayerCharacter();
            // Az akadályok beállítása a megadott szintnek megfelelően.
            SetObstacles(level);
            //taskCompletionSource.SetResult(true);
            // Ha minden sikerült, igaz értéket adunk vissza.
            return true;
        }
        catch (Exception ex)
        {
            // Hiba esetén a hibaüzenetet naplózzuk.
            Debug.LogError("ERROR DURING CHARACTER SETUP" + ex);
            //taskCompletionSource.SetResult(false);
            // Hiba esetén hamis értéket adunk vissza.
            return false;
        }
        
        //return await taskCompletionSource.Task;
    }

    /// <summary>
    /// Beállítja az ellenség karaktereket a megadott szint alapján.
    /// Az ellenségek listáját frissíti a jelenetben található összes EnemyController példány alapján, és meghívja az ellenségek attribútumainak beállításához szükséges eseményt.
    /// </summary>
    /// <param name="level">A szint, amely alapján az ellenségek attribútumait be kell állítani.</param>
    void SetEnemyCharacters(int level)
    {
        // Az ellenségek listájának inicializálása, amely tartalmazza a jelenetben található összes EnemyController példányt.
        enemyList = new List<EnemyController>(FindObjectsOfType<EnemyController>());

        // Ha az ellenségek listája üres, akkor figyelmeztetést naplózunk.
        if (enemyList.Count == 0)
        {
            Debug.LogWarning("No enemies found in the scene.");
        }

        // Meghívjuk az OnSetEnemyAttributes eseményt, hogy beállítsuk az ellenség attribútumait a megadott szint alapján.
        OnSetEnemyAttributes?.Invoke(level);
    }

    /// <summary>
    /// Beállítja a játékos karakterét, beleértve a szükséges manager objektumokat is.
    /// A metódus először megkeresi a jelenetben a PlayerController, GameStateManager és PlayerUpgradeManager objektumokat, 
    /// majd beállítja a játékos attribútumait az aktuálisan megvásárolt fejlesztések és a játék állapotának megfelelően.
    /// </summary>
    void SetPlayerCharacter()
    {
        // A játékos irányító (PlayerController) objektum keresése a jelenetben.
        player = FindObjectOfType<PlayerController>();
        // Ha a PlayerController nem található, hibaüzenet naplózása és visszatérés.
        if (player == null)
        {
            Debug.LogError("PlayerController not found in the scene.");
            return;
        }

        // A játék állapotát kezelő manager (GameStateManager) objektum keresése.
        gameStateManager = FindObjectOfType<GameStateManager>();
        // Ha a GameStateManager nem található, hibaüzenet naplózása és visszatérés.
        if (gameStateManager == null)
        {
            Debug.LogError("GameStateManager not found in the scene.");
            return;
        }

        // A játékos fejlesztéseit kezelő manager (PlayerUpgradeManager) objektum keresése.
        playerUpgradeManager = FindObjectOfType<PlayerUpgradeManager>();
        // Ha a PlayerUpgradeManager nem található, hibaüzenet naplózása és visszatérés.
        if (playerUpgradeManager == null)
        {
            Debug.LogError("PlayerUpgradeManager not found in the scene.");
            return;
        }

        // Ha bármelyik szükséges objektum nem található, a további beállításokat nem végezzük el.
        if (player == null || gameStateManager == null || playerUpgradeManager == null) return;

        // A játékos által megvásárolt fejlesztések listájának lekérése.
        List<PlayerUpgrade> upgrades = playerUpgradeManager.PurchasedPlayerUpgrades;

        // Az összes fejlesztés aktuális értékeinek kinyerése és egyesítése egy listába.
        List<StatValuePair> statValues = upgrades
            .SelectMany(upgrade => upgrade.GetCurrentValues())
            .ToList();

        // Meghívjuk a játékos attribútumainak beállításához szükséges eseményt.
        OnSetPlayerAttributes?.Invoke(statValues, gameStateManager.PlayerHealtPercenatge);
        //player.SetPlayerAttributes(level, statValues, gameStateManager.PlayerHealtPercenatge);
    }

    /// <summary>
    /// Beállítja az akadályok attribútumait a megadott szint alapján.
    /// Az akadályok listáját frissíti a jelenetben található összes ObstacleController példány alapján, 
    /// és meghívja az akadályok attribútumainak beállításához szükséges eseményt.
    /// </summary>
    /// <param name="level">A szint, amely alapján az akadályok attribútumait be kell állítani.</param>
    void SetObstacles(int level)
    {
        // Az akadályok listájának inicializálása, amely tartalmazza a jelenetben található összes ObstacleController példányt.
        List<ObstacleController> obstacles = new(FindObjectsOfType<ObstacleController>());

        // Ha az akadályok listája üres, akkor figyelmeztetést naplózunk.
        if (obstacles.Count == 0)
        {
            Debug.LogWarning("No obstacles found in the scene.");
        }

        // Meghívjuk az OnSetObstacleAttributes eseményt, hogy beállítsuk az akadályok attribútumait a megadott szint alapján.
        OnSetObstacleAttributes?.Invoke(level);
    }
}
