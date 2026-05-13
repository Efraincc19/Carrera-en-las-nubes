using UnityEngine;
using UnityEngine.InputSystem; // Necesario para el nuevo sistema
public class PlayerController : MonoBehaviour
{
    public float forwardSpeed = 10f; // Velocidad de carrera automática
    public float sideSpeed = 8f;    // Velocidad de movimiento lateral
    private Vector2 moveInput;
    private Rigidbody rb;
    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }
    // Este método se conecta con el Input System
    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }
    void FixedUpdate()
    {
        // 1. Movimiento constante hacia adelante
        Vector3 forwardMove = transform.forward * forwardSpeed * Time.fixedDeltaTime;
        // 2. Movimiento lateral basado en el Input
        Vector3 sideMove = transform.right * moveInput.x * sideSpeed * Time.fixedDeltaTime;
        // Aplicamos el movimiento a la posición del Rigidbody
        rb.MovePosition(rb.position + forwardMove + sideMove);
    }

    void Update() 
{
    // Si la posición en el eje Y (altura) es menor a -5
    if (transform.position.y < -5f)
    {
        // Reiniciamos el nivel
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

}
