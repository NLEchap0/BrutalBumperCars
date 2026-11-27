/*
 * NOTE DI SVILUPPO:
 * I calcoli matematici avanzati, il modello fisico arcade (aderenza laterale, deriva,
 * sovrasterzo, curve di accelerazione) e le logiche complesse presenti in questo script
 * sono stati sviluppati con l'ausilio di un'Intelligenza Artificiale.
 */

using UnityEngine;

#region Controllo dell'auto con fisica arcade: disaccoppiato da Input System per supportare sia Player che Bot
[RequireComponent(typeof(Rigidbody))]
public class BumperCarController : MonoBehaviour
{
    #region Dichiarazioni Costanti e Variabili

    #region Costanti Generali e Soglie
    private const float InputDeadzoneThreshold = 0.05f;
    private const float FullStopSpeedThreshold = 0.15f;
    private const float MinimumSteeringSpeed = 0.3f;
    private const float OutOfBoundsFallHeightY = -10f;
    private const float WalkableGroundNormalMinY = 0.78f;
    private const float FlatGroundNormalMinY = 0.995f;
    private const float GroundedCoyoteGraceTime = 0.15f;
    #endregion

    #region Costanti Movimento & Risposta (Anti-Glide)
    private const float MaxForwardSpeed = 24f;
    private const float MaxReverseSpeed = 12f;
    private const float MaxForwardAcceleration = 28f;
    private const float ReverseAcceleration = 14f;
    private const float CounterBrakingDeceleration = 40f;
    private const float CoastingDeceleration = 12f;
    #endregion

    #region Costanti Sterzo & Inerzia
    private const float BaseTurnRateDegreesPerSec = 130f;
    private const float FullSteeringReferenceSpeed = 24f;
    private const float LowSpeedSteeringMultiplier = 0.5f;
    private const float SteeringInputResponseRate = 9f;
    private const float SteeringAngularInertia = 4f;
    #endregion

    #region Costanti Aderenza & Drift Dinamico
    private const float NormalLateralGripRate = 20f;
    private const float DriftBaseLateralGripRate = 2.5f;
    private const float DriftHighSpeedMinGripRate = 0.05f;
    private const float GripModeTransitionRate = 10f;
    private const float DriftTurnRateMultiplier = 1.8f;
    private const float DriftSlipOversteerMultiplier = 2f;
    private const float DriftMaxSpeedMultiplier = 0.75f;
    private const float MinimumSpeedToDrift = 5f;
    #endregion

    #region Costanti Stabilità & Terreno
    private const float DownforcePerSpeedUnit = 1.5f;
    private const float MaxDownforceAcceleration = 25f;
    private const float MaxFlatGroundUpwardSpeed = 1.5f;
    private const float CenterOfMassVerticalOffset = -0.3f;
    private const bool LockPhysicalPitchAndRoll = true;
    private const bool AlignVehicleToGroundSlope = true;
    private const float GroundSlopeAlignmentSpeed = 10f;
    private const float MinimumSlopeTiltAngle = 3f;
    private const float GroundNormalSmoothingRate = 8f;
    private const bool TiltVisualBodyOnly = true;
    private const float MaxAllowedGroundTiltAngle = 28f;
    private const int GroundDetectionLayerMask = ~0; // Everything
    #endregion

    #region Costanti Fisica
    private const bool ApplyFrictionlessPhysicsMaterial = true;
    private const float ColliderBounciness = 0f;
    #endregion

    #region Variabili di Stato e Riferimenti Interni
    private Rigidbody vehicleRigidbody;
    private PhysicsMaterial frictionlessPhysicsMaterial;
    private Transform visualBodyPivot;

    private Vector3 initialSpawnPosition;
    private Quaternion initialSpawnRotation;

    private float targetThrottleInput;
    private float targetSteerInput;
    private bool isDriftButtonHeld;

    private float smoothedSteerInput;
    private float actualAppliedSteer;
    private float activeLateralGrip;
    private float activeMaxSpeedLimit;
    private float lastGroundedTimestamp = float.NegativeInfinity;
    private Vector3 rawGroundNormal = Vector3.up;
    private Vector3 smoothedGroundNormal = Vector3.up;

    public bool IsDrifting { get; private set; }
    public float CurrentSteer => smoothedSteerInput;
    public float EffectiveSteer => actualAppliedSteer;
    public Quaternion VisualTiltRotation => visualBodyPivot != null ? visualBodyPivot.localRotation : Quaternion.identity;
    public Transform VisualPivot => visualBodyPivot;
    private bool IsGrounded => Time.fixedTime - lastGroundedTimestamp <= GroundedCoyoteGraceTime;
    #endregion

    #endregion

    #region 1. API Pubblica (Controllo Esterno e Reset)

    #region Riceve gli input normalizzati da PlayerCarInput o dall'AI dei Bot
    public void SetInputs(float throttle, float steer, bool drift)
    {
        targetThrottleInput = Mathf.Clamp(throttle, -1f, 1f);
        targetSteerInput = Mathf.Clamp(steer, -1f, 1f);
        isDriftButtonHeld = drift;

        if (HasActiveDrivingInput())
            vehicleRigidbody.WakeUp();
    }
    #endregion

    #region Ripristina posizione, velocità e stati fisici interni del veicolo (usato per respawn o reset episodio)
    public void ResetForEpisode(Vector3 position, Quaternion rotation)
    {
        SaveInitialSpawnTransform(position, rotation);
        ResetRigidbodyKinematics(position, rotation);
        ResetInternalControlStates();
        vehicleRigidbody.WakeUp();
    }
    #endregion

    #endregion

    #region 2. Ciclo di Vita Unity (Awake, FixedUpdate, LateUpdate, Collisioni, OnDestroy)

    private void Awake()
    {
        InitializePhysics();

        if (ShouldUseVisualTiltPivot())
            CreateVisualPivot();

        SaveInitialSpawnTransform(transform.position, transform.rotation);
        ResetInternalControlStates();
    }

    #region Loop fisico principale: aggiorna terreno, velocità longitudinale, aderenza laterale, sterzo e deportanza
    private void FixedUpdate()
    {
        if (IsOutOfBounds())
        {
            ResetForEpisode(initialSpawnPosition, initialSpawnRotation);
            return;
        }

        float fixedDeltaTime = Time.fixedDeltaTime;
        UpdateSmoothedSteeringInput(fixedDeltaTime);

        SampleGroundRaycast();
        bool isVehicleGrounded = IsGrounded;
        UpdateSmoothedGroundNormal(isVehicleGrounded, fixedDeltaTime);

        Vector3 currentVelocity = vehicleRigidbody.linearVelocity;
        ClampFlatGroundVerticalBounce(ref currentVelocity, isVehicleGrounded);

        float longitudinalForwardSpeed = ComputeForwardSpeed(currentVelocity);
        UpdateDriftAndSpeedLimitState(isVehicleGrounded, currentVelocity.magnitude, fixedDeltaTime);

        if (isVehicleGrounded)
            ApplyGroundedForces(ref currentVelocity, longitudinalForwardSpeed, fixedDeltaTime);

        vehicleRigidbody.linearVelocity = currentVelocity;
        ApplyAerodynamicDownforce(longitudinalForwardSpeed);
    }
    #endregion

    #region Inclina visivamente il modello 3D della carrozzeria in base alla pendenza del terreno quando la rotazione fisica X/Z del Rigidbody è bloccata
    private void LateUpdate()
    {
        if (visualBodyPivot == null)
            return;

        Quaternion targetVisualRotation = ComputeVisualPivotTargetRotation();
        visualBodyPivot.localRotation = Quaternion.Slerp(
            visualBodyPivot.localRotation,
            targetVisualRotation,
            ExpDecay(GroundSlopeAlignmentSpeed, Time.deltaTime));
    }
    #endregion

    private void OnCollisionEnter(Collision collision) => EvaluateGroundContacts(collision);
    private void OnCollisionStay(Collision collision) => EvaluateGroundContacts(collision);

    private void OnDestroy()
    {
        if (frictionlessPhysicsMaterial != null)
            Destroy(frictionlessPhysicsMaterial);
    }

    #endregion

    #region 3. Inizializzazione Fisica e Setup Visivo

    private void InitializePhysics()
    {
        vehicleRigidbody = GetComponent<Rigidbody>();
        ConfigureRigidbodyProperties();

        if (ApplyFrictionlessPhysicsMaterial)
            AssignFrictionlessMaterialToColliders();
    }

    private void ConfigureRigidbodyProperties()
    {
        vehicleRigidbody.linearDamping = 0.1f;
        vehicleRigidbody.angularDamping = 0f;
        vehicleRigidbody.interpolation = RigidbodyInterpolation.Interpolate;
        vehicleRigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        vehicleRigidbody.centerOfMass = new Vector3(
            vehicleRigidbody.centerOfMass.x,
            CenterOfMassVerticalOffset,
            vehicleRigidbody.centerOfMass.z);

        if (LockPhysicalPitchAndRoll)
            vehicleRigidbody.constraints |= RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
    }

    private void AssignFrictionlessMaterialToColliders()
    {
        frictionlessPhysicsMaterial = new PhysicsMaterial("BumperCar_NoFriction")
        {
            dynamicFriction = 0f,
            staticFriction = 0f,
            frictionCombine = PhysicsMaterialCombine.Minimum,
            bounciness = ColliderBounciness,
            bounceCombine = PhysicsMaterialCombine.Minimum
        };

        foreach (Collider vehicleCollider in GetComponentsInChildren<Collider>())
        {
            if (!vehicleCollider.isTrigger)
                vehicleCollider.sharedMaterial = frictionlessPhysicsMaterial;
        }
    }

    #region Crea un nodo pivot intermedio e vi sposta tutte le mesh visive, escludendo telecamere e collider così da inclinare solo la scocca senza alterare la fisica o il punto di vista
    private void CreateVisualPivot()
    {
        visualBodyPivot = new GameObject("VisualTilt").transform;
        visualBodyPivot.SetParent(transform, false);
        ReparentVisualOnlyChildrenToPivot();
    }
    #endregion

    private bool ShouldUseVisualTiltPivot() => AlignVehicleToGroundSlope && LockPhysicalPitchAndRoll && TiltVisualBodyOnly;

    private void ReparentVisualOnlyChildrenToPivot()
    {
        for (int childIndex = transform.childCount - 1; childIndex >= 0; childIndex--)
        {
            Transform childTransform = transform.GetChild(childIndex);
            if (childTransform != visualBodyPivot && !ContainsCameraOrCollider(childTransform))
                childTransform.SetParent(visualBodyPivot, true);
        }
    }

    private static bool ContainsCameraOrCollider(Transform targetTransform)
    {
        return targetTransform.GetComponentInChildren<Camera>(true) != null
            || targetTransform.GetComponentInChildren<Collider>(true) != null;
    }

    #endregion

    #region 4. Gestione Stato Interno e Reset

    private void SaveInitialSpawnTransform(Vector3 position, Quaternion rotation)
    {
        initialSpawnPosition = position;
        initialSpawnRotation = rotation;
    }

    private void ResetRigidbodyKinematics(Vector3 position, Quaternion rotation)
    {
        vehicleRigidbody.linearVelocity = Vector3.zero;
        vehicleRigidbody.angularVelocity = Vector3.zero;
        vehicleRigidbody.position = position;
        vehicleRigidbody.rotation = rotation;
    }

    private void ResetInternalControlStates()
    {
        targetThrottleInput = 0f;
        targetSteerInput = 0f;
        isDriftButtonHeld = false;

        smoothedSteerInput = 0f;
        actualAppliedSteer = 0f;
        activeMaxSpeedLimit = MaxForwardSpeed;
        activeLateralGrip = NormalLateralGripRate;
        IsDrifting = false;

        rawGroundNormal = Vector3.up;
        smoothedGroundNormal = Vector3.up;
        lastGroundedTimestamp = float.NegativeInfinity;

        if (visualBodyPivot != null)
            visualBodyPivot.localRotation = Quaternion.identity;
    }

    private bool IsOutOfBounds() => transform.position.y < OutOfBoundsFallHeightY;

    private bool HasActiveDrivingInput()
    {
        return Mathf.Abs(targetThrottleInput) > InputDeadzoneThreshold
            || Mathf.Abs(targetSteerInput) > InputDeadzoneThreshold;
    }

    private void UpdateSmoothedSteeringInput(float deltaTime)
    {
        smoothedSteerInput = Mathf.MoveTowards(smoothedSteerInput, targetSteerInput, SteeringInputResponseRate * deltaTime);
    }

    private void UpdateDriftAndSpeedLimitState(bool isVehicleGrounded, float planarSpeed, float deltaTime)
    {
        IsDrifting = isVehicleGrounded && isDriftButtonHeld && planarSpeed >= MinimumSpeedToDrift;
        float targetMaxSpeed = IsDrifting ? MaxForwardSpeed * DriftMaxSpeedMultiplier : MaxForwardSpeed;
        activeMaxSpeedLimit = Mathf.MoveTowards(activeMaxSpeedLimit, targetMaxSpeed, 10f * deltaTime);
    }

    #endregion

    #region 5. Dinamica Longitudinale (Accelerazione, Frenata, Retromarcia)

    private void ApplyGroundedForces(ref Vector3 velocity, float forwardSpeed, float deltaTime)
    {
        ApplyThrottle(ref velocity, forwardSpeed, deltaTime);
        ApplyLateralGrip(ref velocity, deltaTime);
        ApplySteering(velocity, forwardSpeed, deltaTime);
        AlignRigidbodyToGround(deltaTime);
    }

    #region Gestisce accelerazione progressiva, frenata contro-sterzo, retromarcia e decelerazione anti-glide
    private void ApplyThrottle(ref Vector3 velocity, float forwardSpeed, float deltaTime)
    {
        if (IsNeutralThrottleAtFullStop(forwardSpeed))
        {
            velocity -= transform.forward * forwardSpeed;
            return;
        }

        ComputeLongitudinalTargetAndRate(forwardSpeed, out float desiredForwardSpeed, out float appliedAccelerationRate);
        float updatedForwardSpeed = Mathf.MoveTowards(forwardSpeed, desiredForwardSpeed, appliedAccelerationRate * deltaTime);
        velocity += transform.forward * (updatedForwardSpeed - forwardSpeed);
    }
    #endregion

    private bool IsNeutralThrottleAtFullStop(float forwardSpeed)
    {
        return Mathf.Abs(targetThrottleInput) <= 0.01f && Mathf.Abs(forwardSpeed) < FullStopSpeedThreshold;
    }

    private void ComputeLongitudinalTargetAndRate(float forwardSpeed, out float desiredForwardSpeed, out float appliedAccelerationRate)
    {
        if (targetThrottleInput > 0.01f)
        {
            ComputeForwardThrottleParameters(forwardSpeed, out desiredForwardSpeed, out appliedAccelerationRate);
            return;
        }

        if (targetThrottleInput < -0.01f)
        {
            ComputeReverseThrottleParameters(forwardSpeed, out desiredForwardSpeed, out appliedAccelerationRate);
            return;
        }

        desiredForwardSpeed = 0f;
        appliedAccelerationRate = CoastingDeceleration;
    }

    private void ComputeForwardThrottleParameters(float forwardSpeed, out float desiredForwardSpeed, out float appliedAccelerationRate)
    {
        if (forwardSpeed < -FullStopSpeedThreshold)
        {
            desiredForwardSpeed = 0f;
            appliedAccelerationRate = CounterBrakingDeceleration;
            return;
        }

        desiredForwardSpeed = activeMaxSpeedLimit * targetThrottleInput;
        appliedAccelerationRate = forwardSpeed > desiredForwardSpeed
            ? 8f
            : ComputeProgressiveForwardAcceleration(forwardSpeed);
    }

    private static float ComputeProgressiveForwardAcceleration(float forwardSpeed)
    {
        float forwardSpeedRatio = Mathf.Clamp01(Mathf.Max(forwardSpeed, 0f) / MaxForwardSpeed);
        return MaxForwardAcceleration * Mathf.Lerp(0.25f, 1f, Mathf.Pow(forwardSpeedRatio, 2.2f));
    }

    private void ComputeReverseThrottleParameters(float forwardSpeed, out float desiredForwardSpeed, out float appliedAccelerationRate)
    {
        bool isMovingForward = forwardSpeed > FullStopSpeedThreshold;
        desiredForwardSpeed = isMovingForward ? 0f : MaxReverseSpeed * targetThrottleInput;
        appliedAccelerationRate = isMovingForward ? CounterBrakingDeceleration : ReverseAcceleration;
    }

    #endregion

    #region 6. Dinamica Laterale (Aderenza e Drift)

    #region Calcola l'aderenza laterale dinamica e smorza la componente di velocità perpendicolare all'auto
    private void ApplyLateralGrip(ref Vector3 velocity, float deltaTime)
    {
        float targetLateralGrip = ComputeTargetLateralGrip(velocity.magnitude);
        activeLateralGrip = Mathf.MoveTowards(activeLateralGrip, targetLateralGrip, GripModeTransitionRate * deltaTime);

        float lateralSlideSpeed = ComputeLateralSpeed(velocity);
        velocity -= transform.right * (lateralSlideSpeed * ExpDecay(activeLateralGrip, deltaTime));
    }
    #endregion

    private float ComputeTargetLateralGrip(float totalSpeed)
    {
        float normalizedSpeedRatio = Mathf.Clamp01(totalSpeed / MaxForwardSpeed);
        float absoluteSteerIntensity = Mathf.Abs(smoothedSteerInput);

        return IsDrifting
            ? ComputeDriftingTargetGrip(normalizedSpeedRatio, absoluteSteerIntensity)
            : ComputeNormalTargetGrip(normalizedSpeedRatio, absoluteSteerIntensity);
    }

    private static float ComputeDriftingTargetGrip(float normalizedSpeedRatio, float absoluteSteerIntensity)
    {
        float driftGripLossFactor = Mathf.Clamp01(normalizedSpeedRatio * (0.5f + 0.5f * absoluteSteerIntensity));
        return Mathf.Lerp(DriftBaseLateralGripRate, DriftHighSpeedMinGripRate, Mathf.Pow(driftGripLossFactor, 1.3f));
    }

    private static float ComputeNormalTargetGrip(float normalizedSpeedRatio, float absoluteSteerIntensity)
    {
        float highSpeedGripLossRatio = normalizedSpeedRatio * normalizedSpeedRatio * 0.45f * (0.4f + 0.6f * absoluteSteerIntensity);
        return NormalLateralGripRate * (1f - highSpeedGripLossRatio);
    }

    #endregion

    #region 7. Dinamica di Sterzata e Imbardata (Yaw & Sovrasterzo)

    #region Calcola la velocità angolare di imbardata (Yaw) combinando inerzia dello sterzo, direzione e sovrasterzo in drift
    private void ApplySteering(Vector3 velocity, float forwardSpeed, float deltaTime)
    {
        float totalPlanarSpeed = velocity.magnitude;
        if (totalPlanarSpeed < MinimumSteeringSpeed)
        {
            ResetYawSteering();
            return;
        }

        UpdateActualAppliedSteer(deltaTime);
        float targetYawRateDegrees = ComputeBaseYawRateDegrees(totalPlanarSpeed, forwardSpeed);

        if (IsDrifting)
            targetYawRateDegrees += ComputeDriftOversteerYawDegrees(velocity, totalPlanarSpeed);

        SetRigidbodyYawRateDegrees(targetYawRateDegrees);
    }
    #endregion

    private void ResetYawSteering()
    {
        SetRigidbodyYawRateDegrees(0f);
        actualAppliedSteer = 0f;
    }

    private void UpdateActualAppliedSteer(float deltaTime)
    {
        float effectiveSteeringInertia = IsDrifting ? SteeringAngularInertia * 0.5f : SteeringAngularInertia;
        actualAppliedSteer = Mathf.MoveTowards(actualAppliedSteer, smoothedSteerInput, effectiveSteeringInertia * deltaTime);
    }

    private float ComputeBaseYawRateDegrees(float totalPlanarSpeed, float forwardSpeed)
    {
        float speedSteeringEffectiveness = Mathf.Lerp(LowSpeedSteeringMultiplier, 1f, Mathf.Clamp01(totalPlanarSpeed / FullSteeringReferenceSpeed));
        float longitudinalDirectionSign = forwardSpeed >= -FullStopSpeedThreshold ? 1f : -1f;
        float currentBaseTurnRate = IsDrifting ? BaseTurnRateDegreesPerSec * DriftTurnRateMultiplier : BaseTurnRateDegreesPerSec;
        return actualAppliedSteer * currentBaseTurnRate * speedSteeringEffectiveness * longitudinalDirectionSign;
    }

    private float ComputeDriftOversteerYawDegrees(Vector3 velocity, float totalPlanarSpeed)
    {
        float lateralSlideSpeed = ComputeLateralSpeed(velocity);
        float lateralSlipRatio = Mathf.Abs(lateralSlideSpeed) / Mathf.Max(totalPlanarSpeed, 1f);
        float normalizedForwardSpeedRatio = totalPlanarSpeed / MaxForwardSpeed;

        return -Mathf.Sign(lateralSlideSpeed)
            * lateralSlipRatio
            * normalizedForwardSpeedRatio
            * DriftSlipOversteerMultiplier
            * BaseTurnRateDegreesPerSec
            * Mathf.Abs(actualAppliedSteer);
    }

    private void SetRigidbodyYawRateDegrees(float yawRateDegrees)
    {
        Vector3 updatedAngularVelocity = vehicleRigidbody.angularVelocity;
        updatedAngularVelocity.y = yawRateDegrees * Mathf.Deg2Rad;
        vehicleRigidbody.angularVelocity = updatedAngularVelocity;
    }

    #endregion

    #region 8. Rilevamento Terreno, Allineamento e Deportanza

    #region Esegue uno SphereCast verso il basso per rilevare la normale reale della pista ignorando spigoli e altri veicoli
    private void SampleGroundRaycast()
    {
        Vector3 sphereCastOrigin = transform.position + Vector3.up * 0.4f;
        if (Physics.SphereCast(sphereCastOrigin, 0.35f, Vector3.down, out RaycastHit groundHit, 0.75f, GroundDetectionLayerMask, QueryTriggerInteraction.Ignore)
            && groundHit.rigidbody == null
            && IsWalkableSurfaceNormal(groundHit.normal))
        {
            RegisterGroundDetection(groundHit.normal);
        }
    }
    #endregion

    #region Media le normali dei punti di contatto statici per aggiornare lo stato di contatto col terreno
    private void EvaluateGroundContacts(Collision collision)
    {
        if (collision.rigidbody != null)
            return;

        if (TryComputeAverageWalkableContactNormal(collision, out Vector3 averageContactNormal))
            RegisterGroundDetection(averageContactNormal);
    }
    #endregion

    private static bool IsWalkableSurfaceNormal(Vector3 surfaceNormal) => surfaceNormal.y >= WalkableGroundNormalMinY;

    private static bool TryComputeAverageWalkableContactNormal(Collision collision, out Vector3 averageContactNormal)
    {
        Vector3 accumulatedContactNormals = Vector3.zero;
        int validGroundContactCount = 0;

        for (int contactIndex = 0; contactIndex < collision.contactCount; contactIndex++)
        {
            Vector3 contactNormal = collision.GetContact(contactIndex).normal;
            if (IsWalkableSurfaceNormal(contactNormal))
            {
                accumulatedContactNormals += contactNormal;
                validGroundContactCount++;
            }
        }

        if (validGroundContactCount > 0)
        {
            averageContactNormal = accumulatedContactNormals.normalized;
            return true;
        }

        averageContactNormal = Vector3.up;
        return false;
    }

    private void RegisterGroundDetection(Vector3 detectedNormal)
    {
        rawGroundNormal = ClampNormalAngle(detectedNormal, MaxAllowedGroundTiltAngle);
        lastGroundedTimestamp = Time.fixedTime;
    }

    private void UpdateSmoothedGroundNormal(bool isVehicleGrounded, float deltaTime)
    {
        Vector3 targetSurfaceNormal = isVehicleGrounded ? rawGroundNormal : Vector3.up;
        smoothedGroundNormal = Vector3.Slerp(smoothedGroundNormal, targetSurfaceNormal, ExpDecay(GroundNormalSmoothingRate, deltaTime)).normalized;
    }

    private void ClampFlatGroundVerticalBounce(ref Vector3 velocity, bool isVehicleGrounded)
    {
        if (isVehicleGrounded && smoothedGroundNormal.y >= FlatGroundNormalMinY && velocity.y > MaxFlatGroundUpwardSpeed)
            velocity.y = MaxFlatGroundUpwardSpeed;
    }

    private void ApplyAerodynamicDownforce(float forwardSpeed)
    {
        float downforceAcceleration = Mathf.Min(DownforcePerSpeedUnit * Mathf.Abs(forwardSpeed), MaxDownforceAcceleration);
        if (downforceAcceleration > 0f)
            vehicleRigidbody.AddForce(-transform.up * downforceAcceleration, ForceMode.Acceleration);
    }

    #region Allinea la rotazione fisica del Rigidbody alla pendenza del terreno (attivo solo se LockPhysicalPitchAndRoll è false)
    private void AlignRigidbodyToGround(float deltaTime)
    {
        if (!CanAlignPhysicalRigidbodyToGround())
            return;

        if (!TryComputeSlopeAlignedForwardDirection(out Vector3 projectedForwardDirection))
            return;

        Quaternion targetAlignedRotation = Quaternion.LookRotation(projectedForwardDirection, smoothedGroundNormal);
        vehicleRigidbody.MoveRotation(Quaternion.Slerp(
            vehicleRigidbody.rotation,
            targetAlignedRotation,
            ExpDecay(GroundSlopeAlignmentSpeed, deltaTime)));
    }
    #endregion

    private bool CanAlignPhysicalRigidbodyToGround()
    {
        return AlignVehicleToGroundSlope
            && !LockPhysicalPitchAndRoll
            && Vector3.Angle(smoothedGroundNormal, Vector3.up) >= MinimumSlopeTiltAngle;
    }

    private bool TryComputeSlopeAlignedForwardDirection(out Vector3 projectedForwardDirection)
    {
        projectedForwardDirection = Vector3.ProjectOnPlane(transform.forward, smoothedGroundNormal);
        if (projectedForwardDirection.sqrMagnitude < 0.0001f)
            projectedForwardDirection = Vector3.ProjectOnPlane(transform.right, smoothedGroundNormal);

        if (projectedForwardDirection.sqrMagnitude < 0.0001f)
            return false;

        projectedForwardDirection.Normalize();
        return true;
    }

    private Quaternion ComputeVisualPivotTargetRotation()
    {
        Vector3 localGroundUp = IsGrounded
            ? transform.InverseTransformDirection(smoothedGroundNormal).normalized
            : Vector3.up;

        return Vector3.Angle(localGroundUp, Vector3.up) < MinimumSlopeTiltAngle
            ? Quaternion.identity
            : Quaternion.FromToRotation(Vector3.up, localGroundUp);
    }

    #endregion

    #region 9. Utility Matematiche e Vettoriali

    private float ComputeForwardSpeed(Vector3 velocity) => Vector3.Dot(velocity, transform.forward);

    private float ComputeLateralSpeed(Vector3 velocity) => Vector3.Dot(velocity, transform.right);

    #region Limita l'angolo massimo tra la normale rilevata e la verticale assoluta per evitare ribaltamenti su pareti/ostacoli
    private static Vector3 ClampNormalAngle(Vector3 surfaceNormal, float maxAllowedAngleDegrees)
    {
        return Vector3.Angle(Vector3.up, surfaceNormal) <= maxAllowedAngleDegrees
            ? surfaceNormal
            : Vector3.RotateTowards(Vector3.up, surfaceNormal, maxAllowedAngleDegrees * Mathf.Deg2Rad, 0f).normalized;
    }
    #endregion

    #region Fattore di interpolazione esponenziale indipendente dal framerate: 1 - e^(-decayRate * deltaTime)
    private static float ExpDecay(float decayRate, float deltaTime) => 1f - Mathf.Exp(-decayRate * deltaTime);
    #endregion

    #endregion
}
#endregion