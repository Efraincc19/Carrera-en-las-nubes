using UnityEngine;

public class MenuCloud : MonoBehaviour
{
    private float speed;
    private float limitX;

    // Este método lo llamará el Spawner para darle la velocidad y el límite a cada nube
    public void Inicializar(float velocidad, float limiteDerecho)
    {
        speed = velocidad;
        limitX = limiteDerecho;
    }

    void Update()
    {
        // 1. Mover hacia la derecha
        transform.Translate(Vector3.right * speed * Time.deltaTime, Space.World);

        // 2. Si sale de la pantalla, se destruye para no saturar la memoria
        if (transform.position.x > limitX)
        {
            Destroy(gameObject);
        }
    }
}
