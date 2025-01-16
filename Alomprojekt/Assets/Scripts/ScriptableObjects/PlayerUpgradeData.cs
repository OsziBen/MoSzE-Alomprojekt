using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// A PlayerUpgradeData ScriptableObject, amely lehetõvé teszi a játékos fejlesztéseinek kezelését
[CreateAssetMenu(fileName = "PlayerUpgradeData", menuName = "Game/PlayerUpgradeData")]
public class PlayerUpgradeData : ScriptableObject
{
    /// <summary>
    /// Változók
    /// </summary>
    [Header("General Info")]
    [SerializeField]
    private string uniqueID;
    [ContextMenu("Generate guid for ID")]
    private void GenerateGuid()
    {
        uniqueID = System.Guid.NewGuid().ToString();
    }

    public string upgradeName;   // A fejlesztés neve
    public Sprite icon;         // A fejlesztés ikonjának helye
    public bool isHealing = false;  // Gyógyítás-e

    [TextArea]
    public string customDescription;   // Rövid leírás a fejlesztésrõl (paraméteres)
    [HideInInspector]
    public string description;  // Rövid leírás a fejlesztésrõl (kijelzésre alkalmas verzió)

    public bool showOutputInConsole = false;    // Editor-on belül a kijelzésre alkalmas verzió megjelenítése a Console ablakban

    // Fejlesztés szintjének beállításai
    [Header("Level")]
    [Range(1, 4)]
    public int minUpgradeLevel;         // A fejlesztés minimális szintje (1-4 között)
    [Range(1, 4)]
    public int maxUpgradeLevel;         // A fejlesztés maximális szintje (1-4 között)
    [Range(1, 4)]
    public int currentUpgradeLevel;     // Az aktuális szint, alapértelmezetten a minLevel

    [Header("Price")]
    [Range(0, 120)]
    public int basePrice;   // Az alapár, amely az ár meghatározásához szükséges
    [Range(1, 2)]
    public float priceScaleFactor;  // Az ár szorzó tényezõje, amely módosítja az alapárat
    private readonly int maxPrice = 150;    // A maximális ár, amelyet nem lehet túllépni

    // A statisztikai módosítók listája
    [Header("Stat Modifiers")]
    public List<StatModifier> modifiers;  // A lista, amely tartalmazza az összes módosítót

    // A statisztikai módosítók tárolására szolgáló osztály
    [System.Serializable]
    public class StatModifier
    {
        public StatType type;           // A módosítandó statisztikai típus
        [Range(-100f, 100f)]
        public float baseValue;         // Az alapérték, amelyet a fejlesztés saját szintjéhez mérten módosítunk
        [Range(1f, 2f)]
        public float scaleFactor;       // A szorzó, amellyel az érték a fejlesztés saját szintjének növekedésével változik
    }


    // Enum a statisztikai típusok számára (pl. életerõ, sebzés, stb.)
    public enum StatType
    {
        Health,
        MovementSpeed,
        Damage,
        AttackCooldownReduction,
        CriticalHitChance,
        PercentageBasedDMG
    }


    /// <summary>
    /// Getterek és setterek
    /// </summary>
    public string ID
    {
        get { return uniqueID; }
    }


    /// <summary>
    /// A fejlesztés érvényesítése az editorban végrehajtott módosítások után.
    /// Leellenõrzi a módosítókat és a szintbeállításokat.
    /// </summary>
    private void OnValidate()
    {
        ValidateModifiers();  // Ellenõrzi a módosítók egyediségét
        ValidateUpgradeLevels();     // Ellenõrzi, hogy a szintek helyesen vannak beállítva
        currentUpgradeLevel = minUpgradeLevel;  // Alapértelmezés szerint a minimális szintre állítja
        ValidateUniqueID();
        UpdateDescription();
    }
    private void ValidateUniqueID()
    {
        if (string.IsNullOrEmpty(uniqueID))
        {
            Debug.LogError("Unique ID is empty! Please generate or assign a unique ID.", this);
        }
    }

    /// <summary>
    /// Frissíti az objektum leírását az aktuális `customDescription` alapján,
    /// és az esetleges helyettesítõ szövegeket kicseréli a `ReplacePlaceholders` metódussal.
    /// Ha a `showOutputInConsole` be van állítva, akkor a leírás kiírásra kerül a konzolra.
    /// </summary>
    private void UpdateDescription()
    {
        // A helyettesítõ szövegeket kicseréli a `customDescription` változóban.
        description = ReplacePlaceholders(customDescription);

        // Ha a konzol kimenetet engedélyezve van, akkor kiírja a végleges leírást.
        if (showOutputInConsole)
        {
            Debug.Log(description); // Opcionális: kiírás a konzolra a végleges leírás ellenõrzéséhez.
        }
    }


    /// <summary>
    /// A megadott szövegben (inputDescription) helyettesíti a helyõrzõket a megfelelõ értékekkel.
    /// A helyettesítések a fejlõdés szintjei, árak és módosítók alapján történnek.
    /// </summary>
    /// <param name="inputDescription">A helyettesítendõ szöveg.</param>
    /// <returns>A helyettesített szöveg.</returns>
    private string ReplacePlaceholders(string inputDescription)
    {
        // Általános helyõrzõk cseréje a megfelelõ értékekre.
        inputDescription = inputDescription.Replace(@"\name", upgradeName);  // A fejlesztés neve
        inputDescription = inputDescription.Replace(@"\minUpgradeLevel", minUpgradeLevel.ToString());  // Minimális fejlesztési szint
        inputDescription = inputDescription.Replace(@"\maxUpgradeLevel", maxUpgradeLevel.ToString());  // Maximális fejlesztési szint
        inputDescription = inputDescription.Replace(@"\currentUpgradeLevel", currentUpgradeLevel.ToString());  // Jelenlegi fejlesztési szint
        inputDescription = inputDescription.Replace(@"\basePrice", basePrice.ToString());  // Alapár
        inputDescription = inputDescription.Replace(@"\priceScaleFactor", priceScaleFactor.ToString());  // Ár skálázási faktor

        // A fejlesztés árának kiszámítása, figyelembe véve a szintet és az árskálázási tényezõt
        int calculatedPriceValue = GetPrice();
        inputDescription = inputDescription.Replace($@"\price", calculatedPriceValue.ToString());  // Ár helyõrzõ cseréje

        // Dinamikusan a módosítók helyõrzõinek cseréje, ha van módosító
        if (modifiers != null && modifiers.Count > 0)
        {
            for (int i = 0; i < modifiers.Count; i++)
            {
                // Módosító típusának, alapértékének és skálázási tényezõjének cseréje
                inputDescription = inputDescription.Replace($@"\m[{i}].type", modifiers[i].type.ToString());
                inputDescription = inputDescription.Replace($@"\m[{i}].baseValue", modifiers[i].baseValue.ToString());
                inputDescription = inputDescription.Replace($@"\m[{i}].scaleFactor", modifiers[i].scaleFactor.ToString());

                // Módosító értékének kiszámítása a szinttõl függõen
                float calculatedModifierValue = modifiers[i].baseValue * Mathf.Pow(modifiers[i].scaleFactor, currentUpgradeLevel - minUpgradeLevel);
                inputDescription = inputDescription.Replace($@"\m[{i}].value", calculatedModifierValue.ToString("F2"));  // Módosító érték helyõrzõ cseréje
            }
        }

        return inputDescription;  // A végleges, helyettesített szöveg visszaadása
    }


    /// <summary>
    /// Ellenõrzi, hogy a módosítók között nincsenek duplikált statisztikai típusok.
    /// Ha duplikált típus található, akkor nullázza az értékét.
    /// </summary>
    private void ValidateModifiers()
    {
        HashSet<StatType> statTypes = new HashSet<StatType>();  // A statisztikai típusok tárolására egy HashSet, hogy biztosítsuk az egyediséget
        foreach (var statModifier in modifiers)
        {
            // Ha a statisztikai típus már létezik, hibaüzenetet küldünk, és az értéket nullázzuk
            if (statTypes.Contains(statModifier.type))
            {
                Debug.LogError($"[StatTypeData] Duplicate stat type found: {statModifier.type}. This will be removed.");
                statModifier.baseValue = 0f;  // Alapérték nullázása
            }
            else
            {
                statTypes.Add(statModifier.type);  // Ha nincs duplikáció, hozzáadjuk a HashSethez
            }
        }
    }


    /// <summary>
    /// Ellenõrzi, hogy a maxLevel nem kisebb-e mint a minLevel.
    /// Ha igen, akkor automatikusan beállítja a maxLevel-et a minLevel-re.
    /// </summary>
    private void ValidateUpgradeLevels()
    {
        if (maxUpgradeLevel < minUpgradeLevel)
        {
            Debug.LogWarning($"[PlayerUpgrade] maxLevel ({maxUpgradeLevel}) cannot be less than minLevel ({minUpgradeLevel}). Adjusting...");
            maxUpgradeLevel = minUpgradeLevel;  // Ha szükséges, módosítjuk a maxLevel-et
        }

        // Érvényes tartományban tartjuk a minLevel és maxLevel értékeket
        minUpgradeLevel = Mathf.Clamp(minUpgradeLevel, 1, 4);
        maxUpgradeLevel = Mathf.Clamp(maxUpgradeLevel, 1, 4);
    }


    /// <summary>
    /// Növeli a fejlesztés szintjét, ha még nem értük el a maximális szintet.
    /// </summary>
    public void IncreaseCurrentUpgradeLevel()
    {
        if (currentUpgradeLevel < maxUpgradeLevel)
        {
            currentUpgradeLevel++;  // Ha nem a maximális szinten vagyunk, növeljük a szintet
        }
        else
        {
            Debug.LogWarning("[PlayerUpgrade] Already at max level.");  // Ha már a max szinten vagyunk
        }
    }


    /// <summary>
    /// Beállítja a fejlesztés szintjét egy adott szintre, amely a minLevel és maxLevel közötti tartományban van.
    /// </summary>
    /// <param name="level">A kívánt szint</param>
    public void SetCurrentUpgradeLevel(int newLevel)
    {
        currentUpgradeLevel = Mathf.Clamp(newLevel, minUpgradeLevel, maxUpgradeLevel);  // Beállítja a szintet a tartományon belül
    }

    /*
    /// <summary>
    /// Kiszámítja és visszaadja a fejlesztés módosítóinak aktuális értékeit.
    /// Az értékek a szint függvényében skálázódnak.
    /// </summary>
    /// <returns>A statisztikai módosítók aktuális értékei</returns>
    public List<KeyValuePair<StatType, float>> GetCurrentValues()
    {
        List<KeyValuePair<StatType, float>> values = new List<KeyValuePair<StatType, float>>();  // Az eredmények tárolására egy lista

        // Végigiterálunk a módosítókon
        foreach (var modifier in modifiers)
        {
            // Az aktuális érték kiszámítása a baseValue és a scaleFactor segítségével
            float scaledValue = modifier.baseValue * Mathf.Pow(modifier.scaleFactor, currentUpgradeLevel - minUpgradeLevel);
            // A típus és a kiszámított érték párosát hozzáadjuk az eredményeket tartalmazó listához
            values.Add(new KeyValuePair<StatType, float>(modifier.type, scaledValue));
        }

        return values;  // Visszaadjuk az összegyûjtött értékeket
    }
    */

    /// <summary>
    /// A fejlesztés árának kiszámítása az alapár, árskálázási faktor és az aktuális fejlesztési szint alapján.
    /// Az ár egy korlátozott tartományban van, amely a `basePrice` és a `maxPrice` (150) között helyezkedik el.
    /// </summary>
    /// <returns>A kiszámított ár, mint egész szám.</returns>
    public int GetPrice()
    {
        // Az ár kiszámítása: alapár * (árskálázási faktor)^(jelenlegi szint - minimális szint)
        // Az eredmény korlátozása a `basePrice` és `maxPrice` (150) közötti tartományra.
        return (int)Mathf.Clamp(basePrice * Mathf.Pow(priceScaleFactor, currentUpgradeLevel - minUpgradeLevel), basePrice, maxPrice);
    }

}

