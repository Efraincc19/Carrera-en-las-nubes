using UnityEngine;
using UnityEngine.InputSystem; // Necesario para el nuevo sistema
using Unity.Netcode; // For multiplayer

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : NetworkBehaviour
{
    [Header("Movimiento")]
    public float moveSpeed = 8f;         // Velocidad de movimiento general (WASD)
    [Tooltip("Qué tan rápido frena al soltar las teclas. Valores bajos = más deslizamiento (efecto nube). Valores altos = frena antes.")]
    public float frenadoInercia = 3f;    // ¡NUEVA VARIABLE PARA LA INERCIA!

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
        if (playerCamera == null)
            playerCamera = GetComponentInChildren<Camera>(true);

        if (playerAudioListener == null && playerCamera != null)
            playerAudioListener = playerCamera.GetComponent<AudioListener>();

        if (IsOwner)
        {
            if (playerCamera != null)
            {
                playerCamera.gameObject.SetActive(true);
                playerCamera.transform.localPosition = offsetCamara;
                playerCamera.transform.localEulerAngles = rotacionCamara;
            }

            if (playerAudioListener != null) playerAudioListener.enabled = true;

            if (Camera.main != null && Camera.main != playerCamera)
            {
                Camera.main.gameObject.SetActive(false);
            }
        }
        else
        {
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
        cloudSpawner = FindObjectOfType<CloudSpawner>();
        rb.freezeRotation = true;
        initialPosition = transform.position;
    }

    public void OnMove(InputValue value)
    {
        if (!IsOwner) return;
        moveInput = value.Get<Vector2>();
    }

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
        if (!IsOwner) return;

        if (groundCheck != null)
        {
            isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundLayer);
        }
        else
        {
            Debug.LogWarning("PlayerController: groundCheck no está asignado. Usando raycast de respaldo.");
            isGrounded = Physics.Raycast(transform.position, Vector3.down, groundDistance + 0.1f, groundLayer);
        }

        if (transform.position.y < fallThreshold)
        {
            Respawn();
        }
    }

    void FixedUpdate()
    {
        if (!IsOwner) return;

        // 1. Calcular la dirección deseada según los inputs
        Vector3 moveDirection = (transform.forward * moveInput.y) + (transform.right * moveInput.x);
        Vector3 targetVelocityH = moveDirection * moveSpeed;

        // Separamos la velocidad actual en los ejes horizontales (X, Z)
        Vector3 currentVelocityH = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

        // ¡AQUÍ ESTÁ EL CAMBIO MÁGICO!
        // Interpolamos de la velocidad actual a la deseada de forma progresiva
        Vector3 smoothedVelocityH = Vector3.Lerp(currentVelocityH, targetVelocityH, Time.fixedDeltaTime * frenadoInercia);

        // Volvemos a armar el vector de velocidad respetando el eje Y de la física/salto
        rb.linearVelocity = new Vector3(smoothedVelocityH.x, rb.linearVelocity.y, smoothedVelocityH.z);

        // 2. Lógica del Salto
        if (wantsToJump)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpForce, rb.linearVelocity.z);
            wantsToJump = false;
            nextJumpTime = Time.time + jumpCooldown;
        }
    }

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
        transform.position = initialPosition;
        rb.linearVelocity = Vector3.zero;

        if (cloudSpawner != null)
        {
            cloudSpawner.ResetSpawner();
        }
    }
}