using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static UIManager;

public class UpgradeUIController : MonoBehaviour
{
    /// <summary>
    /// V�ltoz�k
    /// </summary>
    [Header("Upgrade Text Value Fields")]
    [SerializeField]
    private TextMeshProUGUI upgradeNameUI; // A friss�t�s nev�nek megjelen�t�s�re szolg�l� sz�veg.
    [SerializeField]
    private TextMeshProUGUI upgradeLevelsUI; // A friss�t�si szintek tartom�ny�nak megjelen�t�s�re szolg�l� sz�veg.
    [SerializeField]
    private Image upgradeIconUI; // A friss�t�s ikonj�t megjelen�t� k�p.
    [SerializeField]
    private TextMeshProUGUI upgradeDescriptionUI; // A friss�t�s le�r�s�nak sz�vege.
    [SerializeField]
    private TextMeshProUGUI upgradeButtonTextUI; // A gomb sz�veg�nek megjelen�t�s�re szolg�l� mez�.
    [SerializeField]
    private Button upgradeButton; // A v�s�rl�st kezel� gomb.

    public string ID; // A friss�t�s egyedi azonos�t�ja.


    /// <summary>
    /// Be�ll�tja a fejleszt�s UI elemeinek sz�vegeit �s �rt�keit a megadott `PurchaseOption` alapj�n.
    /// </summary>
    public void SetUpgradeUITextValues(PurchaseOption purchaseOption)
    {
        // Az azonos�t� �rt�k�nek be�ll�t�sa
        ID = purchaseOption.ID;

        // A fejleszt�s nev�nek �s szintj�nek sz�veges megjelen�t�se, arab sz�mokat r�maira cser�lve
        upgradeNameUI.text = $"{purchaseOption.Name} {ReplaceArabicWithRoman(purchaseOption.currentLevel.ToString())}.";

        // A lehets�ges szintek tartom�ny�nak megjelen�t�se
        upgradeLevelsUI.text = $"Lehets�ges Szintek: {purchaseOption.minLevel} - {purchaseOption.maxLevel}";

        // Az ikon be�ll�t�sa
        upgradeIconUI.sprite = purchaseOption.Icon;

        // A fejleszt�s le�r�s�nak megjelen�t�se
        upgradeDescriptionUI.text = purchaseOption.Description;

        // A gomb sz�veg�nek be�ll�t�sa az �r megjelen�t�s�vel
        upgradeButtonTextUI.text = $"V�s�rl�s - {purchaseOption.Price}";
    }

    /// <summary>
    /// Az arab sz�mokat r�mai sz�mokra cser�li egy sz�vegen bel�l.
    /// </summary>
    /// <param name="input">A bemeneti sz�veg.</param>
    /// <returns>A r�mai sz�mokkal helyettes�tett sz�veg.</returns>
    public static string ReplaceArabicWithRoman(string input)
    {
        // Az arab sz�mokat r�mai sz�mokhoz rendel� sz�t�r.
        var arabicToRoman = new Dictionary<string, string>
    {
        { "1", "I" },
        { "2", "II" },
        { "3", "III" },
        { "4", "IV" }
    };

        // Az arab sz�mok cser�je regex seg�ts�g�vel.
        return Regex.Replace(input, @"\b[1-4]\b", match =>
        {
            return arabicToRoman[match.Value];
        });
    }


}
