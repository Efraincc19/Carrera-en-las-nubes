using UnityEngine;
using UnityEngine.InputSystem; // Necesario para el nuevo sistema
using Unity.Netcode; // For multiplayer

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : NetworkBehaviour
{
    [Header("Movimiento")]
    public float moveSpeed = 8f;     // Velocidad de movimiento general (WASD)
    
    [Header("Salto")]
    public float jumpForce = 8f;     // Fuerza del salto (Aumentada para mayor rango)
    public float jumpCooldown = 2f;  // Tiempo de espera entre saltos
    public LayerMask groundLayer;    // Capa del suelo para detectar si está tocando el piso
    public Transform groundCheck;    // Un objeto vacío a los pies del jugador para el CheckSphere
    public float groundDistance = 0.2f;

    [Header("Respawn")]
    public float fallThreshold = -10f; // Altura a la que el jugador reaparece
    private Vector3 initialPosition; // Posición de inicio

    [Header("Cámara Multijugador")]
    [Tooltip("Asigna aquí la cámara hija del jugador (si la hay)")]
    public Camera playerCamera;
    
    [Tooltip("Asigna aquí el AudioListener de la cámara del jugador (opcional)")]
    public AudioListener playerAudioListener;

    [Header("Ajuste 3ra Persona Automático")]
    [Tooltip("Puedes cambiar estos números en cualquier momento para alejar/acercar la cámara")]
    public Vector3 offsetCamara = new Vector3(0, 5.5f, -10f); // Más lejos y un poco más alta
    public Vector3 rotacionCamara = new Vector3(20f, 0, 0);   // Inclinación hacia abajo

    private CloudSpawner cloudSpawner;
    private PlayerInput playerInput;
    private InputAction jumpInputAction;
    private Vector2 moveInput;
    private Rigidbody rb;
    private bool isGrounded;
    private bool wantsToJump;
    private float nextJumpTime;

    public override void OnNetworkSpawn()
    {
        // Buscar automáticamente la cámara si olvidaste asignarla en el Unity Editor
        if (playerCamera == null)
            playerCamera = GetComponentInChildren<Camera>(true);
            
        if (playerAudioListener == null && playerCamera != null)
            playerAudioListener = playerCamera.GetComponent<AudioListener>();

        if (IsOwner)
        {
            // Activar la cámara personal de este jugador
            if (playerCamera != null) 
            {
                playerCamera.gameObject.SetActive(true);
                // ¡Obligamos por código a que la cámara se aleje a la tercera persona sin importar el prefab!
                playerCamera.transform.localPosition = offsetCamara;
                playerCamera.transform.localEulerAngles = rotacionCamara;
            }

            if (playerAudioListener != null) playerAudioListener.enabled = true;

            // Apagar la cámara principal de la escena (la del menú)
            if (Camera.main != null && Camera.main != playerCamera)
            {
                Camera.main.gameObject.SetActive(false);
            }
        }
        else
        {
            // Si este jugador NO es el nuestro, apagar su cámara
            if (playerCamera != null) playerCamera.gameObject.SetActive(false);
            if (playerAudioListener != null) playerAudioListener.enabled = false;
        }
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        playerInput = GetComponent<PlayerInput>();
    }

    private void OnEnable()
    {
        if (playerInput != null)
        {
            jumpInputAction = playerInput.actions.FindAction("Jump");
            if (jumpInputAction != null)
                jumpInputAction.performed += OnJumpAction;
        }
    }

    private void OnDisable()
    {
        if (jumpInputAction != null)
            jumpInputAction.performed -= OnJumpAction;
    }

    void Start()
    {
        cloudSpawner = FindObjectOfType<CloudSpawner>(); // Buscamos el spawner en la escena automáticamente

        
        // Evita que el Rigidbody rote por colisiones si es un juego en tercera/primera persona
        rb.freezeRotation = true; 
        
        // Guardamos la posición inicial al iniciar la escena
        initialPosition = transform.position;
    }

    // Este método se conecta con el Input Action de Movimiento (Vector2)
    public void OnMove(InputValue value)
    {
        if (!IsOwner) return;
        moveInput = value.Get<Vector2>();
    }

    // Este método se conecta con el Input Action de Salto (Button)
    public void OnJump(InputValue value)
    {
        if (!IsOwner) return;
        if (value.isPressed)
            TryRequestJump();
    }

    private void OnJumpAction(InputAction.CallbackContext context)
    {
        if (!IsOwner) return;
        if (context.performed)
            TryRequestJump();
    }

    private void TryRequestJump()
    {
        if (!IsOwner) return;
        if (isGrounded && Time.time >= nextJumpTime)
            wantsToJump = true;
    }

    void Update()
    {
        if (!IsOwner) return; // Solo controlamos nuestro propio jugador

        // Comprobamos si el jugador está tocando el suelo
        // Nota: Asegúrate de asignar la capa (Layer) de tu suelo en el inspector
        if (groundCheck != null)
        {
            isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundLayer);
        }
        else
        {
            Debug.LogWarning("PlayerController: groundCheck no está asignado. Usando raycast de respaldo.");
            isGrounded = Physics.Raycast(transform.position, Vector3.down, groundDistance + 0.1f, groundLayer);
        }

        // Si el jugador cae por debajo del umbral, lo regresamos al punto de inicio
        if (transform.position.y < fallThreshold)
        {
            Respawn();
        }
    }

    void FixedUpdate()
    {
        if (!IsOwner) return; // Permitir que el NetworkTransform o ClientNetworkTransform sincronice esto para los demas
        
        // 1. Movimiento en base a WASD (Ejes X y Z)
        Vector3 moveDirection = (transform.forward * moveInput.y) + (transform.right * moveInput.x);
        
        // En lugar de MovePosition (que bloquea los saltos y la gravedad), usamos la velocidad
        Vector3 targetVelocity = moveDirection * moveSpeed;
        targetVelocity.y = rb.linearVelocity.y; // Conservamos la caída libre o el salto en progreso
        rb.linearVelocity = targetVelocity;

        // 2. Lógica del Salto
        if (wantsToJump)
        {
            // Aplicamos la fuerza de salto sobrescribiendo la velocidad en Y
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpForce, rb.linearVelocity.z);
            wantsToJump = false; // Reseteamos la petición
            nextJumpTime = Time.time + jumpCooldown;
        }
    }

    // Opcional: Dibuja la esfera de detección de suelo en el editor para guiarte
    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundDistance);
        }
    }

    private void Respawn()
    {
        transform.position = initialPosition; // Vuelve a la posición inicial
        rb.linearVelocity = Vector3.zero;     // Quita la inercia/velocidad de caída

        // Reiniciamos la generación de nubes
        if (cloudSpawner != null)
        {
            cloudSpawner.ResetSpawner();
        }
    }
}