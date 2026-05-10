using UnityEngine;
using UnityEngine.UI;

public class ScoreUI : MonoBehaviour
{
    public Text scoreText;
    public string scorePrefix = "Score: ";

    void Start()
    {
        if (scoreText == null)
            scoreText = GetComponent<Text>();
    }

    void Update()
    {
        if (scoreText != null && GameManager.Instance != null)
            scoreText.text = scorePrefix + GameManager.Instance.Score;
    }
}