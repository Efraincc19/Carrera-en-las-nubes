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
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // Buscar una cámara de escena que NO pertenezca a un jugador de red
        Camera[] allCameras = FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Camera cam in allCameras)
        {
            if (cam == null) continue;
            NetworkObject netObj = cam.GetComponentInParent<NetworkObject>();
            if (netObj == null)
            {
                lobbyCameraInstance = cam;
                lobbyCameraObject = cam.gameObject;
                Debug.Log($"[CameraManager] ✓ Cámara de lobby encontrada: {cam.name}");
                return;
            }
        }

        // Si no hay cámara de escena, crear una de lobby
        Debug.LogWarning("[CameraManager] No se encontró cámara de escena, creando una nueva...");
        CreateLobbyCamera();
    }

    private void CreateLobbyCamera()
    {
        lobbyCameraObject = new GameObject("LobbyCamera");
        lobbyCameraInstance = lobbyCameraObject.AddComponent<Camera>();
        lobbyCameraInstance.clearFlags = CameraClearFlags.SolidColor;
        lobbyCameraInstance.backgroundColor = Color.black;
        lobbyCameraInstance.depth = -100;
        lobbyCameraObject.AddComponent<AudioListener>();
    }

    /// <summary>
    /// Verifica si una cámara es la cámara del lobby (comparación por referencia, no por tag)
    /// </summary>
    public bool IsLobbyCamera(Camera cam)
    {
        return cam != null && cam == lobbyCameraInstance;
    }

    public void ActivateLobbyCamera()
    {
        if (lobbyCameraObject != null)
            lobbyCameraObject.SetActive(true);
    }

    public void DeactivateLobbyCamera()
    {
        if (lobbyCameraObject != null)
        {
            lobbyCameraObject.SetActive(false);
            Debug.Log("[CameraManager] Cámara de lobby desactivada");
        }
    }
}
