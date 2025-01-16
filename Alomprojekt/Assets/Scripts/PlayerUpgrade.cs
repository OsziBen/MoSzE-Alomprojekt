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
    /// V�ltoz�k
    /// </summary>
    private string _uniqueID;
    public string upgradeName;  // A fejleszt�s neve
    public Sprite icon;         // A fejleszt�s ikonj�nak helye
    public bool isHealing;  // Ha igaz, a fejleszt�s gy�gy�t� jelleg�
    public string description;  // A fejleszt�s le�r�sa
    public string customDescription;  // Le�r�s, amely tartalmazhat helyettes�t� sz�vegeket
    public int minUpgradeLevel;  // A minim�lis szint, amelyen el�rhet� a fejleszt�s
    public int maxUpgradeLevel;  // A maxim�lis szint, ameddig a fejleszt�s n�velhet�
    public int currentUpgradeLevel;  // Az aktu�lis fejleszt�si szint
    public int basePrice;  // A fejleszt�s alap�ra
    public float priceScaleFactor;  // Az �r n�vel�s�nek t�nyez�je a szint n�veked�s�vel
    private readonly int maxPrice = 150;  // A fejleszt�s maxim�lis �ra
    public List<StatModifierData> modifiers;  // A fejleszt�shez tartoz� statisztikai m�dos�t�k
    public bool isTempCopy = false; // Ideiglenes m�solat-e?


    [System.Serializable]
    public class StatModifierData
    {
        public PlayerUpgradeData.StatType type;  // A m�dos�t� t�pusa (pl. �leter�, sebz�s)
        public float baseValue;  // A m�dos�t� alap�rt�ke
        public float scaleFactor;  // A m�dos�t� szorz�ja, amely a szinttel n�vekszik
    }

    // Konstruktor, amely a PlayerUpgradeData objektum adatait haszn�lja a PlayerUpgrade objektum l�trehoz�s�hoz
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
            // A m�dos�t�k m�sol�sa
            modifiers.Add(new StatModifierData
            {
                type = modifier.type,
                baseValue = modifier.baseValue,
                scaleFactor = modifier.scaleFactor
            });
        }
    }

    // Konstruktor, amely egy megl�v� PlayerUpgrade m�solat�t hozza l�tre
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
    /// Getterek �s setterek
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
    /// Friss�ti a fejleszt�s le�r�s�t az egy�ni le�r�s alapj�n,
    /// helyettes�tve a param�tereket (pl. �r, szint).
    /// </summary>
    public void RefreshDescription()
    {
        if (string.IsNullOrEmpty(customDescription))
        {
            Debug.LogWarning("Custom description is null or empty.");
            return;
        }

        // Az egy�ni le�r�s helyettes�t�sekkel val� friss�t�se
        description = ReplacePlaceholders(customDescription);
    }


    /// <summary>
    /// A megadott sz�vegben (inputDescription) helyettes�ti a hely�rz�ket a megfelel� �rt�kekkel.
    /// A helyettes�t�sek a fejl�d�s szintjei, �rak �s m�dos�t�k alapj�n t�rt�nnek.
    /// </summary>
    /// <param name="inputDescription">A helyettes�tend� sz�veg.</param>
    /// <returns>A helyettes�tett sz�veg.</returns>
    private string ReplacePlaceholders(string inputDescription)
    {
        // �ltal�nos hely�rz�k cser�je a megfelel� �rt�kekre.
        inputDescription = inputDescription.Replace(@"\name", upgradeName);  // A fejleszt�s neve
        inputDescription = inputDescription.Replace(@"\minUpgradeLevel", minUpgradeLevel.ToString());  // Minim�lis fejleszt�si szint
        inputDescription = inputDescription.Replace(@"\maxUpgradeLevel", maxUpgradeLevel.ToString());  // Maxim�lis fejleszt�si szint
        inputDescription = inputDescription.Replace(@"\currentUpgradeLevel", currentUpgradeLevel.ToString());  // Jelenlegi fejleszt�si szint
        inputDescription = inputDescription.Replace(@"\basePrice", basePrice.ToString());  // Alap�r
        inputDescription = inputDescription.Replace(@"\priceScaleFactor", priceScaleFactor.ToString());  // �r sk�l�z�si faktor

        // A fejleszt�s �r�nak kisz�m�t�sa, figyelembe v�ve a szintet �s az �rsk�l�z�si t�nyez�t
        int calculatedPriceValue = GetPrice();
        inputDescription = inputDescription.Replace($@"\price", calculatedPriceValue.ToString());  // �r hely�rz� cser�je

        // Dinamikusan a m�dos�t�k hely�rz�inek cser�je, ha van m�dos�t�
        if (modifiers != null && modifiers.Count > 0)
        {
            for (int i = 0; i < modifiers.Count; i++)
            {
                // M�dos�t� t�pus�nak, alap�rt�k�nek �s sk�l�z�si t�nyez�j�nek cser�je
                inputDescription = inputDescription.Replace($@"\m[{i}].type", modifiers[i].type.ToString());
                inputDescription = inputDescription.Replace($@"\m[{i}].baseValue", modifiers[i].baseValue.ToString());
                inputDescription = inputDescription.Replace($@"\m[{i}].scaleFactor", modifiers[i].scaleFactor.ToString());

                // M�dos�t� �rt�k�nek kisz�m�t�sa a szintt�l f�gg�en
                float calculatedModifierValue = modifiers[i].baseValue * Mathf.Pow(modifiers[i].scaleFactor, currentUpgradeLevel - minUpgradeLevel);
                inputDescription = inputDescription.Replace($@"\m[{i}].value", calculatedModifierValue.ToString("F2"));  // M�dos�t� �rt�k hely�rz� cser�je
            }
        }

        return inputDescription;  // A v�gleges, helyettes�tett sz�veg visszaad�sa
    }

    /// <summary>
    /// N�veli a fejleszt�s szintj�t, ha m�g nem �rt�k el a maxim�lis szintet.
    /// </summary>
    public void IncreaseCurrentPlayerUpgradeLevel()
    {
        if (currentUpgradeLevel < maxUpgradeLevel)
        {
            currentUpgradeLevel++;  // Ha nem a maxim�lis szinten vagyunk, n�velj�k a szintet
        }
        else
        {
            Debug.LogWarning("[PlayerUpgrade] Already at max level.");  // Ha m�r a max szinten vagyunk
        }
    }


    /// <summary>
    /// Be�ll�tja a fejleszt�s szintj�t egy adott szintre, amely a minLevel �s maxLevel k�z�tti tartom�nyban van.
    /// </summary>
    /// <param name="level">A k�v�nt szint</param>
    public void SetCurrentPlayerUpgradeLevel(int newLevel)
    {
        currentUpgradeLevel = Mathf.Clamp(newLevel, minUpgradeLevel, maxUpgradeLevel);  // Be�ll�tja a szintet a tartom�nyon bel�l
    }

    /// <summary>
    /// Kisz�m�tja �s visszaadja a fejleszt�s m�dos�t�inak aktu�lis �rt�keit.
    /// Az �rt�kek a szint f�ggv�ny�ben sk�l�z�dnak.
    /// </summary>
    /// <returns>A statisztikai m�dos�t�k aktu�lis �rt�kei</returns>
    public List<StatValuePair> GetCurrentValues()
    {
        List<StatValuePair> values = new List<StatValuePair>();  // Az eredm�nyek t�rol�s�ra egy lista

        // V�gigiter�lunk a m�dos�t�kon
        foreach (var modifier in modifiers)
        {
            // Az aktu�lis �rt�k kisz�m�t�sa a baseValue �s a scaleFactor seg�ts�g�vel
            float scaledValue = modifier.baseValue * Mathf.Pow(modifier.scaleFactor, currentUpgradeLevel - minUpgradeLevel);
            // A t�pus �s a kisz�m�tott �rt�k p�ros�t hozz�adjuk az eredm�nyeket tartalmaz� list�hoz
            values.Add(new StatValuePair(modifier.type, scaledValue));
        }

        return values;  // Visszaadjuk az �sszegy�jt�tt �rt�keket
    }


    /// <summary>
    /// A fejleszt�s �r�nak kisz�m�t�sa az alap�r, �rsk�l�z�si faktor �s az aktu�lis fejleszt�si szint alapj�n.
    /// Az �r egy korl�tozott tartom�nyban van, amely a `basePrice` �s a `maxPrice` (150) k�z�tt helyezkedik el.
    /// </summary>
    /// <returns>A kisz�m�tott �r, mint eg�sz sz�m.</returns>
    public int GetPrice()
    {
        // Az �r kisz�m�t�sa: alap�r * (�rsk�l�z�si faktor)^(jelenlegi szint - minim�lis szint)
        // Az eredm�ny korl�toz�sa a `basePrice` �s `maxPrice` (150) k�z�tti tartom�nyra.
        return (int)Mathf.Clamp(basePrice * Mathf.Pow(priceScaleFactor, currentUpgradeLevel - minUpgradeLevel), basePrice, maxPrice);
    }


}
