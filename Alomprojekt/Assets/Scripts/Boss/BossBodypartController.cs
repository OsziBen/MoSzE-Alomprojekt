using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class BossBodypartController : MonoBehaviour
{
    // TODO: Események (eventek) hozzáadása
    public event Action OnBodypartPlayerCollision;

    // Amikor a trigger zónába kerül egy másik objektum
    void OnTriggerStay2D(Collider2D trigger)
    {
        // Ellenőrzi, hogy a trigger a játékosra vonatkozik-e
        if (trigger.gameObject.TryGetComponent<PlayerController>(out var player))
        {
            // Kiírja a konzolra, hogy a játékos beleütközött a testrészbe
            Debug.Log("OUCH!" + player);
            // Ha van olyan esemény, ami erre reagál, meghívja azt
            OnBodypartPlayerCollision?.Invoke();
        }
    }
}
