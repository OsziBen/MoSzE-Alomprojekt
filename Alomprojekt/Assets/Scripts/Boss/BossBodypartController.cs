using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class BossBodypartController : MonoBehaviour
{

    // TODO: eventek
    public event Action OnBodypartPlayerCollision;

    void OnTriggerStay2D(Collider2D trigger)
    {
        if (trigger.gameObject.TryGetComponent<PlayerController>(out var player))
        {
            Debug.Log("OUCH!" + player);
            OnBodypartPlayerCollision?.Invoke();
        }
    }
}
