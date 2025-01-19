using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static GameStateManager;

public class PauseMenuController : MonoBehaviour
{
    /// <summary>
    /// Változók
    /// </summary>
    [Header("Panels")]
    [SerializeField]
    private GameObject mainPanel; // A fõpanel (alapértelmezett megjelenítés).
    [SerializeField]
    private GameObject hintPanel; // A tippablak (segítség megjelenítésére).
    [Header("Buttons")]
    [SerializeField]
    private Button resumeGameButton; // Játék folytatása gomb.
    [SerializeField]
    private Button exitToMainMenuButton; // Kilépés a főmenübe gomb.


    /// <summary>
    /// Komponensek
    /// </summary>
    UIManager uiManager;


    private void Start()
    {
        // A UIManager osztály példányának keresése a jelenetben, hogy hozzáférhessünk a kezelőfunkcióihoz.
        uiManager = FindAnyObjectByType<UIManager>(); 
        // A "resumeGameButton" gombhoz egy eseményfigyelőt adunk, amely meghívja a UIManager megfelelő metódusát, ha a gombra kattintanak.
        resumeGameButton.onClick.AddListener(() => uiManager.ResumeGameButtonClicked()); 
        // A "exitToMainMenuButton" gombhoz egy eseményfigyelőt adunk, amely meghívja a UIManager megfelelő metódusát, ha a gombra kattintanak.
        exitToMainMenuButton.onClick.AddListener(() => uiManager.ExitToMainMenuButtonClicked());
    }


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


}
