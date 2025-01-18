using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Timer : BasePersistentManager<Timer>
{
    //private float runStartTime;
    private float elapsedTime = 0f;
    private bool isTiming;

    // TODO: gameStateManager integráció + scoreboard mentéshez kell
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
        elapsedTime = 0f; // Rögzítjük az indulási idõt
        //isTiming = true;
    }

    public void ResumeTimer()
    {
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

    public void SetTimer(float time)
    {
        elapsedTime = time;
    }

    public string FormatTime(float time)
    {
        int minutes = Mathf.FloorToInt(time / 60); // Percek
        int seconds = Mathf.FloorToInt(time % 60); // Másodpercek
        int centiseconds = Mathf.FloorToInt((time * 100) % 100); // Századmásodpercek

        // Formázás: 99:99:99
        return $"{minutes:00}:{seconds:00}:{centiseconds:00}";
    }

}
