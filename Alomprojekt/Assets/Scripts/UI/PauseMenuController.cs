using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PauseMenuController : MonoBehaviour
{
    [SerializeField]
    private GameObject mainPanel; // A fõpanel (alapértelmezett megjelenítés).
    [SerializeField]
    private GameObject hintPanel; // A tippablak (segítség megjelenítésére).


    /// <summary>
    /// Akkor hívódik meg, amikor a Tippablak megnyitását jelölõ gombra kattintanak.
    /// Elrejti a fõpanelt, és megjeleníti a tippablakot.
    /// </summary>
    public void OnHintPanelOpenButtonClicked()
    {
        // A fõpanel elrejtése
        mainPanel.SetActive(false);
        // A tippablak megjelenítése
        hintPanel.SetActive(true);
    }


    /// <summary>
    /// Akkor hívódik meg, amikor a Tippablak bezárását jelölõ gombra kattintanak.
    /// Elrejti a tippablakot, és visszahozza a fõpanelt.
    /// </summary>
    public void OnHintPanelCloseButtonClicked()
    {
        // A tippablak elrejtése
        hintPanel.SetActive(false);
        // A fõpanel megjelenítése
        mainPanel.SetActive(true);
    }


    /// <summary>
    /// Akkor hívódik meg, amikor a Szünet menü bezárását jelölõ gombra kattintanak.
    /// Elrejti az egész menü GameObject-jét.
    /// </summary>
    public void OnPauseMenuCloseButtonClicked()
    {
        // Az aktuális GameObject elrejtése
        gameObject.SetActive(false);
    }

}
