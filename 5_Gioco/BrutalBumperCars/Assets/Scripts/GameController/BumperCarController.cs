using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Controllo dell'auto con fisica arcade ottimizzata: zero glide eccessivo,
/// transizioni di frenata/retromarcia coerenti, derapata fluida e rotazione libera al 100%.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class BumperCarController : MonoBehaviour
{
    #region Costanti & Parametri Interni Fissi

    private const float InputDeadzone = 0.05f;
    private const float StopThreshold = 0.15f;
    private const float MinSteerSpeed = 0.3f;
    private const float FallLimitY = -10f;
    private const float GroundNormalMinY = 0.5f;
    private const float FlatGroundNormalMinY = 0.995f;
    private const float GroundGraceTime = 0.15f;

    // Configurazione bilanciata fissa per evitare configurazioni errate nell'Inspector
    private const float DefaultMaxAcceleration = 28f;
    private const float DefaultStartAccelerationFactor = 0.25f;
    private const float DefaultAccelerationEasePower = 2.2f;
    private const float DefaultMaxSpeed = 50f;
    private const float DefaultLateralGrip = 20f;
    private const float DefaultDriftTurnMultiplier = 1.8f;
    private const float DefaultSlipOversteerFactor = 2.0f;

    #endregion

    #region Inspector - Configurazione

    [Header("Input")]
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference driftAction;

    [Header("Movimento & Risposta (Anti-Glide)")]
    [Tooltip("Velocità massima in avanti (m/s).")]
    [SerializeField, Min(0f)] private float maxSpeed = DefaultMaxSpeed;

    [Tooltip("Velocità massima in retromarcia (m/s).")]
    [SerializeField, Min(0f)] private float maxReverseSpeed = 12f;

    [Tooltip("Accelerazione massima raggiunta ad alte velocità (m/s²).")]
    [SerializeField, Min(0f)] private float maxAcceleration = DefaultMaxAcceleration;

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
    [SerializeField, Min(0f)] private float lateralGrip = DefaultLateralGrip;

    [Tooltip("Aderenza laterale base durante il drift a bassa velocità (1/s).")]
    [SerializeField, Min(0f)] private float driftBaseGrip = 2.5f;

    [Tooltip("Aderenza minima in drift ad altissima velocità e sterzata massima.")]
    [SerializeField, Min(0f)] private float driftMinHighSpeedGrip = 0.05f;

    [Tooltip("Velocità con cui l'aderenza transita tra normale e drift.")]
    [SerializeField, Min(1f)] private float gripTransitionRate = 10f;

    [Tooltip("Moltiplicatore di sterzata base durante il drift.")]
    [SerializeField, Min(1f)] private float driftTurnMultiplier = DefaultDriftTurnMultiplier;

    [Tooltip("Sovrasterzo naturale causato dallo scivolamento laterale (alimenta i testacoda liberi).")]
    [SerializeField, Min(0f)] private float slipOversteerFactor = DefaultSlipOversteerFactor;

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

    [Header("Fisica")]
    [Tooltip("Applica un materiale privo di attrito ai collider dell'auto.")]
    [SerializeField] private bool useFrictionlessMaterial = true;

    [Tooltip("Rimbalzo del materiale fisico dell'auto.")]
    [SerializeField, Range(0f, 1f)] private float bounciness = 0f;

    #endregion

    #region Stato Interno & Riferimenti

    private Rigidbody body;
    private InputAction move;
    private InputAction drift;
    private PhysicsMaterial carMaterial;
    private Transform visualPivot;

    private Vector3 spawnPosition;
    private Quaternion spawnRotation;

    private float smoothedSteer;
    private float effectiveSteer;
    private float currentGrip;
    private float currentMaxSpeedLimit;
    private float lastGroundedTime = float.NegativeInfinity;
    private Vector3 groundNormal = Vector3.up;
    private Vector3 smoothedGroundNormal = Vector3.up;

    public bool IsDrifting { get; private set; }
    private bool IsGrounded => Time.fixedTime - lastGroundedTime <= GroundGraceTime;

    #endregion

    #region Ciclo di Vita Unity

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

    private void OnEnable()
    {
        move = ResolveAction(moveAction, "Player/Move");
        drift = ResolveAction(driftAction, "Player/Drift");

        if (move == null)
        {
            enabled = false;
            return;
        }

        move.Enable();
        drift?.Enable();
    }

    private void OnDisable()
    {
        move?.Disable();
        drift?.Disable();
    }

    private void OnDestroy()
    {
        if (carMaterial != null)
            Destroy(carMaterial);
    }

    private void FixedUpdate()
    {
        if (transform.position.y < FallLimitY)
        {
            Respawn();
            return;
        }

        ReadInput(out float throttle, out float steerInput, out bool driftPressed);

        smoothedSteer = Mathf.MoveTowards(smoothedSteer, steerInput, steerResponse * Time.fixedDeltaTime);

        bool grounded = IsGrounded;
        UpdateSmoothedGroundNormal(grounded);

        Vector3 velocity = body.linearVelocity;
        if (grounded && smoothedGroundNormal.y >= FlatGroundNormalMinY && velocity.y > maxGroundUpSpeed)
            velocity.y = maxGroundUpSpeed;

        float forwardSpeed = Vector3.Dot(velocity, transform.forward);
        IsDrifting = grounded && driftPressed && velocity.magnitude >= minDriftSpeed;

        UpdateSpeedLimit();

        if (grounded)
        {
            ApplyThrottle(ref velocity, throttle, forwardSpeed);
            ApplyLateralGrip(ref velocity);
            ApplySteering(smoothedSteer, velocity);
        }

        body.linearVelocity = velocity;

        if (alignToGround && grounded)
            AlignRigidbodyToGround();

        ApplyDownforce(forwardSpeed);
    }

    private void LateUpdate()
    {
        UpdateVisualTilt();
    }

    private void OnCollisionEnter(Collision collision) => EvaluateGroundContacts(collision);
    private void OnCollisionStay(Collision collision) => EvaluateGroundContacts(collision);

    #endregion

    #region Inizializzazione & Input

    private void InitializePhysics()
    {
        body = GetComponent<Rigidbody>();
        body.linearDamping = 0.1f;
        body.angularDamping = 0f; // Rotazione completamente libera al 100% (nessun limite di angolo o damping)
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

    private void CreateVisualPivot()
    {
        visualPivot = new GameObject("VisualTilt").transform;
        visualPivot.SetParent(transform, false);

        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            bool isCameraOrCollider = child.GetComponentInChildren<Camera>(true) != null
                                   || child.GetComponentInChildren<Collider>(true) != null;

            if (child != visualPivot && !isCameraOrCollider)
                child.SetParent(visualPivot, true);
        }
    }

    private static InputAction ResolveAction(InputActionReference reference, string fallbackPath)
    {
        if (reference != null && reference.action != null)
            return reference.action;

        return InputSystem.actions != null ? InputSystem.actions.FindAction(fallbackPath, false) : null;
    }

    private void ReadInput(out float throttle, out float steer, out bool driftPressed)
    {
        Vector2 rawInput = move.ReadValue<Vector2>();

        throttle = Mathf.Abs(rawInput.y) > InputDeadzone ? rawInput.y : 0f;
        steer = Mathf.Abs(rawInput.x) > InputDeadzone ? rawInput.x : 0f;
        driftPressed = drift != null && drift.IsPressed();

        if (throttle != 0f || steer != 0f)
            body.WakeUp();
    }

    #endregion

    #region Fisica di Guida Coerente & Senza Glide

    private void UpdateSpeedLimit()
    {
        float targetMax = IsDrifting ? maxSpeed * driftMaxSpeedMultiplier : maxSpeed;
        float rate = 10f;
        currentMaxSpeedLimit = Mathf.MoveTowards(currentMaxSpeedLimit, targetMax, rate * Time.fixedDeltaTime);
    }

    private void ApplyThrottle(ref Vector3 velocity, float throttle, float forwardSpeed)
    {
        float targetSpeed = 0f;
        float accelRate = coastDeceleration;

        if (Mathf.Abs(throttle) > 0.01f)
        {
            if (throttle > 0f) // Accelerazione in avanti (R2 o W)
            {
                targetSpeed = currentMaxSpeedLimit * throttle;

                if (forwardSpeed < -StopThreshold)
                {
                    // Se stiamo andando in retromarcia e premiamo avanti -> frena energicamente
                    accelRate = braking;
                    targetSpeed = 0f;
                }
                else
                {
                    // Ease-In fluida e progressiva
                    float speedRatio = Mathf.Clamp01(Mathf.Max(forwardSpeed, 0f) / maxSpeed);
                    float smoothCurve = Mathf.Pow(speedRatio, DefaultAccelerationEasePower);
                    accelRate = maxAcceleration * Mathf.Lerp(DefaultStartAccelerationFactor, 1f, smoothCurve);

                    if (forwardSpeed > targetSpeed)
                        accelRate = 8f; // Rallentamento controllato se rilasci parzialmente il grilletto
                }
            }
            else // Retromarcia (L2 o S)
            {
                targetSpeed = maxReverseSpeed * throttle; // throttle è negativo

                if (forwardSpeed > StopThreshold)
                {
                    // Se stiamo avanzando e premiamo retro -> frena energicamente
                    accelRate = braking;
                    targetSpeed = 0f;
                }
                else
                {
                    accelRate = reverseAcceleration;
                }
            }
        }
        else
        {
            // Nessun input sui pedali: applica una decelerazione elevata per eliminare completamente il glide
            targetSpeed = 0f;
            accelRate = coastDeceleration;

            // Se siamo vicinissimi allo zero, ferma l'auto per evitare micro-scivolamenti infiniti
            if (Mathf.Abs(forwardSpeed) < StopThreshold)
            {
                velocity -= transform.forward * forwardSpeed;
                return;
            }
        }

        float newSpeed = Mathf.MoveTowards(forwardSpeed, targetSpeed, accelRate * Time.fixedDeltaTime);
        velocity += transform.forward * (newSpeed - forwardSpeed);
    }

    private void ApplyLateralGrip(ref Vector3 velocity)
    {
        float totalSpeed = velocity.magnitude;
        float speedRatio = Mathf.Clamp01(totalSpeed / maxSpeed);
        float steerIntensity = Mathf.Abs(smoothedSteer);

        float targetGrip;

        if (IsDrifting)
        {
            float lossFactor = Mathf.Clamp01(speedRatio * (0.5f + 0.5f * steerIntensity));
            targetGrip = Mathf.Lerp(driftBaseGrip, driftMinHighSpeedGrip, Mathf.Pow(lossFactor, 1.3f));
        }
        else
        {
            float highSpeedLoss = speedRatio * speedRatio * 0.45f * (0.4f + 0.6f * steerIntensity);
            targetGrip = lateralGrip * (1f - highSpeedLoss);
        }

        currentGrip = Mathf.MoveTowards(currentGrip, targetGrip, gripTransitionRate * Time.fixedDeltaTime);

        float lateralSpeed = Vector3.Dot(velocity, transform.right);
        float damping = ComputeExpDecay(currentGrip, Time.fixedDeltaTime);

        velocity -= transform.right * (lateralSpeed * damping);
    }

    private void ApplySteering(float steer, Vector3 velocity)
    {
        Vector3 angular = body.angularVelocity;
        float forwardSpeed = Vector3.Dot(velocity, transform.forward);
        float totalSpeed = velocity.magnitude;

        if (totalSpeed < MinSteerSpeed)
        {
            angular.y = 0f;
            body.angularVelocity = angular;
            effectiveSteer = 0f;
            return;
        }

        float dynamicSteerInertia = IsDrifting ? steeringInertia * 0.5f : steeringInertia;
        effectiveSteer = Mathf.MoveTowards(effectiveSteer, steer, dynamicSteerInertia * Time.fixedDeltaTime);

        float speedFactor = Mathf.Lerp(lowSpeedTurnFactor, 1f, Mathf.Clamp01(totalSpeed / turnReferenceSpeed));
        float directionSign = forwardSpeed >= -StopThreshold ? 1f : -1f;
        float baseTurn = IsDrifting ? turnSpeed * driftTurnMultiplier : turnSpeed;
        float yawRateDeg = effectiveSteer * baseTurn * speedFactor * directionSign;

        // Rotazione e testacoda completamente liberi e continui
        if (IsDrifting)
        {
            float lateralSpeed = Vector3.Dot(velocity, transform.right);
            float slipRatio = Mathf.Abs(lateralSpeed) / Mathf.Max(totalSpeed, 1f);
            float speedRatio = totalSpeed / maxSpeed;

            float oversteerPush = -Mathf.Sign(lateralSpeed) * slipRatio * speedRatio * slipOversteerFactor * turnSpeed;
            yawRateDeg += oversteerPush * Mathf.Abs(effectiveSteer);
        }

        angular.y = yawRateDeg * Mathf.Deg2Rad;
        body.angularVelocity = angular;
    }

    private void ApplyDownforce(float forwardSpeed)
    {
        float force = Mathf.Min(downforce * Mathf.Abs(forwardSpeed), maxDownforceAcceleration);
        if (force > 0f)
            body.AddForce(-transform.up * force, ForceMode.Acceleration);
    }

    #endregion

    #region Gestione Terreno & Allineamento

    private void EvaluateGroundContacts(Collision collision)
    {
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
            groundNormal = normalSum.normalized;
            lastGroundedTime = Time.fixedTime;
        }
    }

    private void UpdateSmoothedGroundNormal(bool grounded)
    {
        Vector3 targetNormal = grounded ? groundNormal : Vector3.up;
        float t = ComputeExpDecay(groundNormalSmoothing, Time.fixedDeltaTime);
        smoothedGroundNormal = Vector3.Slerp(smoothedGroundNormal, targetNormal, t).normalized;
    }

    private void AlignRigidbodyToGround()
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
        float t = ComputeExpDecay(groundAlignmentSpeed, Time.fixedDeltaTime);
        body.MoveRotation(Quaternion.Slerp(body.rotation, targetRotation, t));
    }

    private void UpdateVisualTilt()
    {
        if (visualPivot == null)
            return;

        Vector3 localUp = IsGrounded
            ? transform.InverseTransformDirection(smoothedGroundNormal).normalized
            : Vector3.up;

        Quaternion targetRotation = Vector3.Angle(localUp, Vector3.up) < minTiltAngle
            ? Quaternion.identity
            : Quaternion.FromToRotation(Vector3.up, localUp);

        float t = ComputeExpDecay(groundAlignmentSpeed, Time.deltaTime);
        visualPivot.localRotation = Quaternion.Slerp(visualPivot.localRotation, targetRotation, t);
    }

    #endregion

    #region Utility & Respawn

    private void Respawn()
    {
        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
        body.position = spawnPosition;
        body.rotation = spawnRotation;

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
    }

    private static float ComputeExpDecay(float rate, float deltaTime) => 1f - Mathf.Exp(-rate * deltaTime);

    #endregion
}