using UnityEngine;

/// <summary>
/// Construye un modelo del Caballero de Hollow Knight con primitivas de Unity
/// e implementa animaciones procedurales: idle, caminar, saltar, caer, aterrizar.
/// Lee el estado del PlayerController y el Rigidbody para determinar qué animación reproducir.
/// </summary>
public class HollowKnightSkin : MonoBehaviour
{
    // ─── Colores ───────────────────────────────────────────────
    [Header("Colores del Caballero")]
    public Color cloakColor  = new Color(0.22f, 0.22f, 0.27f, 1f);
    public Color maskColor   = new Color(0.95f, 0.95f, 0.97f, 1f);
    public Color eyeColor    = new Color(0.03f, 0.03f, 0.06f, 1f);
    public Color hornColor   = new Color(0.88f, 0.88f, 0.90f, 1f);
    public Color nailColor   = new Color(0.68f, 0.68f, 0.72f, 1f);

    // ─── Ajustes de animación ──────────────────────────────────
    [Header("Animación")]
    public float walkBobSpeed     = 12f;   // Velocidad del rebote al caminar
    public float walkBobAmount    = 0.06f; // Altura del rebote al caminar
    public float walkTiltAmount   = 8f;    // Inclinación lateral al caminar
    public float cloakSwaySpeed   = 8f;    // Velocidad de ondulación de la capa
    public float cloakSwayAmount  = 0.08f; // Amplitud de ondulación de la capa
    public float idleBreathSpeed  = 2.5f;  // Velocidad de "respiración" en idle
    public float idleBreathAmount = 0.015f;// Amplitud de la respiración
    public float landSquashTime   = 0.15f; // Duración del efecto squash al aterrizar
    public float turnSpeed        = 12f;   // Velocidad de giro

    // ─── Partidas internas ─────────────────────────────────────
    private GameObject skinRoot;
    private Rigidbody rb;

    // Referencias a partes del modelo para animar
    private Transform bodyT;
    private Transform cloakBottomT;
    private Transform cloakTipT;
    private Transform headT;
    private Transform eyeLeftT;
    private Transform eyeRightT;
    private Transform hornLeftT;
    private Transform hornRightT;
    private Transform nailT;
    private Transform nailGuardT;
    private Transform maskDetailT;

    // Posiciones/escalas/rotaciones originales para interpolar
    private Vector3 bodyBasePos, bodyBaseScale;
    private Vector3 cloakBottomBasePos, cloakBottomBaseScale;
    private Vector3 cloakTipBasePos, cloakTipBaseScale;
    private Vector3 headBasePos;
    private Vector3 nailBasePos;
    private Quaternion nailBaseRot;
    private Vector3 nailGuardBasePos;

    // Estado de animación
    private enum AnimState { Idle, Walking, Jumping, Falling }
    private AnimState currentState = AnimState.Idle;
    private bool wasGrounded = true;
    private float landSquashTimer = 0f;
    private float animTime = 0f;
    private float facingDirection = 1f; // 1 = derecha/adelante, -1 = izquierda/atrás
    private float currentFacing = 1f;   // Facing suavizado

    // Partículas de polvo al aterrizar (efecto sencillo)
    private float dustTimer = 0f;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        // Ocultar la malla original (la cápsula por defecto)
        MeshRenderer originalRenderer = GetComponent<MeshRenderer>();
        if (originalRenderer != null) originalRenderer.enabled = false;
        MeshFilter originalFilter = GetComponent<MeshFilter>();
        if (originalFilter != null) originalFilter.mesh = null;

        BuildModel();
        CacheBaseTransforms();
    }

    // ═══════════════════════════════════════════════════════════
    //  CONSTRUCCIÓN DEL MODELO
    // ═══════════════════════════════════════════════════════════

    void BuildModel()
    {
        skinRoot = new GameObject("HollowKnight_Skin");
        skinRoot.transform.SetParent(transform, false);
        skinRoot.transform.localPosition = Vector3.zero;
        skinRoot.transform.localRotation = Quaternion.identity;

        // ── Cuerpo / Capa ──
        GameObject body = MakePart("Body", PrimitiveType.Capsule);
        body.transform.localPosition = new Vector3(0f, -0.05f, 0f);
        body.transform.localScale = new Vector3(0.55f, 0.35f, 0.40f);
        SetColor(body, cloakColor);
        bodyT = body.transform;

        GameObject cloakBottom = MakePart("CloakBottom", PrimitiveType.Sphere);
        cloakBottom.transform.localPosition = new Vector3(0f, -0.25f, 0f);
        cloakBottom.transform.localScale = new Vector3(0.60f, 0.30f, 0.45f);
        SetColor(cloakBottom, cloakColor);
        cloakBottomT = cloakBottom.transform;

        GameObject cloakTip = MakePart("CloakTip", PrimitiveType.Sphere);
        cloakTip.transform.localPosition = new Vector3(0f, -0.42f, 0f);
        cloakTip.transform.localScale = new Vector3(0.25f, 0.10f, 0.20f);
        SetColor(cloakTip, cloakColor * 0.85f);
        cloakTipT = cloakTip.transform;

        // ── Cabeza / Máscara ──
        GameObject head = MakePart("Head", PrimitiveType.Sphere);
        head.transform.localPosition = new Vector3(0f, 0.30f, 0f);
        head.transform.localScale = new Vector3(0.50f, 0.45f, 0.40f);
        SetColor(head, maskColor);
        headT = head.transform;

        // ── Ojos ──
        GameObject eyeL = MakePart("EyeLeft", PrimitiveType.Sphere);
        eyeL.transform.localPosition = new Vector3(-0.10f, 0.30f, 0.18f);
        eyeL.transform.localScale = new Vector3(0.10f, 0.15f, 0.05f);
        SetColor(eyeL, eyeColor);
        eyeLeftT = eyeL.transform;

        GameObject eyeR = MakePart("EyeRight", PrimitiveType.Sphere);
        eyeR.transform.localPosition = new Vector3(0.10f, 0.30f, 0.18f);
        eyeR.transform.localScale = new Vector3(0.10f, 0.15f, 0.05f);
        SetColor(eyeR, eyeColor);
        eyeRightT = eyeR.transform;

        // ── Cuernos ──
        GameObject hornL = MakePart("HornLeft", PrimitiveType.Capsule);
        hornL.transform.localPosition = new Vector3(-0.14f, 0.58f, 0f);
        hornL.transform.localRotation = Quaternion.Euler(0, 0, 25f);
        hornL.transform.localScale = new Vector3(0.06f, 0.18f, 0.06f);
        SetColor(hornL, hornColor);
        hornLeftT = hornL.transform;

        GameObject hornR = MakePart("HornRight", PrimitiveType.Capsule);
        hornR.transform.localPosition = new Vector3(0.14f, 0.58f, 0f);
        hornR.transform.localRotation = Quaternion.Euler(0, 0, -25f);
        hornR.transform.localScale = new Vector3(0.06f, 0.18f, 0.06f);
        SetColor(hornR, hornColor);
        hornRightT = hornR.transform;

        // ── Nail (espada) ──
        GameObject nail = MakePart("Nail", PrimitiveType.Cube);
        nail.transform.localPosition = new Vector3(0.35f, -0.05f, 0.10f);
        nail.transform.localRotation = Quaternion.Euler(0, 0, -15f);
        nail.transform.localScale = new Vector3(0.04f, 0.55f, 0.04f);
        SetColor(nail, nailColor);
        nailT = nail.transform;

        GameObject nailGuard = MakePart("NailGuard", PrimitiveType.Cube);
        nailGuard.transform.localPosition = new Vector3(0.33f, -0.20f, 0.10f);
        nailGuard.transform.localRotation = Quaternion.Euler(0, 0, -15f);
        nailGuard.transform.localScale = new Vector3(0.12f, 0.04f, 0.04f);
        SetColor(nailGuard, nailColor);
        nailGuardT = nailGuard.transform;

        // ── Detalle de máscara ──
        GameObject maskDetail = MakePart("MaskDetail", PrimitiveType.Cube);
        maskDetail.transform.localPosition = new Vector3(0f, 0.32f, 0.20f);
        maskDetail.transform.localScale = new Vector3(0.02f, 0.20f, 0.01f);
        SetColor(maskDetail, new Color(0.80f, 0.80f, 0.83f, 1f));
        maskDetailT = maskDetail.transform;
    }

    void CacheBaseTransforms()
    {
        bodyBasePos        = bodyT.localPosition;
        bodyBaseScale      = bodyT.localScale;
        cloakBottomBasePos = cloakBottomT.localPosition;
        cloakBottomBaseScale = cloakBottomT.localScale;
        cloakTipBasePos    = cloakTipT.localPosition;
        cloakTipBaseScale  = cloakTipT.localScale;
        headBasePos        = headT.localPosition;
        nailBasePos        = nailT.localPosition;
        nailBaseRot        = nailT.localRotation;
        nailGuardBasePos   = nailGuardT.localPosition;
    }

    // ═══════════════════════════════════════════════════════════
    //  ANIMACIÓN — Update
    // ═══════════════════════════════════════════════════════════

    void Update()
    {
        if (rb == null) return;

        float dt = Time.deltaTime;
        animTime += dt;

        // Detectar estado
        bool grounded = IsGrounded();
        float hSpeed = new Vector2(rb.linearVelocity.x, rb.linearVelocity.z).magnitude;
        float vSpeed = rb.linearVelocity.y;

        AnimState newState;
        if (!grounded && vSpeed > 0.5f)
            newState = AnimState.Jumping;
        else if (!grounded && vSpeed <= 0.5f)
            newState = AnimState.Falling;
        else if (hSpeed > 0.5f)
            newState = AnimState.Walking;
        else
            newState = AnimState.Idle;

        // Detección de aterrizaje (estaba en el aire y ahora toca suelo)
        if (grounded && !wasGrounded)
        {
            landSquashTimer = landSquashTime;
        }
        wasGrounded = grounded;

        // Actualizar dirección de facing basándose en la velocidad horizontal
        Vector3 hVel = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
        if (hVel.magnitude > 0.3f)
        {
            // Usar el eje X del movimiento local para determinar dirección
            float localX = Vector3.Dot(hVel.normalized, transform.right);
            if (Mathf.Abs(localX) > 0.2f)
            {
                facingDirection = localX > 0 ? 1f : -1f;
            }
        }

        // Suavizar el giro
        currentFacing = Mathf.Lerp(currentFacing, facingDirection, turnSpeed * dt);

        // Aplicar animación según estado
        currentState = newState;

        switch (currentState)
        {
            case AnimState.Idle:    AnimateIdle(dt);    break;
            case AnimState.Walking: AnimateWalk(dt);    break;
            case AnimState.Jumping: AnimateJump(dt);    break;
            case AnimState.Falling: AnimateFall(dt);    break;
        }

        // Efecto de aterrizaje (squash) se superpone a cualquier estado
        if (landSquashTimer > 0f)
        {
            ApplyLandSquash(dt);
        }

        // Aplicar facing (voltear el modelo en X)
        ApplyFacing();
    }

    // ─── Idle: respiración suave ───────────────────────────────
    void AnimateIdle(float dt)
    {
        float breath = Mathf.Sin(animTime * idleBreathSpeed);
        float breathNorm = breath * idleBreathAmount;

        // El cuerpo sube y baja ligeramente
        bodyT.localPosition = bodyBasePos + Vector3.up * breathNorm;
        bodyT.localScale = bodyBaseScale + new Vector3(-breathNorm * 0.3f, breathNorm, -breathNorm * 0.3f);
        bodyT.localRotation = Quaternion.identity;

        // Cabeza sigue el cuerpo
        headT.localPosition = headBasePos + Vector3.up * breathNorm * 0.5f;

        // Capa inferior se mueve sutilmente
        cloakBottomT.localPosition = cloakBottomBasePos + Vector3.up * breathNorm * 0.3f;
        cloakBottomT.localScale = cloakBottomBaseScale + new Vector3(breathNorm * 0.5f, -breathNorm * 0.3f, breathNorm * 0.3f);

        // Punta de la capa - oscilación muy suave
        float tipSway = Mathf.Sin(animTime * idleBreathSpeed * 0.7f) * 0.01f;
        cloakTipT.localPosition = cloakTipBasePos + new Vector3(tipSway, breathNorm * 0.2f, 0f);
        cloakTipT.localScale = cloakTipBaseScale;

        // Nail descansa en posición base
        nailT.localPosition = nailBasePos + Vector3.up * breathNorm * 0.3f;
        nailT.localRotation = nailBaseRot;
        nailGuardT.localPosition = nailGuardBasePos + Vector3.up * breathNorm * 0.3f;
    }

    // ─── Walk: rebote, balanceo de capa, inclinación ──────────
    void AnimateWalk(float dt)
    {
        float bob = Mathf.Sin(animTime * walkBobSpeed);
        float bobAbs = Mathf.Abs(bob);
        float bobY = bobAbs * walkBobAmount;

        // Cuerpo sube y baja con cada paso + inclinación lateral
        float tilt = bob * walkTiltAmount;
        bodyT.localPosition = bodyBasePos + Vector3.up * bobY;
        bodyT.localScale = bodyBaseScale;
        bodyT.localRotation = Quaternion.Euler(0, 0, tilt * currentFacing);

        // Cabeza rebota
        headT.localPosition = headBasePos + Vector3.up * bobY * 0.7f;

        // Capa inferior se balancea opuesta al cuerpo (efecto de inercia)
        float cloakSway = Mathf.Sin(animTime * cloakSwaySpeed - 0.5f) * cloakSwayAmount;
        cloakBottomT.localPosition = cloakBottomBasePos +
            new Vector3(cloakSway * currentFacing, bobY * 0.4f, 0f);
        cloakBottomT.localScale = cloakBottomBaseScale +
            new Vector3(bobAbs * 0.05f, -bobAbs * 0.03f, 0f);

        // Punta de la capa ondea más agresivamente
        float tipSway = Mathf.Sin(animTime * cloakSwaySpeed * 1.3f - 1f) * cloakSwayAmount * 1.5f;
        cloakTipT.localPosition = cloakTipBasePos +
            new Vector3(tipSway * currentFacing, bobY * 0.2f, 0f);
        cloakTipT.localScale = cloakTipBaseScale + new Vector3(bobAbs * 0.04f, 0f, 0f);

        // Nail se balancea al caminar
        float nailSwing = Mathf.Sin(animTime * walkBobSpeed * 0.8f) * 8f;
        nailT.localPosition = nailBasePos + Vector3.up * bobY * 0.5f;
        nailT.localRotation = nailBaseRot * Quaternion.Euler(0, 0, nailSwing);
        nailGuardT.localPosition = nailGuardBasePos + Vector3.up * bobY * 0.5f;
    }

    // ─── Jump: cuerpo se estira, capa se abre hacia abajo ─────
    void AnimateJump(float dt)
    {
        float jumpPhase = Mathf.Clamp01(rb.linearVelocity.y / 6f); // 0 al pico, 1 al inicio

        // Cuerpo se estira verticalmente (squash & stretch)
        float stretchY = Mathf.Lerp(0f, 0.04f, jumpPhase);
        float squishX  = Mathf.Lerp(0f, -0.04f, jumpPhase);
        bodyT.localPosition = bodyBasePos + Vector3.up * 0.05f;
        bodyT.localScale = bodyBaseScale + new Vector3(squishX, stretchY, squishX);
        bodyT.localRotation = Quaternion.identity;

        // Cabeza sube un poco
        headT.localPosition = headBasePos + Vector3.up * 0.05f;

        // Capa se abre/expande hacia abajo - efecto dramático
        float cloakFlare = Mathf.Lerp(0.08f, 0.15f, jumpPhase);
        cloakBottomT.localPosition = cloakBottomBasePos + new Vector3(0f, -0.06f, 0f);
        cloakBottomT.localScale = cloakBottomBaseScale + new Vector3(cloakFlare, -0.08f, cloakFlare * 0.6f);

        // Punta de la capa baja y se expande
        cloakTipT.localPosition = cloakTipBasePos + new Vector3(0f, -0.10f, 0f);
        cloakTipT.localScale = cloakTipBaseScale + new Vector3(0.10f, -0.02f, 0.08f);

        // Nail apunta ligeramente hacia arriba
        nailT.localPosition = nailBasePos + new Vector3(0.03f, 0.08f, 0f);
        nailT.localRotation = nailBaseRot * Quaternion.Euler(0, 0, 10f);
        nailGuardT.localPosition = nailGuardBasePos + new Vector3(0.03f, 0.08f, 0f);
    }

    // ─── Fall: capa flamea hacia arriba, cuerpo se encoge ─────
    void AnimateFall(float dt)
    {
        float fallSpeed = Mathf.Clamp01(Mathf.Abs(rb.linearVelocity.y) / 8f);

        // Cuerpo se aplasta ligeramente (anticipación del impacto)
        bodyT.localPosition = bodyBasePos;
        bodyT.localScale = bodyBaseScale + new Vector3(0.03f * fallSpeed, -0.03f * fallSpeed, 0.03f * fallSpeed);
        bodyT.localRotation = Quaternion.identity;

        headT.localPosition = headBasePos + Vector3.down * 0.02f * fallSpeed;

        // Capa flamea HACIA ARRIBA - efecto de viento
        float flameOffset = fallSpeed * 0.12f;
        float wobble = Mathf.Sin(animTime * 15f) * 0.03f * fallSpeed;
        cloakBottomT.localPosition = cloakBottomBasePos + new Vector3(wobble, flameOffset, 0f);
        cloakBottomT.localScale = cloakBottomBaseScale + new Vector3(-0.05f * fallSpeed, 0.10f * fallSpeed, -0.03f * fallSpeed);

        // Punta de la capa sube y ondea
        float tipWobble = Mathf.Sin(animTime * 18f + 1f) * 0.05f * fallSpeed;
        cloakTipT.localPosition = cloakTipBasePos + new Vector3(tipWobble, flameOffset * 1.5f, 0f);
        cloakTipT.localScale = cloakTipBaseScale + new Vector3(-0.05f * fallSpeed, 0.05f * fallSpeed, 0f);

        // Nail se balancea con la caída
        float nailSway = Mathf.Sin(animTime * 10f) * 5f * fallSpeed;
        nailT.localPosition = nailBasePos + Vector3.up * 0.03f * fallSpeed;
        nailT.localRotation = nailBaseRot * Quaternion.Euler(0, 0, nailSway);
        nailGuardT.localPosition = nailGuardBasePos + Vector3.up * 0.03f * fallSpeed;
    }

    // ─── Aterrizaje: efecto squash rápido ─────────────────────
    void ApplyLandSquash(float dt)
    {
        landSquashTimer -= dt;
        float t = Mathf.Clamp01(landSquashTimer / landSquashTime);

        // Curva de squash: fuerte al inicio, rebota al final
        float squash;
        if (t > 0.5f)
        {
            // Primera mitad: aplastarse
            squash = Mathf.Lerp(0f, 1f, (t - 0.5f) * 2f);
        }
        else
        {
            // Segunda mitad: rebotar de vuelta
            squash = Mathf.Lerp(0f, 0.5f, t * 2f);
        }

        float squashY = -0.08f * squash;
        float squashX =  0.06f * squash;

        // Aplicar squash al skin root para que afecte a todo el modelo
        skinRoot.transform.localScale = Vector3.one + new Vector3(squashX, squashY, squashX);

        // Crear efecto de partículas de polvo al aterrizar (solo al inicio)
        if (t > 0.9f && dustTimer <= 0f)
        {
            SpawnDustEffect();
            dustTimer = 0.5f;
        }

        if (landSquashTimer <= 0f)
        {
            skinRoot.transform.localScale = Vector3.one;
        }
    }

    // ─── Facing: voltear el modelo según dirección ────────────
    void ApplyFacing()
    {
        // Escalar en X para "voltear" el modelo en la dirección de movimiento
        Vector3 s = skinRoot.transform.localScale;
        float targetScaleX = Mathf.Sign(currentFacing) * Mathf.Abs(s.x);

        // Mantener el efecto de squash si está activo
        if (landSquashTimer <= 0f)
        {
            skinRoot.transform.localScale = new Vector3(targetScaleX, s.y, s.z);
        }
    }

    // ─── Efecto de polvo al aterrizar ─────────────────────────
    void SpawnDustEffect()
    {
        // Crear partículas de polvo simples usando esferas diminutas
        for (int i = 0; i < 6; i++)
        {
            GameObject dust = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            dust.name = "DustParticle";
            Destroy(dust.GetComponent<Collider>());
            dust.transform.position = transform.position + Vector3.down * 0.4f;
            dust.transform.localScale = Vector3.one * Random.Range(0.05f, 0.12f);

            Renderer r = dust.GetComponent<Renderer>();
            Material mat = new Material(Shader.Find("Standard"));
            mat.color = new Color(0.7f, 0.65f, 0.6f, 0.6f);
            // Hacer semitransparente
            mat.SetFloat("_Mode", 3); // Transparent
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = 3000;
            r.material = mat;

            // Dar velocidad aleatoria
            DustParticle dp = dust.AddComponent<DustParticle>();
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            dp.velocity = new Vector3(Mathf.Cos(angle) * Random.Range(1f, 3f),
                                      Random.Range(1f, 2.5f),
                                      Mathf.Sin(angle) * Random.Range(1f, 3f));
            dp.lifetime = Random.Range(0.3f, 0.6f);
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  Detección de suelo (reutiliza la del PlayerController)
    // ═══════════════════════════════════════════════════════════

    bool IsGrounded()
    {
        // Intentar leer del PlayerController si está disponible
        PlayerController pc = GetComponent<PlayerController>();
        if (pc != null && pc.groundCheck != null)
        {
            return Physics.CheckSphere(pc.groundCheck.position, pc.groundDistance, pc.groundLayer);
        }
        // Fallback: raycast hacia abajo
        return Physics.Raycast(transform.position, Vector3.down, 0.6f);
    }

    // ═══════════════════════════════════════════════════════════
    //  Utilidades
    // ═══════════════════════════════════════════════════════════

    GameObject MakePart(string name, PrimitiveType type)
    {
        GameObject obj = GameObject.CreatePrimitive(type);
        obj.name = name;
        obj.transform.SetParent(skinRoot.transform, false);
        Collider col = obj.GetComponent<Collider>();
        if (col != null) Destroy(col);
        return obj;
    }

    void SetColor(GameObject obj, Color color)
    {
        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material mat = new Material(Shader.Find("Standard"));
            mat.color = color;
            mat.SetFloat("_Glossiness", 0.05f);
            mat.SetFloat("_Metallic", 0.0f);
            renderer.material = mat;
        }
    }

    void LateUpdate()
    {
        if (dustTimer > 0f)
            dustTimer -= Time.deltaTime;
    }

    void OnDestroy()
    {
        if (skinRoot != null)
            Destroy(skinRoot);
    }
}

// ═══════════════════════════════════════════════════════════════
//  Componente auxiliar para las partículas de polvo
// ═══════════════════════════════════════════════════════════════
public class DustParticle : MonoBehaviour
{
    public Vector3 velocity;
    public float lifetime = 0.5f;
    private float age = 0f;
    private Vector3 initialScale;

    void Start()
    {
        initialScale = transform.localScale;
    }

    void Update()
    {
        age += Time.deltaTime;
        if (age >= lifetime)
        {
            Destroy(gameObject);
            return;
        }

        // Mover la partícula
        velocity += Vector3.down * 5f * Time.deltaTime; // Gravedad suave
        transform.position += velocity * Time.deltaTime;

        // Encoger con el tiempo
        float t = 1f - (age / lifetime);
        transform.localScale = initialScale * t;

        // Desvanecer
        Renderer r = GetComponent<Renderer>();
        if (r != null)
        {
            Color c = r.material.color;
            c.a = t * 0.6f;
            r.material.color = c;
        }
    }
}
