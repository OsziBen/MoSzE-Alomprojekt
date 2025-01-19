using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StaticObstacleController : ObstacleController
{
    /// <summary>
    /// Komponensek
    /// </summary>
    CharacterSetupManager characterSetupManager; // A karakter beállításokat kezelő manager.


    protected override void Awake()
    {
        // Felkeressük a karakter beállításokat kezelő managert.
        characterSetupManager = FindObjectOfType<CharacterSetupManager>();
        // Feliratkozunk a karakter beállításokat kezelő eseményre, hogy értesüljünk a változásokról.
        characterSetupManager.OnSetObstacleAttributes += SetObstacleAttributesByLevel;
    }

    /// <summary>
    /// A szint (level) alapján állítja be az obstacle tulajdonságait.
    /// </summary>
    /// <param name="level">Az aktuális szint</param>
    protected override void SetObstacleAttributesByLevel(int level)
    {
        // Beállítja az obstacle jelenlegi sprite-ját a szint alapján.
        SetCurrentObstacleSpriteByLevel(level);
        // Miután az obstacle beállításai frissítve lettek, leiratkozunk az eseményről.
        characterSetupManager.OnSetObstacleAttributes -= SetObstacleAttributesByLevel;
    }
}
