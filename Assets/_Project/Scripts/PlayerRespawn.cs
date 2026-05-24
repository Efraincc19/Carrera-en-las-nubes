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

    // Ya no es necesario arrastrar nada aquí, el script lo buscará solo
    private GameObject countdownTextObject;
    private TMP_Text tmpText;
    private Text legacyText;

    private Vector3 initialPosition;
    private Vector3 checkpointPosition;
    private float lastCloudZ = 0f;
    private bool isRespawning = false;

    private Rigidbody rb;
    private PlayerController playerController;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        playerController = GetComponent<PlayerController>();

        // === ¡BUSCADOR AUTOMÁTICO DE TEXTO! ===
        // Buscamos primero si es un texto moderno (TextMeshPro)
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

        // Si no lo encontró, buscamos si es un texto clásico antiguo
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

        // Verificación de seguridad en la Consola
        if (countdownTextObject != null)
        {
            Debug.Log("[RESPAWN] ¡Éxito! El script encontró 'TextoCuentaRegresiva' automáticamente sin necesidad de arrastrarlo.");
        }
        else
        {
            Debug.LogError("[RESPAWN] ¡ERROR! No se encontró ningún objeto llamado 'TextoCuentaRegresiva' dentro del Player. Revisa bien el nombre.");
        }
    }

    void Start()
    {
        initialPosition = transform.position;
        checkpointPosition = initialPosition;
        lastCloudZ = initialPosition.z;

        if (countdownTextObject != null) countdownTextObject.SetActive(false);
    }

    void Update()
    {
        if (!IsOwner || isRespawning) return;

        if (transform.position.y < fallThreshold)
        {
            StartCoroutine(RespawnCoroutine());
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
            lastCloudZ = objetoNube.transform.position.z;
            checkpointPosition = objetoNube.transform.position + new Vector3(0f, 2f, 0f);
        }
    }

    private IEnumerator RespawnCoroutine()
    {
        isRespawning = true;

        if (playerController != null) playerController.enabled = false;

        Vector3 posicionSpawn = CalcularPosicionSiguienteNube();
        transform.position = posicionSpawn;

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;

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

        transform.position = CalcularPosicionSiguienteNube();

        rb.isKinematic = false;

        if (playerController != null)
        {
            playerController.RestablecerSaltos();
            playerController.enabled = true;
        }

        isRespawning = false;
    }

    private void AsignarTexto(string mensaje)
    {
        if (tmpText != null) tmpText.text = mensaje;
        else if (legacyText != null) legacyText.text = mensaje;
    }

    private Vector3 CalcularPosicionSiguienteNube()
    {
        GameObject[] nubes = GameObject.FindGameObjectsWithTag("Cloud");
        GameObject siguienteNube = null;
        float menorDistanciaZ = float.MaxValue;

        foreach (GameObject nube in nubes)
        {
            if (nube.transform.position.z > lastCloudZ)
            {
                float distZ = nube.transform.position.z - lastCloudZ;
                if (distZ < menorDistanciaZ)
                {
                    menorDistanciaZ = distZ;
                    siguienteNube = nube;
                }
            }
        }

        if (siguienteNube != null)
        {
            return siguienteNube.transform.position + new Vector3(0f, 2f, 0f);
        }

        return checkpointPosition;
    }
}