using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
    public float maxVariacionX = 6.0f; // Qué tan a la izquierda o derecha pueden aparecer
    public float minAlturaY = 0.5f;    // Altura mínima de la nube
    public float maxAlturaY = 3.5f;    // Altura máxima de la nube

    void Start()
    {
        for (int i = 0; i < safeAmount; i++)
        {
            SpawnTile();
        }
    }

    void Update()
    {
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

        // 3. Creamos un vector de posición final combinando:
        //    X aleatoria, Y aleatoria, y tu Z que sigue avanzando recto de 20 en 20
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
        
        // Volver a instanciar las nubes iniciales
        for (int i = 0; i < safeAmount; i++)
        {
            SpawnTile();
        }
    }
}
