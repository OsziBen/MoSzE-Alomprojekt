using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

// TODO: ár kezelésénél maximum figyelembe vétele!
public class PlayerUpgradeManager : BasePersistentManager<PlayerUpgradeManager>
{
    /// <summary>
    /// Változók
    /// </summary>
    // PlayerUpgradeData ScriptableObject-eket összesítő lista. Ide történik a beolvasás.
    public List<PlayerUpgradeData> allPlayerUpgradesData = new List<PlayerUpgradeData>();

    // Gyógyulás fejlesztéshez tartozó PlayerUpgradeData ScriptableObject.
    public PlayerUpgradeData healingUpgradeData;

    // PlayerUpgrade típusú fejlesztéseket összesítő lista.
    public List<PlayerUpgrade> allPlayerUpgrades;

    // PlayerUpgrade típusú gyógyulás fejlesztés (külön kezeljük a többi fejlesztéstől).
    public PlayerUpgrade healingUpgrade;

    // Játékos által megvásárolt fejlesztéseket összesítő lista.
    public List<PlayerUpgrade> purchasedPlayerUpgrades;

    // Aktuális megvásárolható fejlesztéseket összesítő lista.
    public List<PlayerUpgrade> shopPlayerUpgrades;

    // Létrehoz egy új példányt a System.Random osztályból, amelyet véletlenszámok generálásához használhatunk.
    System.Random random = new System.Random();

    // A gyógyulás fejlesztés megjelenésének felső határa (75% HP)
    private readonly float healingUperThreshold = 0.75f;

    // A gyógyulás fejlesztés megjelenésének alsó határa (25% HP)
    private readonly float healingLowerThreshold = 0.25f;

    // Felkínált megvásárolható fejlesztések száma
    private readonly int shopChoices = 3;

    /// <summary>
    /// Komponensek
    /// </summary>
    GameStateManager gameStateManager;
    SaveLoadManager saveLoadManager;
    LevelManager levelManager;

    /// <summary>
    /// Getterek és Setterek
    /// </summary>
    public List<PlayerUpgrade> PurchasedPlayerUpgrades
    {
        get { return purchasedPlayerUpgrades; }
    }

    public List<PlayerUpgrade> CurrentShopUpgrades
    {
        get { return shopPlayerUpgrades; }
    }

    public string HealingUpgradeID
    {
        get { return healingUpgrade.ID; }
    }

    // TODO:
    // shopUpgrades kiválasztás algoritmus ->UIManager...
    // FONTOS: max hp növelés esetén a current hp %-os aránya megmarad!!! ->charactersetupmanager
    // mentés rendszer integrálás (kidolgozás után)

    /*
        Jelezd a játékosnak, hogy mekkora eséllyel bukkanhat fel a gyógyulás.
        Például a vásárlási képernyőn egy "Gyógyulási esély: 65%" felirat segíthet.
     */

    /// <summary>
    /// Inicializálja a PlayerUpgradeManager-t, betölti a játékos fejlesztés adatokat és regisztrálja a mentési eseményt.
    /// </summary>
    protected override async void Initialize()
    {
        // Az alap inicializálás meghívása (szülő osztály metódusának hívása)
        base.Initialize();
        // A GameStateManager és SaveLoadManager keresése a jelenlegi jelenetben.
        gameStateManager = FindObjectOfType<GameStateManager>();
        saveLoadManager = FindObjectOfType<SaveLoadManager>();
        // Hozzáadjuk a mentési kérést a SaveLoadManager eseményhez.
        saveLoadManager.OnSaveRequested += Save;

        // Az összes játékos fejlesztés adatainak betöltése aszinkron módon. Ha nem sikerül, hibát loggolunk.
        bool upgradesLoaded = await LoadAllPlayerUpgradesDataAsync();
        if (!upgradesLoaded)
        {
            Debug.LogError("HIBÁS FEJLESZTÉSBETÖLTÉS"); // Hibás fejlesztés betöltés, ha az adatok nem töltődtek be.
        }

        // A játékos fejlesztéseinek betöltése aszinkron módon. Ha nem sikerül, hibát loggolunk.
        bool upgradesDataLoaded = await LoadPlayerUpgradesAsync(allPlayerUpgradesData);
        if (!upgradesDataLoaded)
        {
            Debug.LogError("HIBÁS ADATBETÖLTÉS"); // Hibás adatbetöltés, ha az adatok nem töltődtek be.
        }
    }

    /// <summary>
    /// A játékos által vásárolt fejlesztések mentésére szolgáló metódus.
    /// </summary>
    /// <param name="saveData">A mentéshez szükséges adatokat tartalmazó objektum.</param>
    void Save(SaveData saveData)
    {
        // Végigiterálunk a vásárolt fejlesztéseken.
        foreach (var playerUpgrade in purchasedPlayerUpgrades)
        {
            // Új PlayerUpgradeSaveData objektum létrehozása a fejlesztés mentéséhez.
            PlayerUpgradeSaveData playerUpgradeSaveData = new PlayerUpgradeSaveData();

            // A fejlesztés ID-jának és szintjének elmentése.
            playerUpgradeSaveData.upgradeID = playerUpgrade.ID;
            playerUpgradeSaveData.upgradeLevel = playerUpgrade.currentUpgradeLevel;

            // A fejlesztés mentése a saveData-ban található playerSaveData.upgrades listába.
            saveData.playerSaveData.upgrades.Add(playerUpgradeSaveData);
        }
    }

    /// <summary>
    /// A mentett fejlesztési adatokat betölti és alkalmazza a játékosra aszinkron módon.
    /// </summary>
    /// <param name="loadData">A betöltendő mentett adatokat tartalmazó objektum.</param>
    /// <returns>Visszaadja, hogy sikerült-e a betöltés (true vagy false).</returns>
    public async Task<bool> SetLoadDataAsync(SaveData loadData)
    {
        await Task.Yield();

        try
        {
            // Végigiterálunk a mentett fejlesztéseken.
            foreach (var playerUpgradeSaveData in loadData.playerSaveData.upgrades)
            {
                // A fejlesztés betöltése az összes rendelkezésre álló fejlesztés közül az ID alapján.
                PlayerUpgrade loadedUpgrade = GetPlayerUpgradeFromAvailablesByID(allPlayerUpgrades, playerUpgradeSaveData.upgradeID);
                // A betöltött fejlesztés szintjének beállítása.
                loadedUpgrade.SetCurrentPlayerUpgradeLevel(playerUpgradeSaveData.upgradeLevel);
                // A betöltött fejlesztés hozzáadása a vásárolt fejlesztések listájához.
                purchasedPlayerUpgrades.Add(loadedUpgrade);
                // A betöltött fejlesztés eltávolítása az összes elérhető fejlesztés listájából.
                allPlayerUpgrades.Remove(loadedUpgrade);
            }

            return true; // Ha a betöltés sikerült, true-t adunk vissza.
        }
        catch (Exception ex)
        {
            // Ha hiba történik a betöltés során, hibát loggolunk.
            Debug.LogError($"Error during loading upgrade save data! {ex.Message}");
            return false; // Ha hiba történt, false-t adunk vissza.
        }

    }

    /// <summary>
    /// Az elérhető fejlesztések listájából keres egy fejlesztést az ID alapján.
    /// </summary>
    /// <param name="allPlayerUpgrades">A játékos összes elérhető fejlesztését tartalmazó lista.</param>
    /// <param name="upgradeID">A keresett fejlesztés azonosítója.</param>
    /// <returns>A keresett fejlesztést, ha található, egyébként null-t ad vissza.</returns>
    PlayerUpgrade GetPlayerUpgradeFromAvailablesByID(List<PlayerUpgrade> allPlayerUpgrades, string upgradeID)
    {
        // Keresés a listában az ID alapján, és visszaadja az első találatot.
        return allPlayerUpgrades.Find(x => x.ID == upgradeID);
    }

    /// <summary>
    /// Destroy esetén leiratkozik eventekről.
    /// </summary>
    private void OnDestroy()
    {
        if (saveLoadManager != null)
        {
            saveLoadManager.OnSaveRequested -= Save;
        }
    }


    /// <summary>
    /// Aszinkron metódus, amely betölti az összes játékos fejlesztési adatot az Addressables rendszerből.
    /// A metódus az 'Upgrades' címkét tartalmazó PlayerUpgradeData objektumokat tölti be.
    /// A betöltés befejezése után egy TaskCompletionSource segítségével visszaadja a sikeresség eredményét.
    /// </summary>
    /// <returns>Visszatérési érték: bool - true, ha a betöltés sikeres volt, különben false.</returns>
    async Task<bool> LoadAllPlayerUpgradesDataAsync()
    {
        // TaskCompletionSource létrehozása a betöltési folyamat aszinkron befejezésének kezelésére.
        var taskCompletionSource = new TaskCompletionSource<bool>();

        // Az Addressables rendszer segítségével betölti az 'Upgrades' címkét tartalmazó PlayerUpgradeData objektumokat.
        // Az OnUpgradesLoaded metódus hívódik meg minden egyes betöltött adat objektum esetén.
        var handle = Addressables.LoadAssetsAsync<PlayerUpgradeData>("Upgrades", (playerUpgradeData) =>
        {
            // Minden betöltött adatot az OnUpgradesLoaded metódus dolgoz fel.
            OnUpgradesLoaded(playerUpgradeData);
        });

        // A betöltési folyamat befejeződése után a 'Completed' eseménykezelő hívódik meg.
        handle.Completed += (operation) =>
        {
            // Ellenőrzi, hogy a betöltési folyamat sikeresen befejeződött-e.
            if (operation.Status == AsyncOperationStatus.Succeeded)
            {
                // Ha a betöltés sikeres volt, akkor a megfelelő üzenetet jeleníti meg, és a TaskCompletionSource-t sikeresen befejezi.
                Debug.Log($"All upgrades loaded successfully ({allPlayerUpgradesData.Count})");
                taskCompletionSource.SetResult(true);
            }
            else
            {
                // Ha a betöltés nem sikerült, hibaüzenetet ír ki és a TaskCompletionSource-t hibás eredménnyel zárja le.
                Debug.LogError("Failed to load upgrades");
                taskCompletionSource.SetResult(false);
            }

            /* DEBUG
            // Ezt a részletet akkor használhatjuk, ha szükség van arra, hogy a betöltött adatokat kiírjuk a konzolra
            // és ellenőrizzük a 'playerUpgradeData' objektumok listáját.
            foreach (var playerUpgradeData in allPlayerUpgradesData)
            {
                Debug.Log($"Upgrade in list: {playerUpgradeData.upgradeName}");
            }
            */
        };

        // Várakozik a TaskCompletionSource eredményére (visszaadja a végső sikerességi értéket: true vagy false).
        return await taskCompletionSource.Task;
    }



    /// <summary>
    /// Az OnUpgradesLoaded metódus feldolgozza a betöltött PlayerUpgradeData objektumokat.
    /// Ha a fejlesztés gyógyításhoz kapcsolódik, akkor elmenti a megfelelő változóba.
    /// Ha nem gyógyításhoz kapcsolódik, hozzáadja az összes fejlesztési adat listájához.
    /// </summary>
    /// <param name="playerUpgradeData">A betöltött PlayerUpgradeData objektum, amely tartalmazza az aktuális fejlesztés adatokat.</param>
    private void OnUpgradesLoaded(PlayerUpgradeData playerUpgradeData)
    {
        // Ellenőrzi, hogy a jelenlegi fejlesztés gyógyításhoz kapcsolódik-e.
        if (playerUpgradeData.isHealing)
        {
            // Ha igen, akkor elmenti a gyógyításhoz tartozó adatokat a healingUpgradeData változóba.
            healingUpgradeData = playerUpgradeData;

            // Debug üzenet, amely tájékoztatja, hogy a gyógyítás fejlesztés betöltődött.
            Debug.Log($"Upgrade {playerUpgradeData.upgradeName} loaded");
        }
        else
        {
            // Ha nem gyógyításhoz kapcsolódik, hozzáadja az összes fejlesztési adat listájához.
            allPlayerUpgradesData.Add(playerUpgradeData);

            // Debug üzenet, amely tájékoztatja, hogy egy nem gyógyítási fejlesztés betöltődött.
            Debug.Log($"Upgrade {playerUpgradeData.upgradeName} loaded");
        }
    }


    /// <summary>
    /// Aszinkron metódus, amely betölti a játékos összes fejlesztési adatát és létrehozza az azokhoz tartozó PlayerUpgrade objektumokat.
    /// A gyógyításhoz kapcsolódó fejlesztést külön tárolja, és hibakezelést biztosít a folyamat során.
    /// </summary>
    /// <param name="allPlayerUpgradesData">A játékos összes fejlesztési adatát tartalmazó lista, amely PlayerUpgradeData objektumokat tartalmaz.</param>
    /// <returns>Visszatérési érték: bool. A metódus true értékkel tér vissza, ha a fejlesztések sikeresen betöltődtek, egyébként false.</returns>
    async Task<bool> LoadPlayerUpgradesAsync(List<PlayerUpgradeData> allPlayerUpgradesData)
    {
        // Ellenőrzi, hogy az összes fejlesztési adat érvényes-e. Ha a lista 'null', akkor visszatér 'false' értékkel.
        if (allPlayerUpgradesData == null) return false;

        // TaskCompletionSource létrehozása, amely lehetővé teszi a végrehajtás aszinkron követését.
        var taskCompletionSource = new TaskCompletionSource<bool>();

        // Az aszinkron végrehajtás biztosítása érdekében Yield metódus használata.
        await Task.Yield();

        try
        {
            // Az üres lista inicializálása, amely tárolja a PlayerUpgrade objektumokat.
            allPlayerUpgrades = new List<PlayerUpgrade>();

            // Végigiterál az összes PlayerUpgradeData objektumon, és létrehozza a hozzájuk tartozó PlayerUpgrade objektumokat.
            foreach (var playerUpgradeData in allPlayerUpgradesData)
            {
                // Minden egyes PlayerUpgradeData objektumot PlayerUpgrade objektummá alakítunk és hozzáadjuk a listához.
                allPlayerUpgrades.Add(new PlayerUpgrade(playerUpgradeData));
            }

            // A gyógyításhoz kapcsolódó fejlesztés külön kezelése.
            healingUpgrade = new PlayerUpgrade(healingUpgradeData);

            // A folyamat sikeresen befejeződött, beállítja a TaskCompletionSource értékét true-ra.
            taskCompletionSource.SetResult(true);
        }
        catch (Exception ex)
        {
            // Ha hiba történik a betöltés során, naplózza a hiba üzenetét és beállítja a TaskCompletionSource értékét false-ra.
            Debug.LogError($"Failed to load player upgrades: {ex.Message}");
            taskCompletionSource.SetResult(false);
        }

        // Visszatér a TaskCompletionSource által képviselt végrehajtás eredménye (true vagy false).
        return await taskCompletionSource.Task;
    }



    /// <summary>
    /// Véletlenszerűen választ egy PlayerUpgrade objektumot a megadott fejlesztési lista alapján.
    /// </summary>
    /// <param name="playerUpgradesList">A játékos fejlesztéseit tartalmazó lista, amely PlayerUpgrade objektumokat tartalmaz.</param>
    /// <returns>Visszatérési érték: PlayerUpgrade. A lista véletlenszerűen kiválasztott fejlesztését adja vissza.</returns>
    PlayerUpgrade GetRandomPlayerUpgradeFromList(List<PlayerUpgrade> playerUpgradesList)
    {
        // Véletlenszerű index kiválasztása a listában a random objektum segítségével.
        int randomIndex = random.Next(playerUpgradesList.Count);

        // A véletlenszerűen kiválasztott indexhez tartozó PlayerUpgrade objektum visszaadása.
        return playerUpgradesList[randomIndex];
    }



    /// <summary>
    /// Ellenőrzi, hogy elérhető-e a gyógyító fejlesztés a játékos jelenlegi életerő-aránya (%) alapján.
    /// Minél alacsonyabb ez az érték, annál nagyobb a valószínűsége. Ez legelőször 75% életerő esetén
    /// következhet be, 25% alatt pedig 100% eséllyel következik be.
    /// </summary>
    /// <param name="playerHealthPercentage">A játékos jelenlegi életerejét kifejező százalékos érték (0 és 1 között).</param>
    /// <returns>Visszatérési érték: bool. Igaz, ha a gyógyító fejlesztés elérhető, hamis, ha nem.</returns>
    bool IsHealingUpgradeAvailable(float playerHealthPercentage)
    {
        // Ha a játékos életereje a gyógyítás alsó küszöbe alatt van, a gyógyító fejlesztés biztosan elérhető.
        if (playerHealthPercentage <= healingLowerThreshold)
        {
            return true;
        }

        // Ha az életerő a gyógyítás felső küszöbe alatt van, véletlenszerű eséllyel elérhetővé válhat a fejlesztés.
        if (playerHealthPercentage <= healingUperThreshold)
        {
            // Generál egy véletlenszerű számot 1 és 100 között.
            int chance = random.Next(1, 101);

            // Ha a véletlenszám kisebb vagy egyenlő a számított eséllyel, elérhetővé válik a gyógyító fejlesztés.
            return chance <= (1 - playerHealthPercentage) * 100;
        }

        // Ha a játékos életereje meghaladja a gyógyítás felső küszöbét, a gyógyító fejlesztés nem elérhető.
        return false;
    }


    /// <summary>
    /// Visszaadja a még nem megvásárolt, a jelenlegi játékszinthez érvényes fejlesztéseket.
    /// </summary>
    /// <param name="unpurchasedUpgrades">A még nem megvásárolt fejlesztések listája.</param>
    /// <param name="currentGamelevel">A játék jelenlegi játékszintje.</param>
    /// <returns>Visszatérési érték: List<PlayerUpgrade>. A még nem megvásárolt, a jelenlegi játékszinthez érvényes fejlesztések listája.</returns>
    List<PlayerUpgrade> GetUnpurchasedValidUpgradesByLevel(List<PlayerUpgrade> unpurchasedPlayerUpgrades, int currentGamelevel)
    {
        // Új lista létrehozása a valid (érvényes) fejlesztések tárolásához.
        List<PlayerUpgrade> validPlayerUpgrades = new List<PlayerUpgrade>();

        // A még nem megvásárolt fejlesztések listájának átvizsgálása.
        foreach (var playerUpgrade in unpurchasedPlayerUpgrades)
        {
            // Ha a játék jelenlegi szintje elérte a fejlesztés minimális szintjét, akkor érvényes a fejlesztés.
            if (currentGamelevel == 4 && currentGamelevel == playerUpgrade.maxUpgradeLevel)
            {
                validPlayerUpgrades.Add(new PlayerUpgrade(playerUpgrade));
            }
            if (currentGamelevel < 4 && currentGamelevel >= playerUpgrade.minUpgradeLevel)
            {
                // A fejlesztés másolatának hozzáadása a valid fejlesztések listájához.
                validPlayerUpgrades.Add(new PlayerUpgrade(playerUpgrade));
            }
        }

        // Visszatérünk a valid fejlesztések listájával.
        return validPlayerUpgrades;
    }


    /// <summary>
    /// Visszaadja a megvásárolt, a jelenlegi játékszinthez érvényes fejlesztéseket.
    /// </summary>
    /// <param name="purchasedUpgrades">A megvásárolt fejlesztések listája.</param>
    /// <param name="currentGamelevel">A játék jelenlegi játékszintje.</param>
    /// <returns>Visszatérési érték: List<PlayerUpgrade>. A megvásárolt, a jelenlegi játékszinthez érvényes fejlesztések listája.</returns>
    List<PlayerUpgrade> GetPurchasedValidUpgradesByLevel(List<PlayerUpgrade> purchasedPlayerUpgrades, int currentGamelevel)
    {
        // Új lista létrehozása a valid (érvényes) megvásárolt fejlesztések tárolásához.
        List<PlayerUpgrade> validPlayerUpgrades = new List<PlayerUpgrade>();

        // A megvásárolt fejlesztések listájának átvizsgálása.
        foreach (var playerUpgrade in purchasedPlayerUpgrades)
        {
            // Ha a játék jelenlegi szintje elég alacsony ahhoz, hogy a fejlesztés maximális szintjéig elérhesse,
            // akkor érvényes a fejlesztés a játékos számára.
            if (currentGamelevel <= playerUpgrade.maxUpgradeLevel)
            {
                // A fejlesztés másolatának hozzáadása a valid fejlesztések listájához.
                PlayerUpgrade copy = new PlayerUpgrade(playerUpgrade);
                copy.IsTempCopy = true;
                validPlayerUpgrades.Add(copy);
            }
        }

        // Visszatérünk a valid fejlesztések listájával.
        return validPlayerUpgrades;
    }


    /// <summary>
    /// Összegyűjti a boltban elérhető érvényes fejlesztéseket a játékos aktuális szintje és életerő százaléka alapján.
    /// </summary>
    /// <param name="currentGamelevel">A játék aktuális szintje.</param>
    /// <param name="playerHealthPercentage">A játékos életerő százaléka (0-tól 1-ig terjedő érték).</param>
    /// <returns>Visszatérési érték: bool. A művelet sikerességét jelzi.</returns>
    public async Task<bool> GenerateCurrentShopUpgradesAsync(int currentGamelevel, float playerHealthPercentage)
    {
        // Létrehoz egy TaskCompletionSource objektumot, amely lehetővé teszi az aszinkron művelet állapotának kezelését.
        var taskCompletionSource = new TaskCompletionSource<bool>();

        // Előre biztosítja az aszinkron működést, hogy a metódus később ne blokkolja a hívót.
        await Task.Yield();

        try
        {
            // Lekéri az aktuális szinthez érvényes, már megvásárolt fejlesztéseket.
            List<PlayerUpgrade> validPurchasedPlayerUpgrades = GetPurchasedValidUpgradesByLevel(purchasedPlayerUpgrades, currentGamelevel);

            // Lekéri az aktuális szinthez érvényes, még nem megvásárolt fejlesztéseket.
            List<PlayerUpgrade> validUnpurchasedPlayerUpgrades = GetUnpurchasedValidUpgradesByLevel(allPlayerUpgrades, currentGamelevel);

            // Ha nincs korábban vásárolt fejlesztés...
            if (validPurchasedPlayerUpgrades.Count == 0)
            {
                Debug.Log("NINCS KORÁBBI FEJLESZTÉS");

                // A boltba feltölt néhány véletlenszerű, még nem vásárolt fejlesztést.
                for (int i = 0; i < shopChoices - 1; i++)
                {
                    AddRandomValidPlayerUpgradeToShop(validUnpurchasedPlayerUpgrades, currentGamelevel);
                }

                // Ellenőrzi, hogy a gyógyítási fejlesztés elérhető-e az életerő alapján.
                if (IsHealingUpgradeAvailable(playerHealthPercentage))
                {
                    // Hozzáadja a gyógyító fejlesztést a bolthoz.
                    AddHealingPlayerUpgradeToShop(currentGamelevel);
                }
                else
                {
                    // Ha nem, egy másik véletlenszerű fejlesztést ad hozzá a bolthoz.
                    AddRandomValidPlayerUpgradeToShop(validUnpurchasedPlayerUpgrades, currentGamelevel);
                }
            }
            else
            {
                Debug.Log("VAN KORÁBBI FEJLESZTÉS");

                // Hozzáad egy véletlenszerű, már megvásárolt fejlesztést a bolthoz.
                AddRandomValidPlayerUpgradeToShop(validPurchasedPlayerUpgrades, currentGamelevel);

                // Hozzáad egy véletlenszerű, még nem megvásárolt fejlesztést a bolthoz.
                AddRandomValidPlayerUpgradeToShop(validUnpurchasedPlayerUpgrades, currentGamelevel);

                // Ellenőrzi, hogy a gyógyítási fejlesztés elérhető-e az életerő alapján.
                if (IsHealingUpgradeAvailable(playerHealthPercentage))
                {
                    // Hozzáadja a gyógyító fejlesztést a bolthoz.
                    AddHealingPlayerUpgradeToShop(currentGamelevel);
                }
                else
                {
                    // Ha nem, egy másik véletlenszerű fejlesztést ad hozzá a bolthoz.
                    AddRandomValidPlayerUpgradeToShop(validUnpurchasedPlayerUpgrades, currentGamelevel);
                }
            }

            // A művelet sikeres végrehajtását jelzi.
            taskCompletionSource.SetResult(true);
        }
        catch (Exception ex)
        {
            // Hiba esetén naplózza az üzenetet és hibás eredményt állít be.
            Debug.LogError($"Hiba történt a bolt frissítése során: {ex.Message}");
            taskCompletionSource.SetResult(false);
        }

        // Visszatér az aszinkron művelet eredményével.
        return await taskCompletionSource.Task;
    }


    /// <summary>
    /// Véletlenszerűen kiválaszt egy érvényes játékos fejlesztést, frissíti annak szintjét és leírását, majd hozzáadja a bolthoz.
    /// </summary>
    /// <param name="validPlayerUpgrades">A választható fejlesztések listája.</param>
    /// <param name="currentGamelevel">Az aktuális játékszint, amely a fejlesztés szintjének frissítésére szolgál.</param>
    private void AddRandomValidPlayerUpgradeToShop(List<PlayerUpgrade> validPlayerUpgrades, int currentGamelevel)
    {
        if (validPlayerUpgrades.Count == 0)
        {
            return;
        }

        // Véletlenszerűen kiválaszt egy fejlesztést a megadott listából.
        PlayerUpgrade randomValidPlayerUpgrade = GetRandomPlayerUpgradeFromList(validPlayerUpgrades);

        // Frissíti a fejlesztés szintjét az aktuális játékszint alapján.
        randomValidPlayerUpgrade.SetCurrentPlayerUpgradeLevel(currentGamelevel);

        // Frissíti a fejlesztés leírását a szint változása után.
        randomValidPlayerUpgrade.RefreshDescription();

        // Hozzáadja a fejlesztést a boltban elérhető fejlesztések listájához.
        shopPlayerUpgrades.Add(randomValidPlayerUpgrade);

        // Eltávolítja a kiválasztott fejlesztést az eredeti listából, hogy ne válassza ki újra.
        validPlayerUpgrades.Remove(randomValidPlayerUpgrade);
    }


    /// <summary>
    /// Hozzáadja a gyógyító fejlesztést a bolthoz, és frissíti annak szintjét az aktuális játékszint alapján.
    /// </summary>
    /// <param name="currentGamelevel">Az aktuális játékszint, amely a gyógyító fejlesztés szintjének frissítésére szolgál.</param>
    private void AddHealingPlayerUpgradeToShop(int currentGamelevel)
    {
        // Frissíti a gyógyító fejlesztés szintjét az aktuális játékszint alapján.
        healingUpgrade.SetCurrentPlayerUpgradeLevel(currentGamelevel);

        // Hozzáadja a gyógyító fejlesztést a boltban elérhető fejlesztések listájához.
        shopPlayerUpgrades.Add(healingUpgrade);
    }


    /// <summary>
    /// Kezeli egy játékos fejlesztésvásárlását, és frissíti az adatok listáját a vásárlás eredményeként.
    /// A mentési rendszeren keresztül történő kommunikáció később kerül implementálásra.
    /// </summary>
    /// <param name="playerUpgrade">A megvásárolni kívánt fejlesztés objektuma.</param>
    public async Task<bool> PurchasePlayerUpgrade(string playerUpgradeID)
    {
        await Task.Yield(); // Az aszinkron feladatot átadja más feladatoknak, hogy ne blokkolja a szálat.

        try
        {
            // Ellenőrizzük, hogy a fejlesztés ID-ja érvényes-e. Ha nem, átugorjuk a vásárlást.
            if (string.IsNullOrEmpty(playerUpgradeID))
            {
                SkipPurchase(); // Vásárlás átugrása, ha az ID üres vagy érvénytelen.
                return true;
            }

            // A boltban elérhető fejlesztés lekérése az ID alapján.
            PlayerUpgrade playerUpgrade = GetPlayerUpgradeFromShopByID(shopPlayerUpgrades, playerUpgradeID);

            // A játékos pontjainak levonása a fejlesztés árával.
            DeductPlayerPoints(playerUpgrade.GetPrice());

            // Ha a fejlesztés gyógyító típusú, akkor azt kezeljük külön.
            if (playerUpgrade.isHealing)
            {
                HandleHealingUpgrade(); // Gyógyító fejlesztés kezelése.
            }
            else
            {
                HandleNonHealingUpgrade(playerUpgrade); // Nem gyógyító fejlesztés kezelése.
            }

            // Miután a fejlesztést választották, kiürítjük a boltban elérhető fejlesztéseket.
            ClearShopPlayerUpgradesList();

            return true; // A vásárlás sikeres volt.
        }
        catch (Exception ex)
        {
            // Ha hiba történt a vásárlás során, hibát loggolunk.
            Debug.LogError($"Error during purchasing upgrade! {ex.Message}");
            return false; // Ha hiba történt, false-t adunk vissza.
        }
    }

    /// <summary>
    /// Levonja a játékos pontjait a megadott árnak megfelelően.
    /// </summary>
    /// <param name="price">A levonandó pontok száma.</param>
    private void DeductPlayerPoints(int price)
    {
        // A játékos pontjainak csökkentése a megadott ár szerint.
        gameStateManager.PlayerPoints -= price;

        // A maradék pontok kiírása a konzolra.
        Debug.Log($"Remaining points: {gameStateManager.PlayerPoints}");
    }

    /// <summary>
    /// A játékos életerejét teljes mértékben helyreállítja.
    /// </summary>
    private void HandleHealingUpgrade()
    {
        // A játékos életerejét 100%-ra állítjuk.
        gameStateManager.PlayerHealtPercenatge = 1f;

        // Kiírjuk, hogy a játékos életereje teljesen helyreállt.
        Debug.Log("Player health fully restored.");
    }

    /// <summary>
    /// Kezeli a nem gyógyító fejlesztéseket: ha új, hozzáadja a vásárolt fejlesztésekhez, ha már megvan, frissíti.
    /// </summary>
    /// <param name="playerUpgrade">A játékos által választott fejlesztés.</param>
    private void HandleNonHealingUpgrade(PlayerUpgrade playerUpgrade)
    {
        // Ellenőrizzük, hogy a fejlesztést már megvásárolták-e.
        var match = purchasedPlayerUpgrades.Find(x => x.ID == playerUpgrade.ID);

        if (match == null)
        {
            // Ha még nem vásárolták meg, hozzáadjuk a vásárolt fejlesztések listájához,
            // és eltávolítjuk az összes elérhető fejlesztés közül.
            purchasedPlayerUpgrades.Add(playerUpgrade);
            allPlayerUpgrades.RemoveAll(x => x.ID == playerUpgrade.ID);

            // Kiírjuk a konzolra, hogy a fejlesztést hozzáadták.
            Debug.Log($"Upgrade {playerUpgrade.ID} added to purchased list.");
        }
        else
        {
            // Ha a fejlesztés már meg van vásárolva, frissítjük a szintjét, és újra beállítjuk a leírást.
            match.currentUpgradeLevel = playerUpgrade.currentUpgradeLevel;
            match.RefreshDescription();
            match.IsTempCopy = false;

            // Kiírjuk a konzolra, hogy a fejlesztést frissítettük.
            Debug.Log($"Upgrade {playerUpgrade.ID} updated to level {playerUpgrade.currentUpgradeLevel}.");
        }
    }


    /// <summary>
    /// A boltban elérhető fejlesztések listájából keres egy fejlesztést az ID alapján.
    /// </summary>
    /// <param name="ShopUpgrades">A boltban elérhető fejlesztéseket tartalmazó lista.</param>
    /// <param name="upgradeID">A keresett fejlesztés azonosítója.</param>
    /// <returns>A keresett fejlesztést, ha található, egyébként null-t ad vissza.</returns>
    PlayerUpgrade GetPlayerUpgradeFromShopByID(List<PlayerUpgrade> ShopUpgrades, string upgradeID)
    {
        // Keresés a boltban elérhető fejlesztések listájában az ID alapján, és visszaadja az első találatot.
        return ShopUpgrades.Find(x => x.ID == upgradeID);
    }



    /// <summary>
    /// Kezeli a vásárlás kihagyását a boltban.
    /// A jelenleg elérhető fejlesztések törlése és a bolt adatainak frissítése.
    /// </summary>
    public void SkipPurchase()
    {
        // Törli a boltban megjelenített fejlesztéseket.
        ClearShopPlayerUpgradesList();
    }


    /// <summary>
    /// Törli a boltban jelenleg elérhető fejlesztések listáját.
    /// Előkészíti a listát az új fejlesztések hozzáadására.
    /// </summary>
    /// <param name="shopPlayerUpgrades">A bolt jelenlegi fejlesztési listája, amely törlésre kerül.</param>
    public void ClearShopPlayerUpgradesList()
    {
        // Törli az összes elemet a bolt fejlesztési listájából.
        shopPlayerUpgrades.Clear();
    }


    /// <summary>
    /// Visszaállítja a játékos fejlesztéseit kezelő listákat a kezdeti állapotukba.
    /// A művelet során újratölti az összes elérhető fejlesztést, törli a megvásárolt és a boltban elérhető fejlesztéseket.
    /// Aszinkron módon végrehajtott művelet, amely kezeli a hibákat is.
    /// </summary>
    /// <returns>
    /// Igaz értéket ad vissza, ha a művelet sikeresen végrehajtásra került, hamisat, ha hiba lépett fel.
    /// </returns>
    async public Task<bool> ResetPlayerUpgradesListsAsync()
    {
        // TaskCompletionSource használata, hogy aszinkron módon várhassunk a művelet befejeződésére
        var taskCompletionSource = new TaskCompletionSource<bool>();

        // Az aszinkron műveletet elindítjuk, de nem blokkoljuk a fő szálat
        await Task.Yield();

        try
        {
            // Betölti az összes fejlesztési adatot
            bool loadResult = await LoadPlayerUpgradesAsync(allPlayerUpgradesData);

            // Ha a betöltés nem sikerült, kivételt dobunk
            if (!loadResult)
            {
                throw new Exception("Failed to load player upgrades.");
            }

            // Kiüríti a megvásárolt és boltban elérhető fejlesztéseket
            purchasedPlayerUpgrades.Clear();
            shopPlayerUpgrades.Clear();

            // Ha minden sikeres, a TaskCompletionSource-t beállítjuk igazra
            taskCompletionSource.SetResult(true);
        }
        catch (Exception ex)
        {
            // Hibakezelés: ha bármi hiba történik, naplózzuk a hibát és a TaskCompletionSource-t hamisra állítjuk
            Debug.LogError($"ResetPlayerUpgradesLists failed: {ex.Message}");
            taskCompletionSource.SetResult(false);
        }

        // Visszatérünk a művelet eredményével
        return await taskCompletionSource.Task;
    }


}
