using UnityEngine;
using Unity.Netcode;

public class CameraManager : MonoBehaviour
{
    public static CameraManager Instance;
    
    private Camera lobbyCameraInstance;
    private GameObject lobbyCameraObject;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.Log("[CameraManager] Ya existe una instancia, destruyendo esta...");
            Destroy(gameObject);
            return;
        }
        Instance = this;
        Debug.Log("[CameraManager] ✓ Instancia inicializada");
    }

    private void Start()
    {
        try
        {
            Debug.Log("[CameraManager Start] ✓ Iniciando búsqueda de cámara de lobby...");
            
            // Buscar o crear una cámara de lobby que se mantenga activa
            Camera[] allCameras = FindObjectsOfType<Camera>(true);
            Debug.Log($"[CameraManager] Total de cámaras en escena: {allCameras.Length}");
            
            foreach (Camera cam in allCameras)
            {
                if (cam == null) continue;
                NetworkObject netObj = cam.GetComponentInParent<NetworkObject>();
                Debug.Log($"[CameraManager] Analizando cámara: {cam.name}, tieneNetworkObject: {(netObj != null)}");
                
                if (netObj == null)
                {
                    lobbyCameraInstance = cam;
                    Debug.Log($"[CameraManager] ✓ Cámara de lobby encontrada: {cam.name}");
                    break;
                }
            }

            if (lobbyCameraInstance != null)
            {
                lobbyCameraObject = lobbyCameraInstance.gameObject;
                // Marcamos que esta es la cámara del lobby para no desactivarla
                lobbyCameraObject.tag = "LobbyCamera";
                Debug.Log($"[CameraManager] Cámara marcada como LobbyCamera");
            }
            else
            {
                // Si no hay cámara de escena, crear una de lobby
                Debug.LogWarning("[CameraManager] ⚠ No se encontró cámara de escena, creando una nueva...");
                CreateLobbyCamera();
            }
            
            Debug.Log("[CameraManager Start] ✓ Start completado exitosamente");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[CameraManager Start] ❌ ERROR en Start: {ex.Message}\n{ex.StackTrace}");
        }
    }

    private void CreateLobbyCamera()
    {
        lobbyCameraObject = new GameObject("LobbyCamera");
        lobbyCameraInstance = lobbyCameraObject.AddComponent<Camera>();
        lobbyCameraInstance.clearFlags = CameraClearFlags.SolidColor;
        lobbyCameraInstance.backgroundColor = Color.black;
        lobbyCameraInstance.depth = -100; // Profundidad baja para que otras cámaras la sobreescriban
        lobbyCameraObject.tag = "LobbyCamera";
        
        AudioListener audioListener = lobbyCameraObject.AddComponent<AudioListener>();
        audioListener.enabled = true;
        
        Debug.Log("[CameraManager] ✓ Cámara de lobby creada");
    }

    /// <summary>
    /// Verifica si una cámara es la cámara del lobby
    /// </summary>
    public bool IsLobbyCamera(Camera cam)
    {
        return cam != null && cam.CompareTag("LobbyCamera");
    }

    /// <summary>
    /// Verifica si una cámara pertenece a un jugador de red
    /// </summary>
    public bool IsPlayerCamera(Camera cam)
    {
        if (cam == null) return false;
        NetworkObject netObj = cam.GetComponentInParent<NetworkObject>();
        return netObj != null;
    }

    /// <summary>
    /// Activa la cámara del lobby
    /// </summary>
    public void ActivateLobbyCamera()
    {
        if (lobbyCameraObject != null)
            lobbyCameraObject.SetActive(true);
    }

    /// <summary>
    /// Desactiva la cámara del lobby
    /// </summary>
    public void DeactivateLobbyCamera()
    {
        if (lobbyCameraObject != null)
            lobbyCameraObject.SetActive(false);
    }
}
