using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public class ScoreboardData
{
    public List<ScoreboardEntry> scoreboardEntries;

    public ScoreboardData()
    {
        scoreboardEntries = new List<ScoreboardEntry>();
    }
}

[System.Serializable]
public class ScoreboardEntry
{
    public string date;
    public string playerName;
    public int playerPoints;
    public string finalTime;

    public ScoreboardEntry(string date, string name, int score, string time)
    {
        this.date = date;
        this.playerName = name;
        this.playerPoints = score;
        this.finalTime = time;
    }
}
