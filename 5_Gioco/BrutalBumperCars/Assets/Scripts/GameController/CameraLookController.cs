/*
 * NOTE DI SVILUPPO:
 * I calcoli matematici avanzati, la composizione dei quaternioni per la sospensione
 * dell'abitacolo, la simulazione delle forze G e le logiche complesse presenti in questo
 * script sono stati sviluppati con l'ausilio di un'Intelligenza Artificiale.
 */

using UnityEngine;
using UnityEngine.InputSystem;

#region Controllo visuale in prima persona con sospensione elastica del collo, forze G, vibrazioni e urti
public class CameraLookController : MonoBehaviour
{
    #region Dichiarazioni Costanti, Parametri Inspector e Variabili

    #region Costanti Matematiche e Soglie
    private const float MinimumInputSqrMagnitude = 0.0001f;
    private const float MinimumVelocitySqrMagnitude = 0.2f;
    private const float MinimumSpeedForBobbing = 0.04f;
    private const float MinimumShakeIntensity = 0.001f;
    private const float MaxTotalShakeIntensity = 1.3f;
    private const float ImpactDeltaSpeedNormalizationRange = 18f;
    private const float AutoImpactShakeScale = 0.85f;
    private const float ShakeNoiseFrequency = 35f;
    private const float ShakePositionAmplitude = 0.025f;
    private const float FovInterpolationSpeed = 6f;
    private const float DefaultNormalizedSensitivity = 0.5f;
    #endregion

    #region Riferimenti
    [Header("Riferimenti")]
    [SerializeField] private InputActionReference lookAction;

    [SerializeField, Tooltip("Controller dell'autoscontro (trovato automaticamente nei parent se vuoto).")]
    private BumperCarController carController;

    [SerializeField, Tooltip("Rigidbody dell'autoscontro (trovato automaticamente nei parent se vuoto).")]
    private Rigidbody vehicleRigidbody;

    [SerializeField, Tooltip("Camera in prima persona (usa quella su questo GameObject se vuoto).")]
    private Camera cockpitCamera;
    #endregion

    #region Sensibilità Visuale
    [Header("Sensibilità Visuale")]
    [SerializeField, Min(0.01f)] private float degreesPerMouseUnit = 0.12f;
    [SerializeField, Min(1f)] private float gamepadDegreesPerSecond = 200f;
    [SerializeField, Min(0.01f)] private float gamepadMinSensitivityMultiplier = 0.8f;
    [SerializeField, Min(0.01f)] private float gamepadMaxSensitivityMultiplier = 1.8f;
    [SerializeField, Range(0f, 0.5f)] private float gamepadDeadzone = 0.15f;
    #endregion

    #region Limiti Collo e Inerzia
    [Header("Limiti Collo e Inerzia")]
    [SerializeField] private float maxYaw = 110f;
    [SerializeField] private float minPitch = -45f;
    [SerializeField] private float maxPitch = 60f;
    [SerializeField, Range(0f, 0.15f)] private float mouseSmoothTime = 0.02f;
    [SerializeField, Range(0f, 0.25f)] private float gamepadSmoothTime = 0.06f;
    #endregion

    #region Sospensione Collo & Anti-Clip Abitacolo
    [Header("Sospensione Collo & Anti-Clip Abitacolo")]
    [SerializeField, Range(0f, 0.5f), Tooltip("Quanto il collo assorbe gli scatti improvvisi dell'auto (0 = bloccato al sedile, 0.35 = ammortizzato senza mai uscire dall'auto).")]
    private float neckSuspensionRatio = 0.30f;

    [SerializeField, Tooltip("Quanto velocemente il collo si riallinea all'inclinazione dell'auto (più alto = meno ritardo su rampe ripide).")]
    private float neckRecoverySpeed = 14f;

    [SerializeField, Tooltip("Angolo massimo di assorbimento del collo rispetto all'abitacolo (impedisce di guardare dentro la carrozzeria).")]
    private float maxNeckLagDegrees = 10f;
    #endregion

    #region Dinamica di Guida (Sterzo, Drift & Forze G)
    [Header("Dinamica di Guida (Sterzo, Drift & Forze G)")]
    [SerializeField, Tooltip("Inclinazione laterale della testa in curva normale (gradi).")]
    private float cornerRollDegrees = 6.0f;

    [SerializeField, Tooltip("Inclinazione extra della testa durante il drift (gradi).")]
    private float driftExtraRollDegrees = 4.0f;

    [SerializeField, Tooltip("Anticipo dello sguardo verso l'interno della curva (gradi).")]
    private float steerLookAheadYaw = 6.5f;

    [SerializeField, Tooltip("Beccheggio in accelerazione e frenata (gradi).")]
    private float accelPitchDegrees = 4.0f;

    [SerializeField, Tooltip("Spostamento massimo della testa nel sedile (metri, tenuto stretto per evitare clip).")]
    private float maxHeadGShift = 0.035f;

    [SerializeField] private float dynamicsSmoothSpeed = 9f;
    #endregion

    #region Ritorno Sguardo sul Volante
    [Header("Ritorno Sguardo sul Volante")]
    [SerializeField] private bool autoCenterLook = true;
    [SerializeField, Min(0.2f)] private float autoCenterDelay = 1.5f;
    [SerializeField, Min(0.5f)] private float autoCenterSpeed = 3.5f;
    [SerializeField, Min(0.5f)] private float minSpeedForAutoCenter = 2.5f;
    #endregion

    #region Vibrazioni Pista, FOV e Urti
    [Header("Vibrazioni Pista, FOV e Urti")]
    [SerializeField] private float bobFrequency = 14f;
    [SerializeField] private float bobAmplitude = 0.008f;
    [SerializeField] private float baseFov = 65f;
    [SerializeField] private float maxSpeedFov = 82f;

    [SerializeField, Tooltip("Allineato a maxSpeed di BumperCarController (50 m/s).")]
    private float maxReferenceSpeed = 50f;

    [SerializeField] private float autoImpactThreshold = 7.5f;
    [SerializeField] private float shakeDecay = 4.0f;
    #endregion

    #region Variabili di Stato Interno
    private Quaternion initialLocalRotation;
    private Vector3 initialSeatLocalPosition;

    private float targetYawDegrees;
    private float targetPitchDegrees;
    private float currentYawDegrees;
    private float currentPitchDegrees;
    private float yawSmoothVelocity;
    private float pitchSmoothVelocity;

    private Quaternion laggedBodyRotation = Quaternion.identity;

    private Vector3 previousWorldVelocity;
    private Vector3 previousLocalVelocity;
    private float currentDynamicRollDegrees;
    private float currentDynamicPitchDegrees;
    private float currentDynamicYawDegrees;
    private Vector3 currentHeadGOffset;
    private float roadBobbingTimer;

    private bool isLookActive;
    private bool shouldSkipNextFrame;
    private bool wasLastInputFromGamepad;
    private float elapsedTimeSinceLastInput;

    private float currentShakeIntensity;
    private float shakeNoiseSeed;
    #endregion

    #endregion

    #region 1. API Pubblica (Urti, Reset Sguardo, Attivazione Controllo)

    public void AddImpactShake(float intensity)
    {
        currentShakeIntensity = Mathf.Clamp(currentShakeIntensity + intensity, 0f, MaxTotalShakeIntensity);
    }

    public void ResetLook()
    {
        targetYawDegrees = 0f;
        targetPitchDegrees = 0f;
    }

    public void SetLookEnabled(bool isEnabled)
    {
        isLookActive = isEnabled;
        if (isEnabled)
        {
            shouldSkipNextFrame = true;
            SyncVelocityCache();
        }

        UpdateCursorLockState(isEnabled);
    }

    #endregion

    #region 2. Ciclo di Vita Unity (Awake, OnEnable, OnDisable, Update, LateUpdate)

    private void Awake()
    {
        SaveInitialSeatTransform();
        ResolveComponentReferences();
        InitializeShakeNoiseSeed();
    }

    private void OnEnable()
    {
        if (!HasValidLookAction())
        {
            enabled = false;
            return;
        }

        SubscribeAndEnableLookAction();
        SetLookEnabled(true);
    }

    private void OnDisable()
    {
        UnsubscribeAndDisableLookAction();
        SetLookEnabled(false);
    }

    private void Update()
    {
        if (!isLookActive)
            return;

        if (ConsumeSkipFrameFlag())
            return;

        ReadLookInput();
        HandleAutoCenter();
    }

    private void LateUpdate()
    {
        if (!isLookActive || shouldSkipNextFrame)
            return;

        SimulateCockpitPhysics();
        UpdateDynamicFov();
    }

    #endregion

    #region 3. Inizializzazione, Input System e Cursore

    private void SaveInitialSeatTransform()
    {
        initialLocalRotation = transform.localRotation;
        initialSeatLocalPosition = transform.localPosition;
    }

    private void ResolveComponentReferences()
    {
        if (cockpitCamera == null)
            cockpitCamera = GetComponent<Camera>();

        if (carController == null)
            carController = GetComponentInParent<BumperCarController>();

        if (vehicleRigidbody == null)
            vehicleRigidbody = GetComponentInParent<Rigidbody>();
    }

    private void InitializeShakeNoiseSeed()
    {
        shakeNoiseSeed = Random.Range(0f, 100f);
    }

    private bool HasValidLookAction() => lookAction != null && lookAction.action != null;

    private void SubscribeAndEnableLookAction()
    {
        lookAction.action.performed += OnLookPerformed;
        lookAction.action.Enable();
    }

    private void UnsubscribeAndDisableLookAction()
    {
        if (!HasValidLookAction())
            return;

        lookAction.action.performed -= OnLookPerformed;
        lookAction.action.Disable();
    }

    private void OnLookPerformed(InputAction.CallbackContext callbackContext)
    {
        InputDevice activeDevice = callbackContext.control?.device;
        if (activeDevice != null)
            wasLastInputFromGamepad = activeDevice is Gamepad || activeDevice is Joystick;
    }

    private static void UpdateCursorLockState(bool isLocked)
    {
        Cursor.lockState = isLocked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !isLocked;
    }

    private bool ConsumeSkipFrameFlag()
    {
        if (!shouldSkipNextFrame)
            return false;

        shouldSkipNextFrame = false;
        SyncVelocityCache();
        return true;
    }

    private void SyncVelocityCache()
    {
        if (vehicleRigidbody == null)
            return;

        previousWorldVelocity = vehicleRigidbody.linearVelocity;
        previousLocalVelocity = vehicleRigidbody.transform.InverseTransformDirection(previousWorldVelocity);
    }

    #endregion

    #region 4. Lettura Input Visuale e Centratura Automatica

    #region Legge l'input visuale distinguendo tra spostamento relativo (mouse) e velocità angolare (gamepad)
    private void ReadLookInput()
    {
        Vector2 rawLookDelta = lookAction.action.ReadValue<Vector2>();
        float normalizedSensitivity = GetSensitivitySetting();

        bool hasReceivedLookInput = wasLastInputFromGamepad
            ? ApplyGamepadLookInput(rawLookDelta, normalizedSensitivity)
            : ApplyMouseLookInput(rawLookDelta, normalizedSensitivity);

        UpdateInputIdleTimer(hasReceivedLookInput);
        ClampTargetLookAngles();
    }
    #endregion

    private bool ApplyGamepadLookInput(Vector2 rawStickInput, float normalizedSensitivity)
    {
        Vector2 filteredStickInput = ApplyDeadzone(rawStickInput, gamepadDeadzone);
        if (filteredStickInput.sqrMagnitude <= MinimumInputSqrMagnitude)
            return false;

        float sensitivityMultiplier = Mathf.Lerp(gamepadMinSensitivityMultiplier, gamepadMaxSensitivityMultiplier, normalizedSensitivity);
        float angularStepDegrees = gamepadDegreesPerSecond * sensitivityMultiplier * Time.deltaTime;
        targetYawDegrees += filteredStickInput.x * angularStepDegrees;
        targetPitchDegrees -= filteredStickInput.y * angularStepDegrees;
        return true;
    }

    private bool ApplyMouseLookInput(Vector2 rawMouseDelta, float normalizedSensitivity)
    {
        if (rawMouseDelta.sqrMagnitude <= MinimumInputSqrMagnitude)
            return false;

        // Curva esponenziale centrata su 0.5 per scalare in modo naturale la sensibilità del mouse
        float sensitivityMultiplier = Mathf.Pow(2f, (normalizedSensitivity - 0.5f) * 2f);
        float degreesPerUnit = degreesPerMouseUnit * sensitivityMultiplier;
        targetYawDegrees += rawMouseDelta.x * degreesPerUnit;
        targetPitchDegrees -= rawMouseDelta.y * degreesPerUnit;
        return true;
    }

    private void UpdateInputIdleTimer(bool hasReceivedLookInput)
    {
        elapsedTimeSinceLastInput = hasReceivedLookInput
            ? 0f
            : elapsedTimeSinceLastInput + Time.deltaTime;
    }

    private void ClampTargetLookAngles()
    {
        targetYawDegrees = Mathf.Clamp(targetYawDegrees, -maxYaw, maxYaw);
        targetPitchDegrees = Mathf.Clamp(targetPitchDegrees, minPitch, maxPitch);
    }

    private void HandleAutoCenter()
    {
        if (!CanAutoCenterLook())
            return;

        float interpolationFactor = autoCenterSpeed * Time.deltaTime;
        targetYawDegrees = Mathf.Lerp(targetYawDegrees, 0f, interpolationFactor);
        targetPitchDegrees = Mathf.Lerp(targetPitchDegrees, 0f, interpolationFactor);
    }

    private bool CanAutoCenterLook()
    {
        return autoCenterLook
            && vehicleRigidbody != null
            && elapsedTimeSinceLastInput >= autoCenterDelay
            && vehicleRigidbody.linearVelocity.magnitude > minSpeedForAutoCenter;
    }

    #endregion

    #region 5. Simulazione Fisica dell'Abitacolo (Sospensione Collo, Forze G, Vibrazioni, Urti, FOV)

    #region Simula la fisica completa dell'abitacolo: sospensione elastica del collo, forze G, rollio in curva/drift, vibrazioni del fondo stradale e shake da collisione
    private void SimulateCockpitPhysics()
    {
        float deltaTime = Time.deltaTime;
        if (deltaTime <= 0f)
            return;

        UpdateManualHeadRotation(deltaTime);

        Quaternion currentBodyTilt = GetCurrentBodyTiltRotation();
        Quaternion cockpitBaseRotation = ComputeCockpitSuspensionRotation(currentBodyTilt, deltaTime);

        ComputeVehicleDynamicsTargets(
            deltaTime,
            out float targetDynamicRollDegrees,
            out float targetDynamicPitchDegrees,
            out float targetDynamicYawDegrees,
            out Vector3 targetHeadGShift,
            out float verticalBobbingOffset);

        SmoothVehicleDynamics(
            targetDynamicRollDegrees,
            targetDynamicPitchDegrees,
            targetDynamicYawDegrees,
            targetHeadGShift,
            deltaTime);

        ComputeImpactShakeOffsets(deltaTime, out Vector3 shakePositionOffset, out Vector3 shakeEulerOffset);

        ApplyFinalCockpitTransform(
            currentBodyTilt,
            cockpitBaseRotation,
            verticalBobbingOffset,
            shakePositionOffset,
            shakeEulerOffset);
    }
    #endregion

    private void UpdateManualHeadRotation(float deltaTime)
    {
        float activeSmoothTime = wasLastInputFromGamepad ? gamepadSmoothTime : mouseSmoothTime;
        currentYawDegrees = Mathf.SmoothDamp(currentYawDegrees, targetYawDegrees, ref yawSmoothVelocity, activeSmoothTime);
        currentPitchDegrees = Mathf.SmoothDamp(currentPitchDegrees, targetPitchDegrees, ref pitchSmoothVelocity, activeSmoothTime);
    }

    private Quaternion GetCurrentBodyTiltRotation()
    {
        return carController != null
            ? carController.VisualTiltRotation
            : Quaternion.identity;
    }

    private Quaternion ComputeCockpitSuspensionRotation(Quaternion currentBodyTilt, float deltaTime)
    {
        laggedBodyRotation = Quaternion.Slerp(laggedBodyRotation, currentBodyTilt, neckRecoverySpeed * deltaTime);

        Vector3 relativeLagEuler = (Quaternion.Inverse(currentBodyTilt) * laggedBodyRotation).eulerAngles;
        float clampedLagPitch = Mathf.Clamp(Mathf.DeltaAngle(0f, relativeLagEuler.x) * neckSuspensionRatio, -maxNeckLagDegrees, maxNeckLagDegrees);
        float clampedLagRoll = Mathf.Clamp(Mathf.DeltaAngle(0f, relativeLagEuler.z) * neckSuspensionRatio, -maxNeckLagDegrees, maxNeckLagDegrees);

        return currentBodyTilt * Quaternion.Euler(clampedLagPitch, 0f, clampedLagRoll);
    }

    private void ComputeVehicleDynamicsTargets(
        float deltaTime,
        out float targetRollDegrees,
        out float targetPitchDegrees,
        out float targetYawDegrees,
        out Vector3 targetHeadShift,
        out float verticalBobbingOffset)
    {
        targetRollDegrees = 0f;
        targetPitchDegrees = 0f;
        targetYawDegrees = 0f;
        targetHeadShift = Vector3.zero;
        verticalBobbingOffset = 0f;

        if (vehicleRigidbody == null)
            return;

        Vector3 currentWorldVelocity = vehicleRigidbody.linearVelocity;
        DetectAutomaticImpactShake(currentWorldVelocity);
        previousWorldVelocity = currentWorldVelocity;

        Vector3 currentLocalVelocity = vehicleRigidbody.transform.InverseTransformDirection(currentWorldVelocity);
        Vector3 localAcceleration = (currentLocalVelocity - previousLocalVelocity) / deltaTime;
        previousLocalVelocity = currentLocalVelocity;

        float normalizedSpeedRatio = ComputeNormalizedSpeedRatio(currentLocalVelocity.magnitude);
        float effectiveSteer = carController != null ? carController.EffectiveSteer : 0f;
        bool isDrifting = carController != null && carController.IsDrifting;

        float totalRollRangeDegrees = cornerRollDegrees + (isDrifting ? driftExtraRollDegrees : 0f);
        targetRollDegrees = -effectiveSteer * totalRollRangeDegrees * Mathf.Clamp01(normalizedSpeedRatio * 2.5f);
        targetYawDegrees = effectiveSteer * steerLookAheadYaw * Mathf.Clamp01(normalizedSpeedRatio * 2f);
        targetPitchDegrees = Mathf.Clamp(-localAcceleration.z * 0.025f, -1f, 1f) * accelPitchDegrees;

        targetHeadShift = new Vector3(
            Mathf.Clamp(-localAcceleration.x * 0.002f, -1f, 1f) * maxHeadGShift,
            0f,
            Mathf.Clamp(-localAcceleration.z * 0.002f, -1f, 1f) * maxHeadGShift);

        verticalBobbingOffset = AdvanceRoadBobbingOffset(normalizedSpeedRatio, deltaTime);
    }

    private void DetectAutomaticImpactShake(Vector3 currentWorldVelocity)
    {
        float velocityDeltaMagnitude = (currentWorldVelocity - previousWorldVelocity).magnitude;
        if (velocityDeltaMagnitude > autoImpactThreshold && previousWorldVelocity.sqrMagnitude > MinimumVelocitySqrMagnitude)
        {
            float normalizedImpactStrength = Mathf.Clamp01((velocityDeltaMagnitude - autoImpactThreshold) / ImpactDeltaSpeedNormalizationRange);
            AddImpactShake(normalizedImpactStrength * AutoImpactShakeScale);
        }
    }

    private float AdvanceRoadBobbingOffset(float normalizedSpeedRatio, float deltaTime)
    {
        if (normalizedSpeedRatio <= MinimumSpeedForBobbing)
            return 0f;

        roadBobbingTimer += deltaTime * bobFrequency * Mathf.Lerp(0.6f, 1.6f, normalizedSpeedRatio);
        return Mathf.Sin(roadBobbingTimer) * bobAmplitude * normalizedSpeedRatio;
    }

    private void SmoothVehicleDynamics(
        float targetRollDegrees,
        float targetPitchDegrees,
        float targetYawDegrees,
        Vector3 targetHeadShift,
        float deltaTime)
    {
        float interpolationFactor = dynamicsSmoothSpeed * deltaTime;
        currentDynamicRollDegrees = Mathf.Lerp(currentDynamicRollDegrees, targetRollDegrees, interpolationFactor);
        currentDynamicPitchDegrees = Mathf.Lerp(currentDynamicPitchDegrees, targetPitchDegrees, interpolationFactor);
        currentDynamicYawDegrees = Mathf.Lerp(currentDynamicYawDegrees, targetYawDegrees, interpolationFactor);
        currentHeadGOffset = Vector3.Lerp(currentHeadGOffset, targetHeadShift, interpolationFactor);
    }

    private void ComputeImpactShakeOffsets(float deltaTime, out Vector3 shakePositionOffset, out Vector3 shakeEulerOffset)
    {
        if (currentShakeIntensity <= MinimumShakeIntensity)
        {
            shakePositionOffset = Vector3.zero;
            shakeEulerOffset = Vector3.zero;
            return;
        }

        float noiseSampleTime = Time.time * ShakeNoiseFrequency;
        float noiseX = SampleCenteredPerlinNoise(shakeNoiseSeed, noiseSampleTime);
        float noiseY = SampleCenteredPerlinNoise(shakeNoiseSeed + 15f, noiseSampleTime);
        float noiseZ = SampleCenteredPerlinNoise(shakeNoiseSeed + 30f, noiseSampleTime);

        shakePositionOffset = new Vector3(noiseX, noiseY, 0f) * (currentShakeIntensity * ShakePositionAmplitude);
        shakeEulerOffset = new Vector3(noiseY * 4.0f, noiseX * 3.5f, noiseZ * 5.5f) * currentShakeIntensity;
        currentShakeIntensity = Mathf.MoveTowards(currentShakeIntensity, 0f, shakeDecay * deltaTime);
    }

    private void ApplyFinalCockpitTransform(
        Quaternion currentBodyTilt,
        Quaternion cockpitBaseRotation,
        float verticalBobbingOffset,
        Vector3 shakePositionOffset,
        Vector3 shakeEulerOffset)
    {
        Vector3 anchoredSeatPosition = currentBodyTilt * initialSeatLocalPosition;
        Vector3 totalLocalHeadDisplacement = currentHeadGOffset + new Vector3(0f, verticalBobbingOffset, 0f) + shakePositionOffset;
        transform.localPosition = anchoredSeatPosition + (cockpitBaseRotation * totalLocalHeadDisplacement);

        transform.localRotation =
            cockpitBaseRotation *
            Quaternion.Euler(0f, currentYawDegrees + currentDynamicYawDegrees, 0f) *
            initialLocalRotation *
            Quaternion.Euler(
                currentPitchDegrees + currentDynamicPitchDegrees + shakeEulerOffset.x,
                shakeEulerOffset.y,
                currentDynamicRollDegrees + shakeEulerOffset.z);
    }

    private void UpdateDynamicFov()
    {
        if (cockpitCamera == null || vehicleRigidbody == null)
            return;

        float normalizedSpeedRatio = ComputeNormalizedSpeedRatio(vehicleRigidbody.linearVelocity.magnitude);
        float desiredFieldOfView = Mathf.Lerp(baseFov, maxSpeedFov, normalizedSpeedRatio);
        cockpitCamera.fieldOfView = Mathf.Lerp(cockpitCamera.fieldOfView, desiredFieldOfView, FovInterpolationSpeed * Time.deltaTime);
    }

    #endregion

    #region 6. Utility Matematiche e Impostazioni

    private float ComputeNormalizedSpeedRatio(float currentSpeed)
    {
        return Mathf.Clamp01(currentSpeed / Mathf.Max(maxReferenceSpeed, 0.01f));
    }

    private static float SampleCenteredPerlinNoise(float seedOffset, float sampleTime)
    {
        return (Mathf.PerlinNoise(seedOffset, sampleTime) - 0.5f) * 2f;
    }

    #region Applica una deadzone radiale riscalando in modo continuo l'intervallo [deadzone, 1] su [0, 1]
    private static Vector2 ApplyDeadzone(Vector2 input, float deadzone)
    {
        float inputMagnitude = input.magnitude;
        return inputMagnitude <= deadzone
            ? Vector2.zero
            : (input / inputMagnitude) * ((inputMagnitude - deadzone) / (1f - deadzone));
    }
    #endregion

    private static float GetSensitivitySetting()
    {
        return SettingsManager.Instance != null
            ? Mathf.Clamp01(SettingsManager.Instance.Data.sensitivity)
            : DefaultNormalizedSensitivity;
    }

    #endregion
}
#endregion