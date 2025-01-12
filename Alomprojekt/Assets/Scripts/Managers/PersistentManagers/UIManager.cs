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
    /// V�ltoz�k
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
    /// Esem�nyek
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
    /// Az Update met�dus minden egyes frame-ben megh�v�dik, �s folyamatosan ellen�rzi, hogy
    /// lenyomt�k-e az Escape billenty�t, hogy aktiv�lja a sz�net men�t.
    /// </summary>
    private void Update()
    {
        // Ellen�rizz�k, hogy lenyomt�k-e az Escape billenty�t
        if (pauseMenu != null && UnityEngine.Input.GetKeyDown(KeyCode.Escape))
        {
            // Ha az Escape billenty�t lenyomt�k, aktiv�ljuk a sz�net men�t
            SetPauseMenuActive();
        }
    }


    /// <summary>
    /// Aszinkron m�don bet�lti a j�t�k UI elemeit, p�ld�ul a sz�net men�t, a j�t�kos statisztikai panelj�t �s az upgrade shopot.
    /// A funkci� biztos�tja, hogy a sz�ks�ges elemek �s esem�nyek inicializ�l�sa megt�rt�njen a UI bet�lt�se el�tt.
    /// Hib�k eset�n false �rt�ket ad vissza, siker eset�n true-t.
    /// </summary>
    /// <returns>Visszaadja a UI bet�lt�s�nek sikeress�g�t: true ha siker�lt, false ha hiba t�rt�nt.</returns>
    public async Task<bool> LoadPlayerUIAsync()
    {
        // V�rakoz�st adunk, hogy az aszinkron folyamat ne blokkolja a f� sz�lat
        await Task.Yield();

        try
        {
            // A sz�net men� l�trehoz�sa �s felt�lt�se
            CreateAndPopulatePauseMenuCanvas();

            // A j�t�kos statisztikai panelj�nek l�trehoz�sa �s felt�lt�se
            CreateAndPopulatePlayerStatsCanvas();

            // Az upgrade shop l�trehoz�sa �s felt�lt�se
            CreateAndPopulateUpgradeShopCanvas();

            // Esem�ny rendszer hozz�ad�sa, ha m�g nem l�tezne
            EnsureEventSystemExists();

            // Megkeress�k a sz�net men� GameObject-et a hozz� tartoz� prefab alapj�n
            pauseMenu = FindInChildrenIgnoreClone(pauseMenuCanvas.transform, pauseMenuUIPrefab.name);

            // Megkeress�k a j�t�kost
            player = FindObjectOfType<PlayerController>();
            player.OnPlayerDeath += StopPlayerHealthDetection;
            player.OnHealthChanged += UpdateHealthUIText;


            // Inicializ�ljuk a TextMeshPro refernecia-elemek sz�t�r�t
            textMeshProElementReferences.Clear();
            InitializeCanvasTextElementsDictionary(textMeshProElementReferences, playerUICanvas);

            // Inicializ�ljuk a j�t�kos statisztik�it tartalmaz� sz�t�rat
            playerVariableValues.Clear();
            InitializePlayerStatNamesDictionary(playerVariableValues, player, gameStateManager);


            // A UI sz�vegek be�ll�t�sa a megfelel� �rt�kekkel
            SetCurrentUITextValues(textMeshProElementReferences, playerVariableValues);


            return true; // A UI sikeresen bet�lt�d�tt
        }
        catch (Exception ex)
        {
            // Ha hiba t�rt�nik, napl�zzuk a hiba�zenetet
            Debug.LogError($"Hiba t�rt�nt a j�t�k UI bet�lt�se k�zben: {ex.Message}");
            return false; // Visszaadjuk a hib�t jelz� �rt�ket
        }
    }


    /// <summary>
    /// L�trehozza �s felt�lti a sz�net men� canvas-t.
    /// A sz�net men� prefabj�t hozz�adja a canvas-hoz, �s kezdetben nem akt�vra �ll�tja.
    /// </summary>
    private void CreateAndPopulatePauseMenuCanvas()
    {
        // L�trehozza a sz�net men� canvas-t
        pauseMenuCanvas = CreateCanvas("PauseMenuCanvas", 100);

        // Hozz�adja a sz�net men� prefabj�t a canvas-hoz, kezdetben nem akt�v
        AddUIPrefabToGameObject(pauseMenuCanvas.gameObject, pauseMenuUIPrefab, false);
    }


    /// <summary>
    /// L�trehozza �s felt�lti a j�t�kos statisztikai panelj�t tartalmaz� canvas-t.
    /// A j�t�kos statisztik�kat, a damage statisztik�t �s a men� gombot hozz�adja a canvas-hoz.
    /// </summary>
    private void CreateAndPopulatePlayerStatsCanvas()
    {
        // L�trehozza a j�t�kos statisztikai panel canvas-t
        playerUICanvas = CreateCanvas("GameUICanvas", 10);

        // Hozz�adja a j�t�kos statisztik�kat tartalmaz� prefabot
        AddUIPrefabToGameObject(playerUICanvas.gameObject, playerStatsUIPrefab, true);

        // Hozz�adja a damage statisztik�t tartalmaz� prefabot, kezdetben nem akt�v
        AddUIPrefabToGameObject(playerUICanvas.gameObject, damageStatsUIPrefab, false);

        // Hozz�adja a men� gombot tartalmaz� prefabot, kezdetben nem akt�v
        AddUIPrefabToGameObject(playerUICanvas.gameObject, menuButtonUIPrefab, false);
    }


    /// <summary>
    /// L�trehozza �s felt�lti az upgrade shop canvas-t.
    /// Az upgrade shop prefabot hozz�adja a canvas-hoz, kezdetben nem akt�vra �ll�tva.
    /// </summary>
    private void CreateAndPopulateUpgradeShopCanvas()
    {
        // L�trehozza az upgrade shop canvas-t
        upgradeShopUICanvas = CreateCanvas("UpgradeShopUICanvas", 5);

        // Hozz�adja az upgrade shop prefabj�t a canvas-hoz, kezdetben nem akt�v
        AddUIPrefabToGameObject(upgradeShopUICanvas.gameObject, upgradeShopUIPrefab, false);
    }


    /// <summary>
    /// Canvas-t hoz l�tre vagy keres egy m�r l�tez�t, amely megfelel a megadott n�vnek.
    /// Ha nem tal�lhat�, �j canvas-t hoz l�tre a megadott n�vvel �s rendez�si sorrenddel.
    /// </summary>
    /// <param name="canvasName">A keresett vagy l�trehozand� canvas neve.</param>
    /// <param name="sortingOrder">A canvas rendez�si sorrendje, amely meghat�rozza a rajta l�v� elemek z-index�t.</param>
    /// <returns>Visszaadja a megtal�lt vagy l�trehozott Canvas objektumot.</returns>
    Canvas CreateCanvas(string canvasName, int sortingOrder)
    {
        // Keres�nk egy l�tez� canvas-t a megadott n�vvel
        Canvas existingCanvas = FindObjectsOfType<Canvas>()
            .FirstOrDefault(canvas => canvas.gameObject.name == canvasName);

        if (existingCanvas != null)
        {
            // Ha megtal�ltuk, log-oljuk �s visszaadjuk a l�tez� canvas-t
            Debug.Log($"Found existing canvas: {existingCanvas.name}");
            return existingCanvas; // Visszaadjuk a megtal�lt canvas-t
        }

        // Ha nem tal�ltunk, �j canvas-t hozunk l�tre
        GameObject gameUICanvas = new GameObject(canvasName); // �j GameObject l�trehoz�sa a megadott n�vvel
        Canvas gameCanvas = gameUICanvas.AddComponent<Canvas>(); // Canvas komponens hozz�ad�sa
        gameCanvas.renderMode = RenderMode.ScreenSpaceOverlay; // Be�ll�tjuk a render m�dot (k�perny�n jelenjen meg)
        gameCanvas.sortingOrder = sortingOrder; // Be�ll�tjuk a rendez�si sorrendet

        // Hozz�adunk fontos komponenseket: CanvasScaler �s GraphicRaycaster
        gameUICanvas.AddComponent<CanvasScaler>();
        gameUICanvas.AddComponent<GraphicRaycaster>();

        // Log-oljuk az �j canvas l�trej�tt�t
        Debug.Log($"Created new canvas: {gameUICanvas.name}");
        return gameCanvas; // Visszaadjuk az �j canvas-t
    }


    /// <summary>
    /// Egy UI prefab-t hozz�ad a megadott sz�l� GameObject-hez. Az �j UI objektumot a megadott sz�l�h�z rendeli,
    /// �s be�ll�tja annak aktivit�s�t.
    /// </summary>
    /// <param name="parent">A sz�l� GameObject, amelyhez a UI prefab-t hozz�adjuk.</param>
    /// <param name="UIPrefab">A hozz�adand� UI prefab.</param>
    /// <param name="isActive">Be�ll�tja, hogy az �j UI objektum akt�v vagy inakt�v legyen.</param>
    void AddUIPrefabToGameObject(GameObject parent, GameObject UIPrefab, bool isActive)
    {
        // Ellen�rizz�k, hogy a sz�l� GameObject nem null
        if (parent == null)
        {
            Debug.LogError("Parent reference is null. Ensure the Canvas exists before adding UI elements.");
            return; // Ha a sz�l� null, a met�dus befejez�dik
        }

        // Ellen�rizz�k, hogy a UI prefab nem null
        if (UIPrefab == null)
        {
            Debug.LogError("UIPrefab reference is null. Assign a valid UI prefab.");
            return; // Ha a UI prefab null, a met�dus befejez�dik
        }

        // A prefab p�ld�nyos�t�sa a sz�l� GameObject-hez ad�sa
        GameObject uiInstance = Instantiate(UIPrefab, parent.transform);

        // Az �j UI objektumot a sz�l� objektum legutols� gyermekek�nt �ll�tjuk be
        uiInstance.transform.SetAsLastSibling();

        // Be�ll�tjuk az UI objektum aktivit�s�t
        uiInstance.SetActive(isActive);
    }


    /// <summary>
    /// Ellen�rzi, hogy l�tezik-e m�r EventSystem az adott jelenetben. Ha nem tal�lhat�, l�trehoz egy �j EventSystem objektumot,
    /// �s hozz�adja az InputSystemUIInputModule-ot az input esem�nyek kezel�s�hez.
    /// </summary>
    void EnsureEventSystemExists()
    {
        // Ellen�rizz�k, hogy l�tezik-e m�r EventSystem a jelenetben
        if (FindObjectOfType<EventSystem>() == null)
        {
            // Ha nincs, l�trehozunk egy �j EventSystem objektumot
            GameObject eventSystemObj = new GameObject("EventSystem");
            EventSystem eventSystem = eventSystemObj.AddComponent<EventSystem>(); // Hozz�adjuk az EventSystem komponenst

            // Hozz�adjuk az InputSystemUIInputModule-ot, amely kezeli az input esem�nyeket
            eventSystemObj.AddComponent<InputSystemUIInputModule>();
        }
    }


    /// <summary>
    /// Rekurz�v m�don keres egy gyermeket a hierarchi�ban, amelynek neve az adott `baseName`-el kezd�dik.
    /// A kl�nokat figyelmen k�v�l hagyja (a `Clone` el�tagot tartalmaz� objektumokat nem tal�lja meg).
    /// </summary>
    /// <param name="parent">Az a sz�l�objektum, amelyben keres�nk.</param>
    /// <param name="baseName">Az alap n�v, amellyel a gyermek neve kezd�dik.</param>
    /// <returns>A keresett objektum, ha megtal�lhat�, egy�bk�nt null.</returns>
    private GameObject FindInChildrenIgnoreClone(Transform parent, string baseName)
    {
        // V�gigiter�lunk a sz�l� �sszes gyermek�n
        foreach (Transform child in parent)
        {
            // Ha a gyermek neve a baseName-el kezd�dik, akkor visszaadjuk a gyermeket
            if (child.name.StartsWith(baseName))
            {
                return child.gameObject;
            }

            // Rekurz�v keres�s a gyermekek alatt is
            GameObject result = FindInChildrenIgnoreClone(child, baseName);
            if (result != null)
            {
                return result;
            }
        }

        // Ha nem tal�lunk semmit, akkor null-t adunk vissza
        return null;
    }


    /// <summary>
    /// Le�ll�tja a j�t�kos �leter�-figyel�s�t, �s elt�vol�tja az �sszes esem�nyfigyel�t.
    /// A j�t�kos �leter� v�ltoz�s�ra vonatkoz� esem�nyeket �s a j�t�kos hal�l�t figyel� esem�nyeket t�rli,
    /// valamint a pontok v�ltoz�s�ra vonatkoz� esem�nyt is elt�vol�tja.
    /// </summary>
    void StopPlayerHealthDetection()
    {
        // Elt�vol�tja az 'OnHealthChanged' esem�nyfigyel�t, amely az �leter� v�ltoz�sra reag�l
        player.OnHealthChanged -= UpdateHealthUIText;

        // Elt�vol�tja az 'OnPlayerDeath' esem�nyfigyel�t, amely a j�t�kos hal�l�ra reag�l
        player.OnPlayerDeath -= StopPlayerHealthDetection;

        // Elt�vol�tja az 'OnPointsChanged' esem�nyfigyel�t, amely a pontok v�ltoz�s�ra reag�l
        gameStateManager.OnPointsChanged -= UpdatePointsUIText;
    }


    /// <summary>
    /// Friss�ti a j�t�kos �leterej�t megjelen�t� sz�veges elemet.
    /// A `currentPlayerHP` �rt�ket be�ll�tja a "Health" kulcshoz tartoz� sz�veges elem sz�veg�hez.
    /// </summary>
    /// <param name="currentPlayerHP">A j�t�kos aktu�lis �letereje, amelyet meg kell jelen�teni.</param>
    public void UpdateHealthUIText(float currentPlayerHP)
    {
        // Be�ll�tja a "Health" kulcshoz tartoz� sz�veges elem sz�veg�t a j�t�kos aktu�lis �leterej�re
        textMeshProElementReferences["Health"].text = currentPlayerHP.ToString("F0");
    }


    /// <summary>
    /// Friss�ti a j�t�kos pontjait megjelen�t� sz�veges elemet.
    /// A `points` �rt�ket be�ll�tja a "Points" kulcshoz tartoz� sz�veges elem sz�veg�hez.
    /// </summary>
    /// <param name="points">A j�t�kos aktu�lis pontsz�ma, amelyet meg kell jelen�teni.</param>
    void UpdatePointsUIText(int points)
    {
        // Be�ll�tja a "Points" kulcshoz tartoz� sz�veges elem sz�veg�t a j�t�kos aktu�lis pontsz�m�ra
        textMeshProElementReferences["Points"].text = points.ToString("F0");
    }


    /// <summary>
    /// Inicializ�lja a sz�veges elemeket a megadott v�szon (Canvas) objektumban, 
    /// �s hozz�rendeli �ket a megfelel� `textMeshProElementReferences` sz�t�rhoz 
    /// a nev�k alapj�n, ha azok "Value"-val v�gz�dnek.
    /// </summary>
    /// <param name="textMeshProElementReferences">A sz�t�r, amelyhez a sz�veges elemeket hozz�adjuk t�pusuk alapj�n.</param>
    /// <param name="canvas">A v�szon (Canvas) objektum, amelyen bel�l a sz�veges elemeket keresni kell.</param>
    void InitializeCanvasTextElementsDictionary(Dictionary<string, TextMeshProUGUI> textMeshProElementReferences, Canvas canvas)
    {
        // Kiv�lasztja az �sszes TextMeshProUGUI komponenst a v�szonon, bele�rtve annak �sszes gyermek�t
        TextMeshProUGUI[] textMeshProElements = canvas.GetComponentsInChildren<TextMeshProUGUI>();

        // Iter�lunk a tal�lt elemek k�z�tt
        foreach (var textMesh in textMeshProElements)
        {
            // Ha az elem neve "Value"-ra v�gz�dik, hozz�adjuk a sz�t�rhoz
            if (textMesh.gameObject.name.EndsWith("Value"))
            {
                // A t�pus alapj�n (a nev�b�l) lek�rdezz�k a kulcsot �s hozz�rendelj�k a sz�veges elemet
                textMeshProElementReferences[ExtractValueTypeName(textMesh.gameObject.name)] = textMesh;
            }
        }
    }


    /// <summary>
    /// Kivonja az �rt�k t�pus�nak nev�t a megadott `valueName` alapj�n.
    /// A `valueName` �rt�k�t a 'V' karakter ment�n sz�tv�lasztja, �s visszaadja az els� r�szt.
    /// </summary>
    /// <param name="valueName">A feldolgozand� �rt�k neve, amely a t�pus nev�t tartalmazza.</param>
    /// <returns>Visszaadja az �rt�k t�pus�nak nev�t, amely az els� r�sz a 'V' karakter el�tt.</returns>
    string ExtractValueTypeName(string valueName)
    {
        // A 'V' karakter ment�n sz�tv�lasztjuk a `valueName` �rt�ket
        string[] parts = valueName.Split(new char[] { 'V' });

        // Visszaadjuk a sz�tv�lasztott els� r�szt, amely az �rt�k t�pus�nak neve
        return parts[0];
    }


    /// <summary>
    /// Inicializ�lja a j�t�kos statisztikai �rt�keit a j�t�k �llapot kezel� �s a j�t�kos vez�rl� alapj�n,
    /// �s hozz�adja �ket a `playerVariableValues` sz�t�rhoz.
    /// </summary>
    /// <param name="player">A j�t�kos vez�rl� objektuma, amely tartalmazza a j�t�kos aktu�lis statisztik�it.</param>
    /// <param name="gameStateManager">A j�t�k �llapot kezel� objektum, amely tartalmazza a j�t�kos pontjait.</param>
    void InitializePlayerStatNamesDictionary(Dictionary<string, string> playerVariableValues, PlayerController player, GameStateManager gameStateManager)
    {
        // A sz�t�rhoz hozz�adjuk a j�t�kos k�l�nb�z� statisztikai �rt�keit
        playerVariableValues.Add("Points", gameStateManager.PlayerPoints.ToString("F0"));  // J�t�kos pontjai
        playerVariableValues.Add("Health", player.CurrentHealth.ToString("F0"));  // J�t�kos aktu�lis �letereje
        playerVariableValues.Add("MovementSpeed", player.CurrentMovementSpeed.ToString("F2"));  // J�t�kos mozg�si sebess�ge
        playerVariableValues.Add("Damage", (player.CurrentDMG * (1 / player.CurrentAttackCooldown)).ToString("F2"));  // J�t�kos sebz�s (a t�mad�si-visszat�lt�d�si id� figyelembev�tel�vel)
        playerVariableValues.Add("BaseDMG", player.BaseDMG.ToString("F2"));  // J�t�kos alap sebz�se
        playerVariableValues.Add("AttackCooldown", player.CurrentAttackCooldown.ToString("F2"));  // J�t�kos t�mad�s-visszat�lt�d�si ideje
        playerVariableValues.Add("CritChance", player.CurrentCriticalHitChance.ToString("F2"));  // J�t�kos kritikus tal�lat es�lye
        playerVariableValues.Add("PercentageDMG", player.CurrentPercentageBasedDMG.ToString("F2"));  // J�t�kos sz�zal�kos alap� sebz�se
    }


    /// <summary>
    /// Be�ll�tja a sz�veges elemek �rt�keit a `textElements` �s `variableValues` sz�t�rakban tal�lhat� k�z�s kulcsok alapj�n.
    /// A k�z�s kulcsokhoz tartoz� sz�veges elemeket friss�ti a sz�t�rban t�rolt �rt�kekkel.
    /// </summary>
    void SetCurrentUITextValues(Dictionary<string, TextMeshProUGUI> textMeshProElementReferences, Dictionary<string, string> playerVariableValues)
    {
        // Kiv�lasztjuk a k�z�s kulcsokat a `textElements` �s `variableValues` sz�t�rakb�l
        var commonKeys = textMeshProElementReferences.Keys.Intersect(playerVariableValues.Keys);

        // V�gigiter�lunk a k�z�s kulcsokon
        foreach (var key in commonKeys)
        {
            // Megkeress�k a sz�veges elemet �s az �rt�ket a kulcs alapj�n
            var textElement = textMeshProElementReferences[key];
            var value = playerVariableValues[key];

            // Ha mindkett� nem null, akkor friss�tj�k a sz�veget a t�rolt �rt�kkel
            if (textElement != null && value != null)
            {
                textElement.text = value;
            }
        }
    }


    /// <summary>
    /// Az upgrade shop UI bet�lt�s��rt felel�s aszinkron met�dus. 
    /// Inicializ�lja a sz�ks�ges UI elemeket, be�ll�tja a gombokat, 
    /// �s felt�lti a v�s�rl�si lehet�s�geket.
    /// </summary>
    /// <param name="shopUpgrades">A list�ja a j�t�kos �ltal el�rhet� fejleszt�seknek.</param>
    /// <returns>True, ha a UI sikeresen bet�lt�d�tt, egy�bk�nt false.</returns>
    public async Task<bool> LoadUpgradesShopUIAsync(List<PlayerUpgrade> shopUpgrades)
    {
        // Aszinkron v�rakoz�s, hogy biztos�tsuk a feladatok folyamatos fut�s�t.
        await Task.Yield();

        try
        {
            // UI panelek inicializ�l�sa
            InitializePanels();

            // Gombok �s esem�nykezel�k be�ll�t�sa
            SetUpShopUIButtons();

            // Upgrade lehet�s�gek felt�lt�se a shopban
            PopulateUpgradeOptions(shopUpgrades);

            // Visszat�r�s, ha minden sikeresen megt�rt�nt
            return true;
        }
        catch (Exception ex)
        {
            // Hiba�zenet napl�z�sa, ha valami hiba t�rt�nik
            Debug.LogError($"Hiba t�rt�nt az upgrade shop UI bet�lt�se k�zben: {ex.Message}");
            return false;
        }
    }


    /// <summary>
    /// Inicializ�lja �s aktiv�lja a sz�ks�ges UI panelek �s elemeket az upgrade shophoz.
    /// </summary>
    private void InitializePanels()
    {
        upgradesPanel = FindInChildrenIgnoreClone(upgradeShopUICanvas.transform, "UpgradesGridPanel");
        DMGStatsPanel = FindInChildrenIgnoreClone(playerUICanvas.transform, damageStatsUIPrefab.name);
        menuButtonPanel = FindInChildrenIgnoreClone(playerUICanvas.transform, menuButtonUIPrefab.name);

        // Panelek aktiv�l�sa
        DMGStatsPanel.SetActive(true);
        upgradeShopPanel = FindInChildrenIgnoreClone(upgradeShopUICanvas.transform, upgradeShopUIPrefab.name);
        upgradeShopPanel.SetActive(true);
        menuButtonPanel.SetActive(true);
    }


    /// <summary>
    /// Be�ll�tja a gombok esem�nykezel�it az upgrade shophoz.
    /// </summary>
    private void SetUpShopUIButtons()
    {
        // Men� gomb esem�nykezel�j�nek be�ll�t�sa
        GameObject menuButton = FindInChildrenIgnoreClone(menuButtonPanel.transform, "MenuButton");
        menuButton.GetComponent<Button>().onClick.AddListener(() => SetPauseMenuActive());

        // Skip (�tugr�s) gomb esem�nykezel�j�nek be�ll�t�sa
        GameObject skipButton = FindInChildrenIgnoreClone(upgradeShopUICanvas.transform, "SkipUpgradeButton");
        skipButton.GetComponent<Button>().onClick.AddListener(() => OnBuyButtonClicked(null));
    }


    /// <summary>
    /// Kezeli a sz�net men� aktiv�l�s�t. Ha a sz�net men� objektum nem null, aktiv�lja azt.
    /// Ha a sz�net men� objektum null, figyelmeztet�st �r ki a logba.
    /// </summary>
    public void SetPauseMenuActive()
    {
        OnGamePaused?.Invoke(GameState.Paused);
        pauseMenu.SetActive(true);
        // TODO: event a GameStateManagernek a 'Time.timeScale = 0' be�ll�t�s�hoz

    }


    public void ResumeGameButtonClicked()
    {
        // Az aktu�lis GameObject elrejt�se
        pauseMenu.SetActive(false);

        OnGameResumed?.Invoke(GameState.Playing);
    }


    public void ExitToMainMenuButtonClicked()
    {
        OnBackToMainMenu?.Invoke(GameState.MainMenu);
    }


    /// <summary>
    /// Ez a met�dus akkor h�v�dik meg, amikor a v�s�rl�s gombot megnyomj�k.
    /// Ha �rv�nyes `id` �rt�ket kapunk, akkor megjelen�ti a v�s�rolt elem nev�t a logban.
    /// Ha az `id` null, akkor azt jelzi, hogy a v�s�rl�s el lett hagyva.
    /// </summary>
    /// <param name="id">A v�s�rolt elem azonos�t�ja, amely alapj�n megt�rt�nik a v�s�rl�s nyilv�ntart�sa.</param>
    public void OnBuyButtonClicked(string id)      // PlayerUpgradeManager h�v�sa!
    {
        OnPurchaseOptionChosen?.Invoke(id);

    }


    /// <summary>
    /// Felt�lti az upgrade lehet�s�geket a shopban a megadott lista alapj�n.
    /// </summary>
    /// <param name="shopUpgrades">A j�t�kos �ltal v�laszthat� fejleszt�sek list�ja.</param>
    private void PopulateUpgradeOptions(List<PlayerUpgrade> shopUpgrades)
    {
        foreach (var item in shopUpgrades)
        {
            // V�s�rl�si lehet�s�g l�trehoz�sa �s hozz�ad�sa a list�hoz
            purchaseOptions.Add(CreatePurchaseOption(item));

            // Upgrade UI elemek hozz�ad�sa a v�s�rl�si panelhez
            AddUIPrefabToGameObject(upgradesPanel, CreateInitializedUpgradeUIPrefab(item), true);
        }

        // Esem�nykezel�k hozz�ad�sa minden upgrade gombhoz
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
    /// L�trehozza a v�s�rl�si lehet�s�get (`PurchaseOption`) a megadott `PlayerUpgrade` objektumb�l.
    /// Az �j v�s�rl�si lehet�s�g tartalmazza az upgrade nev�t, le�r�s�t, szintjeit �s egy�b param�tereit.
    /// </summary>
    /// <param name="playerUpgrade">A PlayerUpgrade objektum, amely meghat�rozza a v�s�rl�si lehet�s�g param�tereit.</param>
    /// <returns>A l�trehozott PurchaseOption objektum, amely tartalmazza az upgrade inform�ci�it.</returns>
    PurchaseOption CreatePurchaseOption(PlayerUpgrade playerUpgrade)
    {
        // L�trehozzuk a v�s�rl�si lehet�s�g objektumot
        PurchaseOption purchaseOption = new PurchaseOption();

        // A PlayerUpgrade objektum alapj�n kit�ltj�k a v�s�rl�si lehet�s�g mez�it
        purchaseOption.ID = playerUpgrade.ID; // A v�s�rl�si lehet�s�g azonos�t�ja
        purchaseOption.Name = playerUpgrade.upgradeName; // Az upgrade neve
        purchaseOption.Icon = playerUpgrade.icon; // Az upgrade ikonja
        purchaseOption.minLevel = playerUpgrade.minUpgradeLevel; // Minim�lis szint
        purchaseOption.maxLevel = playerUpgrade.maxUpgradeLevel; // Maxim�lis szint
        purchaseOption.currentLevel = playerUpgrade.currentUpgradeLevel; // Jelenlegi szint
        purchaseOption.Description = playerUpgrade.description; // Az upgrade le�r�sa
        purchaseOption.Price = playerUpgrade.GetPrice(); // Az upgrade �ra, amely a PlayerUpgrade objektumb�l sz�rmazik

        // Visszaadjuk a l�trehozott PurchaseOption objektumot
        return purchaseOption;
    }


    /// <summary>
    /// Az adott PlayerUpgrade t�pus�nak megfelel� UI prefab-ot adja vissza. A prefab t�pus�t a playerUpgrade param�ter hat�rozza meg,
    /// �s az UI elem sz�vegeit az adott friss�t�si lehet�s�g alapj�n �ll�tja be.
    /// </summary>
    /// <param name="playerUpgrade">A PlayerUpgrade objektum, amely meghat�rozza, hogy milyen t�pus� UI prefab-ot hozunk l�tre.</param>
    /// <returns>A megfelel� UI prefab, amely az UpgradeUIController komponenssel �s a megfelel� sz�vegekkel van konfigur�lva.</returns>
    GameObject CreateInitializedUpgradeUIPrefab(PlayerUpgrade playerUpgrade)
    {
        // Ha a playerUpgrade null, akkor null �rt�kkel t�r�nk vissza
        if (playerUpgrade == null)
        {
            return null;
        }

        // A megfelel� prefab v�ltoz� deklar�l�sa
        GameObject prefab;

        // Ha a playerUpgrade egy gy�gy�t�s t�pus� friss�t�s
        if (playerUpgrade.isHealing)
        {
            prefab = healUpgradeUIPrefab; // Hozz�rendelj�k a gy�gy�t�shoz tartoz� prefabot
            prefab.GetComponent<UpgradeUIController>().SetUpgradeUITextValues(CreatePurchaseOption(playerUpgrade)); // Be�ll�tjuk a sz�vegeket
            return prefab; // Visszaadjuk a prefab-ot
        }
        // Ha a playerUpgrade egy ideiglenes m�solat t�pus� friss�t�s
        else if (playerUpgrade.IsTempCopy)
        {
            prefab = nextLevelUpgradeUIPrefab; // Hozz�rendelj�k a k�vetkez� szinthez tartoz� prefabot
            prefab.GetComponent<UpgradeUIController>().SetUpgradeUITextValues(CreatePurchaseOption(playerUpgrade)); // Be�ll�tjuk a sz�vegeket
            return prefab; // Visszaadjuk a prefab-ot
        }
        // Ha sem egyik, akkor norm�l friss�t�st hozunk l�tre
        else
        {
            prefab = normalUpgradeUIPrefab; // Hozz�rendelj�k a norm�l friss�t�st
            prefab.GetComponent<UpgradeUIController>().SetUpgradeUITextValues(CreatePurchaseOption(playerUpgrade)); // Be�ll�tjuk a sz�vegeket
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
    /// A jelenet bet�lt�se ut�n v�grehajtand� m�veletek. Be�ll�tja a "Load Game" gomb referenci�j�t
    /// �s friss�ti annak �llapot�t.
    /// </summary>
    /// <param name="scene">A bet�lt�tt jelenet inform�ci�i.</param>
    /// <param name="mode">A bet�lt�s m�dja (pl. �j jelenet bet�lt�se vagy hozz�ad�s).</param>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (IsMainMenuScene())
        {
            // Be�ll�tjuk a "Load Game" gomb referenci�j�t
            SetMainMenuButtonReferences();

            // Friss�tj�k a gomb �llapot�t (akt�v vagy inakt�v)
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
        // t�bbi gomb is...
        quitGameButton.onClick.AddListener(() => QuitGameButton());
    }


    /// <summary>
    /// Ellen�rzi, hogy az aktu�lis jelenet a "MainMenu" jelenet-e.
    /// </summary>
    /// <returns>
    /// Visszaadja true-t, ha az aktu�lis jelenet neve "MainMenu", k�l�nben false-t.
    /// </returns>
    private bool IsMainMenuScene()
    {
        // Lek�rdezz�k az aktu�lisan bet�lt�tt jelenetet, �s �sszehasonl�tjuk annak nev�t a "MainMenu"-val
        return SceneManager.GetActiveScene().name == "MainMenu";
    }


    /// <summary>
    /// Friss�ti a "Load Game" gomb �llapot�t att�l f�gg�en, hogy l�tezik-e mentett j�t�kf�jl.
    /// Ha l�tezik mentett f�jl, a gomb akt�vv� v�lik, k�l�nben inakt�v.
    /// </summary>
    public void UpdateLoadGameButtonAvailability()
    {
        // Ellen�rizz�k, hogy a loadGameButton referencia nem null
        if (loadGameButton != null)
        {
            // A gomb interakci�s �llapot�t be�ll�tjuk a SaveLoadManager SaveFileExists met�dusa alapj�n
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
