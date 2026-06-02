using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Collections;

public class CloudSpawner : NetworkBehaviour
{
    public GameObject[] cloudPrefabs;
    
    private float tileLength = 20.0f;
    private int safeAmount = 5;
    private const int GOAL_CLOUD_NUMBER = 50; // La nube número 50 es la meta

    [Header("Variación del Camino")]
    public float maxVariacionX = 14.0f;
    public float minAlturaY = -3.0f;
    public float maxAlturaY = -1.2f;

    // Clase para agrupar los datos de la pista de CADA jugador
    private class PlayerTrack
    {
        public float spawnZ = 0.0f;
        public List<GameObject> activeClouds = new List<GameObject>();
        public bool goalSpawned = false; // Flag para saber si ya apareció la meta
    }

    // Diccionario para administrar la pista individual de cada cliente
    private Dictionary<ulong, PlayerTrack> playerTracks = new Dictionary<ulong, PlayerTrack>();

    // Registro de nubes trampa
    private HashSet<GameObject> trapClouds = new HashSet<GameObject>();

    public static CloudSpawner Instance;

    public enum GameState { Lobby, Countdown, Racing, Finished }
    public NetworkVariable<GameState> currentState = new NetworkVariable<GameState>(GameState.Lobby);
    public NetworkVariable<float> countdownTimer = new NetworkVariable<float>(3f);
    public NetworkVariable<FixedString64Bytes> winnerName = new NetworkVariable<FixedString64Bytes>("");

    private void Awake()
    {
        Instance = this;
        // Inicializar el gestor de cámaras
        if (CameraManager.Instance == null)
        {
            Debug.Log("[CloudSpawner] Creando CameraManager...");
            GameObject cameraManagerObj = new GameObject("CameraManager");
            cameraManagerObj.SetActive(true); // Asegurar que está activo
            CameraManager camManager = cameraManagerObj.AddComponent<CameraManager>();
            Debug.Log("[CloudSpawner] CameraManager agregado. Instancia: " + (CameraManager.Instance != null ? "OK" : "NULL"));
        }
        else
        {
            Debug.Log("[CloudSpawner] CameraManager ya existe");
        }
        // Agregar la UI del lobby automáticamente
        gameObject.AddComponent<LobbyUI>();
    }

    public void NotifyPlayerWon(ulong clientId, string name)
    {
        if (!IsServer) return;
        if (currentState.Value != GameState.Racing) return; // Solo gana una vez
        winnerName.Value = name;
        currentState.Value = GameState.Finished;
    }

    public void RequestStartGame()
    {
        if (IsServer && currentState.Value == GameState.Lobby) 
            StartCoroutine(StartGameRoutine());
    }

    private IEnumerator StartGameRoutine()
    {
        currentState.Value = GameState.Countdown;
        countdownTimer.Value = 3f;
        while (countdownTimer.Value > 0)
        {
            yield return null;
            countdownTimer.Value -= Time.deltaTime;
        }
        currentState.Value = GameState.Racing;
        
        // Desactivar la cámara de lobby cuando comienza la carrera
        if (CameraManager.Instance != null)
        {
            CameraManager.Instance.DeactivateLobbyCamera();
        }
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        // Semilla fija opcional
        Random.InitState(12345);
    }

    void Update()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer) return;

        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            ulong id = client.ClientId;

            // Si se conectó un jugador nuevo, le generamos su carril base
            if (!playerTracks.ContainsKey(id))
            {
                playerTracks[id] = new PlayerTrack();
                for (int i = 0; i < safeAmount; i++)
                {
                    SpawnTile(id);
                }
            }

            // Pausar generación dinámica si no estamos corriendo
            if (currentState.Value != GameState.Racing) continue;

            // Si el jugador ya fue instanciado, comprobamos si avanzó
            if (client.PlayerObject != null)
            {
                float playerZ = client.PlayerObject.transform.position.z;
                PlayerTrack track = playerTracks[id];

                // 1. Aseguramos tener nubes generadas hacia ADELANTE (5 nubes), pero solo si la meta no apareció
                while (!track.goalSpawned && track.spawnZ < playerZ + (safeAmount * tileLength))
                {
                    SpawnTile(id);
                }

                // 2. Borrar nubes que han quedado completamente por DETRÁS (5 nubes)
                float deleteThreshold = playerZ - (5f * tileLength);

                while (track.activeClouds.Count > 0)
                {
                    GameObject oldestCloud = track.activeClouds[0];
                    
                    if (oldestCloud == null)
                    {
                        // Si la nube ya fue destruida (ej: era trampa) la sacamos de la cola
                        track.activeClouds.RemoveAt(0);
                        continue;
                    }

                    if (oldestCloud.transform.position.z < deleteThreshold)
                    {
                        DeleteTile(id);
                    }
                    else
                    {
                        // Si la más antigua todavía está en la zona segura, lo dejamos en paz
                        break;
                    }
                }
            }
        }
    }

    void SpawnTile(ulong clientId)
    {
        PlayerTrack track = playerTracks[clientId];
        
        // Calcular el número de nube actual (1-based)
        int cloudNumber = (int)(track.spawnZ / tileLength) + 1;
        
        // Si ya apareció la meta, no generar más nubes
        if (track.goalSpawned)
        {
            return;
        }
        
        int randomIndex = Random.Range(0, cloudPrefabs.Length);

        // Separamos cada carril de jugador por 30 unidades en X basándonos en su ClientId
        float baseOffsetX = clientId * 30f;

        float randomX = Random.Range(-maxVariacionX, maxVariacionX);
        float randomY = Random.Range(minAlturaY, maxAlturaY);

        if (track.spawnZ == 0.0f)
        {
            randomX = 0f; // Primera nube bien centrada en su respectivo carril
            randomY = -2f;
        }

        Vector3 spawnPosition = new Vector3(baseOffsetX + randomX, randomY, track.spawnZ);

        GameObject go = Instantiate(cloudPrefabs[randomIndex], spawnPosition, Quaternion.identity);

        bool isTrap = false;
        bool isGoal = false;
        
        // Detectar si es la nube meta (nube número 50)
        if (cloudNumber == GOAL_CLOUD_NUMBER)
        {
            isGoal = true;
            track.goalSpawned = true;
            Debug.Log($"¡Meta aparecida para el jugador {clientId}! Nube número {cloudNumber}");
        }
        // Solo pueden ser trampa si no es la primera nube y no es la meta
        else if (track.spawnZ > 0.0f && Random.value < 0.3f) // 30% de probabilidad
        {
            isTrap = true;
        }

        if (go.TryGetComponent<NetworkObject>(out NetworkObject netObj))
        {
            netObj.Spawn();
            if (isGoal)
            {
                // Aplicar color amarillo a la meta
                ApplyGoalStyleLocally(go);
                StartCoroutine(ApplyGoalStyleRoutine(netObj));
            }
            else if (isTrap)
            {
                trapClouds.Add(go);
                ApplyStyleLocally(go);
                StartCoroutine(ApplyTrapStyleRoutine(netObj));
            }
        }
        else
        {
            Debug.LogError($"¡Ojo! El prefab de la nube {cloudPrefabs[randomIndex].name} no tiene NetworkObject.");
        }

        track.activeClouds.Add(go);
        track.spawnZ += tileLength;
    }

    private void ApplyStyleLocally(GameObject go)
    {
        foreach (Renderer r in go.GetComponentsInChildren<Renderer>())
        {
            Material[] m = r.materials;
            for (int i = 0; i < m.Length; i++)
            {
                Material original = m[i];
                // Buscamos un shader universal que soporte color en Unity (URP o Built-in)
                Shader litShader = Shader.Find("Universal Render Pipeline/Lit");
                if (litShader == null) litShader = Shader.Find("Standard");

                if (litShader != null)
                {
                    Material trampaMat = new Material(litShader);

                    // Mantenemos la textura base si existe
                    if (original.HasProperty("_MainTex"))
                        trampaMat.SetTexture("_MainTex", original.GetTexture("_MainTex"));
                    else if (original.HasProperty("_BaseMap"))
                        trampaMat.SetTexture("_BaseMap", original.GetTexture("_BaseMap"));

                    // Aplicamos el tinte gris oscuro
                    if (trampaMat.HasProperty("_Color")) trampaMat.color = new Color(0.3f, 0.3f, 0.3f);
                    if (trampaMat.HasProperty("_BaseColor")) trampaMat.SetColor("_BaseColor", new Color(0.3f, 0.3f, 0.3f));

                    m[i] = trampaMat;
                }
            }
            r.materials = m;
        }
    }

    private void ApplyGoalStyleLocally(GameObject go)
    {
        foreach (Renderer r in go.GetComponentsInChildren<Renderer>())
        {
            Material[] m = r.materials;
            for (int i = 0; i < m.Length; i++)
            {
                Material original = m[i];
                // Buscamos un shader universal que soporte color en Unity (URP o Built-in)
                Shader litShader = Shader.Find("Universal Render Pipeline/Lit");
                if (litShader == null) litShader = Shader.Find("Standard");

                if (litShader != null)
                {
                    Material goalMat = new Material(litShader);

                    // Mantenemos la textura base si existe
                    if (original.HasProperty("_MainTex"))
                        goalMat.SetTexture("_MainTex", original.GetTexture("_MainTex"));
                    else if (original.HasProperty("_BaseMap"))
                        goalMat.SetTexture("_BaseMap", original.GetTexture("_BaseMap"));

                    // Aplicamos color amarillo brillante
                    if (goalMat.HasProperty("_Color")) goalMat.color = new Color(1f, 1f, 0f, 1f); // Amarillo
                    if (goalMat.HasProperty("_BaseColor")) goalMat.SetColor("_BaseColor", new Color(1f, 1f, 0f, 1f));

                    m[i] = goalMat;
                }
            }
            r.materials = m;
        }
    }

    IEnumerator ApplyTrapStyleRoutine(NetworkObjectReference cloudRef)
    {
        // Dar tiempo a los clientes a que instancien el objeto por red antes del RPC
        yield return new WaitForSeconds(0.5f);
        ApplyTrapStyleClientRpc(cloudRef);
    }

    IEnumerator ApplyGoalStyleRoutine(NetworkObjectReference cloudRef)
    {
        // Dar tiempo a los clientes a que instancien el objeto por red antes del RPC
        yield return new WaitForSeconds(0.5f);
        ApplyGoalStyleClientRpc(cloudRef);
    }

    void DeleteTile(ulong clientId)
    {
        PlayerTrack track = playerTracks[clientId];
        if (track.activeClouds.Count > 0)
        {
            if (track.activeClouds[0] != null)
            {
                trapClouds.Remove(track.activeClouds[0]);
                Destroy(track.activeClouds[0]);
            }
            track.activeClouds.RemoveAt(0);
        }
    }

    [ClientRpc]
    void ApplyTrapStyleClientRpc(NetworkObjectReference cloudRef)
    {
        // En los clientes a veces el RPC llega antes que la nube, por eso usamos delay
        if (cloudRef.TryGet(out NetworkObject netObj))
        {
            ApplyStyleLocally(netObj.gameObject);
        }
    }

    [ClientRpc]
    void ApplyGoalStyleClientRpc(NetworkObjectReference cloudRef)
    {
        // En los clientes a veces el RPC llega antes que la nube, por eso usamos delay
        if (cloudRef.TryGet(out NetworkObject netObj))
        {
            ApplyGoalStyleLocally(netObj.gameObject);
        }
    }

    public void CheckIfTrap(GameObject cloud)
    {
        // Esto solo lo llama el servidor
        if (trapClouds.Contains(cloud))
        {
            trapClouds.Remove(cloud);
            StartCoroutine(DestroyTrapRoutine(cloud));
        }
    }

    public Vector3 GetSafeRespawnPosition(ulong clientId, float currentZ)
    {
        float myOffsetX = clientId * 30f;
        if (playerTracks.TryGetValue(clientId, out PlayerTrack track))
        {
            float bestZ = currentZ;
            float minDiff = float.MaxValue;
            foreach (var cloud in track.activeClouds)
            {
                if (cloud == null) continue;
                float diff = Mathf.Abs(cloud.transform.position.z - currentZ);
                if (diff < minDiff)
                {
                    minDiff = diff;
                    bestZ = cloud.transform.position.z;
                }
            }
            return new Vector3(myOffsetX, 5f, bestZ);
        }
        return new Vector3(myOffsetX, 5f, Mathf.Max(0, currentZ));
    }

    private IEnumerator DestroyTrapRoutine(GameObject cloud)
    {
        yield return new WaitForSeconds(3f);
        if (cloud != null)
        {
            // Lo quitamos de la lista base para evitar errores al intentar borrarlo otra vez
            foreach (var track in playerTracks.Values)
            {
                if (track.activeClouds.Contains(cloud))
                {
                    track.activeClouds.Remove(cloud);
                    break;
                }
            }
            
            if (cloud.TryGetComponent<NetworkObject>(out var netObj))
            {
                netObj.Despawn();
            }
            Destroy(cloud);
        }
    }

    public void ResetSpawner()
    {
        if (!IsServer) return; 

        // Destruimos todas las nubes de todos los carriles
        foreach (var track in playerTracks.Values)
        {
            foreach (GameObject cloud in track.activeClouds)
            {
                trapClouds.Remove(cloud);
                if (cloud != null) Destroy(cloud);
            }
        }

        playerTracks.Clear();
        Random.InitState(12345);
        // Los carriles se volverán a armar en Update automáticamente
    }
}
