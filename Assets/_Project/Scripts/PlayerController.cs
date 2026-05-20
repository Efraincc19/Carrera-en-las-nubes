using UnityEngine;
using UnityEngine.InputSystem; // Necesario para el nuevo sistema

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("Movimiento")]
    public float moveSpeed = 8f;     // Velocidad de movimiento general (WASD)
    
    [Header("Salto")]
    public float jumpForce = 5f;     // Fuerza del salto
    public float jumpCooldown = 2f;  // Tiempo de espera entre saltos
    public LayerMask groundLayer;    // Capa del suelo para detectar si está tocando el piso
    public Transform groundCheck;    // Un objeto vacío a los pies del jugador para el CheckSphere
    public float groundDistance = 0.2f;

    [Header("Respawn")]
    public float fallThreshold = -10f; // Altura a la que el jugador reaparece
    private Vector3 initialPosition; // Posición de inicio

    private CloudSpawner cloudSpawner;
    private Vector2 moveInput;
    private Rigidbody rb;
    private bool isGrounded;
    private bool wantsToJump;
    private float nextJumpTime;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        cloudSpawner = FindObjectOfType<CloudSpawner>(); // Buscamos el spawner en la escena automáticamente

        
        // Evita que el Rigidbody rote por colisiones si es un juego en tercera/primera persona
        rb.freezeRotation = true; 
        
        // Guardamos la posición inicial al iniciar la escena
        initialPosition = transform.position;
    }

    // Este método se conecta con el Input Action de Movimiento (Vector2)
    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    // Este método se conecta con el Input Action de Salto (Button)
    public void OnJump(InputValue value)
    {
        if (value.isPressed)
        {
            if (isGrounded && Time.time >= nextJumpTime)
            {
                wantsToJump = true;
            }
        }
    }

    void Update()
    {
        // Comprobamos si el jugador está tocando el suelo
        // Nota: Asegúrate de asignar la capa (Layer) de tu suelo en el inspector
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundLayer);

        // Si el jugador cae por debajo del umbral, lo regresamos al punto de inicio
        if (transform.position.y < fallThreshold)
        {
            Respawn();
        }
    }

    void FixedUpdate()
    {
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