/*
 * NOTE DI SVILUPPO:
 * I calcoli matematici avanzati, il modello fisico arcade (aderenza laterale, deriva,
 * sovrasterzo, curve di accelerazione) e le logiche complesse presenti in questo script
 * sono stati sviluppati con l'ausilio di un'Intelligenza Artificiale.
 */

using UnityEngine;

/// <summary>
/// Controllo dell'auto con fisica arcade: disaccoppiato da Input System per supportare sia Player che Bot.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class BumperCarController : MonoBehaviour
{
    private const float InputDeadzone = 0.05f;
    private const float StopThreshold = 0.15f;
    private const float MinSteerSpeed = 0.3f;
    private const float FallLimitY = -10f;
    private const float GroundNormalMinY = 0.78f;
    private const float FlatGroundNormalMinY = 0.995f;
    private const float GroundGraceTime = 0.15f;

    [Header("Movimento & Risposta (Anti-Glide)")]
    [Tooltip("Velocità massima in avanti (m/s).")]
    [SerializeField, Min(0f)] private float maxSpeed = 50f;

    [Tooltip("Velocità massima in retromarcia (m/s).")]
    [SerializeField, Min(0f)] private float maxReverseSpeed = 12f;

    [Tooltip("Accelerazione massima raggiunta ad alte velocità (m/s²).")]
    [SerializeField, Min(0f)] private float maxAcceleration = 28f;

    [Tooltip("Accelerazione in retromarcia (m/s²).")]
    [SerializeField, Min(0f)] private float reverseAcceleration = 14f;

    [Tooltip("Intensità della frenata quando cambi direzione (m/s²).")]
    [SerializeField, Min(0f)] private float braking = 40f;

    [Tooltip("Rallentamento deciso al rilascio dei comandi per eliminare l'effetto glide (m/s²).")]
    [SerializeField, Min(0f)] private float coastDeceleration = 12f;

    [Header("Sterzo & Inerzia")]
    [Tooltip("Rotazione massima dell'auto (gradi/s).")]
    [SerializeField, Min(0f)] private float turnSpeed = 130f;

    [Tooltip("Velocità alla quale lo sterzo raggiunge la massima efficacia (m/s).")]
    [SerializeField, Min(0.01f)] private float turnReferenceSpeed = 16f;

    [Tooltip("Frazione di sterzo disponibile a bassa velocità.")]
    [SerializeField, Range(0f, 1f)] private float lowSpeedTurnFactor = 0.35f;

    [Tooltip("Rapidità di risposta dello sterzo agli input (unità/s).")]
    [SerializeField, Min(0.1f)] private float steerResponse = 9f;

    [Tooltip("Inerzia con cui lo sterzo reagisce quando l'auto scivola.")]
    [SerializeField, Min(0.5f)] private float steeringInertia = 4f;

    [Header("Aderenza & Drift Dinamico")]
    [Tooltip("Aderenza laterale in guida normale (1/s).")]
    [SerializeField, Min(0f)] private float lateralGrip = 20f;

    [Tooltip("Aderenza laterale base durante il drift a bassa velocità (1/s).")]
    [SerializeField, Min(0f)] private float driftBaseGrip = 2.5f;

    [Tooltip("Aderenza minima in drift ad altissima velocità e sterzata massima.")]
    [SerializeField, Min(0f)] private float driftMinHighSpeedGrip = 0.05f;

    [Tooltip("Velocità con cui l'aderenza transita tra normale e drift.")]
    [SerializeField, Min(1f)] private float gripTransitionRate = 10f;

    [Tooltip("Moltiplicatore di sterzata base durante il drift.")]
    [SerializeField, Min(1f)] private float driftTurnMultiplier = 1.8f;

    [Tooltip("Sovrasterzo naturale causato dallo scivolamento laterale (alimenta i testacoda liberi).")]
    [SerializeField, Min(0f)] private float slipOversteerFactor = 2.0f;

    [Tooltip("Moltiplicatore della velocità massima in drift.")]
    [SerializeField, Range(0.3f, 1f)] private float driftMaxSpeedMultiplier = 0.75f;

    [Tooltip("Velocità minima necessaria per iniziare a derapare (m/s).")]
    [SerializeField, Min(0f)] private float minDriftSpeed = 5f;

    [Header("Stabilità & Terreno")]
    [Tooltip("Deportanza per unità di velocità (m/s² per m/s).")]
    [SerializeField, Min(0f)] private float downforce = 1.5f;

    [Tooltip("Deportanza massima applicabile (m/s²).")]
    [SerializeField, Min(0f)] private float maxDownforceAcceleration = 25f;

    [Tooltip("Velocità verticale massima tollerata su terreno piano per assorbire i dossi (m/s).")]
    [SerializeField, Min(0f)] private float maxGroundUpSpeed = 1.5f;

    [Tooltip("Altezza del baricentro (valori negativi evitano ribaltamenti).")]
    [SerializeField] private float centerOfMassHeight = -0.3f;

    [Tooltip("Blocca rollio e beccheggio fisici del Rigidbody.")]
    [SerializeField] private bool freezeTilt = true;

    [Tooltip("Allinea l'inclinazione dell'auto alla normale del terreno.")]
    [SerializeField] private bool alignToGround = true;

    [Tooltip("Rapidità di allineamento all'inclinazione del terreno (1/s).")]
    [SerializeField, Min(0.1f)] private float groundAlignmentSpeed = 10f;

    [Tooltip("Angolo minimo (gradi) per attivare l'inclinazione sul terreno.")]
    [SerializeField, Min(0f)] private float minTiltAngle = 3f;

    [Tooltip("Rapidità di smussamento della normale del terreno (1/s).")]
    [SerializeField, Min(0.1f)] private float groundNormalSmoothing = 8f;

    [Tooltip("Inclina solo il modello 3D se il Rigidbody ha la rotazione X/Z bloccata.")]
    [SerializeField] private bool tiltVisualOnly = true;

    [Tooltip("Angolo massimo (gradi) di inclinazione consentito su rampe o dossi.")]
    [SerializeField, Range(5f, 45f)] private float maxGroundTiltAngle = 28f;

    [Tooltip("Layer considerati come terreno per il rilevamento della pendenza.")]
    [SerializeField] private LayerMask groundLayerMask = ~0;

    [Header("Fisica")]
    [Tooltip("Applica un materiale privo di attrito ai collider dell'auto.")]
    [SerializeField] private bool useFrictionlessMaterial = true;

    [Tooltip("Rimbalzo del materiale fisico dell'auto.")]
    [SerializeField, Range(0f, 1f)] private float bounciness = 0f;

    private Rigidbody body;
    private PhysicsMaterial carMaterial;
    private Transform visualPivot;

    private Vector3 spawnPosition;
    private Quaternion spawnRotation;

    private float inputThrottle;
    private float inputSteer;
    private bool inputDrift;

    private float smoothedSteer;
    private float effectiveSteer;
    private float currentGrip;
    private float currentMaxSpeedLimit;
    private float lastGroundedTime = float.NegativeInfinity;
    private Vector3 groundNormal = Vector3.up;
    private Vector3 smoothedGroundNormal = Vector3.up;

    public bool IsDrifting { get; private set; }
    public float CurrentSteer => smoothedSteer;
    public float EffectiveSteer => effectiveSteer;
    public Quaternion VisualTiltRotation => visualPivot != null ? visualPivot.localRotation : Quaternion.identity;
    public Transform VisualPivot => visualPivot;
    private bool IsGrounded => Time.fixedTime - lastGroundedTime <= GroundGraceTime;

    private void Awake()
    {
        InitializePhysics();

        if (alignToGround && freezeTilt && tiltVisualOnly)
            CreateVisualPivot();

        spawnPosition = transform.position;
        spawnRotation = transform.rotation;
        currentMaxSpeedLimit = maxSpeed;
        currentGrip = lateralGrip;
    }

    private void OnDestroy()
    {
        if (carMaterial != null)
            Destroy(carMaterial);
    }

    /// <summary>
    /// Loop fisico principale: aggiorna terreno, velocità longitudinale, aderenza laterale, sterzo e deportanza.
    /// </summary>
    private void FixedUpdate()
    {
        if (transform.position.y < FallLimitY)
        {
            ResetForEpisode(spawnPosition, spawnRotation);
            return;
        }

        float dt = Time.fixedDeltaTime;
        smoothedSteer = Mathf.MoveTowards(smoothedSteer, inputSteer, steerResponse * dt);

        SampleGroundRaycast();
        bool grounded = IsGrounded;

        Vector3 targetNormal = grounded ? groundNormal : Vector3.up;
        smoothedGroundNormal = Vector3.Slerp(smoothedGroundNormal, targetNormal, ExpDecay(groundNormalSmoothing, dt)).normalized;

        // Smorza i rimbalzi verticali indesiderati su superfici piane
        Vector3 velocity = body.linearVelocity;
        if (grounded && smoothedGroundNormal.y >= FlatGroundNormalMinY && velocity.y > maxGroundUpSpeed)
            velocity.y = maxGroundUpSpeed;

        float forwardSpeed = Vector3.Dot(velocity, transform.forward);
        IsDrifting = grounded && inputDrift && velocity.magnitude >= minDriftSpeed;

        float targetMax = IsDrifting ? maxSpeed * driftMaxSpeedMultiplier : maxSpeed;
        currentMaxSpeedLimit = Mathf.MoveTowards(currentMaxSpeedLimit, targetMax, 10f * dt);

        if (grounded)
        {
            ApplyThrottle(ref velocity, forwardSpeed, dt);
            ApplyLateralGrip(ref velocity, dt);
            ApplySteering(velocity, forwardSpeed, dt);
        }

        body.linearVelocity = velocity;

        if (alignToGround && grounded)
            AlignRigidbodyToGround(dt);

        // Applica deportanza proporzionale alla velocità per mantenere il veicolo incollato alla pista
        float force = Mathf.Min(downforce * Mathf.Abs(forwardSpeed), maxDownforceAcceleration);
        if (force > 0f)
            body.AddForce(-transform.up * force, ForceMode.Acceleration);
    }

    /// <summary>
    /// Inclina visivamente il modello 3D della carrozzeria in base alla pendenza del terreno
    /// quando la rotazione fisica X/Z del Rigidbody è bloccata.
    /// </summary>
    private void LateUpdate()
    {
        if (visualPivot == null)
            return;

        Vector3 localUp = IsGrounded
            ? transform.InverseTransformDirection(smoothedGroundNormal).normalized
            : Vector3.up;

        Quaternion targetRotation = Vector3.Angle(localUp, Vector3.up) < minTiltAngle
            ? Quaternion.identity
            : Quaternion.FromToRotation(Vector3.up, localUp);

        visualPivot.localRotation = Quaternion.Slerp(visualPivot.localRotation, targetRotation, ExpDecay(groundAlignmentSpeed, Time.deltaTime));
    }

    private void OnCollisionEnter(Collision collision) => EvaluateGroundContacts(collision);
    private void OnCollisionStay(Collision collision) => EvaluateGroundContacts(collision);

    /// <summary>
    /// Riceve gli input normalizzati da PlayerCarInput o dall'AI dei Bot.
    /// </summary>
    public void SetInputs(float throttle, float steer, bool drift)
    {
        inputThrottle = Mathf.Clamp(throttle, -1f, 1f);
        inputSteer = Mathf.Clamp(steer, -1f, 1f);
        inputDrift = drift;

        if (Mathf.Abs(inputThrottle) > InputDeadzone || Mathf.Abs(inputSteer) > InputDeadzone)
            body.WakeUp();
    }

    /// <summary>
    /// Ripristina posizione, velocità e stati fisici interni del veicolo (usato per respawn o reset episodio).
    /// </summary>
    public void ResetForEpisode(Vector3 position, Quaternion rotation)
    {
        spawnPosition = position;
        spawnRotation = rotation;

        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
        body.position = position;
        body.rotation = rotation;

        inputThrottle = 0f;
        inputSteer = 0f;
        inputDrift = false;

        smoothedSteer = 0f;
        effectiveSteer = 0f;
        currentMaxSpeedLimit = maxSpeed;
        currentGrip = lateralGrip;
        IsDrifting = false;

        groundNormal = Vector3.up;
        smoothedGroundNormal = Vector3.up;
        lastGroundedTime = float.NegativeInfinity;

        if (visualPivot != null)
            visualPivot.localRotation = Quaternion.identity;

        body.WakeUp();
    }

    private void InitializePhysics()
    {
        body = GetComponent<Rigidbody>();
        body.linearDamping = 0.1f;
        body.angularDamping = 0f;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        body.centerOfMass = new Vector3(body.centerOfMass.x, centerOfMassHeight, body.centerOfMass.z);

        if (freezeTilt)
            body.constraints |= RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        if (!useFrictionlessMaterial)
            return;

        carMaterial = new PhysicsMaterial("BumperCar_NoFriction")
        {
            dynamicFriction = 0f,
            staticFriction = 0f,
            frictionCombine = PhysicsMaterialCombine.Minimum,
            bounciness = bounciness,
            bounceCombine = PhysicsMaterialCombine.Minimum
        };

        foreach (Collider col in GetComponentsInChildren<Collider>())
        {
            if (!col.isTrigger)
                col.sharedMaterial = carMaterial;
        }
    }

    /// <summary>
    /// Crea un nodo pivot intermedio e vi sposta tutte le mesh visive, escludendo telecamere e collider
    /// così da inclinare solo la scocca senza alterare la fisica o il punto di vista.
    /// </summary>
    private void CreateVisualPivot()
    {
        visualPivot = new GameObject("VisualTilt").transform;
        visualPivot.SetParent(transform, false);

        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            bool hasCameraOrCollider = child.GetComponentInChildren<Camera>(true) != null
                                    || child.GetComponentInChildren<Collider>(true) != null;

            if (child != visualPivot && !hasCameraOrCollider)
                child.SetParent(visualPivot, true);
        }
    }

    /// <summary>
    /// Gestisce accelerazione progressiva, frenata contro-sterzo, retromarcia e decelerazione anti-glide.
    /// </summary>
    private void ApplyThrottle(ref Vector3 velocity, float forwardSpeed, float dt)
    {
        float targetSpeed = 0f;
        float accelRate = coastDeceleration;

        if (inputThrottle > 0.01f)
        {
            if (forwardSpeed < -StopThreshold)
            {
                accelRate = braking;
            }
            else
            {
                targetSpeed = currentMaxSpeedLimit * inputThrottle;
                // Curva di potenza non lineare (esponente 2.2): partenza morbida e spinta progressiva verso maxSpeed
                float speedRatio = Mathf.Clamp01(Mathf.Max(forwardSpeed, 0f) / Mathf.Max(maxSpeed, 0.01f));
                accelRate = forwardSpeed > targetSpeed
                    ? 8f
                    : maxAcceleration * Mathf.Lerp(0.25f, 1f, Mathf.Pow(speedRatio, 2.2f));
            }
        }
        else if (inputThrottle < -0.01f)
        {
            if (forwardSpeed > StopThreshold)
            {
                accelRate = braking;
            }
            else
            {
                targetSpeed = maxReverseSpeed * inputThrottle;
                accelRate = reverseAcceleration;
            }
        }
        else if (Mathf.Abs(forwardSpeed) < StopThreshold)
        {
            // Azzera completamente il residuo longitudinale per evitare micro-scivolamenti da fermo
            velocity -= transform.forward * forwardSpeed;
            return;
        }

        float newSpeed = Mathf.MoveTowards(forwardSpeed, targetSpeed, accelRate * dt);
        velocity += transform.forward * (newSpeed - forwardSpeed);
    }

    /// <summary>
    /// Calcola l'aderenza laterale dinamica e smorza la componente di velocità perpendicolare all'auto.
    /// </summary>
    private void ApplyLateralGrip(ref Vector3 velocity, float dt)
    {
        float speedRatio = Mathf.Clamp01(velocity.magnitude / Mathf.Max(maxSpeed, 0.01f));
        float steerIntensity = Mathf.Abs(smoothedSteer);

        float targetGrip;
        if (IsDrifting)
        {
            // In drift l'aderenza cala non-linearmente al crescere di velocità e angolo di sterzata
            float lossFactor = Mathf.Clamp01(speedRatio * (0.5f + 0.5f * steerIntensity));
            targetGrip = Mathf.Lerp(driftBaseGrip, driftMinHighSpeedGrip, Mathf.Pow(lossFactor, 1.3f));
        }
        else
        {
            // Perdita progressiva di grip ad altissime velocità per simulare sottosterzo/deriva naturale
            float highSpeedLoss = speedRatio * speedRatio * 0.45f * (0.4f + 0.6f * steerIntensity);
            targetGrip = lateralGrip * (1f - highSpeedLoss);
        }

        currentGrip = Mathf.MoveTowards(currentGrip, targetGrip, gripTransitionRate * dt);

        // Proietta la velocità sull'asse laterale e la attenua tramite decadimento esponenziale frame-independent
        float lateralSpeed = Vector3.Dot(velocity, transform.right);
        velocity -= transform.right * (lateralSpeed * ExpDecay(currentGrip, dt));
    }

    /// <summary>
    /// Calcola la velocità angolare di imbardata (Yaw) combinando inerzia dello sterzo, direzione e sovrasterzo in drift.
    /// </summary>
    private void ApplySteering(Vector3 velocity, float forwardSpeed, float dt)
    {
        Vector3 angular = body.angularVelocity;
        float totalSpeed = velocity.magnitude;

        if (totalSpeed < MinSteerSpeed)
        {
            angular.y = 0f;
            body.angularVelocity = angular;
            effectiveSteer = 0f;
            return;
        }

        float dynamicInertia = IsDrifting ? steeringInertia * 0.5f : steeringInertia;
        effectiveSteer = Mathf.MoveTowards(effectiveSteer, smoothedSteer, dynamicInertia * dt);

        float speedFactor = Mathf.Lerp(lowSpeedTurnFactor, 1f, Mathf.Clamp01(totalSpeed / turnReferenceSpeed));
        float directionSign = forwardSpeed >= -StopThreshold ? 1f : -1f;
        float baseTurn = IsDrifting ? turnSpeed * driftTurnMultiplier : turnSpeed;
        float yawRateDeg = effectiveSteer * baseTurn * speedFactor * directionSign;

        if (IsDrifting)
        {
            // Calcola il rapporto di imbardata (slipRatio) e aggiunge una coppia di sovrasterzo proporzionale
            float lateralSpeed = Vector3.Dot(velocity, transform.right);
            float slipRatio = Mathf.Abs(lateralSpeed) / Mathf.Max(totalSpeed, 1f);
            float speedRatio = totalSpeed / Mathf.Max(maxSpeed, 0.01f);

            yawRateDeg += -Mathf.Sign(lateralSpeed) * slipRatio * speedRatio * slipOversteerFactor * turnSpeed * Mathf.Abs(effectiveSteer);
        }

        angular.y = yawRateDeg * Mathf.Deg2Rad;
        body.angularVelocity = angular;
    }

    /// <summary>
    /// Esegue uno SphereCast verso il basso per rilevare la normale reale della pista ignorando spigoli e altri veicoli.
    /// </summary>
    private void SampleGroundRaycast()
    {
        Vector3 origin = transform.position + Vector3.up * 0.4f;
        if (Physics.SphereCast(origin, 0.35f, Vector3.down, out RaycastHit hit, 0.75f, groundLayerMask, QueryTriggerInteraction.Ignore))
        {
            if (hit.rigidbody == null && hit.normal.y >= GroundNormalMinY)
            {
                groundNormal = ClampNormalAngle(hit.normal, maxGroundTiltAngle);
                lastGroundedTime = Time.fixedTime;
            }
        }
    }

    /// <summary>
    /// Media le normali dei punti di contatto statici per aggiornare lo stato di contatto col terreno.
    /// </summary>
    private void EvaluateGroundContacts(Collision collision)
    {
        if (collision.rigidbody != null)
            return;

        Vector3 normalSum = Vector3.zero;
        int validContacts = 0;

        for (int i = 0; i < collision.contactCount; i++)
        {
            Vector3 normal = collision.GetContact(i).normal;
            if (normal.y >= GroundNormalMinY)
            {
                normalSum += normal;
                validContacts++;
            }
        }

        if (validContacts > 0)
        {
            groundNormal = ClampNormalAngle(normalSum.normalized, maxGroundTiltAngle);
            lastGroundedTime = Time.fixedTime;
        }
    }

    /// <summary>
    /// Allinea la rotazione fisica del Rigidbody alla pendenza del terreno (attivo solo se freezeTilt è false).
    /// </summary>
    private void AlignRigidbodyToGround(float dt)
    {
        if (freezeTilt || Vector3.Angle(smoothedGroundNormal, Vector3.up) < minTiltAngle)
            return;

        Vector3 up = smoothedGroundNormal;
        Vector3 forward = Vector3.ProjectOnPlane(transform.forward, up);

        if (forward.sqrMagnitude < 0.0001f)
            forward = Vector3.ProjectOnPlane(transform.right, up);

        if (forward.sqrMagnitude < 0.0001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(forward.normalized, up);
        body.MoveRotation(Quaternion.Slerp(body.rotation, targetRotation, ExpDecay(groundAlignmentSpeed, dt)));
    }

    /// <summary>
    /// Limita l'angolo massimo tra la normale rilevata e la verticale assoluta per evitare ribaltamenti su pareti/ostacoli.
    /// </summary>
    private static Vector3 ClampNormalAngle(Vector3 normal, float maxAngleDegrees)
    {
        return Vector3.Angle(Vector3.up, normal) <= maxAngleDegrees
            ? normal
            : Vector3.RotateTowards(Vector3.up, normal, maxAngleDegrees * Mathf.Deg2Rad, 0f).normalized;
    }

    /// <summary>
    /// Fattore di interpolazione esponenziale indipendente dal framerate: 1 - e^(-rate * dt).
    /// </summary>
    private static float ExpDecay(float rate, float dt) => 1f - Mathf.Exp(-rate * dt);
}