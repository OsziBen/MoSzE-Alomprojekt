using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using UnityEngine;

public class ObstacleController : MonoBehaviour
{
    [Header("Prefab ID")]
    [SerializeField]
    protected string prefabID;

    [ContextMenu("Generate guid for ID")]
    private void GenerateGuid()
    {
        prefabID = System.Guid.NewGuid().ToString();
    }

    [Header("Sprites and Colliders")]
    [SerializeField]
    private List<SpriteLevelData> spriteLevelDataList;

    CharacterSetupManager characterSetupManager;

    public string ID
    {
        get { return prefabID; }
    }

    private void Awake()
    {
        characterSetupManager = FindObjectOfType<CharacterSetupManager>();
        characterSetupManager.OnSetObstacleAttributes += SetObstacleAttributesByLevel;
    }

    private void OnValidate()
    {
        ValidateUniqueID();
        //ValidateUniqueSpriteLevels();
    }

    private void ValidateUniqueID()
    {
        if (string.IsNullOrEmpty(prefabID))
        {
            Debug.LogError("Prefab ID is empty! Please generate or assign a unique ID.", this);
        }
    }

    private void ValidateUniqueSpriteLevels()
    {
        HashSet<int> levelSet = new HashSet<int>();
        foreach (var data in spriteLevelDataList)
        {
            if (levelSet.Contains(data.level))
            {
                Debug.LogError($"Duplicate level {data.level} found in LevelSpriteDataList.");
            }
            else
            {
                levelSet.Add(data.level);
            }
        }
    }

    void SetObstacleAttributesByLevel(int level)
    {
        SetCurrentObstacleSpriteByLevel(level);
        characterSetupManager.OnSetObstacleAttributes -= SetObstacleAttributesByLevel;
    }

    void SetCurrentObstacleSpriteByLevel(int level)
    {
        foreach (var item in spriteLevelDataList)
        {
            Debug.Log(item.level);
        }
        var currentSpriteLevelData = spriteLevelDataList.FirstOrDefault(x => x.level == level);

        if (currentSpriteLevelData != null)
        {
            // If item is found, update the sprite and collider
            this.GetComponent<SpriteRenderer>().sprite = currentSpriteLevelData.sprite;
            currentSpriteLevelData.collider.enabled = true;
        }
        else
        {
            // Handle case where no matching level is found
            Debug.LogWarning($"No SpriteLevelData found for level {level}. Make sure the level exists in the data list.");
        }
    }
}
