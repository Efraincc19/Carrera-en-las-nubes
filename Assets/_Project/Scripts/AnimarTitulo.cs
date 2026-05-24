using UnityEngine;
using System.Collections;

public class AnimarTitulo : MonoBehaviour
{
    public float duracion = 1.5f; // Cuánto tarda en llegar
    public float retrasoInicial = 0.5f; // Espera un poco antes de moverse

    private RectTransform rectTransform;
    private Vector2 posicionFinal;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        // Guardamos la posición central (0) como el destino
        posicionFinal = new Vector2(-93, rectTransform.anchoredPosition.y);
    }

    void Start()
    {
        // Iniciamos la rutina de movimiento
        StartCoroutine(MoverTitulo());
    }

    IEnumerator MoverTitulo()
    {
        // Esperamos el tiempo de retraso
        yield return new WaitForSeconds(retrasoInicial);

        float tiempoTranscurrido = 0;
        Vector2 posicionInicial = rectTransform.anchoredPosition;

        while (tiempoTranscurrido < duracion)
        {
            tiempoTranscurrido += Time.deltaTime;

            // Calculamos el progreso (0 a 1)
            float progreso = tiempoTranscurrido / duracion;

            // Usamos una curva de suavizado para que frene bonito (SmoothStep)
            float suavizado = Mathf.SmoothStep(0, 1, progreso);

            // Movemos el RectTransform
            rectTransform.anchoredPosition = Vector2.Lerp(posicionInicial, posicionFinal, suavizado);

            yield return null; // Espera al siguiente cuadro
        }

        // Aseguramos que quede exactamente en el centro al final
        rectTransform.anchoredPosition = posicionFinal;
    }
}