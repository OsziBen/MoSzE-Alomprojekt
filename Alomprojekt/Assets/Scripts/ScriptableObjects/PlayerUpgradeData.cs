using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// A PlayerUpgradeData ScriptableObject, amely lehet�v� teszi a j�t�kos fejleszt�seinek kezel�s�t
[CreateAssetMenu(fileName = "PlayerUpgradeData", menuName = "Game/PlayerUpgradeData")]
public class PlayerUpgradeData : ScriptableObject
{
    /// <summary>
    /// V�ltoz�k
    /// </summary>
    [Header("General Info")]
    [SerializeField]
    private string uniqueID;
    [ContextMenu("Generate guid for ID")]
    private void GenerateGuid()
    {
        uniqueID = System.Guid.NewGuid().ToString();
    }

    public string upgradeName;   // A fejleszt�s neve
    public Sprite icon;         // A fejleszt�s ikonj�nak helye
    public bool isHealing = false;  // Gy�gy�t�s-e

    [TextArea]
    public string customDescription;   // R�vid le�r�s a fejleszt�sr�l (param�teres)
    [HideInInspector]
    public string description;  // R�vid le�r�s a fejleszt�sr�l (kijelz�sre alkalmas verzi�)

    public bool showOutputInConsole = false;    // Editor-on bel�l a kijelz�sre alkalmas verzi� megjelen�t�se a Console ablakban

    // Fejleszt�s szintj�nek be�ll�t�sai
    [Header("Level")]
    [Range(1, 4)]
    public int minUpgradeLevel;         // A fejleszt�s minim�lis szintje (1-4 k�z�tt)
    [Range(1, 4)]
    public int maxUpgradeLevel;         // A fejleszt�s maxim�lis szintje (1-4 k�z�tt)
    [Range(1, 4)]
    public int currentUpgradeLevel;     // Az aktu�lis szint, alap�rtelmezetten a minLevel

    [Header("Price")]
    [Range(0, 120)]
    public int basePrice;   // Az alap�r, amely az �r meghat�roz�s�hoz sz�ks�ges
    [Range(1, 2)]
    public float priceScaleFactor;  // Az �r szorz� t�nyez�je, amely m�dos�tja az alap�rat
    private readonly int maxPrice = 150;    // A maxim�lis �r, amelyet nem lehet t�ll�pni

    // A statisztikai m�dos�t�k list�ja
    [Header("Stat Modifiers")]
    public List<StatModifier> modifiers;  // A lista, amely tartalmazza az �sszes m�dos�t�t

    // A statisztikai m�dos�t�k t�rol�s�ra szolg�l� oszt�ly
    [System.Serializable]
    public class StatModifier
    {
        public StatType type;           // A m�dos�tand� statisztikai t�pus
        [Range(-100f, 100f)]
        public float baseValue;         // Az alap�rt�k, amelyet a fejleszt�s saj�t szintj�hez m�rten m�dos�tunk
        [Range(1f, 2f)]
        public float scaleFactor;       // A szorz�, amellyel az �rt�k a fejleszt�s saj�t szintj�nek n�veked�s�vel v�ltozik
    }


    // Enum a statisztikai t�pusok sz�m�ra (pl. �leter�, sebz�s, stb.)
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
    /// Getterek �s setterek
    /// </summary>
    public string ID
    {
        get { return uniqueID; }
    }


    /// <summary>
    /// A fejleszt�s �rv�nyes�t�se az editorban v�grehajtott m�dos�t�sok ut�n.
    /// Leellen�rzi a m�dos�t�kat �s a szintbe�ll�t�sokat.
    /// </summary>
    private void OnValidate()
    {
        ValidateModifiers();  // Ellen�rzi a m�dos�t�k egyedis�g�t
        ValidateUpgradeLevels();     // Ellen�rzi, hogy a szintek helyesen vannak be�ll�tva
        currentUpgradeLevel = minUpgradeLevel;  // Alap�rtelmez�s szerint a minim�lis szintre �ll�tja
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
    /// Friss�ti az objektum le�r�s�t az aktu�lis `customDescription` alapj�n,
    /// �s az esetleges helyettes�t� sz�vegeket kicser�li a `ReplacePlaceholders` met�dussal.
    /// Ha a `showOutputInConsole` be van �ll�tva, akkor a le�r�s ki�r�sra ker�l a konzolra.
    /// </summary>
    private void UpdateDescription()
    {
        // A helyettes�t� sz�vegeket kicser�li a `customDescription` v�ltoz�ban.
        description = ReplacePlaceholders(customDescription);

        // Ha a konzol kimenetet enged�lyezve van, akkor ki�rja a v�gleges le�r�st.
        if (showOutputInConsole)
        {
            Debug.Log(description); // Opcion�lis: ki�r�s a konzolra a v�gleges le�r�s ellen�rz�s�hez.
        }
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
    /// Ellen�rzi, hogy a m�dos�t�k k�z�tt nincsenek duplik�lt statisztikai t�pusok.
    /// Ha duplik�lt t�pus tal�lhat�, akkor null�zza az �rt�k�t.
    /// </summary>
    private void ValidateModifiers()
    {
        HashSet<StatType> statTypes = new HashSet<StatType>();  // A statisztikai t�pusok t�rol�s�ra egy HashSet, hogy biztos�tsuk az egyedis�get
        foreach (var statModifier in modifiers)
        {
            // Ha a statisztikai t�pus m�r l�tezik, hiba�zenetet k�ld�nk, �s az �rt�ket null�zzuk
            if (statTypes.Contains(statModifier.type))
            {
                Debug.LogError($"[StatTypeData] Duplicate stat type found: {statModifier.type}. This will be removed.");
                statModifier.baseValue = 0f;  // Alap�rt�k null�z�sa
            }
            else
            {
                statTypes.Add(statModifier.type);  // Ha nincs duplik�ci�, hozz�adjuk a HashSethez
            }
        }
    }


    /// <summary>
    /// Ellen�rzi, hogy a maxLevel nem kisebb-e mint a minLevel.
    /// Ha igen, akkor automatikusan be�ll�tja a maxLevel-et a minLevel-re.
    /// </summary>
    private void ValidateUpgradeLevels()
    {
        if (maxUpgradeLevel < minUpgradeLevel)
        {
            Debug.LogWarning($"[PlayerUpgrade] maxLevel ({maxUpgradeLevel}) cannot be less than minLevel ({minUpgradeLevel}). Adjusting...");
            maxUpgradeLevel = minUpgradeLevel;  // Ha sz�ks�ges, m�dos�tjuk a maxLevel-et
        }

        // �rv�nyes tartom�nyban tartjuk a minLevel �s maxLevel �rt�keket
        minUpgradeLevel = Mathf.Clamp(minUpgradeLevel, 1, 4);
        maxUpgradeLevel = Mathf.Clamp(maxUpgradeLevel, 1, 4);
    }


    /// <summary>
    /// N�veli a fejleszt�s szintj�t, ha m�g nem �rt�k el a maxim�lis szintet.
    /// </summary>
    public void IncreaseCurrentUpgradeLevel()
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
    public void SetCurrentUpgradeLevel(int newLevel)
    {
        currentUpgradeLevel = Mathf.Clamp(newLevel, minUpgradeLevel, maxUpgradeLevel);  // Be�ll�tja a szintet a tartom�nyon bel�l
    }

    /*
    /// <summary>
    /// Kisz�m�tja �s visszaadja a fejleszt�s m�dos�t�inak aktu�lis �rt�keit.
    /// Az �rt�kek a szint f�ggv�ny�ben sk�l�z�dnak.
    /// </summary>
    /// <returns>A statisztikai m�dos�t�k aktu�lis �rt�kei</returns>
    public List<KeyValuePair<StatType, float>> GetCurrentValues()
    {
        List<KeyValuePair<StatType, float>> values = new List<KeyValuePair<StatType, float>>();  // Az eredm�nyek t�rol�s�ra egy lista

        // V�gigiter�lunk a m�dos�t�kon
        foreach (var modifier in modifiers)
        {
            // Az aktu�lis �rt�k kisz�m�t�sa a baseValue �s a scaleFactor seg�ts�g�vel
            float scaledValue = modifier.baseValue * Mathf.Pow(modifier.scaleFactor, currentUpgradeLevel - minUpgradeLevel);
            // A t�pus �s a kisz�m�tott �rt�k p�ros�t hozz�adjuk az eredm�nyeket tartalmaz� list�hoz
            values.Add(new KeyValuePair<StatType, float>(modifier.type, scaledValue));
        }

        return values;  // Visszaadjuk az �sszegy�jt�tt �rt�keket
    }
    */

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

