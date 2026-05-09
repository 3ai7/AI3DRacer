using UnityEngine;

public class Obstacle : MonoBehaviour
{
    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            if (GameManager.Instance != null)
                GameManager.Instance.GameOver();
        }
    }
}