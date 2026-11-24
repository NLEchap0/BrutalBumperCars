using UnityEngine;
using UnityEngine.InputSystem;

public class CameraLookController : MonoBehaviour
{
    [Header("Riferimenti")]
    [SerializeField]
    private InputActionReference lookAction;

    [SerializeField, Tooltip("Controller dell'autoscontro (trovato automaticamente nei parent se vuoto).")]
    private BumperCarController carController;

    [SerializeField, Tooltip("Rigidbody dell'autoscontro (trovato automaticamente nei parent se vuoto).")]
    private Rigidbody vehicleRigidbody;

    [SerializeField, Tooltip("Camera in prima persona (usa quella su questo GameObject se vuoto).")]
    private Camera cockpitCamera;

    [Header("Sensibilità Visuale")]
    [SerializeField, Min(0.01f)]
    private float degreesPerMouseUnit = 0.12f;

    [SerializeField, Min(1f)]
    private float gamepadDegreesPerSecond = 200f;

    [SerializeField, Min(0.01f)]
    private float gamepadMinSensitivityMultiplier = 0.8f;

    [SerializeField, Min(0.01f)]
    private float gamepadMaxSensitivityMultiplier = 1.8f;

    [SerializeField, Range(0f, 0.5f)]
    private float gamepadDeadzone = 0.15f;

    [Header("Limiti Collo e Inerzia")]
    [SerializeField]
    private float maxYaw = 110f;

    [SerializeField]
    private float minPitch = -45f;

    [SerializeField]
    private float maxPitch = 60f;

    [SerializeField, Range(0f, 0.15f)]
    private float mouseSmoothTime = 0.02f;

    [SerializeField, Range(0f, 0.25f)]
    private float gamepadSmoothTime = 0.06f;

    [Header("Sospensione Collo & Anti-Clip Abitacolo")]
    [SerializeField, Range(0f, 0.5f), Tooltip("Quanto il collo assorbe gli scatti improvvisi dell'auto (0 = bloccato al sedile, 0.35 = ammortizzato senza mai uscire dall'auto).")]
    private float neckSuspensionRatio = 0.30f;

    [SerializeField, Tooltip("Quanto velocemente il collo si riallinea all'inclinazione dell'auto (più alto = meno ritardo su rampe ripide).")]
    private float neckRecoverySpeed = 14f;

    [SerializeField, Tooltip("Angolo massimo di assorbimento del collo rispetto all'abitacolo (impedisce di guardare dentro la carrozzeria).")]
    private float maxNeckLagDegrees = 10f;

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

    [SerializeField]
    private float dynamicsSmoothSpeed = 9f;

    [Header("Ritorno Sguardo sul Volante")]
    [SerializeField]
    private bool autoCenterLook = true;

    [SerializeField, Min(0.2f)]
    private float autoCenterDelay = 1.5f;

    [SerializeField, Min(0.5f)]
    private float autoCenterSpeed = 3.5f;

    [SerializeField, Min(0.5f)]
    private float minSpeedForAutoCenter = 2.5f;

    [Header("Vibrazioni Pista, FOV e Urti")]
    [SerializeField]
    private float bobFrequency = 14f;

    [SerializeField]
    private float bobAmplitude = 0.008f;

    [SerializeField]
    private float baseFov = 65f;

    [SerializeField]
    private float maxSpeedFov = 82f;

    [SerializeField, Tooltip("Allineato a maxSpeed di BumperCarController (50 m/s).")]
    private float maxReferenceSpeed = 50f;

    [SerializeField]
    private float autoImpactThreshold = 7.5f;

    [SerializeField]
    private float shakeDecay = 4.0f;

    // Stato iniziale rispetto al root dell'auto
    private Quaternion initialLocalRotation;
    private Vector3 initialSeatLocalPosition;

    // Rotazione manuale del collo
    private float targetYaw;
    private float targetPitch;
    private float currentYaw;
    private float currentPitch;
    private float yawVelocity;
    private float pitchVelocity;

    // Sospensione relativa all'abitacolo
    private Quaternion laggedBodyRotation = Quaternion.identity;

    // Dinamica di guida
    private Vector3 lastWorldVelocity;
    private Vector3 lastLocalVelocity;
    private float currentDynRoll;
    private float currentDynPitch;
    private float currentDynYaw;
    private Vector3 currentHeadOffset;
    private float bobTimer;

    // Input e stato
    private bool lookEnabled;
    private bool skipNextFrame;
    private bool lastInputWasGamepad;
    private float timeSinceLastInput;

    // Shake
    private float shakeIntensity;
    private float shakeSeed;

    // Cache sensibilità
    private float cachedSensitivity = -1f;
    private float mouseSensitivityMultiplier = 1f;
    private float cachedGamepadSensitivity = -1f;
    private float gamepadSensitivityMultiplier = 1f;

    private void Awake()
    {
        initialLocalRotation = transform.localRotation;
        initialSeatLocalPosition = transform.localPosition;

        if (cockpitCamera == null)
            cockpitCamera = GetComponent<Camera>();

        if (carController == null)
            carController = GetComponentInParent<BumperCarController>();

        if (vehicleRigidbody == null)
            vehicleRigidbody = GetComponentInParent<Rigidbody>();

        shakeSeed = Random.Range(0f, 100f);
    }

    private void OnEnable()
    {
        if (lookAction == null || lookAction.action == null)
        {
            enabled = false;
            return;
        }

        lookAction.action.performed += OnLookPerformed;
        lookAction.action.Enable();
        SetLookEnabled(true);
    }

    private void OnDisable()
    {
        if (lookAction != null && lookAction.action != null)
        {
            lookAction.action.performed -= OnLookPerformed;
            lookAction.action.Disable();
        }

        SetLookEnabled(false);
    }

    private void OnLookPerformed(InputAction.CallbackContext ctx)
    {
        InputDevice device = ctx.control?.device;
        if (device != null)
        {
            lastInputWasGamepad = device is Gamepad || device is Joystick;
        }
    }

    private void Update()
    {
        if (!lookEnabled)
            return;

        if (skipNextFrame)
        {
            skipNextFrame = false;
            return;
        }

        ReadLookInput();
        HandleAutoCenter();
    }

    private void LateUpdate()
    {
        if (!lookEnabled)
            return;

        SimulateCockpitPhysics();
        UpdateDynamicFov();
    }

    private void ReadLookInput()
    {
        Vector2 lookInput = lookAction.action.ReadValue<Vector2>();
        bool hasInput = false;

        if (lastInputWasGamepad)
        {
            Vector2 stick = ApplyDeadzone(lookInput, gamepadDeadzone);
            if (stick.sqrMagnitude > 0.0001f)
            {
                hasInput = true;
                float rotationSpeed = gamepadDegreesPerSecond * GetGamepadSensitivityMultiplier() * Time.deltaTime;
                targetYaw += stick.x * rotationSpeed;
                targetPitch -= stick.y * rotationSpeed;
            }
        }
        else
        {
            if (lookInput.sqrMagnitude > 0.0001f)
            {
                hasInput = true;
                float rotationSpeed = degreesPerMouseUnit * GetMouseSensitivityMultiplier();
                targetYaw += lookInput.x * rotationSpeed;
                targetPitch -= lookInput.y * rotationSpeed;
            }
        }

        if (hasInput)
            timeSinceLastInput = 0f;
        else
            timeSinceLastInput += Time.deltaTime;

        targetYaw = Mathf.Clamp(targetYaw, -maxYaw, maxYaw);
        targetPitch = Mathf.Clamp(targetPitch, minPitch, maxPitch);
    }

    private void HandleAutoCenter()
    {
        if (!autoCenterLook || vehicleRigidbody == null)
            return;

        if (timeSinceLastInput < autoCenterDelay)
            return;

        if (vehicleRigidbody.linearVelocity.magnitude > minSpeedForAutoCenter)
        {
            targetYaw = Mathf.Lerp(targetYaw, 0f, autoCenterSpeed * Time.deltaTime);
            targetPitch = Mathf.Lerp(targetPitch, 0f, autoCenterSpeed * Time.deltaTime);
        }
    }

    private void SimulateCockpitPhysics()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f)
            return;

        // 1. Rotazione collo manuale
        float smoothTime = lastInputWasGamepad ? gamepadSmoothTime : mouseSmoothTime;
        currentYaw = Mathf.SmoothDamp(currentYaw, targetYaw, ref yawVelocity, smoothTime);
        currentPitch = Mathf.SmoothDamp(currentPitch, targetPitch, ref pitchVelocity, smoothTime);

        // 2. Rotazione esatta dell'abitacolo (VisualTilt)
        Quaternion bodyTilt = carController != null
            ? carController.VisualTiltRotation
            : Quaternion.identity;

        // Calcola una rotazione smorzata che segue l'abitacolo con un leggero ritardo elastico
        laggedBodyRotation = Quaternion.Slerp(laggedBodyRotation, bodyTilt, neckRecoverySpeed * dt);

        // Differenza tra l'inclinazione attuale dell'abitacolo e quella ritardata (sospensione del collo)
        Quaternion neckLagDelta = Quaternion.Inverse(bodyTilt) * laggedBodyRotation;
        Vector3 lagEuler = neckLagDelta.eulerAngles;
        float lagPitch = Mathf.Clamp(NormalizeAngle(lagEuler.x) * neckSuspensionRatio, -maxNeckLagDegrees, maxNeckLagDegrees);
        float lagRoll = Mathf.Clamp(NormalizeAngle(lagEuler.z) * neckSuspensionRatio, -maxNeckLagDegrees, maxNeckLagDegrees);
        Quaternion safeNeckSuspension = Quaternion.Euler(lagPitch, 0f, lagRoll);

        // Rotazione effettiva della testa nello spazio del root dell'auto:
        // segue al 100% l'inclinazione grande dell'abitacolo, ma ammortizza gli scatti secchi fino a maxNeckLagDegrees
        Quaternion cockpitBaseRotation = bodyTilt * safeNeckSuspension;

        // 3. Dinamica da sterzo, drift e accelerazioni
        float targetDynRoll = 0f;
        float targetDynPitch = 0f;
        float targetDynYaw = 0f;
        Vector3 targetHeadShift = Vector3.zero;
        float bobY = 0f;

        if (vehicleRigidbody != null)
        {
            Transform vTransform = vehicleRigidbody.transform;
            Vector3 worldVel = vehicleRigidbody.linearVelocity;
            Vector3 localVel = vTransform.InverseTransformDirection(worldVel);
            float speedRatio = Mathf.Clamp01(localVel.magnitude / maxReferenceSpeed);

            // Rilevamento automatico urti
            float deltaSpeed = (worldVel - lastWorldVelocity).magnitude;
            if (deltaSpeed > autoImpactThreshold && lastWorldVelocity.sqrMagnitude > 0.2f)
            {
                float impactNorm = Mathf.Clamp01((deltaSpeed - autoImpactThreshold) / 18f);
                AddImpactShake(impactNorm * 0.85f);
            }
            lastWorldVelocity = worldVel;

            Vector3 localAccel = (localVel - lastLocalVelocity) / dt;
            lastLocalVelocity = localVel;

            float steer = carController != null ? carController.EffectiveSteer : 0f;
            bool isDrifting = carController != null && carController.IsDrifting;

            // Rollio in curva + extra roll durante il drift
            float rollAmount = cornerRollDegrees + (isDrifting ? driftExtraRollDegrees : 0f);
            targetDynRoll = -steer * rollAmount * Mathf.Clamp01(speedRatio * 2.5f);

            // Anticipo sguardo verso l'interno della curva
            targetDynYaw = steer * steerLookAheadYaw * Mathf.Clamp01(speedRatio * 2f);

            // Beccheggio accelerazione/frenata
            targetDynPitch = Mathf.Clamp(-localAccel.z * 0.025f, -1f, 1f) * accelPitchDegrees;

            // Spostamento testa per forze G (contenuto dentro maxHeadGShift)
            targetHeadShift = new Vector3(
                Mathf.Clamp(-localAccel.x * 0.002f, -1f, 1f) * maxHeadGShift,
                0f,
                Mathf.Clamp(-localAccel.z * 0.002f, -1f, 1f) * maxHeadGShift
            );

            // Vibrazione fondo arena
            if (speedRatio > 0.04f)
            {
                bobTimer += dt * bobFrequency * Mathf.Lerp(0.6f, 1.6f, speedRatio);
                bobY = Mathf.Sin(bobTimer) * bobAmplitude * speedRatio;
            }
        }

        float lerpT = dynamicsSmoothSpeed * dt;
        currentDynRoll = Mathf.Lerp(currentDynRoll, targetDynRoll, lerpT);
        currentDynPitch = Mathf.Lerp(currentDynPitch, targetDynPitch, lerpT);
        currentDynYaw = Mathf.Lerp(currentDynYaw, targetDynYaw, lerpT);
        currentHeadOffset = Vector3.Lerp(currentHeadOffset, targetHeadShift, lerpT);

        // 4. Shake da impatto
        Vector3 shakePos = Vector3.zero;
        Vector3 shakeRot = Vector3.zero;
        if (shakeIntensity > 0.001f)
        {
            float t = Time.time * 35f;
            float nX = (Mathf.PerlinNoise(shakeSeed, t) - 0.5f) * 2f;
            float nY = (Mathf.PerlinNoise(shakeSeed + 15f, t) - 0.5f) * 2f;
            float nZ = (Mathf.PerlinNoise(shakeSeed + 30f, t) - 0.5f) * 2f;

            shakePos = new Vector3(nX, nY, 0f) * (shakeIntensity * 0.025f);
            shakeRot = new Vector3(nY * 4.0f, nX * 3.5f, nZ * 5.5f) * shakeIntensity;

            shakeIntensity = Mathf.MoveTowards(shakeIntensity, 0f, shakeDecay * dt);
        }

        // 5. POSIZIONE ANCORATA AL SEDILE INCLINATO:
        // Ruota la posizione base del sedile esattamente con bodyTilt, così quando l'auto si inclina
        // molto (avanti, indietro o di lato) la telecamera si sposta insieme al sedile e non esce mai dall'auto!
        Vector3 anchoredSeatPos = bodyTilt * initialSeatLocalPosition;
        Vector3 localHeadDynamics = cockpitBaseRotation * (currentHeadOffset + new Vector3(0f, bobY, 0f) + shakePos);

        transform.localPosition = anchoredSeatPos + localHeadDynamics;

        // 6. ROTAZIONE FINALE:
        // Base abitacolo (con sospensione max 10°) -> Yaw collo -> Rotazione iniziale -> Pitch + Roll dinamico
        transform.localRotation =
            cockpitBaseRotation *
            Quaternion.Euler(0f, currentYaw + currentDynYaw, 0f) *
            initialLocalRotation *
            Quaternion.Euler(currentPitch + currentDynPitch + shakeRot.x, shakeRot.y, currentDynRoll + shakeRot.z);
    }

    private void UpdateDynamicFov()
    {
        if (cockpitCamera == null || vehicleRigidbody == null)
            return;

        float speedRatio = Mathf.Clamp01(vehicleRigidbody.linearVelocity.magnitude / maxReferenceSpeed);
        float desiredFov = Mathf.Lerp(baseFov, maxSpeedFov, speedRatio);

        cockpitCamera.fieldOfView = Mathf.Lerp(cockpitCamera.fieldOfView, desiredFov, 6f * Time.deltaTime);
    }

    public void AddImpactShake(float intensity)
    {
        shakeIntensity = Mathf.Clamp(shakeIntensity + intensity, 0f, 1.3f);
    }

    public void ResetLook()
    {
        targetYaw = 0f;
        targetPitch = 0f;
    }

    private static Vector2 ApplyDeadzone(Vector2 input, float deadzone)
    {
        float magnitude = input.magnitude;
        if (magnitude <= deadzone)
            return Vector2.zero;

        return (input / magnitude) * ((magnitude - deadzone) / (1f - deadzone));
    }

    private static float NormalizeAngle(float angle)
    {
        while (angle > 180f) angle -= 360f;
        while (angle < -180f) angle += 360f;
        return angle;
    }

    private float GetMouseSensitivityMultiplier()
    {
        float settingValue = GetSensitivitySetting();
        if (!Mathf.Approximately(settingValue, cachedSensitivity))
        {
            cachedSensitivity = settingValue;
            mouseSensitivityMultiplier = Mathf.Pow(2f, (settingValue - 0.5f) * 2f);
        }
        return mouseSensitivityMultiplier;
    }

    private float GetGamepadSensitivityMultiplier()
    {
        float settingValue = GetSensitivitySetting();
        if (!Mathf.Approximately(settingValue, cachedGamepadSensitivity))
        {
            cachedGamepadSensitivity = settingValue;
            gamepadSensitivityMultiplier = Mathf.Lerp(
                gamepadMinSensitivityMultiplier,
                gamepadMaxSensitivityMultiplier,
                settingValue
            );
        }
        return gamepadSensitivityMultiplier;
    }

    private static float GetSensitivitySetting()
    {
        if (SettingsManager.Instance == null)
            return 0.5f;

        return Mathf.Clamp01(SettingsManager.Instance.Data.sensitivity);
    }

    public void SetLookEnabled(bool isEnabled)
    {
        lookEnabled = isEnabled;
        if (isEnabled)
            skipNextFrame = true;

        Cursor.lockState = isEnabled ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !isEnabled;
    }
}