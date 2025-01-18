using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Timer : BasePersistentManager<Timer>
{
    //private float runStartTime;
    private float elapsedTime = 0f;
    private bool isTiming;

    // TODO: gameStateManager integr�ci� + scoreboard ment�shez kell
    void Update()
    {
        if (isTiming)
        {
            elapsedTime += Time.deltaTime;
            //Debug.Log("Elapsed Time: " + FormatTime(elapsedTime));
        }
    }

    public void RestartTimer()
    {
        elapsedTime = 0f; // R�gz�tj�k az indul�si id�t
        //isTiming = true;
    }

    public void ResumeTimer()
    {
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

    public void SetTimer(float time)
    {
        elapsedTime = time;
    }

    public string FormatTime(float time)
    {
        int minutes = Mathf.FloorToInt(time / 60); // Percek
        int seconds = Mathf.FloorToInt(time % 60); // M�sodpercek
        int centiseconds = Mathf.FloorToInt((time * 100) % 100); // Sz�zadm�sodpercek

        // Form�z�s: 99:99:99
        return $"{minutes:00}:{seconds:00}:{centiseconds:00}";
    }

}
