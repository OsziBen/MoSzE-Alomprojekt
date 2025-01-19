using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using UnityEngine;

public abstract class ObstacleController : MonoBehaviour
{
    [Header("Prefab ID")]
    [SerializeField]
    protected string prefabID; // Az obstacle prefab azonosítója

    [ContextMenu("Generate guid for ID")]
    // Új GUID generálása az ID-hoz
    private void GenerateGuid()
    {
        prefabID = System.Guid.NewGuid().ToString();
    }

    [Header("Sprites and Colliders")]
    [SerializeField]
    private List<SpriteLevelData> spriteLevelDataList; // Lista, amely tartalmazza az szintekhez rendelt sprite-okat és collidereket

    // Az obstacle ID gettere
    public string ID
    {
        get { return prefabID; }
    }

    // Absztrakt ébredési metódus, ami az öröklődő osztályokban lesz implementálva
    protected abstract void Awake();

    /// <summary>
    /// A Unity Editor által hívott metódus, amely akkor fut le, amikor a komponens vagy az objektum tulajdonságait módosítják a szerkesztőben.
    /// </summary>
    private void OnValidate()
    {
        // ValidateUniqueID(); // teszteléshez kikommentelni.
        //ValidateUniqueSpriteLevels();
    }

    /// <summary>
    /// Ellenőrzi, hogy az obstacle-hoz rendelt prefab ID érvényes és nem üres-e.
    /// Ha üres, hibát ír ki a konzolra, jelezve, hogy generálni vagy hozzárendelni kell egy egyedi ID-t.
    /// </summary>
    private void ValidateUniqueID()
    {
        // Ha az ID üres vagy null, akkor
        if (string.IsNullOrEmpty(prefabID))
        {
            Debug.LogError("Prefab ID is empty! Please generate or assign a unique ID.", this);
        }
    }

    /// <summary>
    /// Ellenőrzi, hogy a `spriteLevelDataList` listában szereplő elemek egyediek-e.
    /// Ha bármilyen duplikált elemet talál, hibát ír ki a konzolra.
    /// </summary>
    private void ValidateUniqueSpriteLevels()
    {
        HashSet<int> levelSet = new HashSet<int>(); // Egy új hashset létrehozása, hogy ellenőrizze az elemek egyediségét
        foreach (var data in spriteLevelDataList) // Végigmegy az összes SpriteLevelData objektumon a listában
        {
            // Ha már tartalmazza a halmaz az elemet, akkor duplikációt találtunk
            if (levelSet.Contains(data.level))
            {
                Debug.LogError($"Duplicate level {data.level} found in LevelSpriteDataList.");
            }
            else
            {
                levelSet.Add(data.level); // Ha nem található duplikáció, hozzáadja a szintet a halmazhoz
            }
        }
    }

    // Absztrakt metódus, ami a szinthez tartozó obstacle attribútumokat állítja be
    protected abstract void SetObstacleAttributesByLevel(int level);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="level"></param>
    protected void SetCurrentObstacleSpriteByLevel(int level)
    {
        // Megkeresi az aktuális szinthez tartozó adatokat
        var currentSpriteLevelData = spriteLevelDataList.FirstOrDefault(x => x.level == level);

        if (currentSpriteLevelData != null)
        {
            // Ha talál adatot, beállítja a sprite-ot és a collidert
            this.GetComponent<SpriteRenderer>().sprite = currentSpriteLevelData.sprite;
            currentSpriteLevelData.collider.enabled = true;
        }
        else
        {
            // Ha nem talál megfelelő szintet, akkor figyelmeztetést küld
            Debug.LogWarning($"No SpriteLevelData found for level {level}. Make sure the level exists in the data list.");
        }
    }
}
