using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

[System.Serializable]
public class SpriteLevelData
{
    [Range(1, 4)]
    public int level;
    public Sprite sprite;
    public Collider2D collider;

}

