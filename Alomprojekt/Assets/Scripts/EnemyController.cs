using Assets.Scripts;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.TextCore.Text;

public class EnemyController : Assets.Scripts.Character
{
    /// <summary>
    /// V�ltoz�k
    /// </summary>


    /// <summary>
    /// Komponenesek
    /// </summary>


    /// <summary>
    /// Getterek �s Setterek
    /// </summary>


    /// <summary>
    /// Esem�nyek
    /// </summary>
    public event Action<float> OnPlayerCollision; // J�t�kossal val� �tk�z�s


/// <summary>
/// Esem�nykezel�, amely akkor h�v�dik meg, amikor egy m�sik collider 2D-es triggerrel �tk�zik.
/// Ellen�rzi, hogy a trigger objektum a PlayerController-t tartalmazza, �s ha igen, 
/// megh�vja az OnPlayerCollision esem�nyt, amely a j�t�kos sebz�s�t kezeli.
/// </summary>
/// <param name="trigger">Az �tk�z� collider, amely a trigger esem�nyt v�ltja ki</param>
    void OnTriggerEnter2D(Collider2D trigger)
    {
        //Debug.Log(player);
        if (trigger.gameObject.TryGetComponent<PlayerController>(out var palyer))
        {
            //Debug.Log(BaseDMG);
            OnPlayerCollision?.Invoke(-BaseDMG);
        }
    }

}