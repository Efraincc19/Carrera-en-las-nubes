using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using TMPro;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    [Header("UI Elements")]
    public TMP_InputField ipInputField;
    public Button hostButton;
    public Button joinButton;
    
    [Header("Menu Panel (Arrástralo aquí)")]
    public GameObject menuCanvasObject;

    private GameObject mobileControlsCanvas;

    void Awake()
    {
        // Encontrar el canvas de controles móviles al inicio cuando todavía está activo
        GameObject joystickFondo = GameObject.Find("Joytick_Fondo");
        if (joystickFondo != null)
        {
            Canvas parentCanvas = joystickFondo.GetComponentInParent<Canvas>();
            if (parentCanvas != null)
            {
                mobileControlsCanvas = parentCanvas.gameObject;
                mobileControlsCanvas.SetActive(false); // Ocultar al inicio para que no moleste en el menú
            }
        }
    }

    void Start()
    {
        // Asignar listeners a los botones
        if (hostButton != null)
            hostButton.onClick.AddListener(StartHost);
        
        if (joinButton != null)
            joinButton.onClick.AddListener(StartClient);
        
        // Dirección IP por defecto para localhost
        if (ipInputField != null)
            ipInputField.text = "127.0.0.1";
    }

    private void StartHost()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.StartHost();
            HideMenu();
            ShowMobileControls();
        }
        else
        {
            Debug.LogError("No se encontró el NetworkManager en la escena.");
        }
    }

    private void StartClient()
    {
        if (NetworkManager.Singleton == null) return;

        string ipAddress = ipInputField.text;
        
        // Si no está vacío, configuramos el UnityTransport con la nueva IP
        if (!string.IsNullOrEmpty(ipAddress))
        {
            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            if (transport != null)
            {
                transport.SetConnectionData(ipAddress, 7777);
            }
        }

        NetworkManager.Singleton.StartClient();
        HideMenu();
        ShowMobileControls();
    }

    private void ShowMobileControls()
    {
        if (mobileControlsCanvas != null)
        {
            mobileControlsCanvas.SetActive(true);
        }
    }

    private void HideMenu()
    {
        // Si el usuario nos dijo explícitamente cuál es el menú, lo apagamos:
        if (menuCanvasObject != null)
        {
            menuCanvasObject.SetActive(false);
            return;
        }

        // Buscar el Canvas padre y apagarlo completo (asegura que todo el menú desaparezca)
        Canvas menuCanvas = GetComponentInParent<Canvas>();
        if (menuCanvas != null)
        {
            menuCanvas.gameObject.SetActive(false);
        }
        else
        {
            // Fallback: si no está en un Canvas y no asignó el objeto, apagar este objeto
            gameObject.SetActive(false);
        }
    }
}
