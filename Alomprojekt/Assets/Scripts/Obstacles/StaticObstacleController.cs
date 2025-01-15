using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StaticObstacleController : ObstacleController
{
    /// <summary>
    /// Komponensek
    /// </summary>
    CharacterSetupManager characterSetupManager;


    protected override void Awake()
    {
        characterSetupManager = FindObjectOfType<CharacterSetupManager>();
        characterSetupManager.OnSetObstacleAttributes += SetObstacleAttributesByLevel;
    }

    protected override void SetObstacleAttributesByLevel(int level)
    {
        SetCurrentObstacleSpriteByLevel(level);
        characterSetupManager.OnSetObstacleAttributes -= SetObstacleAttributesByLevel;
    }
}
