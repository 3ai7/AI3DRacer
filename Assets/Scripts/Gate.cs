using UnityEngine;

public class Gate : MonoBehaviour
{
    private bool collected = false;

    void OnTriggerEnter(Collider other)
    {
        if (!collected && other.gameObject.CompareTag("Player"))
        {
            collected = true;
            if (GameManager.Instance != null)
                GameManager.Instance.AddScore(1);

            // Optional visual feedback
            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            foreach (Renderer r in renderers)
            {
                if (r.material.HasProperty("_Color"))
                {
                    Color c = r.material.color;
                    c.a = 0.3f;
                    r.material.color = c;
                }
            }

            Destroy(gameObject, 0.5f);
        }
    }
}