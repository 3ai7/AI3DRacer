using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public int Score { get; private set; } = 0;
    public bool IsGameOver { get; private set; } = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        IsGameOver = false;
        Score = 0;
    }

    public void AddScore(int points)
    {
        if (IsGameOver) return;
        Score += points;
        Debug.Log("Score: " + Score);
    }

    public void GameOver()
    {
        if (IsGameOver) return;
        IsGameOver = true;
        Debug.Log("Game Over! Final Score: " + Score);

        Car car = FindObjectOfType<Car>();
        if (car != null)
            car.FallApart();
    }
}