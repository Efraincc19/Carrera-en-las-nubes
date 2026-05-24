using UnityEngine;
using Unity.Netcode;

public class NubeMovil : NetworkBehaviour
{
    [Header("Configuración de Movimiento")]
    public float velocidad = 3f;
    public float distancia = 14f;

    private Vector3 posicionInicial;
    private bool posicionRegistrada = false; // Nos asegura que ya fue acomodada por el Spawner

    void FixedUpdate()
    {
        if (!IsServer) return;

        // Si es el primer cuadro, esperamos a que el Spawner la haya puesto en su sitio real
        if (!posicionRegistrada)
        {
            posicionInicial = transform.position;
            posicionRegistrada = true;
        }

        // Movimiento vaivén en el eje X
        float movimientoX = Mathf.PingPong(Time.time * velocidad, distancia * 2) - distancia;

        transform.position = new Vector3(posicionInicial.x + movimientoX, posicionInicial.y, posicionInicial.z);
    }
}
