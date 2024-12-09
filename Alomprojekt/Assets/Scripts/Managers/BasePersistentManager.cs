using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class BasePersistentManager<T> : MonoBehaviour where T : BasePersistentManager<T>
{
    public static T Instance { get; private set; }

    protected virtual void Awake()
    {
        if (Instance == null)
        {
            Instance = (T)this;
            DontDestroyOnLoad(gameObject);
            Initialize();
        }
        else
        {
            Destroy(gameObject);
        }
    }


    protected virtual void Initialize()
    {
        Debug.Log("Hello, I am " + this.name);
    }

}
