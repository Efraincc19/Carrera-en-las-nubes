using UnityEngine;
using UnityEngine.InputSystem; // Necesario para el nuevo sistema

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("Movimiento")]
    public float moveSpeed = 8f;     // Velocidad de movimiento general (WASD)
    
    [Header("Salto")]
    public float jumpForce = 5f;     // Fuerza del salto
    public LayerMask groundLayer;    // Capa del suelo para detectar si está tocando el piso
    public Transform groundCheck;    // Un objeto vacío a los pies del jugador para el CheckSphere
    public float groundDistance = 0.2f;

    private Vector2 moveInput;
    private Rigidbody rb;
    private bool isGrounded;
    private bool wantsToJump;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        
        // Evita que el Rigidbody rote por colisiones si es un juego en tercera/primera persona
        rb.freezeRotation = true; 
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
            if (isGrounded)
            {
                wantsToJump = true;
            }
            else
            {
                Debug.LogWarning("Intento de salto fallido: isGrounded es false. ¡Asegúrate de que 'Ground Layer' está asignado correctamente al piso en Unity!");
            }
        }
    }

    void Update()
    {
        // Comprobamos si el jugador está tocando el suelo
        // Nota: Asegúrate de asignar la capa (Layer) de tu suelo en el inspector
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundLayer);
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
}