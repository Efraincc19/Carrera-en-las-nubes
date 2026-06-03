using UnityEngine;

/// <summary>
/// Construye un modelo de Hornet de Hollow Knight: Silksong con primitivas de Unity
/// e implementa animaciones procedurales: idle, caminar, saltar, caer, aterrizar.
/// Lee el estado del PlayerController y el Rigidbody para determinar qué animación reproducir.
/// </summary>
public class SilksongSkin : MonoBehaviour
{
    // ─── Colores ───────────────────────────────────────────────
    [Header("Colores de Hornet")]
    public Color cloakColor  = new Color(0.55f, 0.08f, 0.08f, 1f);   // Crimson red
    public Color maskColor   = new Color(0.95f, 0.93f, 0.90f, 1f);   // Warm white porcelain
    public Color eyeColor    = new Color(0.03f, 0.03f, 0.06f, 1f);   // Dark voids
    public Color hornColor   = new Color(0.85f, 0.83f, 0.80f, 1f);   // Bone/ivory
    public Color needleColor = new Color(0.75f, 0.75f, 0.80f, 1f);   // Silver steel
    public Color silkColor   = new Color(0.90f, 0.15f, 0.15f, 1f);   // Bright red silk

    // ─── Ajustes de animación ──────────────────────────────────
    [Header("Animación")]
    public float walkBobSpeed     = 14f;    // Más rápido, más ágil
    public float walkBobAmount    = 0.05f;  // Pasos más ligeros
    public float walkTiltAmount   = 6f;     // Menos inclinación, más elegante
    public float cloakSwaySpeed   = 10f;    // Capa más rápida
    public float cloakSwayAmount  = 0.12f;  // Movimiento de capa más dramático
    public float idleBreathSpeed  = 3f;     // Respiración ligeramente más rápida
    public float idleBreathAmount = 0.012f; // Más sutil
    public float landSquashTime   = 0.12f;  // Recuperación más rápida, más ágil
    public float turnSpeed        = 15f;    // Giro más rápido

    // ─── Partidas internas ─────────────────────────────────────
    private GameObject skinRoot;
    private Rigidbody rb;

    // Referencias a partes del modelo para animar
    private Transform bodyT;
    private Transform cloakBottomT;
    private Transform cloakTipT;
    private Transform cloakTailT;
    private Transform headT;
    private Transform eyeLeftT;
    private Transform eyeRightT;
    private Transform hornT;
    private Transform needleT;
    private Transform needleHandleT;
    private Transform silkDetailT;
    private Transform maskDetailT;

    // Posiciones/escalas/rotaciones originales para interpolar
    private Vector3 bodyBasePos, bodyBaseScale;
    private Vector3 cloakBottomBasePos, cloakBottomBaseScale;
    private Vector3 cloakTipBasePos, cloakTipBaseScale;
    private Vector3 cloakTailBasePos, cloakTailBaseScale;
    private Vector3 headBasePos;
    private Vector3 needleBasePos;
    private Quaternion needleBaseRot;
    private Vector3 needleHandleBasePos;
    private Vector3 silkDetailBasePos;
    private Quaternion silkDetailBaseRot;

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
        skinRoot = new GameObject("Silksong_Skin");
        skinRoot.transform.SetParent(transform, false);
        skinRoot.transform.localPosition = Vector3.zero;
        skinRoot.transform.localRotation = Quaternion.identity;

        // ── Cuerpo / Capa ── (más delgado que el Knight)
        GameObject body = MakePart("Body", PrimitiveType.Capsule);
        body.transform.localPosition = new Vector3(0f, -0.05f, 0f);
        body.transform.localScale = new Vector3(0.48f, 0.38f, 0.35f);
        SetColor(body, cloakColor);
        bodyT = body.transform;

        GameObject cloakBottom = MakePart("CloakBottom", PrimitiveType.Sphere);
        cloakBottom.transform.localPosition = new Vector3(0f, -0.28f, 0f);
        cloakBottom.transform.localScale = new Vector3(0.65f, 0.35f, 0.50f);
        SetColor(cloakBottom, cloakColor);
        cloakBottomT = cloakBottom.transform;

        GameObject cloakTip = MakePart("CloakTip", PrimitiveType.Sphere);
        cloakTip.transform.localPosition = new Vector3(0f, -0.48f, 0f);
        cloakTip.transform.localScale = new Vector3(0.20f, 0.12f, 0.18f);
        SetColor(cloakTip, cloakColor * 0.85f);
        cloakTipT = cloakTip.transform;

        // ── Cola de capa (pieza extra para la capa larga de Hornet) ──
        GameObject cloakTail = MakePart("CloakTail", PrimitiveType.Sphere);
        cloakTail.transform.localPosition = new Vector3(0f, -0.55f, -0.05f);
        cloakTail.transform.localScale = new Vector3(0.12f, 0.08f, 0.15f);
        SetColor(cloakTail, cloakColor * 0.80f);
        cloakTailT = cloakTail.transform;

        // ── Cabeza / Máscara ── (más angular, mentón puntiagudo)
        GameObject head = MakePart("Head", PrimitiveType.Sphere);
        head.transform.localPosition = new Vector3(0f, 0.32f, 0f);
        head.transform.localScale = new Vector3(0.48f, 0.42f, 0.38f);
        SetColor(head, maskColor);
        headT = head.transform;

        // ── Ojos ── (ligeramente más angulares / altos)
        GameObject eyeL = MakePart("EyeLeft", PrimitiveType.Sphere);
        eyeL.transform.localPosition = new Vector3(-0.09f, 0.32f, 0.17f);
        eyeL.transform.localScale = new Vector3(0.09f, 0.16f, 0.05f);
        SetColor(eyeL, eyeColor);
        eyeLeftT = eyeL.transform;

        GameObject eyeR = MakePart("EyeRight", PrimitiveType.Sphere);
        eyeR.transform.localPosition = new Vector3(0.09f, 0.32f, 0.17f);
        eyeR.transform.localScale = new Vector3(0.09f, 0.16f, 0.05f);
        SetColor(eyeR, eyeColor);
        eyeRightT = eyeR.transform;

        // ── Cuerno único ── (centro, curvado hacia adelante)
        GameObject horn = MakePart("Horn", PrimitiveType.Capsule);
        horn.transform.localPosition = new Vector3(0f, 0.62f, 0.02f);
        horn.transform.localRotation = Quaternion.Euler(-15f, 0f, 0f);
        horn.transform.localScale = new Vector3(0.05f, 0.20f, 0.05f);
        SetColor(horn, hornColor);
        hornT = horn.transform;

        // ── Aguja (arma) ── (mucho más delgada y larga que el nail)
        GameObject needle = MakePart("Needle", PrimitiveType.Cube);
        needle.transform.localPosition = new Vector3(0.30f, -0.05f, 0.08f);
        needle.transform.localRotation = Quaternion.Euler(0f, 0f, -12f);
        needle.transform.localScale = new Vector3(0.025f, 0.70f, 0.025f);
        SetColor(needle, needleColor);
        needleT = needle.transform;

        // ── Mango de la aguja ──
        GameObject needleHandle = MakePart("NeedleHandle", PrimitiveType.Cube);
        needleHandle.transform.localPosition = new Vector3(0.28f, -0.30f, 0.08f);
        needleHandle.transform.localRotation = Quaternion.Euler(0f, 0f, -12f);
        needleHandle.transform.localScale = new Vector3(0.06f, 0.03f, 0.03f);
        SetColor(needleHandle, needleColor * 0.8f);
        needleHandleT = needleHandle.transform;

        // ── Hilo de seda rojo en la aguja ──
        GameObject silkDetail = MakePart("SilkDetail", PrimitiveType.Cube);
        silkDetail.transform.localPosition = new Vector3(0.28f, -0.35f, 0.08f);
        silkDetail.transform.localRotation = Quaternion.Euler(0f, 0f, -15f);
        silkDetail.transform.localScale = new Vector3(0.015f, 0.18f, 0.015f);
        SetColor(silkDetail, silkColor);
        silkDetailT = silkDetail.transform;

        // ── Detalle de máscara (grieta vertical en la máscara de Hornet) ──
        GameObject maskDetail = MakePart("MaskDetail", PrimitiveType.Cube);
        maskDetail.transform.localPosition = new Vector3(0f, 0.34f, 0.19f);
        maskDetail.transform.localScale = new Vector3(0.015f, 0.18f, 0.01f);
        SetColor(maskDetail, new Color(0.82f, 0.80f, 0.78f, 1f));
        maskDetailT = maskDetail.transform;
    }

    void CacheBaseTransforms()
    {
        bodyBasePos          = bodyT.localPosition;
        bodyBaseScale        = bodyT.localScale;
        cloakBottomBasePos   = cloakBottomT.localPosition;
        cloakBottomBaseScale = cloakBottomT.localScale;
        cloakTipBasePos      = cloakTipT.localPosition;
        cloakTipBaseScale    = cloakTipT.localScale;
        cloakTailBasePos     = cloakTailT.localPosition;
        cloakTailBaseScale   = cloakTailT.localScale;
        headBasePos          = headT.localPosition;
        needleBasePos        = needleT.localPosition;
        needleBaseRot        = needleT.localRotation;
        needleHandleBasePos  = needleHandleT.localPosition;
        silkDetailBasePos    = silkDetailT.localPosition;
        silkDetailBaseRot    = silkDetailT.localRotation;
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

        // Cola de la capa - ondulación lenta y sutil
        float tailSway = Mathf.Sin(animTime * idleBreathSpeed * 0.5f) * 0.008f;
        cloakTailT.localPosition = cloakTailBasePos + new Vector3(tailSway, breathNorm * 0.15f, 0f);
        cloakTailT.localScale = cloakTailBaseScale;

        // Aguja descansa en posición base
        needleT.localPosition = needleBasePos + Vector3.up * breathNorm * 0.3f;
        needleT.localRotation = needleBaseRot;
        needleHandleT.localPosition = needleHandleBasePos + Vector3.up * breathNorm * 0.3f;

        // Hilo de seda oscila muy levemente en idle
        float silkSway = Mathf.Sin(animTime * idleBreathSpeed * 1.2f) * 0.005f;
        silkDetailT.localPosition = silkDetailBasePos + new Vector3(silkSway, breathNorm * 0.2f, 0f);
        silkDetailT.localRotation = silkDetailBaseRot;
    }

    // ─── Walk: rebote, balanceo de capa, inclinación ──────────
    void AnimateWalk(float dt)
    {
        float bob = Mathf.Sin(animTime * walkBobSpeed);
        float bobAbs = Mathf.Abs(bob);
        float bobY = bobAbs * walkBobAmount;

        // Cuerpo sube y baja con cada paso + inclinación lateral (más sutil)
        float tilt = bob * walkTiltAmount;
        bodyT.localPosition = bodyBasePos + Vector3.up * bobY;
        bodyT.localScale = bodyBaseScale;
        bodyT.localRotation = Quaternion.Euler(0, 0, tilt * currentFacing);

        // Cabeza rebota
        headT.localPosition = headBasePos + Vector3.up * bobY * 0.7f;

        // Capa inferior se balancea opuesta al cuerpo (efecto de inercia) - más dramático
        float cloakSway = Mathf.Sin(animTime * cloakSwaySpeed - 0.5f) * cloakSwayAmount;
        cloakBottomT.localPosition = cloakBottomBasePos +
            new Vector3(cloakSway * currentFacing, bobY * 0.4f, 0f);
        cloakBottomT.localScale = cloakBottomBaseScale +
            new Vector3(bobAbs * 0.06f, -bobAbs * 0.04f, 0f);

        // Punta de la capa ondea más agresivamente
        float tipSway = Mathf.Sin(animTime * cloakSwaySpeed * 1.3f - 1f) * cloakSwayAmount * 1.5f;
        cloakTipT.localPosition = cloakTipBasePos +
            new Vector3(tipSway * currentFacing, bobY * 0.2f, 0f);
        cloakTipT.localScale = cloakTipBaseScale + new Vector3(bobAbs * 0.05f, 0f, 0f);

        // Cola de la capa sigue a la punta con retraso (más fluido)
        float tailSway = Mathf.Sin(animTime * cloakSwaySpeed * 1.1f - 1.8f) * cloakSwayAmount * 1.8f;
        cloakTailT.localPosition = cloakTailBasePos +
            new Vector3(tailSway * currentFacing, bobY * 0.15f, -bobAbs * 0.02f);
        cloakTailT.localScale = cloakTailBaseScale + new Vector3(bobAbs * 0.03f, 0f, bobAbs * 0.02f);

        // Aguja se balancea al caminar
        float needleSwing = Mathf.Sin(animTime * walkBobSpeed * 0.8f) * 8f;
        needleT.localPosition = needleBasePos + Vector3.up * bobY * 0.5f;
        needleT.localRotation = needleBaseRot * Quaternion.Euler(0, 0, needleSwing);
        needleHandleT.localPosition = needleHandleBasePos + Vector3.up * bobY * 0.5f;

        // Hilo de seda ondea con la aguja al caminar
        float silkSwing = Mathf.Sin(animTime * cloakSwaySpeed * 1.5f - 0.8f) * 0.02f;
        silkDetailT.localPosition = silkDetailBasePos +
            new Vector3(silkSwing * currentFacing, bobY * 0.3f, 0f);
        silkDetailT.localRotation = silkDetailBaseRot * Quaternion.Euler(0, 0, needleSwing * 1.2f);
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

        // Capa se abre/expande hacia abajo - efecto más dramático (Hornet es más aérea)
        float cloakFlare = Mathf.Lerp(0.10f, 0.20f, jumpPhase);
        cloakBottomT.localPosition = cloakBottomBasePos + new Vector3(0f, -0.08f, 0f);
        cloakBottomT.localScale = cloakBottomBaseScale + new Vector3(cloakFlare, -0.10f, cloakFlare * 0.6f);

        // Punta de la capa baja y se expande
        cloakTipT.localPosition = cloakTipBasePos + new Vector3(0f, -0.12f, 0f);
        cloakTipT.localScale = cloakTipBaseScale + new Vector3(0.12f, -0.03f, 0.10f);

        // Cola de la capa se extiende hacia abajo en el salto
        float tailFlutter = Mathf.Sin(animTime * 12f) * 0.02f;
        cloakTailT.localPosition = cloakTailBasePos + new Vector3(tailFlutter, -0.15f, -0.03f);
        cloakTailT.localScale = cloakTailBaseScale + new Vector3(0.04f, 0.03f, 0.06f);

        // Aguja apunta ligeramente hacia arriba
        needleT.localPosition = needleBasePos + new Vector3(0.03f, 0.08f, 0f);
        needleT.localRotation = needleBaseRot * Quaternion.Euler(0, 0, 10f);
        needleHandleT.localPosition = needleHandleBasePos + new Vector3(0.03f, 0.08f, 0f);

        // Hilo de seda se estira detrás de la aguja
        float silkTrail = Mathf.Sin(animTime * 8f) * 0.015f;
        silkDetailT.localPosition = silkDetailBasePos + new Vector3(silkTrail, 0.06f, -0.02f);
        silkDetailT.localRotation = silkDetailBaseRot * Quaternion.Euler(5f, 0, 8f);
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

        // Capa flamea HACIA ARRIBA - efecto de viento (más dramático, Hornet usa seda)
        float flameOffset = fallSpeed * 0.15f;
        float wobble = Mathf.Sin(animTime * 15f) * 0.04f * fallSpeed;
        cloakBottomT.localPosition = cloakBottomBasePos + new Vector3(wobble, flameOffset, 0f);
        cloakBottomT.localScale = cloakBottomBaseScale + new Vector3(-0.06f * fallSpeed, 0.12f * fallSpeed, -0.04f * fallSpeed);

        // Punta de la capa sube y ondea
        float tipWobble = Mathf.Sin(animTime * 18f + 1f) * 0.06f * fallSpeed;
        cloakTipT.localPosition = cloakTipBasePos + new Vector3(tipWobble, flameOffset * 1.5f, 0f);
        cloakTipT.localScale = cloakTipBaseScale + new Vector3(-0.06f * fallSpeed, 0.06f * fallSpeed, 0f);

        // Cola de la capa aletea más en la caída
        float tailWobble = Mathf.Sin(animTime * 20f + 2f) * 0.07f * fallSpeed;
        float tailLift = fallSpeed * 0.18f;
        cloakTailT.localPosition = cloakTailBasePos + new Vector3(tailWobble, tailLift, 0.03f * fallSpeed);
        cloakTailT.localScale = cloakTailBaseScale + new Vector3(-0.03f * fallSpeed, 0.04f * fallSpeed, 0.02f * fallSpeed);

        // Aguja se balancea con la caída
        float needleSway = Mathf.Sin(animTime * 10f) * 5f * fallSpeed;
        needleT.localPosition = needleBasePos + Vector3.up * 0.03f * fallSpeed;
        needleT.localRotation = needleBaseRot * Quaternion.Euler(0, 0, needleSway);
        needleHandleT.localPosition = needleHandleBasePos + Vector3.up * 0.03f * fallSpeed;

        // Hilo de seda aletea dramáticamente en la caída
        float silkFlutter = Mathf.Sin(animTime * 14f + 0.5f) * 0.03f * fallSpeed;
        silkDetailT.localPosition = silkDetailBasePos + new Vector3(silkFlutter, 0.04f * fallSpeed, -0.01f * fallSpeed);
        silkDetailT.localRotation = silkDetailBaseRot * Quaternion.Euler(0, 0, needleSway * 1.5f);
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

    // ─── Efecto de polvo al aterrizar (rojo, seda) ────────────
    void SpawnDustEffect()
    {
        // Crear partículas de polvo rojas simples usando esferas diminutas
        for (int i = 0; i < 6; i++)
        {
            GameObject dust = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            dust.name = "DustParticle";
            Destroy(dust.GetComponent<Collider>());
            dust.transform.position = transform.position + Vector3.down * 0.4f;
            dust.transform.localScale = Vector3.one * Random.Range(0.05f, 0.12f);

            Renderer r = dust.GetComponent<Renderer>();
            Material mat = new Material(Shader.Find("Standard"));
            mat.color = new Color(0.85f, 0.2f, 0.2f, 0.6f);
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
