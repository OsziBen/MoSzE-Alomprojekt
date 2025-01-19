using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using UnityEngine;

// TODO: ellenség halálakor eventre feliratkozni, pontok számolása (-> mentés)
public class GameStateManager : BasePersistentManager<GameStateManager>
{
    /// <summary>
    /// Változók
    /// </summary>
    private int _points = 0; // Játékos pontszáma
    private float _playerHealthPercentage = 1f; // Játékos életerejének százalékos értéke (1 = teljes életerő)
    private GameLevel _currentLevel; // Az aktuális szint
    private bool _isStateChanging = false; // Jelző, hogy éppen változik-e a játék állapota

    private float _currentRunTime = 0f; // Az aktuális játékidő
    private string _currentRunDate = string.Empty; // Az aktuális játék dátuma (pl. ha van mentett játék)
    private string _currentRunPlayerName = string.Empty; // Az aktuális játékos neve

    // Várakozó állapotváltozások sorba rendezve
    private Queue<Func<Task>> deferredStateChanges = new Queue<Func<Task>>();

    // A játék szintjeit reprezentáló enum
    public enum GameLevel
    {
        Level1, // Első szint
        Level2, // Második szint
        Level3, // Harmadik szint
        Level4, // Negyedik szint
        BossBattle // Bossa harc
    }


    // A játék állapotait reprezentáló enum
    public enum GameState
    {
        MainMenu, // Főmenü
        LoadingNewGame, // Új játék betöltése
        LoadingNextLevel, // Következő szint betöltése
        LoadingSavedGame, // Mentett játék betöltése
        Playing, // Játék folyamatban
        Paused, // Szüneteltetett állapot
        GameOver, // Játék vége
        Victory, // Győzelem
        PlayerUpgrade, // Játékos fejlesztés
        Quitting // Kilépés
    }


    /// <summary>
    /// Komponensek
    /// </summary>
    LevelManager levelmanager;
    GameSceneManager gameSceneManager;
    SaveLoadManager saveLoadManager;
    PlayerUpgradeManager playerUpgradeManager;
    UIManager uiManager;
    Timer timer;


    /// <summary>
    /// Getterek és Setterek
    /// </summary>
    public bool IsStateChanging
    {
        get { return _isStateChanging; }
        set { _isStateChanging = value; }
    }
    public float PlayerHealtPercenatge
    {
        get { return _playerHealthPercentage; }
        set { _playerHealthPercentage = value; }
    }

    public int PlayerPoints
    {
        get { return _points; }
        set { _points = value; }
    }

    public GameLevel CurrentLevel
    {
        get { return _currentLevel; }
        set { _currentLevel = value; }
    }

    public float CurrentRunTime
    {
        get { return _currentRunTime; }
        set { _currentRunTime = value; }
    }

    public string CurrentRunDate
    {
        get { return _currentRunDate; }
        set { _currentRunDate = value; }
    }

    public string CurrentRunPlayerName
    {
        get { return _currentRunPlayerName; }
        set { _currentRunPlayerName = value; }
    }


    public GameState CurrentState { get; private set; } = GameState.MainMenu;


    /// <summary>
    /// Események
    /// </summary>
    //public event Action<GameState> OnStateChanged;
    public event Action<int> OnPointsChanged;

    /// <summary>
    /// Inicializálja a GameStateManager-t, összegyűjti a Persistent Manager referenciákat és feliratkozik az eseményekre.
    /// </summary>
    protected override async void Initialize()
    {
        base.Initialize();
        // Persistent Manager referenciák összegűjtése
        levelmanager = FindObjectOfType<LevelManager>();
        gameSceneManager = FindObjectOfType<GameSceneManager>();
        saveLoadManager = FindObjectOfType<SaveLoadManager>();
        playerUpgradeManager = FindObjectOfType<PlayerUpgradeManager>();
        uiManager = FindAnyObjectByType<UIManager>();
        timer = FindObjectOfType<Timer>();

        // Persistent Manager esemény-feliratkozások
        levelmanager.OnLevelCompleted += IsActualLevelCompleted; // Szint befejezése esemény feliratkozás
        levelmanager.OnGameFinished += IsGameFinished; // Játék befejezése esemény feliratkozás
        levelmanager.OnPointsAdded += AddPoints; // Pont hozzáadása esemény feliratkozás
        saveLoadManager.OnSaveRequested += SaveGameData; // Mentés kérés esemény feliratkozás
        saveLoadManager.OnScoreboardUpdateRequested += UpdateScoreboardData; // Pontszám frissítése kérés esemény feliratkozás
        uiManager.OnStartNewGame += HandleStateChanged; // Új játék kezdése esemény feliratkozás
        uiManager.OnLoadGame += HandleStateChanged; // Mentett játék betöltése esemény feliratkozás
        uiManager.OnExitGame += HandleStateChanged; // Kilépés esemény feliratkozás
        uiManager.OnGamePaused += HandleStateChanged; // Játék szüneteltetése esemény feliratkozás
        uiManager.OnGameResumed += HandleStateChanged; // Játék folytatása esemény feliratkozás
        uiManager.OnBackToMainMenu += HandleStateChanged; // Visszatérés a főmenübe esemény feliratkozás
        uiManager.OnPurchaseOptionChosen += IsPurchaseOptionChosen; // Vásárlási opció kiválasztása esemény feliratkozás

        //await Task.Yield();
    }

    /// <summary>
    /// Kezeli az állapotváltásokat. A játék állapotának módosítása előtt biztosítja, hogy a korábbi állapotváltozás befejeződjön.
    /// </summary>
    /// <param name="newState">Az új játékállapot, amire váltani kell</param>
    private async void HandleStateChanged(GameState newState)
    {
        Debug.Log("State is changing to: " + newState); // Kiírja az új állapotot a konzolra

        // Várakozik, hogy a SetState befejeződjön, mielőtt folytatná a következő művelettel
        await SetState(newState);
    }

    /// <summary>
    /// Frissíti a pontszámokat a scoreboard (ponttáblázat) adatain.
    /// </summary>
    /// <param name="scoreboardData">A ponttáblázat adatai, amelyet frissíteni kell</param>
    void UpdateScoreboardData(ScoreboardData scoreboardData)
    {
        // Létrehoz egy új rekordot a ponttáblázathoz a futás adataival
        ScoreboardEntry newScoreboardEntry = new ScoreboardEntry(CurrentRunDate, CurrentRunPlayerName, PlayerPoints, timer.FormatTime(CurrentRunTime));

        // Hozzáadja az új rekordot a ponttáblázat bejegyzéseihez
        scoreboardData.scoreboardEntries.Add(newScoreboardEntry);
    }

    /// <summary>
    /// Ment egy játékállapotot a mentett adatokba.
    /// </summary>
    /// <param name="saveData">A mentett adatok, amelyekbe elmentjük a játék állapotát</param>
    void SaveGameData(SaveData saveData)
    {
        // Beállítja a mentett adatokat az aktuális játék állapotának megfelelően
        saveData.gameData.gameLevel = _currentLevel.ToString(); // Az aktuális szint mentése
        saveData.gameData.points = PlayerPoints; // A játékos pontszáma mentése
        saveData.playerSaveData.currentHealtPercentage = PlayerHealtPercenatge; // A játékos aktuális életerejének mentése
        saveData.gameData.currentRunTime = CurrentRunTime; // Az aktuális játékidő mentése
    }

    /// <summary>
    /// Betölti a játék állapotát a mentett adatokból, és beállítja a megfelelő változókat.
    /// </summary>
    /// <param name="loadData">A betöltött adatok, amelyek tartalmazzák a játék állapotát</param>
    /// <returns>True, ha a betöltés sikeres volt, különben false</returns>
    async Task<bool> SetLoadDataAsync(SaveData loadData)
    {
        try
        {
            // Beállítja az aktuális szintet a mentett adatok alapján
            await SetCurrentLevel(LevelNameToGameLevel(loadData.gameData.gameLevel));
            // Beállítja a játékos pontszámát a mentett adatok alapján
            PlayerPoints = loadData.gameData.points;
            // Beállítja a játékos életerejét a mentett adatok alapján
            PlayerHealtPercenatge = loadData.playerSaveData.currentHealtPercentage;
            // Beállítja az aktuális játékidőt a mentett adatok alapján
            CurrentRunTime = loadData.gameData.currentRunTime;
            // Betölti a játékos fejlesztéseit
            await playerUpgradeManager.SetLoadDataAsync(loadData);

            // Sikeres betöltés
            return true;
        }
        catch (Exception ex)
        {
            // Ha hiba történik, kiírja a hibát a konzolra
            Debug.LogError($"Error during load setup in GameStateManager! {ex.Message}");
            return false; // Hibás betöltés
        }

    }

    /// <summary>
    /// Átalakítja a szint nevét a megfelelő GameLevel enummá.
    /// </summary>
    /// <param name="levelName">A szint neve, amit át kell alakítani</param>
    /// <returns>A megfelelő GameLevel érték</returns>
    /// <exception cref="ArgumentException">Ha érvénytelen szint név kerül átadásra</exception>
    public GameLevel LevelNameToGameLevel(string levelName)
    {
        // Megpróbálja átalakítani a szint nevét a GameLevel enumeráció megfelelő értékévé
        if (Enum.TryParse(levelName, out GameLevel level))
        {
            return level; // Ha sikeres, visszaadja a GameLevel értéket
        }
        else
        {
            // Ha nem sikerül az átalakítás, kivételt dob érvénytelen szint névvel
            throw new ArgumentException($"Invalid level value: {levelName}");
        }
    }

    /// <summary>
    /// Események leiratkozása, amikor a GameStateManager objektum megsemmisül.
    /// </summary>
    private void OnDestroy()
    {
        // Ha a saveLoadManager létezik, leiratkozik a mentési és pontszám frissítési eseményekről
        if (saveLoadManager != null)
        {
            saveLoadManager.OnSaveRequested -= SaveGameData; // Mentési esemény leiratkozás
            saveLoadManager.OnScoreboardUpdateRequested -= UpdateScoreboardData; // Pontszám frissítési esemény leiratkozás
        }

        // Ha a levelmanager létezik, leiratkozik a szint befejezése, pont hozzáadása és játék befejezése eseményekről
        if (levelmanager != null)
        {
            levelmanager.OnLevelCompleted -= IsActualLevelCompleted; // Szint befejezése esemény leiratkozás
            levelmanager.OnPointsAdded -= AddPoints; // Pont hozzáadása esemény leiratkozás
            levelmanager.OnGameFinished -= IsGameFinished; // Játék befejezése esemény leiratkozás
        }

        // Ha az uiManager létezik, leiratkozik a különböző játék állapot változásokat kezelő eseményekről
        if (uiManager != null)
        {
            uiManager.OnStartNewGame -= HandleStateChanged; // Új játék kezdése esemény leiratkozás
            uiManager.OnLoadGame -= HandleStateChanged; // Mentett játék betöltése esemény leiratkozás
            uiManager.OnExitGame -= HandleStateChanged; // Kilépés esemény leiratkozás
            uiManager.OnGamePaused -= HandleStateChanged; // Játék szüneteltetése esemény leiratkozás
            uiManager.OnGameResumed -= HandleStateChanged; // Játék folytatása esemény leiratkozás
            uiManager.OnBackToMainMenu -= HandleStateChanged; // Visszatérés a főmenübe esemény leiratkozás
            uiManager.OnPurchaseOptionChosen -= IsPurchaseOptionChosen; // Vásárlási opció kiválasztása esemény leiratkozás
        }
    }

    /// <summary>
    /// Hozzáadja a pontokat a játékos összpontszámához, és értesíti az eseménykezelőket a változásról.
    /// </summary>
    /// <param name="points">A hozzáadott pontok száma</param>
    public void AddPoints(int points)
    {
        this.PlayerPoints += points; // A pontok hozzáadása a játékos összpontszámához
        OnPointsChanged?.Invoke(PlayerPoints); // Az esemény meghívása, hogy értesítse a többi rendszert a pontszám változásáról
    }

    /// <summary>
    /// Kezeli a játék befejezését, és a megfelelő UI elemet jeleníti meg a játékos számára.
    /// </summary>
    /// <param name="isFinished">A játék befejezésének állapota (true ha vége van, false ha nem)</param>
    async void IsGameFinished(bool isFinished)
    {
        if (isFinished)
        {
            // Ha a játék véget ért és a játékos nyert, a győzelem UI elemet jelenítjük meg
            await SetState(GameState.Victory);
        }
        else
        {
            // Ha a játék véget ért és a játékos veszített, a Game Over UI elemet jelenítjük meg
            await SetState(GameState.GameOver);
        }
    }

    /// <summary>
    /// Kezeli az aktuális szint befejezését, és ennek megfelelően frissíti a játék állapotát.
    /// </summary>
    /// <param name="isCompleted">A szint befejezésének állapota (true ha befejeződött, false ha nem)</param>
    /// <param name="playerHealthPercentage">A játékos aktuális életereje (százalékos érték)</param>
    async void IsActualLevelCompleted(bool isCompleted, float playerHealthPercentage)
    {
        this.PlayerHealtPercenatge = playerHealthPercentage; // Frissíti a játékos életerejét
        Debug.Log(PlayerHealtPercenatge); // Kiírja a konzolra az új életerőt

        try
        {
            if (isCompleted)
            {
                // Ha a szint befejeződött, a játékos fejlesztéseihez vezet
                await SetState(GameState.PlayerUpgrade);
            }
            else
            {
                // Ha a szint nem lett befejezve, a játékos vereségét jeleníti meg
                await SetState(GameState.GameOver);
            }

        }
        catch (Exception ex)
        {
            // Ha hiba történik, kiírja a hibát a konzolra
            Debug.LogError($"Hiba történt az IsActualLevelCompleted metódusban: {ex.Message}");
        }
    }

    /// <summary>
    /// Kezeli a vásárlási opció kiválasztását, és végrehajtja a játékos fejlesztésének megvásárlását.
    /// </summary>
    /// <param name="upgradeID">A fejlesztés azonosítója, amelyet a játékos vásárolni szeretne</param>
    async void IsPurchaseOptionChosen(string upgradeID)
    {
        bool asyncOperation;

        // Aszinkron módon megpróbálja végrehajtani a játékos fejlesztésének megvásárlását
        asyncOperation = await playerUpgradeManager.PurchasePlayerUpgrade(upgradeID);

        if (!asyncOperation)
        {
            // Ha a vásárlás nem sikerült, hibát ír ki
            Debug.LogError("Hiba fejlesztés-vásárlása során!");
        }

        // Miután a vásárlás befejeződött (vagy sikertelen), a következő szint betöltésére vált
        await SetState(GameState.LoadingNextLevel);
    }



    // TODO: cursor setter method
    /// <summary>
    /// Beállítja a játék aktuális állapotát és kezeli az állapotok közötti átmeneteket.
    /// </summary>
    /// <param name="newState">Az új játékállapot, amelyre váltani szeretnénk</param>
    /// <returns>Aszinkron feladat, amely biztosítja, hogy az állapotváltás befejeződjön</returns>
    public async Task SetState(GameState newState)
    {
        // Ha már folyamatban van egy állapotváltás, akkor nem végezhetünk el újabb váltást
        if (IsStateChanging)
        {
            Debug.LogWarning("State change already in progress! " + newState);
            return;
        }

        // Beállítja, hogy éppen egy állapotváltás történik
        IsStateChanging = true;

        try
        {
            // Ha az aktuális állapot már az új állapot, akkor nem kell semmit csinálni
            if (CurrentState == newState)
            {
                return;
            }

            CurrentState = newState; // Beállítja az új állapotot
            //OnStateChanged?.Invoke(CurrentState);

            bool asyncOperation;
            switch (CurrentState)
            {
                case GameState.MainMenu:
                    timer.StopTimer(); // Megállítja az időzítőt
                    asyncOperation = await gameSceneManager.LoadUtilitySceneAsync("MainMenu"); // Betölti a főmenü jelenetet
                    // Visszaállítja a játékos fejlesztéseit
                    asyncOperation = await playerUpgradeManager.ResetPlayerUpgradesListsAsync();
                    // UIManager gombokat beállító függvény hívása!
                    break;

                case GameState.LoadingNewGame:
                    // Új játék kezdése: alapértékek beállítása
                    this.PlayerPoints = 0;
                    this.PlayerHealtPercenatge = 1f;
                    this.CurrentRunTime = 0f;
                    asyncOperation = await ResetCurrentLevel(); // Az aktuális szint visszaállítása

                    // mentés törlése új játék esetén
                    //asyncOperation = await saveLoadManager.DeleteSaveFile();

                    // Animált bevezető betöltése
                    Time.timeScale = 1;
                    // load newGame cutscene :: sceneManager
                    asyncOperation = await gameSceneManager.LoadAnimatedCutsceneAsync("NewGame");

                    // // Az első szint betöltése :: LevelManager
                    asyncOperation = await levelmanager.LoadNewLevelAsync(GameLevelToInt(CurrentLevel));
                    if (asyncOperation)
                    {
                        // Ha a szint sikeresen betöltődött, állapotot váltunk a "Playing"-re
                        DeferStateChange(() => SetState(GameState.Playing));
                        timer.RestartTimer(); // Újraindítja az időzítőt
                    }

                    break;

                case GameState.LoadingNextLevel:
                    // Következő szint betöltése
                    Time.timeScale = 1;
                    CurrentRunTime = timer.GetElapsedTime();

                    asyncOperation = await IncrementCurrentLevel(); // Az aktuális szint növelése

                    // BOSS harc kezelése
                    if (CurrentLevel == GameLevel.BossBattle)
                    {
                        asyncOperation = await levelmanager.LoadBossLevelAsync();
                        if (asyncOperation)
                        {
                            // Ha a szint betöltődött, váltunk "Playing" állapotra
                            DeferStateChange(() => SetState(GameState.Playing));
                        }

                    }
                    else
                    {
                        // Egyéb szintek esetén animált átvezetők betöltése
                        string cutsceneRefName = "LevelTransition" + (GameLevelToInt(CurrentLevel) - 1).ToString() + GameLevelToInt(CurrentLevel).ToString();
                        asyncOperation = await gameSceneManager.LoadAnimatedCutsceneAsync(cutsceneRefName);

                        asyncOperation = await levelmanager.LoadNewLevelAsync(GameLevelToInt(CurrentLevel));
                        if (asyncOperation)
                        {
                            // Ha a szint betöltődött, váltunk "Playing" állapotra
                            DeferStateChange(() => SetState(GameState.Playing));
                        }
                    }
                    break;

                case GameState.LoadingSavedGame:
                    Time.timeScale = 1;
                    // Mentett játék betöltése
                    SaveData loadData = await saveLoadManager.LoadGameAsync(); // Mentett adat betöltése
                    if (loadData != null)
                    {
                        // GameStateManager adatok beállítása
                        asyncOperation = await SetLoadDataAsync(loadData); 

                        // LevelManager hívása loadData-val
                        asyncOperation = await levelmanager.LoadSavedLevelAsync(loadData);
                        if (asyncOperation)
                        {
                            // Ha a szint sikeresen betöltődött, váltunk "Playing" állapotra
                            DeferStateChange(() => SetState(GameState.Playing));
                            timer.SetTimer(CurrentRunTime); // Beállítja a korábbi időt
                        }
                    }
                    else
                    {
                        // Ha nincs betöltött adat, hibát ír ki
                        Debug.LogError("LOAD DATA IS NULL!");
                    }

                    break;

                case GameState.Playing:
                    Time.timeScale = 1; // A játék folytatása
                    timer.ResumeTimer(); // Folytatja az időzítőt
                    //Cursor.visible = false;  // Elrejti a kurzort
                    //Cursor.lockState = CursorLockMode.Locked;  // A kurzort középre zárja
                    break;

                case GameState.Paused:
                    Time.timeScale = 0; // A játék megállítása, időzítő leállítása
                    timer.StopTimer();
                    //Cursor.visible = true;  // Megjeleníti a kurzort
                    //Cursor.lockState = CursorLockMode.None;  // Unlockolja a kurzort
                    break;

                case GameState.GameOver:
                    // GameOver: mentés törlése
                    asyncOperation = await saveLoadManager.DeleteSaveFile();

                    // csak akkor látszódjon a kisfilm, ha boss szinten vagyunk a halálkor!
                    if (CurrentLevel == GameLevel.BossBattle)
                    {
                        asyncOperation = await gameSceneManager.LoadAnimatedCutsceneAsync("Defeat");
                    }
                    Time.timeScale = 0; // Játék leállítása
                    asyncOperation = await uiManager.DisplayDefeatPanelAsync(); // Defeat panel megjelenítése

                    break;

                case GameState.Victory:
                    // Győzelem esetén: idő leállítása és győzelem animáció betöltése
                    timer.StopTimer();
                    CurrentRunTime = timer.GetElapsedTime();
                    Debug.Log(timer.FormatTime(CurrentRunTime));

                    // végső idő
                    CurrentRunTime = timer.GetElapsedTime();    // formázás stringként
                    // aktuális dátum
                    CurrentRunDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                    
                    asyncOperation = await saveLoadManager.DeleteSaveFile();

                    asyncOperation = await gameSceneManager.LoadAnimatedCutsceneAsync("Victory");
                    Time.timeScale = 0;
                    // Győzelem panel megjelenítése
                    asyncOperation = await uiManager.DisplayVictoryPanelAsync();

                    break;

                case GameState.PlayerUpgrade:
                    // Fejlesztések képernyője: kurzor láthatóvá tétele és időzítő leállítása
                    Cursor.visible = true;
                    Cursor.lockState = CursorLockMode.None;
                    Time.timeScale = 0;
                    timer.StopTimer();

                    // A vásárolható fejlesztések betöltése
                    asyncOperation = await playerUpgradeManager.GenerateCurrentShopUpgradesAsync(GameLevelToInt(CurrentLevel), PlayerHealtPercenatge);
                    asyncOperation = await uiManager.LoadUpgradesShopUIAsync(playerUpgradeManager.CurrentShopUpgrades);

                    break;

                case GameState.Quitting:
                    // Kilépés a játékból
                    Debug.Log("EXIT Game");
                    Application.Quit();
                    break;
            }

            Debug.Log(CurrentState); // Aktuális állapot kiírása konzolra
        }
        finally
        {
            IsStateChanging = false; // Az állapotváltás befejeződött

            // Végrehajtja az elhalasztott állapotváltásokat
            while (deferredStateChanges.Count > 0)
            {
                var deferredAction = deferredStateChanges.Dequeue();
                await deferredAction(); // Elhalasztott műveletek végrehajtása
            }
        }


    }
    /// <summary>
    /// Elhalasztja az állapotváltást és hozzáadja a várakozó akciók listájához.
    /// Az elhalasztott akció később kerül végrehajtásra.
    /// </summary>
    /// <param name="action">Az akció, amely az állapotváltást végzi, aszinkron módon.</param>
    private void DeferStateChange(Func<Task> action)
    {
        // Hozzáadja az akciót a deferredStateChanges várólistához
        deferredStateChanges.Enqueue(action);
    }

    /// <summary>
    /// Átalakítja a játék szintjét (GameLevel) egész számra (int).
    /// Mivel a GameLevel enum alapértelmezés szerint 0-tól kezdődő értékekkel rendelkezik, a metódus
    /// hozzáad 1-et, hogy 1-től kezdődően térjen vissza.
    /// </summary>
    /// <param name="gameLevel">A játék szintje (GameLevel), amelyet egész számra kell konvertálni.</param>
    /// <returns>Az enum értékének megfelelő egész szám, 1-től kezdődően.</returns>
    public int GameLevelToInt(GameLevel gameLevel)
    {
        return (int)gameLevel + 1; // A GameLevel enum értékéhez hozzáadunk 1-et, hogy 1-től kezdődő számot kapjunk
    }

    /// <summary>
    /// Átalakítja a megadott szint számot a megfelelő GameLevel enum értékké.
    /// Ha a megadott szám nem érvényes szintérték, ArgumentException kivételt dob.
    /// </summary>
    /// <param name="levelValue">Az int érték, amely a játék szintjét képviseli.</param>
    /// <returns>A GameLevel enum értéke, amely megfelel az adott egész számnak.</returns>
    /// <exception cref="ArgumentException">Ha a levelValue nem érvényes szintérték, kivételt dob.</exception>
    public GameLevel IntToGameLevel(int levelValue)
    {
        // Ellenőrzi, hogy a levelValue érvényes érték-e a GameLevel enum számára
        if (Enum.IsDefined(typeof(GameLevel), levelValue))
        {
            return (GameLevel)levelValue; // Visszaadja a megfelelő GameLevel értéket
        }
        else
        {
            throw new ArgumentException($"Invalid level value: {levelValue}"); // Ha nem érvényes, kivételt dob
        }
    }

    /// <summary>
    /// Beállítja az aktuális szintet (GameLevel) és elvégzi a szükséges aszinkron műveleteket.
    /// A művelet befejezése után igaz értéket ad vissza, ha sikeres, különben hibát naplóz.
    /// </summary>
    /// <param name="level">A beállítandó szint (GameLevel).</param>
    /// <returns>True, ha a szint beállítása sikerült, false, ha hiba történt.</returns>
    async Task<bool> SetCurrentLevel(GameLevel level)
    {
        await Task.Yield(); // Az aszinkron művelet elhalasztása, hogy a vezérlés más feladatoknak is lehetőséget adjon.

        try
        {
            CurrentLevel = level; // Az aktuális szint beállítása
            return true; // Sikeres beállítás
        }
        catch (Exception ex)
        {
            Debug.LogError($"ERROR DURING INCREMIENTING LEVEL! {ex.Message}"); // Hiba esetén naplózás
            return false; // Hiba esetén false érték visszaadása
        }
    }

    /// <summary>
    /// Növeli az aktuális szintet (GameLevel) és elvégzi a szükséges aszinkron műveleteket.
    /// A művelet befejezése után igaz értéket ad vissza, ha sikeres, különben hibát naplóz.
    /// </summary>
    /// <returns>True, ha a szint növelése sikerült, false, ha hiba történt.</returns>
    async Task<bool> IncrementCurrentLevel()
    {
        await Task.Yield(); // Az aszinkron művelet elhalasztása, hogy a vezérlés más feladatoknak is lehetőséget adjon.

        try
        {
            CurrentLevel++; // Az aktuális szint növelése
            return true; // Sikeres növelés
        }
        catch (Exception ex)
        {
            Debug.LogError($"ERROR DURING INCREMIENTING LEVEL! {ex.Message}"); // Hiba esetén naplózás
            return false; // Hiba esetén false érték visszaadása
        }
    }


    /// <summary>
    /// Visszaállítja az aktuális szintet az első szintre (Level1) és elvégzi a szükséges aszinkron műveleteket.
    /// A művelet befejezése után igaz értéket ad vissza, ha sikeres, különben hibát naplóz.
    /// </summary>
    /// <returns>True, ha a szint visszaállítása sikerült, false, ha hiba történt.</returns>
    async Task<bool> ResetCurrentLevel()
    {
        await Task.Yield(); // Az aszinkron művelet elhalasztása, hogy a vezérlés más feladatoknak is lehetőséget adjon.

        try
        {
            CurrentLevel = GameLevel.Level1; // Az aktuális szint visszaállítása az első szintre
            return true; // Sikeres visszaállítás
        }
        catch (Exception ex)
        {
            Debug.LogError($"ERROR DURING INCREMIENTING LEVEL! {ex.Message}"); // Hiba esetén naplózás
            return false; // Hiba esetén false érték visszaadása
        }
    }

}
