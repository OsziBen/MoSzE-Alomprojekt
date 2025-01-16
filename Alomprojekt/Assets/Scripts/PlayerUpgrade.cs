using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static PlayerUpgradeData;
using StatValuePair = System.Collections.Generic.KeyValuePair<PlayerUpgradeData.StatType, float>;

[System.Serializable]
public class PlayerUpgrade
{
    /// <summary>
    /// Változók
    /// </summary>
    private string _uniqueID;
    public string upgradeName;  // A fejlesztés neve
    public Sprite icon;         // A fejlesztés ikonjának helye
    public bool isHealing;  // Ha igaz, a fejlesztés gyógyító jellegû
    public string description;  // A fejlesztés leírása
    public string customDescription;  // Leírás, amely tartalmazhat helyettesítõ szövegeket
    public int minUpgradeLevel;  // A minimális szint, amelyen elérhetõ a fejlesztés
    public int maxUpgradeLevel;  // A maximális szint, ameddig a fejlesztés növelhetõ
    public int currentUpgradeLevel;  // Az aktuális fejlesztési szint
    public int basePrice;  // A fejlesztés alapára
    public float priceScaleFactor;  // Az ár növelésének tényezõje a szint növekedésével
    private readonly int maxPrice = 150;  // A fejlesztés maximális ára
    public List<StatModifierData> modifiers;  // A fejlesztéshez tartozó statisztikai módosítók
    public bool isTempCopy = false; // Ideiglenes másolat-e?


    [System.Serializable]
    public class StatModifierData
    {
        public PlayerUpgradeData.StatType type;  // A módosító típusa (pl. életerõ, sebzés)
        public float baseValue;  // A módosító alapértéke
        public float scaleFactor;  // A módosító szorzója, amely a szinttel növekszik
    }

    // Konstruktor, amely a PlayerUpgradeData objektum adatait használja a PlayerUpgrade objektum létrehozásához
    public PlayerUpgrade(PlayerUpgradeData upgrade)
    {
        if (upgrade == null)
        {
            Debug.LogError("PlayerUpgrade object is null in UpgradeData constructor.");
            return;
        }

        ID = upgrade.ID;
        upgradeName = upgrade.upgradeName;
        icon = upgrade.icon;
        isHealing = upgrade.isHealing;
        description = upgrade.description;
        customDescription = upgrade.customDescription;
        minUpgradeLevel = upgrade.minUpgradeLevel;
        maxUpgradeLevel = upgrade.maxUpgradeLevel;
        currentUpgradeLevel = upgrade.currentUpgradeLevel;
        basePrice = upgrade.basePrice;
        priceScaleFactor = upgrade.priceScaleFactor;
        isTempCopy = false;

        modifiers = new List<StatModifierData>();

        foreach (var modifier in upgrade.modifiers)
        {
            // A módosítók másolása
            modifiers.Add(new StatModifierData
            {
                type = modifier.type,
                baseValue = modifier.baseValue,
                scaleFactor = modifier.scaleFactor
            });
        }
    }

    // Konstruktor, amely egy meglévõ PlayerUpgrade másolatát hozza létre
    public PlayerUpgrade(PlayerUpgrade original)
    {
        this.ID = original.ID;
        this.upgradeName = original.upgradeName;
        this.icon = original.icon;
        this.isHealing = original.isHealing;
        this.description = original.description;
        this.customDescription = original.customDescription;
        this.minUpgradeLevel = original.minUpgradeLevel;
        this.maxUpgradeLevel = original.maxUpgradeLevel;
        this.currentUpgradeLevel = original.currentUpgradeLevel;
        this.basePrice = original.basePrice;
        this.priceScaleFactor = original.priceScaleFactor;
        this.modifiers = new List<StatModifierData>(original.modifiers);
        this.isTempCopy = original.isTempCopy;
    }

    /// <summary>
    /// Getterek és setterek
    /// </summary>
    public string ID
    {
        get { return _uniqueID; }
        set { _uniqueID = value; }
    }

    public bool IsTempCopy
    {
        get { return isTempCopy; }
        set { isTempCopy = value; }
    }


    /// <summary>
    /// Frissíti a fejlesztés leírását az egyéni leírás alapján,
    /// helyettesítve a paramétereket (pl. ár, szint).
    /// </summary>
    public void RefreshDescription()
    {
        if (string.IsNullOrEmpty(customDescription))
        {
            Debug.LogWarning("Custom description is null or empty.");
            return;
        }

        // Az egyéni leírás helyettesítésekkel való frissítése
        description = ReplacePlaceholders(customDescription);
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
    /// Növeli a fejlesztés szintjét, ha még nem értük el a maximális szintet.
    /// </summary>
    public void IncreaseCurrentPlayerUpgradeLevel()
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
    public void SetCurrentPlayerUpgradeLevel(int newLevel)
    {
        currentUpgradeLevel = Mathf.Clamp(newLevel, minUpgradeLevel, maxUpgradeLevel);  // Beállítja a szintet a tartományon belül
    }

    /// <summary>
    /// Kiszámítja és visszaadja a fejlesztés módosítóinak aktuális értékeit.
    /// Az értékek a szint függvényében skálázódnak.
    /// </summary>
    /// <returns>A statisztikai módosítók aktuális értékei</returns>
    public List<StatValuePair> GetCurrentValues()
    {
        List<StatValuePair> values = new List<StatValuePair>();  // Az eredmények tárolására egy lista

        // Végigiterálunk a módosítókon
        foreach (var modifier in modifiers)
        {
            // Az aktuális érték kiszámítása a baseValue és a scaleFactor segítségével
            float scaledValue = modifier.baseValue * Mathf.Pow(modifier.scaleFactor, currentUpgradeLevel - minUpgradeLevel);
            // A típus és a kiszámított érték párosát hozzáadjuk az eredményeket tartalmazó listához
            values.Add(new StatValuePair(modifier.type, scaledValue));
        }

        return values;  // Visszaadjuk az összegyûjtött értékeket
    }


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
