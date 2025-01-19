using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A Timer osztály egy időzítő rendszert valósít meg, amely képes az idő mérésére, újraindítására, megállítására,
/// valamint az eltelt idő lekérdezésére. A BasePersistentManager osztályt örökli, így az instance singletonként
/// érhető el, és az időzítő állapotát a játék teljes időtartama alatt megőrzi.
/// </summary>
public class Timer : BasePersistentManager<Timer>
{
    // Az eltelt időt tárolja.
    private float elapsedTime = 0f;

    // Annak állapotát jelzi, hogy az időzítő fut-e.
    private bool isTiming;

    /// <summary>
    /// A Unity Update metódusa minden képkocka során meghívódik.
    /// Ha az időzítő aktív (isTiming igaz), növeli az eltelt időt az időközben eltelt másodpercekkel.
    /// </summary>
    void Update()
    {
        if (isTiming)
        {
            elapsedTime += Time.deltaTime; // Növeli az eltelt időt a delta idővel (másodpercben).
            //Debug.Log("Elapsed Time: " + FormatTime(elapsedTime));
        }
    }

    /// <summary>
    /// Az időzítő újraindítása: visszaállítja az eltelt időt 0-ra, de nem indítja újra automatikusan az időzítést.
    /// </summary>
    public void RestartTimer()
    {
        elapsedTime = 0f; // Visszaállítja az időzítőt alaphelyzetbe.
        //isTiming = true;
    }

    /// <summary>
    /// Az időzítő folytatása, amely lehetővé teszi az idő mérést onnan, ahol megállt.
    /// </summary>
    public void ResumeTimer()
    {
        isTiming = true; // Az időzítőt aktív állapotba állítja.
    }

    /// <summary>
    /// Az időzítő leállítása, amely megállítja az idő mérést.
    /// </summary>
    public void StopTimer()
    {
        isTiming = false; // Az időzítőt inaktív állapotba állítja.
    }

    /// <summary>
    /// Az eltelt idő lekérdezése másodpercben.
    /// </summary>
    /// <returns>A mért idő másodpercben, lebegőpontos értékként.</returns>
    public float GetElapsedTime()
    {
        return elapsedTime; // Visszaadja az eltelt időt másodpercben.
    }

    /// <summary>
    /// Beállít egy adott értéket az eltelt időnek, például külső mentésből történő visszatöltéshez.
    /// </summary>
    /// <param name="time">Az eltelt idő új értéke másodpercben.</param>
    public void SetTimer(float time)
    {
        elapsedTime = time; // Beállítja az eltelt időt a megadott értékre.
    }

    /// <summary>
    /// Az eltelt idő formázása emberi olvasásra alkalmas formátumra: "mm:ss:cs" (perc:másodperc:századmásodperc).
    /// </summary>
    /// <param name="time">Az idő másodpercben, amelyet formázni kell.</param>
    /// <returns>A formázott idő sztringként (például "02:15:34").</returns>
    public string FormatTime(float time)
    {
        // Percek kiszámítása (egész rész).
        int minutes = Mathf.FloorToInt(time / 60);

        // Másodpercek kiszámítása (az 1 percen belüli maradék).
        int seconds = Mathf.FloorToInt(time % 60);

        // Századmásodpercek kiszámítása (az 1 másodpercen belüli rész).
        int centiseconds = Mathf.FloorToInt((time * 100) % 100);

        // Az idő formázása "mm:ss:cs" alakban, nullákkal kitöltve.
        return $"{minutes:00}:{seconds:00}:{centiseconds:00}";
    }
}
