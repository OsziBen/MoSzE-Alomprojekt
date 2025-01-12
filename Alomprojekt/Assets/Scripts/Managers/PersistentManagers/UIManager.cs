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
    [System.Serializable]
    public class PurchaseOption
    {
        public string ID;
        public string Name;
        public int minLevel;
        public int maxLevel;
        public int currentLevel;
        public Sprite Icon;
        public string Description;
        public int Price;
    }


    /// <summary>
    /// Változók
    /// </summary>
    GameObject DMGStatsPanel;
    GameObject upgradeShopPanel;
    GameObject menuButtonPanel;
    Canvas pauseMenuCanvas;
    Canvas playerUICanvas;
    Canvas upgradeShopUICanvas;

    public List<PurchaseOption> purchaseOptions;

    private Dictionary<string, TextMeshProUGUI> textMeshProElementReferences = new Dictionary<string, TextMeshProUGUI>();

    private Dictionary<string, string> playerVariableValues = new Dictionary<string, string>();


    /// <summary>
    /// Komponensek
    /// </summary>
    SaveLoadManager saveLoadManager;
    PlayerController player;
    GameStateManager gameStateManager;


    [Header("Main Menu")]
    [SerializeField]
    private Button newGameButton;
    [SerializeField]
    private Button loadGameButton;
    [SerializeField]
    private Button settingsButton;
    [SerializeField]
    private Button scoreboardButton;
    [SerializeField]
    private Button quitGameButton;


    [Header("UI Panels")]
    public GameObject menuButtonUIPrefab;
    public GameObject pauseMenuUIPrefab;
    public GameObject playerStatsUIPrefab;
    public GameObject damageStatsUIPrefab;
    public GameObject upgradeShopUIPrefab;
    public GameObject pauseMenu;


    [Header("Shop UI")]
    [SerializeField]
    private GameObject upgradesPanel;
    [SerializeField]
    private GameObject normalUpgradeUIPrefab;
    [SerializeField]
    private GameObject nextLevelUpgradeUIPrefab;
    [SerializeField]
    private GameObject healUpgradeUIPrefab;


    /// <summary>
    /// Események
    /// </summary>
    public event Action<GameState> OnStartNewGame;
    public event Action<GameState> OnLoadGame;
    public event Action<GameState> OnExitGame;
    public event Action<GameState> OnGamePaused;
    public event Action<GameState> OnGameResumed;
    public event Action<GameState> OnBackToMainMenu;
    public event Action<string> OnPurchaseOptionChosen;


    /// <summary>
    /// 
    /// </summary>
    protected override async void Initialize()
    {
        base.Initialize();
        saveLoadManager = FindObjectOfType<SaveLoadManager>();
        gameStateManager = FindObjectOfType<GameStateManager>();

        gameStateManager.OnPointsChanged += UpdatePointsUIText;
    }


    /// <summary>
    /// 
    /// </summary>
    void Start()
    {
        //SetMainMenuButtonReferences();
        //UpdateMainMenuButtons();

    }




    /// <summary>
    /// Az Update metódus minden egyes frame-ben meghívódik, és folyamatosan ellenõrzi, hogy
    /// lenyomták-e az Escape billentyût, hogy aktiválja a szünet menüt.
    /// </summary>
    private void Update()
    {
        // Ellenõrizzük, hogy lenyomták-e az Escape billentyût
        if (pauseMenu != null && UnityEngine.Input.GetKeyDown(KeyCode.Escape))
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

        // Eltávolítja az 'OnPointsChanged' eseményfigyelõt, amely a pontok változására reagál
        gameStateManager.OnPointsChanged -= UpdatePointsUIText;
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
        upgradesPanel = FindInChildrenIgnoreClone(upgradeShopUICanvas.transform, "UpgradesGridPanel");
        DMGStatsPanel = FindInChildrenIgnoreClone(playerUICanvas.transform, damageStatsUIPrefab.name);
        menuButtonPanel = FindInChildrenIgnoreClone(playerUICanvas.transform, menuButtonUIPrefab.name);

        // Panelek aktiválása
        DMGStatsPanel.SetActive(true);
        upgradeShopPanel = FindInChildrenIgnoreClone(upgradeShopUICanvas.transform, upgradeShopUIPrefab.name);
        upgradeShopPanel.SetActive(true);
        menuButtonPanel.SetActive(true);
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


    public void ResumeGameButtonClicked()
    {
        // Az aktuális GameObject elrejtése
        pauseMenu.SetActive(false);

        OnGameResumed?.Invoke(GameState.Playing);
    }


    public void ExitToMainMenuButtonClicked()
    {
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

        // Eseménykezelõk hozzáadása minden upgrade gombhoz
        List<Button> buttons = upgradesPanel.GetComponentsInChildren<Button>().ToList();
        foreach (var button in buttons)
        {
            button.onClick.AddListener(() => OnBuyButtonClicked(button.GetComponentInParent<UpgradeUIController>().ID));
            if (button.GetComponentInParent<UpgradeUIController>().Price > gameStateManager.PlayerPoints)
            {
                button.interactable = false;
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
    /// 
    /// </summary>
    public void StartNewGameButton()
    {
        OnStartNewGame?.Invoke(GameState.LoadingNewGame);
    }


    /// <summary>
    /// 
    /// </summary>
    public void LoadGameButton()
    {
        OnLoadGame?.Invoke(GameState.LoadingSavedGame);
    }


    /// <summary>
    /// 
    /// </summary>
    public void QuitGameButton()
    {
        OnExitGame?.Invoke(GameState.Quitting);
    }


    /// <summary>
    /// A jelenet betöltése után végrehajtandó mûveletek. Beállítja a "Load Game" gomb referenciáját
    /// és frissíti annak állapotát.
    /// </summary>
    /// <param name="scene">A betöltött jelenet információi.</param>
    /// <param name="mode">A betöltés módja (pl. új jelenet betöltése vagy hozzáadás).</param>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (IsMainMenuScene())
        {
            // Beállítjuk a "Load Game" gomb referenciáját
            SetMainMenuButtonReferences();

            // Frissítjük a gomb állapotát (aktív vagy inaktív)
            UpdateMainMenuButtons();

        }
    }


    void SetMainMenuButtonReferences()
    {
        if (IsMainMenuScene())
        {
            newGameButton = GameObject.Find("NewGameButton").GetComponent<Button>();
            loadGameButton = GameObject.Find("LoadGameButton").GetComponent<Button>();
            settingsButton = GameObject.Find("SettingsButton").GetComponent<Button>();
            scoreboardButton = GameObject.Find("ScoreboardButton").GetComponent<Button>();
            quitGameButton = GameObject.Find("QuitGameButton").GetComponent<Button>();
        }
    }

    void UpdateMainMenuButtons()
    {
        newGameButton.onClick.AddListener(() => StartNewGameButton());
        loadGameButton.onClick.AddListener(() => LoadGameButton());
        UpdateLoadGameButtonAvailability();
        // többi gomb is...
        quitGameButton.onClick.AddListener(() => QuitGameButton());
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
    /// 
    /// </summary>
    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }


    /// <summary>
    /// 
    /// </summary>
    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }


    /// <summary>
    /// 
    /// </summary>
    private void OnDestroy()
    {
        if (gameStateManager != null)
        {
            gameStateManager.OnPointsChanged -= UpdatePointsUIText;
        }
    }

}
