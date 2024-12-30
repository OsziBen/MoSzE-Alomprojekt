using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PauseMenuController : MonoBehaviour
{
    [SerializeField]
    private GameObject mainPanel; // A f�panel (alap�rtelmezett megjelen�t�s).
    [SerializeField]
    private GameObject hintPanel; // A tippablak (seg�ts�g megjelen�t�s�re).


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


    /// <summary>
    /// Akkor h�v�dik meg, amikor a Sz�net men� bez�r�s�t jel�l� gombra kattintanak.
    /// Elrejti az eg�sz men� GameObject-j�t.
    /// </summary>
    public void OnPauseMenuCloseButtonClicked()
    {
        // Az aktu�lis GameObject elrejt�se
        gameObject.SetActive(false);
    }

}
