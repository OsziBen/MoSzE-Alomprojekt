using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Ez az osztály egy gomb engedélyezéséért vagy tiltásáért felelős,
// a beviteli mező (TMP_InputField) tartalmának alapján.
public class ButtonEnableBasedOnInput : MonoBehaviour
{
    // A TMP_InputField komponens, amely a felhasználói szövegbevitelt kezeli.
    [SerializeField]
    private TMP_InputField inputField;

    // A gomb, amelynek az "interactable" tulajdonságát változtatjuk.
    [SerializeField]
    private Button backButton;

    // A Start() metódus a script indulásakor fut le.
    private void Start()
    {
        // A gomb kezdetben inaktív (nem kattintható).
        backButton.interactable = false;

        // Feliratkozunk az inputField "onValueChanged" eseményére,
        // amely akkor fut le, ha a beviteli mező tartalma megváltozik.
        inputField.onValueChanged.AddListener(CheckInput);
    }

    // Ez a metódus ellenőrzi a beviteli mező tartalmát.
    // Ha a mező nem üres, a gomb aktiválódik, különben inaktív lesz.
    void CheckInput(string input)
    {
        if (!string.IsNullOrEmpty(input))
        {
            // Ha van szöveg a mezőben, a gomb kattinthatóvá válik.
            backButton.interactable = true;
        }
        else
        {
            // Ha a mező üres, a gomb inaktív lesz.
            backButton.interactable = false;
        }
    }
}
