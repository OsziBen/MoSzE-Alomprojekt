using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public class ScoreEntry : MonoBehaviour
{
    public string Name;
    public int Score;
    public float TimeElapsed;
    public string Date;

    public ScoreEntry(string name, int score, float timeElapsed)
    {
        Name = name;
        Score = score;
        TimeElapsed = timeElapsed;
        Date = System.DateTime.Now.ToString("yyyy-MM-dd");
    }
}

[System.Serializable]
public class Scoreboard
{
    public List<ScoreEntry> Entries = new List<ScoreEntry>();
}
