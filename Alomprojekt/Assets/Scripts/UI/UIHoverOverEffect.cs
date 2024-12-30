using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIHoverOverEffect : MonoBehaviour
{
    /// <summary>
    /// V�ltoz�k
    /// </summary>
    [SerializeField]
    private GameObject textWindow;  // A sz�vegablak, amely interakci�k sor�n megjelenhet vagy elt�nhet.


    /// <summary>
    /// Kezeli a viselked�st, amikor egy elem f�l� viszik a kurzort.
    /// Aktiv�lja a hozz� tartoz� sz�vegablakot, ha az nem null.
    /// </summary>
    public void OnHoverEnter()
    {
        // Ellen�rizz�k, hogy a textWindow objektum inicializ�lva van-e
        if (textWindow != null)
        {
            // A sz�vegablak l�that�v� t�tele
            textWindow.SetActive(true);
        }
    }


    /// <summary>
    /// Kezeli a viselked�st, amikor a kurzor elhagyja az elemet.
    /// Deaktiv�lja a hozz� tartoz� sz�vegablakot, ha az nem null.
    /// </summary>
    public void OnHoverExit()
    {
        // Ellen�rizz�k, hogy a textWindow objektum inicializ�lva van-e
        if (textWindow != null)
        {
            // A sz�vegablak elrejt�se
            textWindow.SetActive(false);
        }
    }

}
