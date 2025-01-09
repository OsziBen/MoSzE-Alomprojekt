using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class BaseTransientManager<T> : MonoBehaviour where T : BaseTransientManager<T>
{
    public static T Instance { get; private set; }

    protected virtual void Awake()
    {
        if (Instance == null)
        {
            Instance = (T)this;
            Initialize();
        }
        else
        {
            Destroy(gameObject);
        }
    }


    protected virtual async void Initialize()
    {
        Debug.Log("Hello, I am " + this.name);
    }

}
