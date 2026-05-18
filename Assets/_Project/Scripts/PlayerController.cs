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
    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    // Este método se conecta con el Input Action de Salto (Button)
    public void OnJump(InputAction.CallbackContext context)
    {
        // Detecta el momento exacto en que se presiona el botón
        if (context.started && isGrounded)
        {
            wantsToJump = true;
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
        // moveInput.x es A/D o Flechas Izquierda/Derecha
        // moveInput.y es W/S o Flechas Arriba/Abajo
        Vector3 moveDirection = (transform.forward * moveInput.y) + (transform.right * moveInput.x);
        
        // Calculamos la nueva posición manteniendo la velocidad actual en Y (para que la gravedad funcione)
        Vector3 targetMovePosition = rb.position + moveDirection * moveSpeed * Time.fixedDeltaTime;
        rb.MovePosition(targetMovePosition);

        // 2. Lógica del Salto
        if (wantsToJump)
        {
            // Aplicamos una fuerza hacia arriba usando Velocity Change para un salto instantáneo
            rb.AddForce(Vector3.up * jumpForce, ForceMode.VelocityChange);
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