using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Controllo dell'auto con fisica arcade: accelerazione, frenata e retromarcia gestite
/// direttamente sulla velocità lungo l'asse di marcia, aderenza laterale e derapata (drift) opzionale.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class BumperCarController : MonoBehaviour
{
    [Header("Input")]
    [SerializeField]
    private InputActionReference moveAction;

    [SerializeField]
    private InputActionReference driftAction;

    [Header("Movimento")]
    // Accelerazione in avanti da fermo (m/s^2). Diminuisce gradualmente man mano che ci si avvicina alla velocità massima.
    [SerializeField, Min(0f)]
    private float acceleration = 24f;

    // Quanta accelerazione si perde alla velocità massima (0 = costante, 0.7 = a vmax resta il 30%).
    // Valori alti = spinta più progressiva e più tempo per raggiungere la velocità massima.
    [SerializeField, Range(0f, 0.95f)]
    private float accelerationFalloff = 0.6f;

    // Accelerazione in retromarcia (m/s^2).
    [SerializeField, Min(0f)]
    private float reverseAcceleration = 8f;

    // Intensità della frenata (m/s^2).
    [SerializeField, Min(0f)]
    private float braking = 25f;

    // Rallentamento quando si rilascia l'acceleratore (m/s^2).
    [SerializeField, Min(0f)]
    private float coastDeceleration = 4f;

    // Velocità massima in avanti (m/s).
    [SerializeField, Min(0f)]
    private float maxSpeed = 40f;

    // Velocità massima in retromarcia (m/s).
    [SerializeField, Min(0f)]
    private float maxReverseSpeed = 7f;

    [Header("Sterzo")]
    // Rotazione massima dell'auto (gradi/s) quando lo sterzo è al massimo dell'efficacia.
    [SerializeField, Min(0f)]
    private float turnSpeed = 130f;

    // Velocità alla quale lo sterzo raggiunge la massima efficacia (m/s).
    // Tra la velocità minima e questa, la capacità di sterzo cresce in modo continuo.
    [SerializeField, Min(0.01f)]
    private float turnReferenceSpeed = 14f;

    // Frazione di sterzo disponibile a bassa velocità (0.35 = 35% di turnSpeed appena ci si muove).
    [SerializeField, Range(0f, 1f)]
    private float lowSpeedTurnFactor = 0.35f;

    // Rapidità con cui lo sterzo raggiunge il valore richiesto (unità/s). Rende morbida la tastiera.
    [SerializeField, Min(0.1f)]
    private float steerResponse = 8f;

    [Header("Aderenza & Drift")]
    // Rapidità con cui viene annullata la velocità laterale durante la guida normale (1/s).
    [SerializeField, Min(0f)]
    private float lateralGrip = 22f;

    // Aderenza laterale ridotta durante la derapata (valori bassi = scivola di più).
    [SerializeField, Min(0f)]
    private float driftLateralGrip = 0.6f;

    // Moltiplicatore di sterzata durante il drift per accentuare il sovrasterzo.
    [SerializeField, Min(1f)]
    private float driftTurnMultiplier = 2.3f;

    // Velocità minima necessaria per iniziare a derapare.
    [SerializeField, Min(0f)]
    private float minDriftSpeed = 3.5f;

    [Header("Stabilità")]
    // Deportanza per unità di velocità (m/s^2 per ogni m/s). Tenere basso: valori alti frenano l'auto.
    [SerializeField, Min(0f)]
    private float downforce = 1.5f;

    // Deportanza massima applicabile (m/s^2). Un limite evita che l'auto venga schiacciata
    // sul terreno e rimbalzi in modo violento su ogni minimo dislivello.
    [SerializeField, Min(0f)]
    private float maxDownforceAcceleration = 20f;

    // Velocità verticale massima verso l'alto tollerata su terreno piano (m/s).
    // Assorbe i piccoli rimbalzi causati da dossi e fughe tra le mesh.
    [SerializeField, Min(0f)]
    private float maxGroundUpSpeed = 1.5f;

    // Altezza del baricentro (bassa per evitare ribaltamenti).
    [SerializeField]
    private float centerOfMassHeight = -0.3f;

    // Blocca rollio e beccheggio per evitare oscillazioni verticali.
    [SerializeField]
    private bool freezeTilt = true;

    [Header("Terreno")]
    // Allinea l'auto alla normale del terreno: su una rampa l'auto si inclina
    // seguendo il piano invece di restare orizzontale.
    [SerializeField]
    private bool alignToGround = true;

    // Rapidità con cui l'auto raggiunge l'inclinazione del terreno (1/s).
    [SerializeField, Min(0.1f)]
    private float groundAlignmentSpeed = 10f;

    // Angolo minimo (gradi) perché l'auto si inclini davvero: i micro-disallineamenti
    // del terreno non devono ruotare l'auto ad ogni contatto.
    [SerializeField, Min(0f)]
    private float minTiltAngle = 3f;

    // Rapidità con cui la normale del terreno viene mediata nel tempo (1/s).
    // Evita che un singolo contatto su un dosso inclini l'auto di scatto.
    [SerializeField, Min(0.1f)]
    private float groundNormalSmoothing = 8f;

    // Con il corpo bloccato su X/Z inclina solo il modello visivo,
    // così i vincoli del Rigidbody non vengono contrastati.
    [SerializeField]
    private bool tiltVisualOnly = true;

    [Header("Fisica")]
    // Applica ai collider dell'auto un materiale senza attrito: è il motore a muovere l'auto,
    // non l'attrito col terreno (che la frena e la fa girare di lato).
    [SerializeField]
    private bool useFrictionlessMaterial = true;

    // Rimbalzo dell'auto (0 = nessuno). Con 0 l'auto non rimbalza né sul terreno né contro i muri.
    [SerializeField, Range(0f, 1f)]
    private float bounciness = 0f;

    private Rigidbody body;
    private InputAction move;
    private InputAction drift;
    private PhysicsMaterial carMaterial;

    private Vector3 spawnPosition;
    private Quaternion spawnRotation;

    private float smoothedSteer;
    private float lastGroundedTime = float.NegativeInfinity;
    private Vector3 groundNormal = Vector3.up;
    private Vector3 smoothedGroundNormal = Vector3.up;
    private Transform visualPivot;

    private const float InputDeadzone = 0.05f;
    private const float StopThreshold = 0.15f;
    private const float MinSteerSpeed = 0.3f;
    private const float FallLimitY = -10f;
    private const float GroundNormalMinY = 0.5f;
    private const float FlatGroundNormalMinY = 0.995f;
    private const float GroundGraceTime = 0.15f;

    public bool IsDrifting { get; private set; }

    private void Awake()
    {
        body = GetComponent<Rigidbody>();

        // Il drag del Rigidbody frenerebbe l'auto in modo incontrollato: la decelerazione è gestita via codice.
        body.linearDamping = 0f;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        // Abbassa il centro di massa
        body.centerOfMass = new Vector3(
            body.centerOfMass.x,
            centerOfMassHeight,
            body.centerOfMass.z
        );

        if (freezeTilt)
        {
            body.constraints |= RigidbodyConstraints.FreezeRotationX
                | RigidbodyConstraints.FreezeRotationZ;
        }

        if (useFrictionlessMaterial)
            ApplyCarMaterial();

        if (alignToGround && freezeTilt && tiltVisualOnly)
            CreateVisualPivot();

        spawnPosition = transform.position;
        spawnRotation = transform.rotation;
    }

    private void OnDestroy()
    {
        if (carMaterial != null)
            Destroy(carMaterial);
    }

    private void OnEnable()
    {
        move = moveAction != null ? moveAction.action : null;
        if (move == null && InputSystem.actions != null)
            move = InputSystem.actions.FindAction("Player/Move", false);

        drift = driftAction != null ? driftAction.action : null;
        if (drift == null && InputSystem.actions != null)
            drift = InputSystem.actions.FindAction("Player/Drift", false);

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

    private void OnCollisionEnter(Collision collision) => CheckGround(collision);

    private void OnCollisionStay(Collision collision) => CheckGround(collision);

    private void FixedUpdate()
    {
        if (transform.position.y < FallLimitY)
        {
            Respawn();
            return;
        }

        Vector2 input = move.ReadValue<Vector2>();

        // Deadzone su entrambi gli assi: evita che il leggero drift di uno stick faccia sterzare l'auto.
        float throttle = Mathf.Abs(input.y) > InputDeadzone ? input.y : 0f;
        float steerInput = Mathf.Abs(input.x) > InputDeadzone ? input.x : 0f;

        if (throttle != 0f || steerInput != 0f)
            body.WakeUp();

        bool grounded = Time.fixedTime - lastGroundedTime <= GroundGraceTime;

        // La normale del terreno viene mediata: sui dossi i contatti cambiano di continuo
        // e seguirli istantaneamente faceva "scattare" l'auto.
        UpdateSmoothedGroundNormal(grounded);

        Vector3 velocity = body.linearVelocity;

        // Su terreno quasi piano assorbe i piccoli rimbalzi verticali dovuti a dossi
        // e piccole differenze di altezza tra le mesh.
        if (grounded && smoothedGroundNormal.y >= FlatGroundNormalMinY && velocity.y > maxGroundUpSpeed)
            velocity.y = maxGroundUpSpeed;

        // Velocità attuale lungo l'asse di marcia (positiva = avanti, negativa = retro)
        float forwardSpeed = Vector3.Dot(velocity, transform.forward);

        // Controllo stato derapata
        bool driftPressed = drift != null && drift.IsPressed();
        IsDrifting = grounded && driftPressed && Mathf.Abs(forwardSpeed) >= minDriftSpeed;

        smoothedSteer = Mathf.MoveTowards(smoothedSteer, steerInput, steerResponse * Time.fixedDeltaTime);

        // Acceleratore, sterzo e aderenza funzionano solo con l'auto a contatto col terreno.
        if (grounded)
        {
            ApplyThrottle(ref velocity, throttle, forwardSpeed);
            ApplyLateralGrip(ref velocity);

            float newForwardSpeed = Vector3.Dot(velocity, transform.forward);
            ApplySteering(smoothedSteer, newForwardSpeed);
        }

        // Una sola scrittura della velocità per evitare che i vari passaggi si sovrascrivano.
        body.linearVelocity = velocity;

        if (alignToGround && grounded)
            AlignToGround();

        ApplyDownforce(forwardSpeed);
    }

    private void LateUpdate()
    {
        UpdateVisualTilt();
    }

    private void UpdateSmoothedGroundNormal(bool grounded)
    {
        Vector3 target = grounded ? groundNormal : Vector3.up;
        float t = 1f - Mathf.Exp(-groundNormalSmoothing * Time.fixedDeltaTime);

        smoothedGroundNormal = Vector3.Slerp(smoothedGroundNormal, target, t).normalized;
    }

    private void CheckGround(Collision collision)
    {
        Vector3 accumulatedNormal = Vector3.zero;
        bool foundGround = false;

        for (int i = 0; i < collision.contactCount; i++)
        {
            Vector3 normal = collision.GetContact(i).normal;
            if (normal.y >= GroundNormalMinY)
            {
                accumulatedNormal += normal;
                foundGround = true;
            }
        }

        if (foundGround)
        {
            groundNormal = accumulatedNormal.normalized;
            lastGroundedTime = Time.fixedTime;
        }
    }

    // Inclina l'auto sulla normale del terreno mantenendo la direzione di marcia (yaw).
    private void AlignToGround()
    {
        // Con il corpo bloccato su X/Z non si ruota mai il Rigidbody: i vincoli
        // riporterebbero l'auto in posizione ad ogni passo fisico, creando vibrazioni.
        if (freezeTilt)
            return;

        if (Vector3.Angle(smoothedGroundNormal, Vector3.up) < minTiltAngle)
            return;

        Vector3 up = smoothedGroundNormal;
        Vector3 forward = Vector3.ProjectOnPlane(transform.forward, up);

        if (forward.sqrMagnitude < 0.0001f)
            forward = Vector3.ProjectOnPlane(transform.right, up);

        if (forward.sqrMagnitude < 0.0001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(forward.normalized, up);
        float t = 1f - Mathf.Exp(-groundAlignmentSpeed * Time.fixedDeltaTime);

        body.MoveRotation(Quaternion.Slerp(body.rotation, targetRotation, t));
    }

    // Inclina solo il modello visivo: la fisica resta dritta e stabile.
    private void UpdateVisualTilt()
    {
        if (visualPivot == null)
            return;

        bool grounded = Time.fixedTime - lastGroundedTime <= GroundGraceTime;

        Vector3 localUp = grounded
            ? transform.InverseTransformDirection(smoothedGroundNormal).normalized
            : Vector3.up;

        Quaternion target = Vector3.Angle(localUp, Vector3.up) < minTiltAngle
            ? Quaternion.identity
            : Quaternion.FromToRotation(Vector3.up, localUp);

        float t = 1f - Mathf.Exp(-groundAlignmentSpeed * Time.deltaTime);

        visualPivot.localRotation = Quaternion.Slerp(visualPivot.localRotation, target, t);
    }

    // Sposta il modello sotto un pivot dedicato, lasciando fuori camera e collider.
    private void CreateVisualPivot()
    {
        visualPivot = new GameObject("VisualTilt").transform;
        visualPivot.SetParent(transform, false);

        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);

            if (child == visualPivot)
                continue;

            if (child.GetComponentInChildren<Camera>(true) != null)
                continue;

            if (child.GetComponentInChildren<Collider>(true) != null)
                continue;

            child.SetParent(visualPivot, true);
        }
    }

    private void ApplyCarMaterial()
    {
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

    private void Respawn()
    {
        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
        body.position = spawnPosition;
        body.rotation = spawnRotation;

        smoothedSteer = 0f;
        IsDrifting = false;
        groundNormal = Vector3.up;
        smoothedGroundNormal = Vector3.up;
        lastGroundedTime = float.NegativeInfinity;

        if (visualPivot != null)
            visualPivot.localRotation = Quaternion.identity;
    }

    private void ApplyThrottle(ref Vector3 velocity, float throttle, float currentForwardSpeed)
    {
        float targetSpeed;
        float rate;

        if (throttle > 0f)
        {
            targetSpeed = maxSpeed * throttle;

            // Se stiamo retrocedendo frena, altrimenti accelera in modo progressivo:
            // la spinta cala man mano che ci si avvicina alla velocità massima.
            float speedFraction = Mathf.Clamp01(Mathf.Max(currentForwardSpeed, 0f) / maxSpeed);
            float progressiveAcceleration = acceleration * (1f - accelerationFalloff * speedFraction);
            rate = currentForwardSpeed < -StopThreshold ? braking : progressiveAcceleration;

            // Già oltre la velocità richiesta (acceleratore parziale): rallenta per inerzia
            if (currentForwardSpeed > targetSpeed)
                rate = coastDeceleration;
        }
        else if (throttle < 0f)
        {
            targetSpeed = maxReverseSpeed * throttle; // negativa

            // Se stiamo avanzando frena, altrimenti fai retromarcia
            rate = currentForwardSpeed > StopThreshold ? braking : reverseAcceleration;

            if (currentForwardSpeed < targetSpeed)
                rate = coastDeceleration;
        }
        else
        {
            // Nessun input: rallenta per inerzia fino a fermarsi
            targetSpeed = 0f;
            rate = coastDeceleration;
        }

        float newSpeed = Mathf.MoveTowards(currentForwardSpeed, targetSpeed, rate * Time.fixedDeltaTime);

        // Modifica solo la componente lungo l'asse di marcia, senza toccare il resto (gravità, urti laterali)
        velocity += transform.forward * (newSpeed - currentForwardSpeed);
    }

    private void ApplySteering(float steer, float currentForwardSpeed)
    {
        Vector3 angular = body.angularVelocity;
        float absSpeed = Mathf.Abs(currentForwardSpeed);

        // Non sterza se l'auto è ferma o priva di inerzia
        if (absSpeed < MinSteerSpeed)
        {
            angular.y = 0f;
            body.angularVelocity = angular;
            return;
        }

        // La capacità di sterzo cresce con la velocità: da lowSpeedTurnFactor fino al 100% a turnReferenceSpeed.
        float speedRatio = Mathf.Clamp01(absSpeed / turnReferenceSpeed);
        float speedTurnFactor = Mathf.Lerp(lowSpeedTurnFactor, 1f, speedRatio);

        float currentTurnSpeed = IsDrifting ? turnSpeed * driftTurnMultiplier : turnSpeed;
        float turn = steer * currentTurnSpeed * speedTurnFactor * Mathf.Sign(currentForwardSpeed);

        // Imposta solo la rotazione sull'asse Y: senza input vale 0, quindi l'auto va dritta.
        angular.y = turn * Mathf.Deg2Rad;
        body.angularVelocity = angular;
    }

    private void ApplyLateralGrip(ref Vector3 velocity)
    {
        float grip = IsDrifting ? driftLateralGrip : lateralGrip;

        // Estrai solo la velocità lungo l'asse laterale (transform.right)
        float lateralSpeed = Vector3.Dot(velocity, transform.right);

        // Decadimento esponenziale: stabile anche con valori alti di grip e indipendente dal timestep
        float t = 1f - Mathf.Exp(-grip * Time.fixedDeltaTime);
        velocity -= transform.right * (lateralSpeed * t);
    }

    private void ApplyDownforce(float currentForwardSpeed)
    {
        float acceleration = Mathf.Min(
            downforce * Mathf.Abs(currentForwardSpeed),
            maxDownforceAcceleration
        );

        if (acceleration <= 0f)
            return;

        body.AddForce(-transform.up * acceleration, ForceMode.Acceleration);
    }
}