using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DynamicObstacleController : ObstacleController
{
    protected override void Awake()
    {
        // event feliratkoz�sok
    }


    protected override void SetObstacleAttributesByLevel(int level)
    {
        SetCurrentObstacleSpriteByLevel(level);
    }
}
