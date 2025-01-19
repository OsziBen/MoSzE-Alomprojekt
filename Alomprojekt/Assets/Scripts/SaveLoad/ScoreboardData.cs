using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// A ranglistát reprezentáló osztály
[System.Serializable]
public class ScoreboardData
{
    // A ranglista bejegyzéseit tároló lista
    public List<ScoreboardEntry> scoreboardEntries;

    // Konstruktor, amely inicializálja a ranglistát
    public ScoreboardData()
    {
        // Üres lista létrehozása
        scoreboardEntries = new List<ScoreboardEntry>();
    }
}

// Egy ranglista bejegyzését reprezentáló osztály
[System.Serializable]
public class ScoreboardEntry
{
    // A játék dátuma
    public string date;
    // A játékos neve
    public string playerName;
    // A játékos által szerzett pontok
    public int playerPoints;
    // A játékos végső ideje
    public string finalTime;

    // Konstruktor, amely inicializálja a bejegyzést a megadott paraméterekkel
    public ScoreboardEntry(string date, string name, int score, string time)
    {
        this.date = date;           // Dátum beállítása
        this.playerName = name;     // Játékos név beállítása
        this.playerPoints = score;  // Pontok beállítása
        this.finalTime = time;      // Végső idő beállítása
    }
}
