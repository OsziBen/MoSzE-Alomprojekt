using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// A ScoreEntry osztály egy pontszámot, játékosnevet, eltelt időt és dátumot tartalmaz.
[System.Serializable] // Ez a jelölés lehetővé teszi, hogy az osztály példányai megjelenjenek az Unity Editorban.
public class ScoreEntry : MonoBehaviour
{
    public string Name; // A játékos neve.
    public int Score; // A játékos által elért pontszám.
    public float TimeElapsed; // A játék során eltelt idő.
    public string Date; // A pontszám elérésének dátuma, string formátumban.

    // Konstruktor, amely inicializálja a ScoreEntry példány adatait.
    public ScoreEntry(string name, int score, float timeElapsed)
    {
        Name = name; // Beállítja a játékos nevét.
        Score = score; // Beállítja az elért pontszámot.
        TimeElapsed = timeElapsed; // Beállítja az eltelt időt.
        Date = System.DateTime.Now.ToString("yyyy-MM-dd"); // Beállítja a mai dátumot az "ÉÉÉÉ-HH-NN" formátumban.
    }
}

// A Scoreboard osztály egy lista, amely ScoreEntry elemeket tartalmaz.
// Ez az osztály egy egyszerű tároló a játékosok pontszámainak kezelésére.
[System.Serializable] // Ez lehetővé teszi, hogy a Scoreboard példányai megjelenjenek az Unity Editorban.
public class Scoreboard
{
    public List<ScoreEntry> Entries = new List<ScoreEntry>(); // Lista, amely a játékosok pontszámait tárolja.
}
