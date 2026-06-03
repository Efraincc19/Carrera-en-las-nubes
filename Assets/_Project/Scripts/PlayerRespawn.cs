using UnityEngine;
using Unity.Netcode;
using System.Collections;
using TMPro;
using UnityEngine.UI;

[RequireComponent(typeof(Rigidbody))]
public class PlayerRespawn : NetworkBehaviour
{
    [Header("Configuración de Muerte")]
    public float fallThreshold = -10f;

    private GameObject countdownTextObject;
    private TMP_Text tmpText;
    private Text legacyText;

    private bool isRespawning = false;

    private Rigidbody rb;
    private PlayerController playerController;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        playerController = GetComponent<PlayerController>();

        // Buscador automático de texto para la cuenta regresiva
        TMP_Text[] todosLosTMP = GetComponentsInChildren<TMP_Text>(true);
        foreach (TMP_Text t in todosLosTMP)
        {
            if (t.gameObject.name == "TextoCuentaRegresiva")
            {
                tmpText = t;
                countdownTextObject = t.gameObject;
                break;
            }
        }

        if (countdownTextObject == null)
        {
            Text[] todosLosLegacy = GetComponentsInChildren<Text>(true);
            foreach (Text t in todosLosLegacy)
            {
                if (t.gameObject.name == "TextoCuentaRegresiva")
                {
                    legacyText = t;
                    countdownTextObject = t.gameObject;
                    break;
                }
            }
        }
    }

    void Start()
    {
        if (countdownTextObject != null) countdownTextObject.SetActive(false);
    }

    void Update()
    {
        // Solo el dueño detecta la caída (para mostrar la UI de cuenta regresiva)
        if (!IsOwner || isRespawning) return;

        if (transform.position.y < fallThreshold)
        {
            // El SERVIDOR maneja el teleport real en PlayerController.FixedUpdate.
            // Aquí solo mostramos la cuenta regresiva visual.
            StartCoroutine(RespawnCountdownVisual());
        }
    }

    /// <summary>
    /// Muestra la cuenta regresiva visual al caer. NO modifica la posición ni la física.
    /// El servidor se encarga del teleport real en PlayerController.FixedUpdate.
    /// </summary>
    private IEnumerator RespawnCountdownVisual()
    {
        isRespawning = true;

        if (countdownTextObject != null)
        {
            countdownTextObject.SetActive(true);

            AsignarTexto("3");
            yield return new WaitForSeconds(1f);

            AsignarTexto("2");
            yield return new WaitForSeconds(1f);

            AsignarTexto("1");
            yield return new WaitForSeconds(1f);

            countdownTextObject.SetActive(false);
        }
        else
        {
            yield return new WaitForSeconds(3f);
        }

        if (playerController != null)
            playerController.RestablecerSaltos();

        isRespawning = false;
    }

    private void AsignarTexto(string mensaje)
    {
        if (tmpText != null) tmpText.text = mensaje;
        else if (legacyText != null) legacyText.text = mensaje;
    }
}