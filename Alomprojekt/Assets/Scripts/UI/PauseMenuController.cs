using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static GameStateManager;

public class PauseMenuController : MonoBehaviour
{
    /// <summary>
    /// V�ltoz�k
    /// </summary>
    [Header("Panels")]
    [SerializeField]
    private GameObject mainPanel; // A f�panel (alap�rtelmezett megjelen�t�s).
    [SerializeField]
    private GameObject hintPanel; // A tippablak (seg�ts�g megjelen�t�s�re).
    [Header("Buttons")]
    [SerializeField]
    private Button resumeGameButton;
    [SerializeField]
    private Button exitToMainMenuButton;


    /// <summary>
    /// Komponensek
    /// </summary>
    UIManager uiManager;


    private void Start()
    {
        uiManager = FindAnyObjectByType<UIManager>();
        resumeGameButton.onClick.AddListener(() => uiManager.ResumeGameButtonClicked());
        exitToMainMenuButton.onClick.AddListener(() => uiManager.ExitToMainMenuButtonClicked());
    }


    /// <summary>
    /// Akkor h�v�dik meg, amikor a Tippablak megnyit�s�t jel�l� gombra kattintanak.
    /// Elrejti a f�panelt, �s megjelen�ti a tippablakot.
    /// </summary>
    public void OnHintPanelOpenButtonClicked()
    {
        // A f�panel elrejt�se
        mainPanel.SetActive(false);
        // A tippablak megjelen�t�se
        hintPanel.SetActive(true);
    }


    /// <summary>
    /// Akkor h�v�dik meg, amikor a Tippablak bez�r�s�t jel�l� gombra kattintanak.
    /// Elrejti a tippablakot, �s visszahozza a f�panelt.
    /// </summary>
    public void OnHintPanelCloseButtonClicked()
    {
        // A tippablak elrejt�se
        hintPanel.SetActive(false);
        // A f�panel megjelen�t�se
        mainPanel.SetActive(true);
    }


}
