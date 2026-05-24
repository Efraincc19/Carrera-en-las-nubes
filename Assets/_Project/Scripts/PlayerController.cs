using UnityEngine;
using UnityEngine.InputSystem; // Necesario para el nuevo sistema
using Unity.Netcode; // For multiplayer
using TMPro; // ¡VITAL! Para que el script entienda qué es TextMeshPro

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : NetworkBehaviour
{
    [Header("Movimiento")]
    public float moveSpeed = 8f;         // Velocidad de movimiento general (WASD)
    [Tooltip("Qué tan rápido frena al soltar las teclas. Valores bajos = más deslizamiento (efecto nube). Valores altos = frena antes.")]
    public float frenadoInercia = 3f;

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

    [Header("HUD de Puntuación (¡NUEVO!)")]
    public GameObject hudCanvas;       // Casilla para arrastrar tu Canvas
    public TMP_Text scoreText;         // Casilla para arrastrar tu TextoContador
    private int score = 0;             // El número de nubes recolectadas
    private GameObject lastTouchedCloud; // Evita que una misma nube te dé mil puntos

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

            // ¡NUEVO! Activamos NUESTRO HUD en nuestra pantalla
            if (hudCanvas != null) hudCanvas.SetActive(true);
            UpdateScoreUI();

            if (Camera.main != null && Camera.main != playerCamera)
            {
                Camera.main.gameObject.SetActive(false);
            }
        }
        else
        {
            if (playerCamera != null) playerCamera.gameObject.SetActive(false);
            if (playerAudioListener != null) playerAudioListener.enabled = false;

            // ¡NUEVO! Ocultamos el HUD de los clones de otros jugadores
            if (hudCanvas != null) hudCanvas.SetActive(false);
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

        Vector3 moveDirection = (transform.forward * moveInput.y) + (transform.right * moveInput.x);
        Vector3 targetVelocityH = moveDirection * moveSpeed;

        Vector3 currentVelocityH = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        Vector3 smoothedVelocityH = Vector3.Lerp(currentVelocityH, targetVelocityH, Time.fixedDeltaTime * frenadoInercia);

        rb.linearVelocity = new Vector3(smoothedVelocityH.x, rb.linearVelocity.y, smoothedVelocityH.z);

        if (wantsToJump)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpForce, rb.linearVelocity.z);
            wantsToJump = false;
            nextJumpTime = Time.time + jumpCooldown;
        }
    }

    // ¡NUEVO! Detecta cuando pisas una nube con el Tag "Cloud"
    private void OnCollisionEnter(Collision collision)
    {
        if (!IsOwner) return;

        if (collision.gameObject.CompareTag("Cloud"))
        {
            if (collision.gameObject != lastTouchedCloud)
            {
                score++;
                lastTouchedCloud = collision.gameObject;
                UpdateScoreUI();
            }
        }
    }

    // ¡NUEVO! Actualiza el texto en la pantalla
    void UpdateScoreUI()
    {
        if (scoreText != null)
        {
            scoreText.text = "Nubes: " + score;
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

        // Reseteamos los puntos del jugador al morir
        score = 0;
        lastTouchedCloud = null;
        UpdateScoreUI();

       
    }
}