using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode; // For finding local player

public class CloudSpawner : MonoBehaviour
{
    public GameObject[] cloudPrefabs;
    public Transform playerTransform;
    private float spawnZ = 0.0f;
    private float tileLength = 20.0f;
    private int safeAmount = 5;
    private List<GameObject> activeClouds = new List<GameObject>();

    [Header("Variación del Camino")]
    // Modifica estos valores en el inspector para controlar qué tan esparcidas están
    public float maxVariacionX = 14.0f; // Qué tan a la izquierda o derecha pueden aparecer
    public float minAlturaY = -3.0f;   // Altura mínima (ahora debajo del mundo)
    public float maxAlturaY = -1.2f;   // Altura máxima (ahora debajo del mundo)

    void Start()
    {
        // Usamos una semilla fija para que la aleatoriedad sea igual en todos los dispositivos
        // y todos los jugadores tengan exactamente el mismo camino de nubes
        Random.InitState(12345);

        for (int i = 0; i < safeAmount; i++)
        {
            SpawnTile();
        }
    }

    void Update()
    {
        // Si no tenemos rastreado a nuestro jugador, intentamos buscar el jugador local de Netcode
        if (playerTransform == null && NetworkManager.Singleton != null && NetworkManager.Singleton.IsConnectedClient)
        {
            var localPlayerObj = NetworkManager.Singleton.LocalClient.PlayerObject;
            if (localPlayerObj != null)
            {
                playerTransform = localPlayerObj.transform;
            }
        }

        // Si seguimos sin nuestro jugador (ej: aún no ha cargado/conectado), salimos
        if (playerTransform == null) return;

        if (playerTransform.position.z - 9 > (spawnZ - safeAmount * tileLength))
        {
            SpawnTile();
            DeleteTile();
        }
    }

    void SpawnTile()
    {
        int randomIndex = Random.Range(0, cloudPrefabs.Length);

        // 1. Calculamos una desviación aleatoria para los lados (Izquierda / Derecha)
        float randomX = Random.Range(-maxVariacionX, maxVariacionX);

        // 2. Calculamos una altura aleatoria (Arriba / Abajo)
        float randomY = Random.Range(minAlturaY, maxAlturaY);

        // === NUEVO ===
        // Si es la PRIMERA nube de la carrera (spawnZ = 0), la forzamos a aparecer 
        // exactamente en el centro y a una altura perfecta para aterrizar.
        if (spawnZ == 0.0f)
        {
            randomX = 0f;
            randomY = -2f; // Un poco debajo del jugador
        }

        // 3. Creamos un vector de posición final combinando:
        Vector3 spawnPosition = new Vector3(randomX, randomY, spawnZ);

        // 4. Instanciamos la nube en esa nueva posición variada
        GameObject go = Instantiate(cloudPrefabs[randomIndex], spawnPosition, Quaternion.identity);

        activeClouds.Add(go);
        spawnZ += tileLength;
    }

    void DeleteTile()
    {
        Destroy(activeClouds[0]);
        activeClouds.RemoveAt(0);
    }

    public void ResetSpawner()
    {
        // Destruir todas las nubes activas
        foreach (GameObject cloud in activeClouds)
        {
            if (cloud != null)
            {
                Destroy(cloud);
            }
        }
        
        // Limpiar la lista y reiniciar la posición Z
        activeClouds.Clear();
        spawnZ = 0.0f;
        
        // Repetimos la semilla para que vuelva a ser idéntico al iniciar
        Random.InitState(12345);

        // Volver a instanciar las nubes iniciales
        for (int i = 0; i < safeAmount; i++)
        {
            SpawnTile();
        }
    }
}
