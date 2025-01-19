using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using System.Linq;
using UnityEngine.Windows;
using static GameStateManager;

public class UIManager : BasePersistentManager<UIManager>
{
    // A vásárlási lehetőségek osztálya, amely tartalmazza a szükséges információkat egy vásárlási opcióról
    [System.Serializable]
    public class PurchaseOption
    {
        public string ID; // A vásárlási opció egyedi azonosítója
        public string Name; // A vásárlási opció neve
        public int minLevel; // Minimális szint, amely szükséges a vásárláshoz
        public int maxLevel; // Maximális szint, amelyen a vásárlás érvényes
        public int currentLevel; // Az aktuális szint
        public Sprite Icon; // A vásárlási opcióhoz tartozó ikon
        public string Description; // A vásárlási opció leírása
        public int Price; // A vásárlás ára
    }

    /// <summary>
    /// A változók, amelyeket az UI kezelése során használunk
    /// </summary>
    GameObject DMGStatsPanel; // A sebzés statisztikák panelje
    GameObject upgradeShopPanel; // A fejlesztés boltjának panelje
    GameObject menuButtonPanel; // A menü gomb panelje
    Canvas pauseMenuCanvas; // A szünet menü canvas
    Canvas playerUICanvas; // A játékos UI-ja
    Canvas upgradeShopUICanvas; // A fejlesztési bolt UI-ja

    public List<PurchaseOption> purchaseOptions; // Vásárlási opciók listája

    // Egy szótár, amely a TextMeshProUGUI elemeket tárolja az azonosítójukkal
    private Dictionary<string, TextMeshProUGUI> textMeshProElementReferences = new Dictionary<string, TextMeshProUGUI>();

    // Egy szótár, amely a játékos változóinak értékeit tárolja
    private Dictionary<string, string> playerVariableValues = new Dictionary<string, string>();

    bool isPauseMenuEnabled; // A szünet menü állapotát tároló változó

    /// <summary>
    /// Komponensek, amelyeket az UI kezeléséhez használunk
    /// </summary>
    SaveLoadManager saveLoadManager; // A játék mentését és betöltését kezelő komponens
    PlayerController player; // A játékos vezérlését kezelő komponens
    GameStateManager gameStateManager; // A játék állapotát kezelő komponens

    [Header("Main Menu")]
    [SerializeField]
    private Button newGameButton; // Az új játék gomb
    [SerializeField]
    private Button loadGameButton; // A betöltés gomb
    [SerializeField]
    private Button settingsButton; // A beállítások gomb
    [SerializeField]
    private Button scoreboardButton; // Az eredménylista gomb
    [SerializeField]
    private Button quitGameButton; // A kilépés gomb

    [Header("Scoreboard Menu Panels")]
    [SerializeField]
    private GameObject scoreboardPanel; // Az eredménylista panelje
    [SerializeField]
    private GameObject scoreboardEntryPrefab; // Az eredménylista bejegyzés prefabja

    [Header("UI Panels")]
    public GameObject menuButtonUIPrefab; // A menü gombok UI prefabja
    public GameObject pauseMenuUIPrefab; // A szünet menü UI prefabja
    public GameObject playerStatsUIPrefab; // A játékos statjai UI prefabja
    public GameObject damageStatsUIPrefab; // A sebzés statjai UI prefabja
    public GameObject upgradeShopUIPrefab; // A fejlesztési bolt UI prefabja
    public GameObject pauseMenu; // A szünet menü

    [Header("Shop UI")]
    [SerializeField]
    private GameObject upgradesPanel; // A fejlesztések panelje
    [SerializeField]
    private GameObject normalUpgradeUIPrefab; // A normál fejlesztés UI prefabja
    [SerializeField]
    private GameObject nextLevelUpgradeUIPrefab; // A következő szint fejlesztés UI prefabja
    [SerializeField]
    private GameObject healUpgradeUIPrefab; // A heal fejlesztés UI prefabja

    [Header("Victory/Defeat Panels")]
    [SerializeField]
    private GameObject victoryPanelPrefab; // A győzelem panel prefabja
    [SerializeField]
    private GameObject defeatPanelPrefab; // A vereség panel prefabja

    /// <summary>
    /// Események, amelyek a játék különböző állapotváltozásait kezelik
    /// </summary>
    public event Action<GameState> OnStartNewGame; // Az esemény, amely akkor aktiválódik, amikor új játékot indítunk
    public event Action<GameState> OnLoadGame; // Az esemény, amely akkor aktiválódik, amikor egy mentett játékot betöltünk
    public event Action<GameState> OnExitGame; // Az esemény, amely akkor aktiválódik, amikor kilépünk a játékból
    public event Action<GameState> OnGamePaused; // Az esemény, amely akkor aktiválódik, amikor a játék szüneteltetve van
    public event Action<GameState> OnGameResumed; // Az esemény, amely akkor aktiválódik, amikor a játék folytatódik
    public event Action<GameState> OnBackToMainMenu; // Az esemény, amely akkor aktiválódik, amikor visszatérünk a főmenübe
    public event Action<string> OnPurchaseOptionChosen; // Az esemény, amely akkor aktiválódik, amikor a felhasználó választ egy vásárlási opciót

    /// <summary>
    /// Inicializálja a szükséges komponenseket és eseményeket
    /// </summary>
    protected override async void Initialize()
    {
        base.Initialize(); // Meghívja az ősosztály (BasePersistentManager) Initialize metódusát

        // Kiválasztja és tárolja a SaveLoadManager példányát a játékban
        saveLoadManager = FindObjectOfType<SaveLoadManager>();

        // Kiválasztja és tárolja a GameStateManager példányát a játékban
        gameStateManager = FindObjectOfType<GameStateManager>();

        // Feliratkozik a GameStateManager OnPointsChanged eseményére, hogy frissítse a pontokat megjelenítő UI-t
        gameStateManager.OnPointsChanged += UpdatePointsUIText;
    }

    /// <summary>
    /// A komponens inicializálása után, a játék indításakor végrehajtódik
    /// </summary>
    void Start()
    {
        //SetMainMenuButtonReferences(); // A főmenü gombok hivatkozásainak beállítása
        //UpdateMainMenuButtons(); // A főmenü gombok frissítése
    }

    /// <summary>
    /// Az Update metódus minden egyes frame-ben meghívódik, és folyamatosan ellenõrzi, hogy
    /// lenyomták-e az Escape billentyût, hogy aktiválja a szünet menüt.
    /// </summary>
    private void Update()
    {
        // Ellenõrizzük, hogy lenyomták-e az Escape billentyût
        if (isPauseMenuEnabled && pauseMenu != null && UnityEngine.Input.GetKeyDown(KeyCode.Escape))
        {
            // Ha az Escape billentyût lenyomták, aktiváljuk a szünet menüt
            SetPauseMenuActive();
        }
    }


    /// <summary>
    /// Aszinkron módon betölti a játék UI elemeit, például a szünet menüt, a játékos statisztikai paneljét és az upgrade shopot.
    /// A funkció biztosítja, hogy a szükséges elemek és események inicializálása megtörténjen a UI betöltése elõtt.
    /// Hibák esetén false értéket ad vissza, siker esetén true-t.
    /// </summary>
    /// <returns>Visszaadja a UI betöltésének sikerességét: true ha sikerült, false ha hiba történt.</returns>
    public async Task<bool> LoadPlayerUIAsync()
    {
        // Várakozást adunk, hogy az aszinkron folyamat ne blokkolja a fõ szálat
        await Task.Yield();

        try
        {
            // A szünet menü létrehozása és feltöltése
            CreateAndPopulatePauseMenuCanvas();

            // A játékos statisztikai paneljének létrehozása és feltöltése
            CreateAndPopulatePlayerStatsCanvas();

            // Az upgrade shop létrehozása és feltöltése
            CreateAndPopulateUpgradeShopCanvas();

            // Esemény rendszer hozzáadása, ha még nem létezne
            EnsureEventSystemExists();

            // Megkeressük a szünet menü GameObject-et a hozzá tartozó prefab alapján
            pauseMenu = FindInChildrenIgnoreClone(pauseMenuCanvas.transform, pauseMenuUIPrefab.name);

            // Megkeressük a játékost
            player = FindObjectOfType<PlayerController>();
            player.OnPlayerDeath += StopPlayerHealthDetection;
            player.OnHealthChanged += UpdateHealthUIText;


            // Inicializáljuk a TextMeshPro refernecia-elemek szótárát
            textMeshProElementReferences.Clear();
            InitializeCanvasTextElementsDictionary(textMeshProElementReferences, playerUICanvas);

            // Inicializáljuk a játékos statisztikáit tartalmazó szótárat
            playerVariableValues.Clear();
            InitializePlayerStatNamesDictionary(playerVariableValues, player, gameStateManager);


            // A UI szövegek beállítása a megfelelõ értékekkel
            SetCurrentUITextValues(textMeshProElementReferences, playerVariableValues);

            isPauseMenuEnabled = true;

            return true; // A UI sikeresen betöltõdött
        }
        catch (Exception ex)
        {
            // Ha hiba történik, naplózzuk a hibaüzenetet
            Debug.LogError($"Hiba történt a játék UI betöltése közben: {ex.Message}");
            return false; // Visszaadjuk a hibát jelzõ értéket
        }
    }


    /// <summary>
    /// Létrehozza és feltölti a szünet menü canvas-t.
    /// A szünet menü prefabját hozzáadja a canvas-hoz, és kezdetben nem aktívra állítja.
    /// </summary>
    private void CreateAndPopulatePauseMenuCanvas()
    {
        // Létrehozza a szünet menü canvas-t
        pauseMenuCanvas = CreateCanvas("PauseMenuCanvas", 100);

        // Hozzáadja a szünet menü prefabját a canvas-hoz, kezdetben nem aktív
        AddUIPrefabToGameObject(pauseMenuCanvas.gameObject, pauseMenuUIPrefab, false);
    }


    /// <summary>
    /// Létrehozza és feltölti a játékos statisztikai paneljét tartalmazó canvas-t.
    /// A játékos statisztikákat, a damage statisztikát és a menü gombot hozzáadja a canvas-hoz.
    /// </summary>
    private void CreateAndPopulatePlayerStatsCanvas()
    {
        // Létrehozza a játékos statisztikai panel canvas-t
        playerUICanvas = CreateCanvas("GameUICanvas", 10);

        // Hozzáadja a játékos statisztikákat tartalmazó prefabot
        AddUIPrefabToGameObject(playerUICanvas.gameObject, playerStatsUIPrefab, true);

        // Hozzáadja a damage statisztikát tartalmazó prefabot, kezdetben nem aktív
        AddUIPrefabToGameObject(playerUICanvas.gameObject, damageStatsUIPrefab, false);

        // Hozzáadja a menü gombot tartalmazó prefabot, kezdetben nem aktív
        AddUIPrefabToGameObject(playerUICanvas.gameObject, menuButtonUIPrefab, false);
    }


    /// <summary>
    /// Létrehozza és feltölti az upgrade shop canvas-t.
    /// Az upgrade shop prefabot hozzáadja a canvas-hoz, kezdetben nem aktívra állítva.
    /// </summary>
    private void CreateAndPopulateUpgradeShopCanvas()
    {
        // Létrehozza az upgrade shop canvas-t
        upgradeShopUICanvas = CreateCanvas("UpgradeShopUICanvas", 5);

        // Hozzáadja az upgrade shop prefabját a canvas-hoz, kezdetben nem aktív
        AddUIPrefabToGameObject(upgradeShopUICanvas.gameObject, upgradeShopUIPrefab, false);
    }


    /// <summary>
    /// Canvas-t hoz létre vagy keres egy már létezõt, amely megfelel a megadott névnek.
    /// Ha nem található, új canvas-t hoz létre a megadott névvel és rendezési sorrenddel.
    /// </summary>
    /// <param name="canvasName">A keresett vagy létrehozandó canvas neve.</param>
    /// <param name="sortingOrder">A canvas rendezési sorrendje, amely meghatározza a rajta lévõ elemek z-indexét.</param>
    /// <returns>Visszaadja a megtalált vagy létrehozott Canvas objektumot.</returns>
    Canvas CreateCanvas(string canvasName, int sortingOrder)
    {
        // Keresünk egy létezõ canvas-t a megadott névvel
        Canvas existingCanvas = FindObjectsOfType<Canvas>()
            .FirstOrDefault(canvas => canvas.gameObject.name == canvasName);

        if (existingCanvas != null)
        {
            // Ha megtaláltuk, log-oljuk és visszaadjuk a létezõ canvas-t
            Debug.Log($"Found existing canvas: {existingCanvas.name}");
            return existingCanvas; // Visszaadjuk a megtalált canvas-t
        }

        // Ha nem találtunk, új canvas-t hozunk létre
        GameObject gameUICanvas = new GameObject(canvasName); // Új GameObject létrehozása a megadott névvel
        Canvas gameCanvas = gameUICanvas.AddComponent<Canvas>(); // Canvas komponens hozzáadása
        gameCanvas.renderMode = RenderMode.ScreenSpaceOverlay; // Beállítjuk a render módot (képernyõn jelenjen meg)
        gameCanvas.sortingOrder = sortingOrder; // Beállítjuk a rendezési sorrendet

        // Hozzáadunk fontos komponenseket: CanvasScaler és GraphicRaycaster
        gameUICanvas.AddComponent<CanvasScaler>();
        gameUICanvas.AddComponent<GraphicRaycaster>();

        // Log-oljuk az új canvas létrejöttét
        Debug.Log($"Created new canvas: {gameUICanvas.name}");
        return gameCanvas; // Visszaadjuk az új canvas-t
    }


    /// <summary>
    /// Egy UI prefab-t hozzáad a megadott szülõ GameObject-hez. Az új UI objektumot a megadott szülõhöz rendeli,
    /// és beállítja annak aktivitását.
    /// </summary>
    /// <param name="parent">A szülõ GameObject, amelyhez a UI prefab-t hozzáadjuk.</param>
    /// <param name="UIPrefab">A hozzáadandó UI prefab.</param>
    /// <param name="isActive">Beállítja, hogy az új UI objektum aktív vagy inaktív legyen.</param>
    void AddUIPrefabToGameObject(GameObject parent, GameObject UIPrefab, bool isActive)
    {
        // Ellenõrizzük, hogy a szülõ GameObject nem null
        if (parent == null)
        {
            Debug.LogError("Parent reference is null. Ensure the Canvas exists before adding UI elements.");
            return; // Ha a szülõ null, a metódus befejezõdik
        }

        // Ellenõrizzük, hogy a UI prefab nem null
        if (UIPrefab == null)
        {
            Debug.LogError("UIPrefab reference is null. Assign a valid UI prefab.");
            return; // Ha a UI prefab null, a metódus befejezõdik
        }

        // A prefab példányosítása a szülõ GameObject-hez adása
        GameObject uiInstance = Instantiate(UIPrefab, parent.transform);

        // Az új UI objektumot a szülõ objektum legutolsó gyermekeként állítjuk be
        uiInstance.transform.SetAsLastSibling();

        // Beállítjuk az UI objektum aktivitását
        uiInstance.SetActive(isActive);
    }


    /// <summary>
    /// Ellenõrzi, hogy létezik-e már EventSystem az adott jelenetben. Ha nem található, létrehoz egy új EventSystem objektumot,
    /// és hozzáadja az InputSystemUIInputModule-ot az input események kezeléséhez.
    /// </summary>
    void EnsureEventSystemExists()
    {
        // Ellenõrizzük, hogy létezik-e már EventSystem a jelenetben
        if (FindObjectOfType<EventSystem>() == null)
        {
            // Ha nincs, létrehozunk egy új EventSystem objektumot
            GameObject eventSystemObj = new GameObject("EventSystem");
            EventSystem eventSystem = eventSystemObj.AddComponent<EventSystem>(); // Hozzáadjuk az EventSystem komponenst

            // Hozzáadjuk az InputSystemUIInputModule-ot, amely kezeli az input eseményeket
            eventSystemObj.AddComponent<InputSystemUIInputModule>();
        }
    }


    /// <summary>
    /// Rekurzív módon keres egy gyermeket a hierarchiában, amelynek neve az adott `baseName`-el kezdõdik.
    /// A klónokat figyelmen kívül hagyja (a `Clone` elõtagot tartalmazó objektumokat nem találja meg).
    /// </summary>
    /// <param name="parent">Az a szülõobjektum, amelyben keresünk.</param>
    /// <param name="baseName">Az alap név, amellyel a gyermek neve kezdõdik.</param>
    /// <returns>A keresett objektum, ha megtalálható, egyébként null.</returns>
    private GameObject FindInChildrenIgnoreClone(Transform parent, string baseName)
    {
        // Végigiterálunk a szülõ összes gyermekén
        foreach (Transform child in parent)
        {
            // Ha a gyermek neve a baseName-el kezdõdik, akkor visszaadjuk a gyermeket
            if (child.name.StartsWith(baseName))
            {
                return child.gameObject;
            }

            // Rekurzív keresés a gyermekek alatt is
            GameObject result = FindInChildrenIgnoreClone(child, baseName);
            if (result != null)
            {
                return result;
            }
        }

        // Ha nem találunk semmit, akkor null-t adunk vissza
        return null;
    }


    /// <summary>
    /// Leállítja a játékos életerõ-figyelését, és eltávolítja az összes eseményfigyelõt.
    /// A játékos életerõ változására vonatkozó eseményeket és a játékos halálát figyelõ eseményeket törli,
    /// valamint a pontok változására vonatkozó eseményt is eltávolítja.
    /// </summary>
    void StopPlayerHealthDetection()
    {
        // Eltávolítja az 'OnHealthChanged' eseményfigyelõt, amely az életerõ változásra reagál
        player.OnHealthChanged -= UpdateHealthUIText;

        // Eltávolítja az 'OnPlayerDeath' eseményfigyelõt, amely a játékos halálára reagál
        player.OnPlayerDeath -= StopPlayerHealthDetection;
    }


    /// <summary>
    /// Frissíti a játékos életerejét megjelenítõ szöveges elemet.
    /// A `currentPlayerHP` értéket beállítja a "Health" kulcshoz tartozó szöveges elem szövegéhez.
    /// </summary>
    /// <param name="currentPlayerHP">A játékos aktuális életereje, amelyet meg kell jeleníteni.</param>
    public void UpdateHealthUIText(float currentPlayerHP)
    {
        // Beállítja a "Health" kulcshoz tartozó szöveges elem szövegét a játékos aktuális életerejére
        textMeshProElementReferences["Health"].text = currentPlayerHP.ToString("F0");
    }


    /// <summary>
    /// Frissíti a játékos pontjait megjelenítõ szöveges elemet.
    /// A `points` értéket beállítja a "Points" kulcshoz tartozó szöveges elem szövegéhez.
    /// </summary>
    /// <param name="points">A játékos aktuális pontszáma, amelyet meg kell jeleníteni.</param>
    void UpdatePointsUIText(int points)
    {
        // Beállítja a "Points" kulcshoz tartozó szöveges elem szövegét a játékos aktuális pontszámára
        textMeshProElementReferences["Points"].text = points.ToString("F0");
    }


    /// <summary>
    /// Inicializálja a szöveges elemeket a megadott vászon (Canvas) objektumban, 
    /// és hozzárendeli õket a megfelelõ `textMeshProElementReferences` szótárhoz 
    /// a nevük alapján, ha azok "Value"-val végzõdnek.
    /// </summary>
    /// <param name="textMeshProElementReferences">A szótár, amelyhez a szöveges elemeket hozzáadjuk típusuk alapján.</param>
    /// <param name="canvas">A vászon (Canvas) objektum, amelyen belül a szöveges elemeket keresni kell.</param>
    void InitializeCanvasTextElementsDictionary(Dictionary<string, TextMeshProUGUI> textMeshProElementReferences, Canvas canvas)
    {
        // Kiválasztja az összes TextMeshProUGUI komponenst a vászonon, beleértve annak összes gyermekét
        TextMeshProUGUI[] textMeshProElements = canvas.GetComponentsInChildren<TextMeshProUGUI>();

        // Iterálunk a talált elemek között
        foreach (var textMesh in textMeshProElements)
        {
            // Ha az elem neve "Value"-ra végzõdik, hozzáadjuk a szótárhoz
            if (textMesh.gameObject.name.EndsWith("Value"))
            {
                // A típus alapján (a nevébõl) lekérdezzük a kulcsot és hozzárendeljük a szöveges elemet
                textMeshProElementReferences[ExtractValueTypeName(textMesh.gameObject.name)] = textMesh;
            }
        }
    }


    /// <summary>
    /// Kivonja az érték típusának nevét a megadott `valueName` alapján.
    /// A `valueName` értékét a 'V' karakter mentén szétválasztja, és visszaadja az elsõ részt.
    /// </summary>
    /// <param name="valueName">A feldolgozandó érték neve, amely a típus nevét tartalmazza.</param>
    /// <returns>Visszaadja az érték típusának nevét, amely az elsõ rész a 'V' karakter elõtt.</returns>
    string ExtractValueTypeName(string valueName)
    {
        // A 'V' karakter mentén szétválasztjuk a `valueName` értéket
        string[] parts = valueName.Split(new char[] { 'V' });

        // Visszaadjuk a szétválasztott elsõ részt, amely az érték típusának neve
        return parts[0];
    }


    /// <summary>
    /// Inicializálja a játékos statisztikai értékeit a játék állapot kezelõ és a játékos vezérlõ alapján,
    /// és hozzáadja õket a `playerVariableValues` szótárhoz.
    /// </summary>
    /// <param name="player">A játékos vezérlõ objektuma, amely tartalmazza a játékos aktuális statisztikáit.</param>
    /// <param name="gameStateManager">A játék állapot kezelõ objektum, amely tartalmazza a játékos pontjait.</param>
    void InitializePlayerStatNamesDictionary(Dictionary<string, string> playerVariableValues, PlayerController player, GameStateManager gameStateManager)
    {
        // A szótárhoz hozzáadjuk a játékos különbözõ statisztikai értékeit
        playerVariableValues.Add("Points", gameStateManager.PlayerPoints.ToString("F0"));  // Játékos pontjai
        playerVariableValues.Add("Health", player.CurrentHealth.ToString("F0"));  // Játékos aktuális életereje
        playerVariableValues.Add("MovementSpeed", player.CurrentMovementSpeed.ToString("F2"));  // Játékos mozgási sebessége
        playerVariableValues.Add("Damage", (player.CurrentDMG * (1 / player.CurrentAttackCooldown)).ToString("F2"));  // Játékos sebzés (a támadási-visszatöltõdési idõ figyelembevételével)
        playerVariableValues.Add("BaseDMG", player.BaseDMG.ToString("F2"));  // Játékos alap sebzése
        playerVariableValues.Add("AttackCooldown", player.CurrentAttackCooldown.ToString("F2"));  // Játékos támadás-visszatöltõdési ideje
        playerVariableValues.Add("CritChance", player.CurrentCriticalHitChance.ToString("F2"));  // Játékos kritikus találat esélye
        playerVariableValues.Add("PercentageDMG", player.CurrentPercentageBasedDMG.ToString("F2"));  // Játékos százalékos alapú sebzése
    }


    /// <summary>
    /// Beállítja a szöveges elemek értékeit a `textElements` és `variableValues` szótárakban található közös kulcsok alapján.
    /// A közös kulcsokhoz tartozó szöveges elemeket frissíti a szótárban tárolt értékekkel.
    /// </summary>
    void SetCurrentUITextValues(Dictionary<string, TextMeshProUGUI> textMeshProElementReferences, Dictionary<string, string> playerVariableValues)
    {
        // Kiválasztjuk a közös kulcsokat a `textElements` és `variableValues` szótárakból
        var commonKeys = textMeshProElementReferences.Keys.Intersect(playerVariableValues.Keys);

        // Végigiterálunk a közös kulcsokon
        foreach (var key in commonKeys)
        {
            // Megkeressük a szöveges elemet és az értéket a kulcs alapján
            var textElement = textMeshProElementReferences[key];
            var value = playerVariableValues[key];

            // Ha mindkettõ nem null, akkor frissítjük a szöveget a tárolt értékkel
            if (textElement != null && value != null)
            {
                textElement.text = value;
            }
        }
    }


    /// <summary>
    /// Az upgrade shop UI betöltéséért felelõs aszinkron metódus. 
    /// Inicializálja a szükséges UI elemeket, beállítja a gombokat, 
    /// és feltölti a vásárlási lehetõségeket.
    /// </summary>
    /// <param name="shopUpgrades">A listája a játékos által elérhetõ fejlesztéseknek.</param>
    /// <returns>True, ha a UI sikeresen betöltõdött, egyébként false.</returns>
    public async Task<bool> LoadUpgradesShopUIAsync(List<PlayerUpgrade> shopUpgrades)
    {
        // Aszinkron várakozás, hogy biztosítsuk a feladatok folyamatos futását.
        await Task.Yield();

        try
        {
            // UI panelek inicializálása
            InitializePanels();

            // Gombok és eseménykezelõk beállítása
            SetUpShopUIButtons();

            // Upgrade lehetõségek feltöltése a shopban
            PopulateUpgradeOptions(shopUpgrades);

            // Visszatérés, ha minden sikeresen megtörtént
            return true;
        }
        catch (Exception ex)
        {
            // Hibaüzenet naplózása, ha valami hiba történik
            Debug.LogError($"Hiba történt az upgrade shop UI betöltése közben: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Inicializálja és aktiválja a szükséges UI panelek és elemeket az upgrade shophoz.
    /// </summary>
    private void InitializePanels()
    {
        // Az upgrade shop UI-ján belül megtalálja az "UpgradesGridPanel" panelt, és eltárolja a változóban
        upgradesPanel = FindInChildrenIgnoreClone(upgradeShopUICanvas.transform, "UpgradesGridPanel");
        // A játékos UI-ján belül megtalálja a sebzés statok panelt és eltárolja a változóban
        DMGStatsPanel = FindInChildrenIgnoreClone(playerUICanvas.transform, damageStatsUIPrefab.name);
        // A játékos UI-ján belül megtalálja a menü gomb panelt és eltárolja a változóban
        menuButtonPanel = FindInChildrenIgnoreClone(playerUICanvas.transform, menuButtonUIPrefab.name);

        // Panelek aktiválása
        DMGStatsPanel.SetActive(true); // A sebzés statok panel aktiválása
        upgradeShopPanel = FindInChildrenIgnoreClone(upgradeShopUICanvas.transform, upgradeShopUIPrefab.name); // Az upgrade shop panel keresése
        upgradeShopPanel.SetActive(true); // Az upgrade shop panel aktiválása
        menuButtonPanel.SetActive(true); // A menü gomb panel aktiválása
    }

    /// <summary>
    /// Beállítja a gombok eseménykezelõit az upgrade shophoz.
    /// </summary>
    private void SetUpShopUIButtons()
    {
        // Menü gomb eseménykezelõjének beállítása
        GameObject menuButton = FindInChildrenIgnoreClone(menuButtonPanel.transform, "MenuButton");
        menuButton.GetComponent<Button>().onClick.AddListener(() => SetPauseMenuActive());

        // Skip (átugrás) gomb eseménykezelõjének beállítása
        GameObject skipButton = FindInChildrenIgnoreClone(upgradeShopUICanvas.transform, "SkipUpgradeButton");
        skipButton.GetComponent<Button>().onClick.AddListener(() => OnBuyButtonClicked(null));
    }

    /// <summary>
    /// Kezeli a szünet menü aktiválását. Ha a szünet menü objektum nem null, aktiválja azt.
    /// Ha a szünet menü objektum null, figyelmeztetést ír ki a logba.
    /// </summary>
    public void SetPauseMenuActive()
    {
        OnGamePaused?.Invoke(GameState.Paused);
        pauseMenu.SetActive(true);
        // TODO: event a GameStateManagernek a 'Time.timeScale = 0' beállításához

    }

    /// <summary>
    /// A szünet menü folytatása gombra kattintva végrehajtódó művelet
    /// </summary>
    public void ResumeGameButtonClicked()
    {
        // Az aktuális GameObject (szünet menü) elrejtése
        pauseMenu.SetActive(false);

        // Az OnGameResumed esemény meghívása, jelezve, hogy a játék folytatódik
        OnGameResumed?.Invoke(GameState.Playing);
    }

    /// <summary>
    /// A visszatérés a főmenübe gombra kattintva végrehajtódó művelet
    /// </summary>
    public void ExitToMainMenuButtonClicked()
    {
        // Az OnBackToMainMenu esemény meghívása, jelezve, hogy visszatérünk a főmenübe
        OnBackToMainMenu?.Invoke(GameState.MainMenu);
    }


    /// <summary>
    /// Ez a metódus akkor hívódik meg, amikor a vásárlás gombot megnyomják.
    /// Ha érvényes `id` értéket kapunk, akkor megjeleníti a vásárolt elem nevét a logban.
    /// Ha az `id` null, akkor azt jelzi, hogy a vásárlás el lett hagyva.
    /// </summary>
    /// <param name="id">A vásárolt elem azonosítója, amely alapján megtörténik a vásárlás nyilvántartása.</param>
    public void OnBuyButtonClicked(string id)      // PlayerUpgradeManager hívása!
    {
        OnPurchaseOptionChosen?.Invoke(id);

    }


    /// <summary>
    /// Feltölti az upgrade lehetõségeket a shopban a megadott lista alapján.
    /// </summary>
    /// <param name="shopUpgrades">A játékos által választható fejlesztések listája.</param>
    private void PopulateUpgradeOptions(List<PlayerUpgrade> shopUpgrades)
    {
        foreach (var item in shopUpgrades)
        {
            // Vásárlási lehetõség létrehozása és hozzáadása a listához
            purchaseOptions.Add(CreatePurchaseOption(item));

            // Upgrade UI elemek hozzáadása a vásárlási panelhez
            AddUIPrefabToGameObject(upgradesPanel, CreateInitializedUpgradeUIPrefab(item), true);
        }

        // Eseménykezelők hozzáadása minden upgrade gombhoz
        List<Button> buttons = upgradesPanel.GetComponentsInChildren<Button>().ToList(); // Összegyűjti az összes gombot a panelről
        foreach (var button in buttons)
        {
            // Hozzáad egy eseménykezelőt a gombhoz, amely aktiválódik a gomb megnyomásakor
            button.onClick.AddListener(() => OnBuyButtonClicked(button.GetComponentInParent<UpgradeUIController>().ID));

            // Ha a gombhoz tartozó upgrade ára nagyobb, mint a játékos pontjai, akkor a gomb nem lesz interaktív
            if (button.GetComponentInParent<UpgradeUIController>().Price > gameStateManager.PlayerPoints)
            {
                button.interactable = false; // A gomb deaktiválása
            }
        }
    }


    /// <summary>
    /// Létrehozza a vásárlási lehetõséget (`PurchaseOption`) a megadott `PlayerUpgrade` objektumból.
    /// Az új vásárlási lehetõség tartalmazza az upgrade nevét, leírását, szintjeit és egyéb paramétereit.
    /// </summary>
    /// <param name="playerUpgrade">A PlayerUpgrade objektum, amely meghatározza a vásárlási lehetõség paramétereit.</param>
    /// <returns>A létrehozott PurchaseOption objektum, amely tartalmazza az upgrade információit.</returns>
    PurchaseOption CreatePurchaseOption(PlayerUpgrade playerUpgrade)
    {
        // Létrehozzuk a vásárlási lehetõség objektumot
        PurchaseOption purchaseOption = new PurchaseOption();

        // A PlayerUpgrade objektum alapján kitöltjük a vásárlási lehetõség mezõit
        purchaseOption.ID = playerUpgrade.ID; // A vásárlási lehetõség azonosítója
        purchaseOption.Name = playerUpgrade.upgradeName; // Az upgrade neve
        purchaseOption.Icon = playerUpgrade.icon; // Az upgrade ikonja
        purchaseOption.minLevel = playerUpgrade.minUpgradeLevel; // Minimális szint
        purchaseOption.maxLevel = playerUpgrade.maxUpgradeLevel; // Maximális szint
        purchaseOption.currentLevel = playerUpgrade.currentUpgradeLevel; // Jelenlegi szint
        purchaseOption.Description = playerUpgrade.description; // Az upgrade leírása
        purchaseOption.Price = playerUpgrade.GetPrice(); // Az upgrade ára, amely a PlayerUpgrade objektumból származik

        // Visszaadjuk a létrehozott PurchaseOption objektumot
        return purchaseOption;
    }


    /// <summary>
    /// Az adott PlayerUpgrade típusának megfelelõ UI prefab-ot adja vissza. A prefab típusát a playerUpgrade paraméter határozza meg,
    /// és az UI elem szövegeit az adott frissítési lehetõség alapján állítja be.
    /// </summary>
    /// <param name="playerUpgrade">A PlayerUpgrade objektum, amely meghatározza, hogy milyen típusú UI prefab-ot hozunk létre.</param>
    /// <returns>A megfelelõ UI prefab, amely az UpgradeUIController komponenssel és a megfelelõ szövegekkel van konfigurálva.</returns>
    GameObject CreateInitializedUpgradeUIPrefab(PlayerUpgrade playerUpgrade)
    {
        // Ha a playerUpgrade null, akkor null értékkel térünk vissza
        if (playerUpgrade == null)
        {
            return null;
        }

        // A megfelelõ prefab változó deklarálása
        GameObject prefab;

        // Ha a playerUpgrade egy gyógyítás típusú frissítés
        if (playerUpgrade.isHealing)
        {
            prefab = healUpgradeUIPrefab; // Hozzárendeljük a gyógyításhoz tartozó prefabot
            prefab.GetComponent<UpgradeUIController>().SetUpgradeUITextValues(CreatePurchaseOption(playerUpgrade)); // Beállítjuk a szövegeket
            return prefab; // Visszaadjuk a prefab-ot
        }
        // Ha a playerUpgrade egy ideiglenes másolat típusú frissítés
        else if (playerUpgrade.IsTempCopy)
        {
            prefab = nextLevelUpgradeUIPrefab; // Hozzárendeljük a következõ szinthez tartozó prefabot
            prefab.GetComponent<UpgradeUIController>().SetUpgradeUITextValues(CreatePurchaseOption(playerUpgrade)); // Beállítjuk a szövegeket
            return prefab; // Visszaadjuk a prefab-ot
        }
        // Ha sem egyik, akkor normál frissítést hozunk létre
        else
        {
            prefab = normalUpgradeUIPrefab; // Hozzárendeljük a normál frissítést
            prefab.GetComponent<UpgradeUIController>().SetUpgradeUITextValues(CreatePurchaseOption(playerUpgrade)); // Beállítjuk a szövegeket
            return prefab; // Visszaadjuk a prefab-ot
        }
    }


    /// <summary>
    /// Az új játék indítása gombra kattintva végrehajtódó művelet
    /// </summary>
    public void StartNewGameButton()
    {
        // Az OnStartNewGame esemény meghívása, jelezve, hogy új játék kezdődik
        OnStartNewGame?.Invoke(GameState.LoadingNewGame);
    }


    /// <summary>
    /// A mentett játék betöltése gombra kattintva végrehajtódó művelet
    /// </summary>
    public void LoadGameButton()
    {
        // Az OnLoadGame esemény meghívása, jelezve, hogy egy mentett játék betöltése történik
        OnLoadGame?.Invoke(GameState.LoadingSavedGame);
    }

    /// <summary>
    /// Az eredménylista menü megnyitása
    /// </summary>
    public void OpenScoreboardMenu()
    {
        // Az eredménylista panel aktiválása, hogy megjelenjen a képernyőn
        scoreboardPanel.SetActive(true);
    }

    /// <summary>
    /// Az eredménylista menü bezárása
    /// </summary>
    public void CloseScoreboardMenu()
    {
        // Az eredménylista panel deaktiválása, hogy eltűnjön a képernyőről
        scoreboardPanel.SetActive(false);
    }


    /// <summary>
    /// A játék kilépése gombra kattintva végrehajtódó művelet
    /// </summary>
    public void QuitGameButton()
    {
        // Az OnExitGame esemény meghívása, jelezve, hogy a játék bezárása történik
        OnExitGame?.Invoke(GameState.Quitting);
    }


    /// <summary>
    /// A jelenet betöltése után végrehajtandó mûveletek. Beállítja a "Load Game" gomb referenciáját
    /// és frissíti annak állapotát.
    /// </summary>
    /// <param name="scene">A betöltött jelenet információi.</param>
    /// <param name="mode">A betöltés módja (pl. új jelenet betöltése vagy hozzáadás).</param>
    private async void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (IsMainMenuScene())
        {
            await Task.Yield();
            // Beállítjuk a "Load Game" gomb referenciáját
            SetMainMenuButtonReferences();

            SetScoreboardData(await saveLoadManager.LoadScoreboardAsync());

            // Frissítjük a gomb állapotát (aktív vagy inaktív)
            UpdateMainMenuButtons();

        }
    }

    /// <summary>
    /// Beállítja a főmenü gombok hivatkozásait a megfelelő UI elemekhez
    /// </summary>
    void SetMainMenuButtonReferences()
    {
        // Ha a jelenlegi jelenet a főmenü
        if (IsMainMenuScene())
        {
            // A gombok hivatkozásainak beállítása a főmenüben található gombokhoz
            newGameButton = GameObject.Find("NewGameButton").GetComponent<Button>(); // Új játék gomb
            loadGameButton = GameObject.Find("LoadGameButton").GetComponent<Button>(); // Betöltés gomb
            settingsButton = GameObject.Find("SettingsButton").GetComponent<Button>(); // Beállítások gomb
            scoreboardButton = GameObject.Find("ScoreboardButton").GetComponent<Button>(); // Eredménylista gomb
            quitGameButton = GameObject.Find("QuitGameButton").GetComponent<Button>(); // Kilépés gomb
        }
    }

    /// <summary>
    /// Frissíti a főmenü gombokat, hozzáadva az eseménykezelőket
    /// </summary>
    void UpdateMainMenuButtons()
    {
        // A gombokhoz hozzáadjuk a megfelelő eseménykezelőket
        newGameButton.onClick.AddListener(() => StartNewGameButton()); // Új játék gomb eseménykezelője
        loadGameButton.onClick.AddListener(() => LoadGameButton()); // Betöltés gomb eseménykezelője
        UpdateLoadGameButtonAvailability(); // A Betöltés gomb elérhetőségének frissítése

        scoreboardButton.onClick.AddListener(() => OpenScoreboardMenu()); // Eredménylista gomb eseménykezelője
                                                                          // A vissza gomb eseménykezelőjének hozzáadása az eredménylista panelen
        FindInChildrenIgnoreClone(scoreboardPanel.transform, "Back").GetComponent<Button>().onClick.AddListener(() => CloseScoreboardMenu());

        quitGameButton.onClick.AddListener(() => QuitGameButton()); // Kilépés gomb eseménykezelője
    }

    /// <summary>
    /// Beállítja az eredménylista adatokat és hozzáadja a bejegyzéseket a megfelelő panelhez
    /// </summary>
    /// <param name="scoreboardData">Az eredménylista adatait tartalmazó objektum</param>
    void SetScoreboardData(ScoreboardData scoreboardData)
    {
        // Kiválasztja az eredménylista panelt a canvas-on belül
        scoreboardPanel = FindInChildrenIgnoreClone(FindObjectOfType<Canvas>().transform, "ScoreBoard");
        // Kiválasztja az eredménylista tartalmát, ahol az egyes bejegyzések megjelennek
        GameObject scoreboardContentPanel = FindInChildrenIgnoreClone(scoreboardPanel.transform, "Content");

        // Végigiterál a scoreboardData-ban található összes eredmény bejegyzésen
        foreach (var data in scoreboardData.scoreboardEntries)
        {
            // Hozzáadja az egyes eredményeket az eredménylista panelhez
            AddScoreEntryToPanel(scoreboardContentPanel, scoreboardEntryPrefab, data);
        }
    }

    /// <summary>
    /// Hozzáad egy új eredménybejegyzést az eredménylista panelhez.
    /// </summary>
    /// <param name="panel">A szülő panel, amelyhez hozzáadjuk az új bejegyzést</param>
    /// <param name="scoreEntryPrefab">A UI prefab, amelyet példányosítunk az új bejegyzéshez</param>
    /// <param name="entryData">Az egyes eredmény adatokat tartalmazó objektum</param>
    void AddScoreEntryToPanel(GameObject panel, GameObject scoreEntryPrefab, ScoreboardEntry entryData)
    {
        // Ellenõrizzük, hogy a szülõ GameObject nem null
        if (panel == null)
        {
            Debug.LogError($"Parent panel is null. This might happen if the UI hierarchy is not properly initialized. Check the Canvas and Content GameObject.");
            return;
        }

        // Ellenõrizzük, hogy a UI prefab nem null
        if (scoreEntryPrefab == null)
        {
            Debug.LogError("UIPrefab reference is null. Assign a valid UI prefab.");
            return; // Ha a UI prefab null, a metódus befejezõdik
        }

        // A prefab példányosítása a szülõ GameObject-hez adása
        GameObject uiInstance = Instantiate(scoreEntryPrefab, panel.transform);

        // A példányosított UI elemet feltöltjük az eredmény adatával
        PopulateScoreEntry(uiInstance, entryData);

        // Az új UI objektumot a szülõ objektum legutolsó gyermekeként állítjuk be
        uiInstance.transform.SetAsLastSibling();
    }

    /// <summary>
    /// Feltölti az eredmény bejegyzést a megfelelő adatokkal.
    /// </summary>
    /// <param name="uiInstance">A példányosított UI objektum, amelyet frissítünk</param>
    /// <param name="entryData">Az eredmény adatokat tartalmazó objektum</param>
    void PopulateScoreEntry(GameObject uiInstance, ScoreboardEntry entryData)
        {
        // Az adatokat tartalmazó kulcs-érték párok
        Dictionary<string, string> fieldValues = new Dictionary<string, string>()
        {
            { "Name", entryData.playerName },  // Játékos neve
            { "Points", entryData.playerPoints.ToString() },  // Játékos pontszáma
            { "Time", entryData.finalTime },  // Játék végén eltelt idő
            { "Date", entryData.date }  // Eredmény dátuma
        };

        // Végigiterálunk a mezőkön és frissítjük az UI elemeket
        foreach (var field in fieldValues)
        {
            // Megkeressük a megfelelő UI elemet a szülő objektumban
            GameObject fieldObject = FindInChildrenIgnoreClone(uiInstance.transform, field.Key);
            if (fieldObject != null)
            {
                // Ha megtaláltuk, akkor az új értéket hozzárendeljük
                TMP_Text textComponent = fieldObject.GetComponent<TMP_Text>();
                if (textComponent != null)
                {
                    // A szöveg frissítése az adott mező értékével
                    textComponent.text = field.Value;
                }
            }
        }
    }



    /// <summary>
    /// Ellenõrzi, hogy az aktuális jelenet a "MainMenu" jelenet-e.
    /// </summary>
    /// <returns>
    /// Visszaadja true-t, ha az aktuális jelenet neve "MainMenu", különben false-t.
    /// </returns>
    private bool IsMainMenuScene()
    {
        // Lekérdezzük az aktuálisan betöltött jelenetet, és összehasonlítjuk annak nevét a "MainMenu"-val
        return SceneManager.GetActiveScene().name == "MainMenu";
    }


    /// <summary>
    /// Frissíti a "Load Game" gomb állapotát attól függõen, hogy létezik-e mentett játékfájl.
    /// Ha létezik mentett fájl, a gomb aktívvá válik, különben inaktív.
    /// </summary>
    public void UpdateLoadGameButtonAvailability()
    {
        // Ellenõrizzük, hogy a loadGameButton referencia nem null
        if (loadGameButton != null)
        {
            // A gomb interakciós állapotát beállítjuk a SaveLoadManager SaveFileExists metódusa alapján
            loadGameButton.interactable = saveLoadManager.SaveFileExists();
        }
    }

    /// <summary>
    /// A győzelem panel megjelenítése aszinkron módon.
    /// </summary>
    /// <returns>Visszaadja, hogy a művelet sikeresen befejeződött-e</returns>
    public async Task<bool> DisplayVictoryPanelAsync()
    {
        // Aszinkron művelet elindítása
        await Task.Yield();

        try
        {
            // A szünetmenü kikapcsolása
            isPauseMenuEnabled = false;
            // Új canvas létrehozása a győzelem panelhez
            Canvas victoryCanvas = CreateCanvas("Canvas", 10);
            // Ellenőrizzük, hogy az Event System létezik
            EnsureEventSystemExists();
            // A győzelem panel hozzáadása az új canvas-ra
            AddUIPrefabToGameObject(victoryCanvas.gameObject, victoryPanelPrefab, true);
            // A vissza gomb hozzáadása a győzelem panelhez
            GameObject menuButton = FindInChildrenIgnoreClone(victoryCanvas.transform, "Back");
            TMP_InputField inputField = FindInChildrenIgnoreClone(victoryCanvas.transform, "NameInputField").GetComponent<TMP_InputField>();
            // A vissza gomb eseménykezelőjének hozzáadása
            menuButton.GetComponent<Button>().onClick.AddListener(() => CloseVictoryPanel(inputField.text));

            // Ha minden rendben, visszaadjuk, hogy sikeres volt a művelet
            return true;
        }
        catch (Exception ex)
        {
            // Ha hiba történt, kiírjuk a hibát
            Debug.LogError($"{ex.Message}");
            return false; // Hibás végrehajtás esetén false-t adunk vissza
        }
    }

    /// <summary>
    /// A vereség panel megjelenítése aszinkron módon.
    /// </summary>
    /// <returns>Visszaadja, hogy a művelet sikeresen befejeződött-e</returns>
    public async Task<bool> DisplayDefeatPanelAsync()
    {
        // Aszinkron művelet elindítása
        await Task.Yield();

        try
        {
            // A szünetmenü kikapcsolása
            isPauseMenuEnabled = false;
            // Új canvas létrehozása a vereség panelhez
            Canvas defeatCanvas = CreateCanvas("Canvas", 10);
            // Ellenőrizzük, hogy az Event System létezik
            EnsureEventSystemExists();
            // A vereség panel hozzáadása az új canvas-ra
            AddUIPrefabToGameObject(defeatCanvas.gameObject, defeatPanelPrefab, true);
            // A vissza gomb hozzáadása a vereség panelhez
            GameObject menuButton = FindInChildrenIgnoreClone(defeatCanvas.transform, "Back");
            // A vissza gomb eseménykezelőjének hozzáadása
            menuButton.GetComponent<Button>().onClick.AddListener(() => ExitToMainMenuButtonClicked());

            // Ha minden rendben, visszaadjuk, hogy sikeres volt a művelet
            return true;
        }
        catch (Exception ex)
        {
            // Ha hiba történt, kiírjuk a hibát
            Debug.LogError($"{ex.Message}");
            return false; // Hibás végrehajtás esetén false-t adunk vissza
        }
        
    }

    /// <summary>
    /// A győzelem panel bezárása, miután a játékos beírta a nevét.
    /// </summary>
    /// <param name="input">A játékos által beírt név</param>
    public async void CloseVictoryPanel(string input)
    {
        // Kiírja a konzolra a játékos által beírt nevet
        Debug.Log(input);

        // Beállítja a játékos nevét a jelenlegi futás adataihoz
        gameStateManager.CurrentRunPlayerName = input;

        // Aszinkron módon frissíti az eredménylistát
        bool asyncOperation = await saveLoadManager.UpdateScoreboardDataAsync();

        // Visszatérünk a főmenübe
        OnBackToMainMenu?.Invoke(GameState.MainMenu);
    }


    /// <summary>
    /// A szkript aktiválásakor regisztrálja az eseményt, hogy a jelenet betöltésekor meghívódjon az OnSceneLoaded metódus.
    /// </summary>
    private void OnEnable()
    {
        // Hozzáadja az OnSceneLoaded eseménykezelőt, hogy a jelenet betöltésekor meghívódjon
        SceneManager.sceneLoaded += OnSceneLoaded;
    }


    /// <summary>
    /// A szkript deaktiválásakor eltávolítja az eseménykezelőt a jelenet betöltésekor.
    /// </summary>
    private void OnDisable()
    {
        // Eltávolítja az OnSceneLoaded eseménykezelőt, hogy ne hívódjon meg a jelenet betöltésekor
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }


    /// <summary>
    /// A szkript megsemmisítésekor eltávolítja az eseménykezelőt a pontok változásának figyelésére.
    /// </summary>
    private void OnDestroy()
    {
        // Ha a gameStateManager nem null, eltávolítja az eseménykezelőt, amely figyeli a pontok változását
        if (gameStateManager != null)
        {
            gameStateManager.OnPointsChanged -= UpdatePointsUIText;
        }
    }
}
