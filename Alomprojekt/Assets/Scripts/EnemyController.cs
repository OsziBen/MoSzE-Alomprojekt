using Assets.Scripts;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.TextCore.Text;

public class EnemyController : Assets.Scripts.Character
{
    /// <summary>
    /// Változók
    /// </summary>


    /// <summary>
    /// Komponenesek
    /// </summary>


    /// <summary>
    /// Getterek és Setterek
    /// </summary>


    /// <summary>
    /// Események
    /// </summary>
    public event Action<float> OnPlayerCollision; // Játékossal való ütközés


/// <summary>
/// Eseménykezelõ, amely akkor hívódik meg, amikor egy másik collider 2D-es triggerrel ütközik.
/// Ellenõrzi, hogy a trigger objektum a PlayerController-t tartalmazza, és ha igen, 
/// meghívja az OnPlayerCollision eseményt, amely a játékos sebzését kezeli.
/// </summary>
/// <param name="trigger">Az ütközõ collider, amely a trigger eseményt váltja ki</param>
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