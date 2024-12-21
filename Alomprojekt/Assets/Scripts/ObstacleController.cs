using System.Collections;
using System.Collections.Generic;
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

    public string ID
    {
        get { return prefabID; }
    }

    private void OnValidate()
    {
        ValidateUniqueID();
    }

    private void ValidateUniqueID()
    {
        if (string.IsNullOrEmpty(prefabID))
        {
            Debug.LogError("Prefab ID is empty! Please generate or assign a unique ID.", this);
        }
    }
}
