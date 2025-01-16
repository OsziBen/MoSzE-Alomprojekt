using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Timer : MonoBehaviour
{
    private float runStartTime;
    private float elapsedTime;
    private bool isTiming;

    // TODO: gameStateManager integráció + scoreboard mentéshez kell
    void Update()
    {
        if (isTiming)
        {
            elapsedTime = Time.time - runStartTime;
        }
    }

    public void StartTimer()
    {
        runStartTime = Time.time; // Rögzítjük az indulási idõt
        isTiming = true;
    }

    public void StopTimer()
    {
        isTiming = false; // Leállítjuk az idõzítést
    }

    public float GetElapsedTime()
    {
        return elapsedTime; // Visszaadjuk az eltelt idõt
    }
}
