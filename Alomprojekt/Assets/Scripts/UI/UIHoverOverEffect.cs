using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIHoverOverEffect : MonoBehaviour
{
    /// <summary>
    /// Változók
    /// </summary>
    [SerializeField]
    private GameObject textWindow;  // A szövegablak, amely interakciók során megjelenhet vagy eltûnhet.


    /// <summary>
    /// Kezeli a viselkedést, amikor egy elem fölé viszik a kurzort.
    /// Aktiválja a hozzá tartozó szövegablakot, ha az nem null.
    /// </summary>
    public void OnHoverEnter()
    {
        // Ellenõrizzük, hogy a textWindow objektum inicializálva van-e
        if (textWindow != null)
        {
            // A szövegablak láthatóvá tétele
            textWindow.SetActive(true);
        }
    }


    /// <summary>
    /// Kezeli a viselkedést, amikor a kurzor elhagyja az elemet.
    /// Deaktiválja a hozzá tartozó szövegablakot, ha az nem null.
    /// </summary>
    public void OnHoverExit()
    {
        // Ellenõrizzük, hogy a textWindow objektum inicializálva van-e
        if (textWindow != null)
        {
            // A szövegablak elrejtése
            textWindow.SetActive(false);
        }
    }

}
