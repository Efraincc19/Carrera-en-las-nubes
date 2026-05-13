using UnityEngine;
using UnityEngine.SceneManagement; // Necesario para reiniciar escenas
public class Hazard : MonoBehaviour
{
    // Este método se activa automáticamente cuando algo choca con el objeto
    private void OnCollisionEnter(Collision collision)
    {
        // Si lo que nos chocó tiene el Tag "Player"
        if (collision.gameObject.CompareTag("Player"))
        {
            // Reinicia la escena actual
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
}