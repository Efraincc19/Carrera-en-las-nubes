using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode; // Importante para NetworkBehaviour y NetworkManager

public class CloudSpawner : NetworkBehaviour // Cambiado de MonoBehaviour a NetworkBehaviour
{
    public GameObject[] cloudPrefabs;
    private float spawnZ = 0.0f;
    private float tileLength = 20.0f;
    private int safeAmount = 5;
    private List<GameObject> activeClouds = new List<GameObject>();

    [Header("Variación del Camino")]
    public float maxVariacionX = 14.0f;
    public float minAlturaY = -3.0f;
    public float maxAlturaY = -1.2f;

    // Reemplazamos Start por OnNetworkSpawn (La forma correcta en Netcode)
    public override void OnNetworkSpawn()
    {
        // REGLA DE ORO: Solo el servidor genera el mapa
        if (!IsServer) return;

        // Semilla fija para que el camino sea idéntico en cada partida
        Random.InitState(12345);

        for (int i = 0; i < safeAmount; i++)
        {
            SpawnTile();
        }
    }

    void Update()
    {
        // Si no somos el servidor, no hacemos nada aquí. Las nubes nos llegarán por red.
        if (!IsServer) return;

        // === NUEVO PARA MULTIJUGADOR ===
        // Buscamos cuál de todos los jugadores conectados va más adelante en el eje Z
        float maxPlayerZ = -999f;
        bool algúnJugadorConectado = false;

        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (client.PlayerObject != null)
            {
                algúnJugadorConectado = true;
                if (client.PlayerObject.transform.position.z > maxPlayerZ)
                {
                    maxPlayerZ = client.PlayerObject.transform.position.z;
                }
            }
        }

        // Si aún no hay jugadores en la partida, esperamos
        if (!algúnJugadorConectado) return;

        // Generamos más mapa en base al jugador que va ganando la carrera
        if (maxPlayerZ - 9 > (spawnZ - safeAmount * tileLength))
        {
            SpawnTile();
            DeleteTile();
        }
    }

    void SpawnTile()
    {
        // (Solo se ejecuta en el Servidor gracias al filtro del Update/OnNetworkSpawn)
        int randomIndex = Random.Range(0, cloudPrefabs.Length);

        float randomX = Random.Range(-maxVariacionX, maxVariacionX);
        float randomY = Random.Range(minAlturaY, maxAlturaY);

        if (spawnZ == 0.0f)
        {
            randomX = 0f;
            randomY = -2f;
        }

        Vector3 spawnPosition = new Vector3(randomX, randomY, spawnZ);

        // 1. Instanciamos la nube en el servidor
        GameObject go = Instantiate(cloudPrefabs[randomIndex], spawnPosition, Quaternion.identity);

        // 2. ¡EL PASO CLAVE! Le avisamos a Netcode que esta nube existe en la red
        if (go.TryGetComponent<NetworkObject>(out NetworkObject netObj))
        {
            netObj.Spawn(); // Esto hace que aparezca en la pantalla de todos los clientes
        }
        else
        {
            Debug.LogError($"¡Ojo! El prefab de la nube {cloudPrefabs[randomIndex].name} no tiene el componente NetworkObject.");
        }

        activeClouds.Add(go);
        spawnZ += tileLength;
    }

    void DeleteTile()
    {
        // Al destruir un NetworkObject en el servidor, Netcode lo borra automáticamente en los clientes
        if (activeClouds[0] != null)
        {
            Destroy(activeClouds[0]);
        }
        activeClouds.RemoveAt(0);
    }

    public void ResetSpawner()
    {
        if (!IsServer) return; // Solo el servidor puede reiniciar el mapa

        foreach (GameObject cloud in activeClouds)
        {
            if (cloud != null)
            {
                Destroy(cloud); // El Destroy de Unity se encarga de des-spawnear en red
            }
        }

        activeClouds.Clear();
        spawnZ = 0.0f;

        Random.InitState(12345);

        for (int i = 0; i < safeAmount; i++)
        {
            SpawnTile();
        }
    }
}
