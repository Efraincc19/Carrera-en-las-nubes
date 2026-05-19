    using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class CloudSpawner : MonoBehaviour
{
    public GameObject[] cloudPrefabs; // Arrastra tus prefabs de nubes aquí
    public Transform playerTransform; // Arrastra al Jugador aquí
    private float spawnZ = 0.0f; // Posición en Z donde se creará la siguiente nube
    private float tileLength = 20.0f; // Qué tan larga es cada sección de nube
    private int safeAmount = 5; // Cuántas nubes habrá siempre en pantalla
    private List<GameObject> activeClouds = new List<GameObject>();
    void Start()
    {
        // Generar las primeras nubes al iniciar
        for (int i = 0; i < safeAmount; i++)
        {
            SpawnTile();
        }
    }
    void Update()
    {
        // Si el jugador se acerca al final de las nubes existentes, crea una nueva y borra la vieja
        if (playerTransform.position.z - 10 > (spawnZ - safeAmount * tileLength))
        {
            SpawnTile();
            DeleteTile();
        }
    }
    void SpawnTile()
    {
        // Elige una nube aleatoria del array
        int randomIndex = Random.Range(0, cloudPrefabs.Length);
        GameObject go = Instantiate(cloudPrefabs[randomIndex], transform.forward * spawnZ, Quaternion.identity);
        activeClouds.Add(go);
        spawnZ += tileLength;
    }
    void DeleteTile()
    {
        Destroy(activeClouds[0]);
        activeClouds.RemoveAt(0);
    }
}
