using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DynamicObstacleController : ObstacleController
{
    protected override void Awake()
    {
        // event feliratkozások
    }


    protected override void SetObstacleAttributesByLevel(int level)
    {
        SetCurrentObstacleSpriteByLevel(level);
    }
}
