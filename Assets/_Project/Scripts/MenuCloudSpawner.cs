using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;

public class MenuCloudSpawner : NetworkBehaviour // (No necesita Netcode, pero lo dejamos por si acaso)
{
    [Header("Prefabs de las Nubes")]
    public GameObject[] nubesPrefabs;

    [Header("Configuración del Tiempo")]
    public float tiempoEntreNubes = 3f;

    [Header("Posiciones (RELATIVAS al Spawner, ¡NUEVO!)")]
    [Tooltip("Aparecen a +/- X de la posición de este objeto")]
    public float rangoSpawnX = 50f;     // Rango total de aparición
    [Tooltip("Aparecen a +/- Y de la posición de este objeto")]
    public float rangoSpawnY = 10f;      // Rango total de altura
    [Tooltip("Límite Z para que pasen por detrás de la cámara (Relativo a Z de este objeto)")]
    public float limitXCancela = 60f;   // Dónde mueren (Tiene que ser más grande que rangoSpawnX)

    [Header("Velocidad de las Nubes")]
    public float velocidadMinima = 1.5f;
    public float velocidadMaxima = 3.5f;

    void Start()
    {
        // Solo corre en local para el menú
        InvokeRepeating(nameof(SpawnearNube), 0.5f, tiempoEntreNubes);

        // Spawn inicial aleatorio
        for (int i = 0; i < 3; i++)
        {
            SpawnearNubeEnPosicionAleatoria();
        }
    }

    void SpawnearNube()
    {
        if (nubesPrefabs == null || nubesPrefabs.Length == 0) return;

        int indiceAleatorio = Random.Range(0, nubesPrefabs.Length);

        // === LOGICA RELATIVA ===
        // 1. Calculamos la posición relativa al centro de este objeto
        float alturaAleatoria = transform.position.y + Random.Range(-rangoSpawnY, rangoSpawnY);
        // Empieza a la IZQUIERDA de la posición de este objeto
        float xInicio = transform.position.x - rangoSpawnX;

        Vector3 posicionSpawn = new Vector3(xInicio, alturaAleatoria, transform.position.z);

        // 2. Crear el clon
        GameObject nuevaNube = Instantiate(nubesPrefabs[indiceAleatorio], posicionSpawn, Quaternion.identity);

        // 3. Inyectar movimiento
        MenuCloud componenteMovimiento = nuevaNube.AddComponent<MenuCloud>();
        float velocidadAleatoria = Random.Range(velocidadMinima, velocidadMaxima);

        // El límite absoluto de destrucción es relativo a este objeto
        componenteMovimiento.Inicializar(velocidadAleatoria, transform.position.x + limitXCancela);
    }

    // Truco visual para el inicio
    void SpawnearNubeEnPosicionAleatoria()
    {
        if (nubesPrefabs == null || nubesPrefabs.Length == 0) return;

        int indiceAleatorio = Random.Range(0, nubesPrefabs.Length);
        float xAleatoria = transform.position.x + Random.Range(-rangoSpawnX, rangoSpawnX);
        float alturaAleatoria = transform.position.y + Random.Range(-rangoSpawnY, rangoSpawnY);

        Vector3 posicionSpawn = new Vector3(xAleatoria, alturaAleatoria, transform.position.z);
        GameObject nuevaNube = Instantiate(nubesPrefabs[indiceAleatorio], posicionSpawn, Quaternion.identity);

        MenuCloud componenteMovimiento = nuevaNube.AddComponent<MenuCloud>();
        float velocidadAleatoria = Random.Range(velocidadMinima, velocidadMaxima);
        componenteMovimiento.Inicializar(velocidadAleatoria, transform.position.x + limitXCancela);
    }
}