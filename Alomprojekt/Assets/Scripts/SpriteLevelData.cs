using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

// Adattípus az enemy-k szinttől függő kinézetének tárolásához.
[System.Serializable]
public class SpriteLevelData
{
    [Range(1, 4)]
    public int level; // A szint száma.
    public Sprite sprite; // A szinten használni kívánt sprite.
    public Collider2D collider; // A sprite-hoz illő collider.

}

