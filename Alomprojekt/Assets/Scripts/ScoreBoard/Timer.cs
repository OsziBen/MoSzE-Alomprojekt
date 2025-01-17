using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Timer : MonoBehaviour
{
    private float runStartTime;
    private float elapsedTime;
    private bool isTiming;

    // TODO: gameStateManager integr�ci� + scoreboard ment�shez kell
    void Update()
    {
        if (isTiming)
        {
            elapsedTime = Time.time - runStartTime;
        }
    }

    public void StartTimer()
    {
        runStartTime = Time.time; // R�gz�tj�k az indul�si id�t
        isTiming = true;
    }

    public void StopTimer()
    {
        isTiming = false; // Le�ll�tjuk az id�z�t�st
    }

    public float GetElapsedTime()
    {
        return elapsedTime; // Visszaadjuk az eltelt id�t
    }
}
