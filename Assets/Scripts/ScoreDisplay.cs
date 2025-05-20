using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ScoreDisplay : MonoBehaviour
{
    public TextMeshProUGUI textDisplay;

    public void UpdateScoreText(Dictionary<string, int> playerScores)
    {
        string output = "Score:\n";

        foreach (KeyValuePair<string, int> entry in playerScores)
        {
            output += $"{entry.Key} - {entry.Value}\n";
        }

        textDisplay.text = output; // 123124124
    }
}
