using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;
using TMPro;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : NetworkBehaviour
{
    [Header("Movimiento")]
    public float moveSpeed = 8f;
    [Tooltip("Qué tan rápido frena al soltar las teclas. Valores bajos = más deslizamiento (efecto nube).")]
    public float frenadoInercia = 3f;

    [Header("Salto")]
    public float jumpForce = 8f;
    public float jumpCooldown = 2f;
    public LayerMask groundLayer;
    public Transform groundCheck;
    public float groundDistance = 0.2f;

    [Header("Respawn")]
    public float fallThreshold = -10f;
    private Vector3 initialPosition;

    [Header("Cámara Multijugador")]
    public Camera playerCamera;
    public AudioListener playerAudioListener;

    [Header("Ajuste 3ra Persona Automático")]
    public Vector3 offsetCamara = new Vector3(0, 5.5f, -10f);
    public Vector3 rotacionCamara = new Vector3(20f, 0, 0);

    [Header("HUD de Puntuación")]
    public GameObject hudCanvas;
    public TMP_Text scoreText;
    private int score = 0;
    private HashSet<int> nubesVisitadas = new HashSet<int>();

    [Header("Efectos de Sonido (¡NUEVO!)")]
    [Tooltip("Arrastra aquí el componente AudioSource de este Player")]
    public AudioSource audioSource;
    [Tooltip("Arrastra aquí tu archivo de sonido (.mp3 o .wav)")]
    public AudioClip sonidoPunto;

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
            if (hudCanvas != null) hudCanvas.SetActive(false);
        }
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        playerInput = GetComponent<PlayerInput>();

        // Si olvidaste asignar el AudioSource en el inspector, intentamos buscarlo automáticamente
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
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

    private void OnCollisionEnter(Collision collision)
    {
        if (!IsOwner) return;

        bool esNube = collision.gameObject.CompareTag("Cloud") ||
                     (collision.transform.parent != null && collision.transform.parent.CompareTag("Cloud"));

        if (esNube)
        {
            GameObject objetoNube = collision.gameObject.CompareTag("Cloud") ? collision.gameObject : collision.transform.parent.gameObject;
            int nubeID = objetoNube.GetInstanceID();

            if (!nubesVisitadas.Contains(nubeID))
            {
                nubesVisitadas.Add(nubeID);
                score++;
                UpdateScoreUI();

                // ¡NUEVO! Reproduce el sonidito una sola vez sin interrumpir otros audios
                if (audioSource != null && sonidoPunto != null)
                {
                    audioSource.PlayOneShot(sonidoPunto);
                }

                Debug.Log($"¡Punto anotado! Nube ID: {nubeID}. Puntuación: {score}");
            }
        }
    }

    void UpdateScoreUI()
    {
        if (scoreText != null)
        {
            scoreText.text = "Nubes: " + score;
        }
    }

    private void Respawn()
    {
        transform.position = initialPosition;
        rb.linearVelocity = Vector3.zero;

        score = 0;
        nubesVisitadas.Clear();
        UpdateScoreUI();
    }
}