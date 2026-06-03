using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;
using Unity.Collections;
using TMPro;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : NetworkBehaviour
{
    [Header("Movimiento")]
    public float moveSpeed = 8f;
    [Tooltip("Qué tan rápido frena al soltar las teclas.")]
    public float frenadoInercia = 3f;

    [Header("Salto")]
    public float jumpForce = 8f;
    public float jumpCooldown = 0.5f;
    public LayerMask groundLayer;
    public Transform groundCheck;
    public float groundDistance = 0.2f;

    [Header("Cámara Multijugador")]
    public Camera playerCamera;
    public AudioListener playerAudioListener;

    [Header("Ajuste 3ra Persona Automático")]
    public Vector3 offsetCamara = new Vector3(0, 5.5f, -10f);
    public Vector3 rotacionCamara = new Vector3(20f, 0, 0);

    [Header("Control Táctil de Cámara")]
    public float sensibilidadCamara = 0.2f;
    private float currentCameraRotX;
    private float currentCameraRotY;

    [Header("HUD de Puntuación")]
    public GameObject hudCanvas;
    public TMP_Text scoreText;
    private List<float> nubePosicionesZ = new List<float>(); // Posiciones Z de las nubes para chequeo de puntos
    private float lastProcessedCloudZ = -100f; // Última posición Z de nube procesada para contar puntos

    // Score sincronizado en red para que todos vean el marcador
    public NetworkVariable<int> score = new NetworkVariable<int>(0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    [Header("Efectos de Sonido")]
    public AudioSource audioSource;
    public AudioClip sonidoPunto;

    private int saltosRestantes = 2;
    private PlayerInput playerInput;
    private Vector2 moveInput;
    private Rigidbody rb;
    private bool isGrounded;
    private bool wantsToJump;
    private float nextJumpTime;

    // Nombre del jugador sincronizado por red
    public NetworkVariable<FixedString32Bytes> playerName =
        new NetworkVariable<FixedString32Bytes>("Jugador",
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

    [ServerRpc(RequireOwnership = false)]
    public void SetPlayerNameServerRpc(FixedString32Bytes name)
    {
        playerName.Value = name;
    }

    public override void OnNetworkSpawn()
    {
        // Calcular el carril que le toca a este jugador basado en su ClientId
        float myOffsetX = OwnerClientId * 30f;
        if (IsServer || IsOwner)
        {
            Vector3 pos = new Vector3(myOffsetX, 5f, 0f);
            transform.position = pos;
            if (rb == null) rb = GetComponent<Rigidbody>();
            if (rb != null) rb.position = pos;
        }

        Debug.Log($"[PlayerController OnNetworkSpawn] ClientId={OwnerClientId}, IsOwner={IsOwner}, IsServer={IsServer}");

        // ── Asignar skin según rol: Host = Hollow Knight, Invitado = Hornet (Silksong) ──
        ApplyCharacterSkin();

        // Asegurar que encontramos la cámara
        if (playerCamera == null)
        {
            playerCamera = GetComponentInChildren<Camera>(true);
            Debug.Log($"[PlayerController] Buscando cámara hija... Encontrada: {(playerCamera != null ? playerCamera.name : "NINGUNA")}");
        }

        if (playerAudioListener == null && playerCamera != null)
            playerAudioListener = playerCamera.GetComponent<AudioListener>();

        // Habilitar el PlayerInput solo para el dueño, y desactivarlo para clones
        if (playerInput != null)
            playerInput.enabled = IsOwner;

        if (IsOwner)
        {
            // Configurar cámara con un frame de delay para evitar race conditions
            StartCoroutine(SetupCameraDelayed());

            if (hudCanvas != null)
                hudCanvas.SetActive(true);
            UpdateScoreUI();
        }
        else
        {
            // Este jugador NO es el dueño — desactivar su cámara y HUD
            if (playerCamera != null)
                playerCamera.gameObject.SetActive(false);
            
            if (playerAudioListener != null)
                playerAudioListener.enabled = false;
            
            if (hudCanvas != null)
                hudCanvas.SetActive(false);
        }
    }

    /// <summary>
    /// Configura la cámara del jugador con un frame de delay para que todos los
    /// NetworkObjects estén spawneados antes de buscar y desactivar otras cámaras.
    /// </summary>
    private IEnumerator SetupCameraDelayed()
    {
        yield return null; // Esperar un frame

        if (!IsOwner) yield break;

        // Buscar la cámara si no está asignada
        if (playerCamera == null)
            playerCamera = GetComponentInChildren<Camera>(true);

        if (playerAudioListener == null && playerCamera != null)
            playerAudioListener = playerCamera.GetComponent<AudioListener>();

        if (playerCamera == null)
        {
            Debug.LogError($"[PlayerController] ❌ NO se encontró Camera en el prefab del jugador {OwnerClientId}");
            yield break;
        }

        // Desactivar cámara de lobby
        if (CameraManager.Instance != null)
            CameraManager.Instance.DeactivateLobbyCamera();

        // Activar MI cámara
        playerCamera.gameObject.SetActive(true);
        playerCamera.enabled = true;
        currentCameraRotX = rotacionCamara.x;
        currentCameraRotY = rotacionCamara.y;

        if (playerAudioListener != null)
            playerAudioListener.enabled = true;

        // Desactivar cámaras de OTROS jugadores (solo el componente Camera, no el GameObject)
        Camera[] allCameras = FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Camera cam in allCameras)
        {
            if (cam == playerCamera) continue;
            if (CameraManager.Instance != null && CameraManager.Instance.IsLobbyCamera(cam)) continue;

            NetworkObject camNetObj = cam.GetComponentInParent<NetworkObject>();
            if (camNetObj != null && camNetObj != GetComponent<NetworkObject>())
            {
                cam.enabled = false; // Solo desactivar componente, no el GameObject entero
                AudioListener al = cam.GetComponent<AudioListener>();
                if (al != null) al.enabled = false;
            }
        }

        Debug.Log($"[PlayerController] ✓ Cámara configurada para jugador {OwnerClientId}");
    }

    /// <summary>
    /// Asigna la skin correcta según el rol del jugador.
    /// Host (ServerClientId) = Hollow Knight (el Caballero)
    /// Invitados = Hornet (Silksong)
    /// Se ejecuta en TODAS las copias para que todos vean las skins correctas.
    /// </summary>
    private void ApplyCharacterSkin()
    {
        // Limpiar skins existentes para evitar duplicados
        var existingKnight = GetComponent<HollowKnightSkin>();
        if (existingKnight != null) Destroy(existingKnight);
        var existingHornet = GetComponent<SilksongSkin>();
        if (existingHornet != null) Destroy(existingHornet);

        // Host (ClientId 0 / ServerClientId) → Caballero de Hollow Knight
        // Invitados → Hornet de Silksong
        if (OwnerClientId == NetworkManager.ServerClientId)
        {
            gameObject.AddComponent<HollowKnightSkin>();
            Debug.Log($"[PlayerController] Skin asignada: Hollow Knight (Host) para jugador {OwnerClientId}");
        }
        else
        {
            gameObject.AddComponent<SilksongSkin>();
            Debug.Log($"[PlayerController] Skin asignada: Hornet/Silksong (Invitado) para jugador {OwnerClientId}");
        }
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        playerInput = GetComponent<PlayerInput>();
        if (playerCamera == null)
            playerCamera = GetComponentInChildren<Camera>(true);
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        
        Debug.Log($"[PlayerController Awake] Prefab inicializado. Camera encontrada: {(playerCamera != null ? playerCamera.name : "NINGUNA")}");
    }


    void Start()
    {
        rb.freezeRotation = true;
    }

    [ServerRpc(RequireOwnership = false)]
    private void SubmitMoveServerRpc(Vector2 input, ServerRpcParams rpcParams = default)
    {
        if (rpcParams.Receive.SenderClientId == OwnerClientId)
            moveInput = input;
    }

    [ServerRpc(RequireOwnership = false)]
    private void SubmitJumpServerRpc(ServerRpcParams rpcParams = default)
    {
        if (rpcParams.Receive.SenderClientId == OwnerClientId)
            TryRequestJump();
    }

    public void OnMove(InputValue value)
    {
        if (!IsOwner) return;
        Vector2 input = value.Get<Vector2>();
        moveInput = input; // Lo aplicamos localmente también
        if (!IsServer) SubmitMoveServerRpc(input);
    }

    public void OnJump(InputValue value)
    {
        if (!IsOwner) return;
        if (value.isPressed)
        {
            TryLocalJump();
            if (!IsServer) SubmitJumpServerRpc();
        }
    }

    private void TryLocalJump()
    {
        // Para predecir localmente (opcional) pero garantizamos que llegue al server
        if (IsServer) TryRequestJump();
    }

    private void TryRequestJump()
    {
        // Solo el servidor lo debería validar
        if (!IsServer) return;
        if (saltosRestantes > 0)
        {
            if (saltosRestantes == 2 && Time.time < nextJumpTime) return;
            wantsToJump = true;
        }
    }

    void Update()
    {
        // El chequeo del piso lo hace el Servidor
        if (!IsServer) return;

        if (groundCheck != null)
        {
            isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundLayer);
        }
        else
        {
            isGrounded = Physics.Raycast(transform.position, Vector3.down, groundDistance + 0.1f, groundLayer);
        }

        // === SISTEMA DE PUNTOS BASADO EN POSICIÓN Z ===
        CheckCloudPointsBasedOnZPosition();
    }

    private void CheckCloudPointsBasedOnZPosition()
    {
        // Usar la lista cacheada de nubes del propio carril en vez de buscar por tag cada frame
        if (CloudSpawner.Instance == null) return;
        List<GameObject> myClouds = CloudSpawner.Instance.GetCloudsForPlayer(OwnerClientId);

        float currentPlayerZ = transform.position.z;
        float pointGiveThreshold = 2f;

        foreach (GameObject cloud in myClouds)
        {
            if (cloud == null) continue;
            float cloudZ = cloud.transform.position.z;

            if (currentPlayerZ > cloudZ + pointGiveThreshold && cloudZ > lastProcessedCloudZ)
            {
                score.Value++;
                lastProcessedCloudZ = cloudZ;
                UpdateScoreClientRpc(score.Value);

                if (audioSource != null && sonidoPunto != null)
                    audioSource.PlayOneShot(sonidoPunto);

                if (score.Value >= 50 && CloudSpawner.Instance != null)
                {
                    CloudSpawner.Instance.NotifyPlayerWon(OwnerClientId, playerName.Value.ToString());
                }
            }
        }
    }

    void FixedUpdate()
    {
        // En Netcode por defecto las físicas son del Servidor
        if (!IsServer) return;

        if (rb.position.y < -10f)
        {
            Vector3 safePos;
            if (CloudSpawner.Instance != null)
            {
                safePos = CloudSpawner.Instance.GetSafeRespawnPosition(OwnerClientId, rb.position.z);
            }
            else
            {
                safePos = new Vector3(OwnerClientId * 30f, 5f, Mathf.Max(0, rb.position.z));
            }
            rb.position = safePos;
            rb.linearVelocity = Vector3.zero;
        }

        Vector3 moveDirection = (transform.forward * moveInput.y) + (transform.right * moveInput.x);
        Vector3 targetVelocityH = moveDirection * moveSpeed;

        Vector3 currentVelocityH = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        Vector3 smoothedVelocityH = Vector3.Lerp(currentVelocityH, targetVelocityH, Time.fixedDeltaTime * frenadoInercia);

        rb.linearVelocity = new Vector3(smoothedVelocityH.x, rb.linearVelocity.y, smoothedVelocityH.z);

        // === ¡MÁGICA MODIFICACIÓN DEL SALTO DOBLE! ===
        if (wantsToJump)
        {
            // Si saltosRestantes es igual a 1, significa que ya gastamos el primero,
            // por lo tanto, este es el segundo salto y va a la mitad de fuerza.
            float fuerzaDelSaltoActual = (saltosRestantes == 1) ? jumpForce * 0.6f : jumpForce;

            rb.linearVelocity = new Vector3(rb.linearVelocity.x, fuerzaDelSaltoActual, rb.linearVelocity.z);
            wantsToJump = false;
            saltosRestantes--;

            if (saltosRestantes == 1)
            {
                nextJumpTime = Time.time + jumpCooldown;
            }
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!IsServer) return;

        bool esNube = collision.gameObject.CompareTag("Cloud") ||
                     (collision.transform.parent != null && collision.transform.parent.CompareTag("Cloud"));

        if (esNube)
        {
            saltosRestantes = 2;

            GameObject objetoNube = collision.gameObject.CompareTag("Cloud") ? collision.gameObject : collision.transform.parent.gameObject;
            
            // Avisar al Spawner por si es una nube trampa
            if (CloudSpawner.Instance != null)
            {
                CloudSpawner.Instance.CheckIfTrap(objetoNube);
            }
            // Nota: La suma de puntos se maneja ahora en CheckCloudPointsBasedOnZPosition()
        }
    }

    [ClientRpc]
    private void UpdateScoreClientRpc(int newScore)
    {
        UpdateScoreUI(newScore);
    }

    void UpdateScoreUI(int val = -1)
    {
        if (val < 0) val = score.Value;
        if (scoreText != null)
            scoreText.text = "Nubes: " + val;
    }

    public void RestablecerSaltos()
    {
        saltosRestantes = 2;
        moveInput = Vector2.zero;
    }

    public void RestablecerPuntos()
    {
        score.Value = 0;
        lastProcessedCloudZ = -100f;
        nubePosicionesZ.Clear();
        UpdateScoreUI();
    }

    void LateUpdate()
    {
        if (!IsOwner || playerCamera == null) return;

        // Evitar que la cámara rote si estamos tocando un botón o el joystick de la UI
        bool isPointerOverUI = false;
        if (UnityEngine.EventSystems.EventSystem.current != null)
        {
            if (UnityEngine.InputSystem.Touchscreen.current != null && UnityEngine.InputSystem.Touchscreen.current.touches.Count > 0)
            {
                // Solo checkear el primer dedo
                if (UnityEngine.InputSystem.Touchscreen.current.touches[0].press.isPressed)
                {
                    int touchId = UnityEngine.InputSystem.Touchscreen.current.touches[0].touchId.ReadValue();
                    if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject(touchId))
                        isPointerOverUI = true;
                }
            }
            else if (UnityEngine.InputSystem.Mouse.current != null && UnityEngine.InputSystem.Mouse.current.leftButton.isPressed)
            {
                if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
                    isPointerOverUI = true;
            }
        }

        if (isPointerOverUI) return;

        Vector2 delta = Vector2.zero;
        bool isDragging = false;

        if (UnityEngine.InputSystem.Touchscreen.current != null && UnityEngine.InputSystem.Touchscreen.current.primaryTouch.press.isPressed)
        {
            delta = UnityEngine.InputSystem.Touchscreen.current.primaryTouch.delta.ReadValue();
            isDragging = true;
        }
        else if (UnityEngine.InputSystem.Mouse.current != null && UnityEngine.InputSystem.Mouse.current.leftButton.isPressed)
        {
            delta = UnityEngine.InputSystem.Mouse.current.delta.ReadValue();
            isDragging = true;
        }

        if (isDragging)
        {
            currentCameraRotY += delta.x * sensibilidadCamara;
            currentCameraRotX -= delta.y * sensibilidadCamara;
            currentCameraRotX = Mathf.Clamp(currentCameraRotX, -20f, 80f);
        }

        // Aplicamos la rotación matemática y definimos la posición alejándola del pivote hacia atrás
        Vector3 pivot = transform.position + Vector3.up * offsetCamara.y; 
        Quaternion rotation = Quaternion.Euler(currentCameraRotX, currentCameraRotY, 0);
        float distance = Mathf.Abs(offsetCamara.z);
        
        Vector3 finalPos = pivot - (rotation * Vector3.forward * distance);

        playerCamera.transform.position = finalPos;
        playerCamera.transform.rotation = rotation;
    }
}