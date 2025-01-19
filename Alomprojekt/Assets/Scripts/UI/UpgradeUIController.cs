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
    /// Változók
    /// </summary>
    [Header("Upgrade Text Value Fields")]
    [SerializeField]
    private TextMeshProUGUI upgradeNameUI; // Az upgrade nevének megjelenítésére szolgáló szöveg.
    [SerializeField]
    private TextMeshProUGUI upgradeLevelsUI; // Az upgrade szintek tartományának megjelenítésére szolgáló szöveg.
    [SerializeField]
    private Image upgradeIconUI; // Az upgrade ikonját megjelenítõ kép.
    [SerializeField]
    private TextMeshProUGUI upgradeDescriptionUI; // Az upgrade leírásának szövege.
    [SerializeField]
    private TextMeshProUGUI upgradeButtonTextUI; // A gomb szövegének megjelenítésére szolgáló mezõ.
    [SerializeField]
    private Button upgradeButton; // A vásárlást kezelõ gomb.

    public string ID; // Az upgrade egyedi azonosítója.

    public int Price; // Az upgrade ára.



    /// <summary>
    /// Beállítja a fejlesztés UI elemeinek szövegeit és értékeit a megadott `PurchaseOption` alapján.
    /// </summary>
    public void SetUpgradeUITextValues(PurchaseOption purchaseOption)
    {
        // Az azonosító értékének beállítása
        ID = purchaseOption.ID;

        Price = purchaseOption.Price;

        // A fejlesztés nevének és szintjének szöveges megjelenítése, arab számokat rómaira cserélve
        upgradeNameUI.text = $"{purchaseOption.Name} {ReplaceArabicWithRoman(purchaseOption.currentLevel.ToString())}.";

        // A lehetséges szintek tartományának megjelenítése
        upgradeLevelsUI.text = $"Lehetséges Szintek: {purchaseOption.minLevel} - {purchaseOption.maxLevel}";

        // Az ikon beállítása
        upgradeIconUI.sprite = purchaseOption.Icon;

        // A fejlesztés leírásának megjelenítése
        upgradeDescriptionUI.text = purchaseOption.Description;

        // A gomb szövegének beállítása az ár megjelenítésével
        upgradeButtonTextUI.text = $"Vásárlás - {purchaseOption.Price}";
    }

    /// <summary>
    /// Az arab számokat római számokra cseréli egy szövegen belül.
    /// </summary>
    /// <param name="input">A bemeneti szöveg.</param>
    /// <returns>A római számokkal helyettesített szöveg.</returns>
    public static string ReplaceArabicWithRoman(string input)
    {
        // Az arab számokat római számokhoz rendelõ szótár.
        var arabicToRoman = new Dictionary<string, string>
    {
        { "1", "I" },
        { "2", "II" },
        { "3", "III" },
        { "4", "IV" }
    };

        // Az arab számok cseréje regex segítségével.
        return Regex.Replace(input, @"\b[1-4]\b", match =>
        {
            return arabicToRoman[match.Value];
        });
    }


}
