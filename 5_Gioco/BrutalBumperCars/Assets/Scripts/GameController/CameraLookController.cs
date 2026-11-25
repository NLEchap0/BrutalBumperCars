/*
 * NOTE DI SVILUPPO:
 * I calcoli matematici avanzati, la composizione dei quaternioni per la sospensione
 * dell'abitacolo, la simulazione delle forze G e le logiche complesse presenti in questo
 * script sono stati sviluppati con l'ausilio di un'Intelligenza Artificiale.
 */

using UnityEngine;
using UnityEngine.InputSystem;

public class CameraLookController : MonoBehaviour
{
    [Header("Riferimenti")]
    [SerializeField] private InputActionReference lookAction;

    [SerializeField, Tooltip("Controller dell'autoscontro (trovato automaticamente nei parent se vuoto).")]
    private BumperCarController carController;

    [SerializeField, Tooltip("Rigidbody dell'autoscontro (trovato automaticamente nei parent se vuoto).")]
    private Rigidbody vehicleRigidbody;

    [SerializeField, Tooltip("Camera in prima persona (usa quella su questo GameObject se vuoto).")]
    private Camera cockpitCamera;

    [Header("Sensibilità Visuale")]
    [SerializeField, Min(0.01f)] private float degreesPerMouseUnit = 0.12f;
    [SerializeField, Min(1f)] private float gamepadDegreesPerSecond = 200f;
    [SerializeField, Min(0.01f)] private float gamepadMinSensitivityMultiplier = 0.8f;
    [SerializeField, Min(0.01f)] private float gamepadMaxSensitivityMultiplier = 1.8f;
    [SerializeField, Range(0f, 0.5f)] private float gamepadDeadzone = 0.15f;

    [Header("Limiti Collo e Inerzia")]
    [SerializeField] private float maxYaw = 110f;
    [SerializeField] private float minPitch = -45f;
    [SerializeField] private float maxPitch = 60f;
    [SerializeField, Range(0f, 0.15f)] private float mouseSmoothTime = 0.02f;
    [SerializeField, Range(0f, 0.25f)] private float gamepadSmoothTime = 0.06f;

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

    [SerializeField] private float dynamicsSmoothSpeed = 9f;

    [Header("Ritorno Sguardo sul Volante")]
    [SerializeField] private bool autoCenterLook = true;
    [SerializeField, Min(0.2f)] private float autoCenterDelay = 1.5f;
    [SerializeField, Min(0.5f)] private float autoCenterSpeed = 3.5f;
    [SerializeField, Min(0.5f)] private float minSpeedForAutoCenter = 2.5f;

    [Header("Vibrazioni Pista, FOV e Urti")]
    [SerializeField] private float bobFrequency = 14f;
    [SerializeField] private float bobAmplitude = 0.008f;
    [SerializeField] private float baseFov = 65f;
    [SerializeField] private float maxSpeedFov = 82f;

    [SerializeField, Tooltip("Allineato a maxSpeed di BumperCarController (50 m/s).")]
    private float maxReferenceSpeed = 50f;

    [SerializeField] private float autoImpactThreshold = 7.5f;
    [SerializeField] private float shakeDecay = 4.0f;

    private Quaternion initialLocalRotation;
    private Vector3 initialSeatLocalPosition;

    private float targetYaw;
    private float targetPitch;
    private float currentYaw;
    private float currentPitch;
    private float yawVelocity;
    private float pitchVelocity;

    private Quaternion laggedBodyRotation = Quaternion.identity;

    private Vector3 lastWorldVelocity;
    private Vector3 lastLocalVelocity;
    private float currentDynRoll;
    private float currentDynPitch;
    private float currentDynYaw;
    private Vector3 currentHeadOffset;
    private float bobTimer;

    private bool lookEnabled;
    private bool skipNextFrame;
    private bool lastInputWasGamepad;
    private float timeSinceLastInput;

    private float shakeIntensity;
    private float shakeSeed;

    private void Awake()
    {
        initialLocalRotation = transform.localRotation;
        initialSeatLocalPosition = transform.localPosition;

        if (cockpitCamera == null) cockpitCamera = GetComponent<Camera>();
        if (carController == null) carController = GetComponentInParent<BumperCarController>();
        if (vehicleRigidbody == null) vehicleRigidbody = GetComponentInParent<Rigidbody>();

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
            lastInputWasGamepad = device is Gamepad || device is Joystick;
    }

    private void Update()
    {
        if (!lookEnabled)
            return;

        if (skipNextFrame)
        {
            skipNextFrame = false;
            SyncVelocityCache();
            return;
        }

        ReadLookInput();
        HandleAutoCenter();
    }

    private void LateUpdate()
    {
        if (!lookEnabled || skipNextFrame)
            return;

        SimulateCockpitPhysics();
        UpdateDynamicFov();
    }

    /// <summary>
    /// Legge l'input visuale distinguendo tra spostamento relativo (mouse) e velocità angolare (gamepad).
    /// </summary>
    private void ReadLookInput()
    {
        Vector2 lookInput = lookAction.action.ReadValue<Vector2>();
        float sensitivity = GetSensitivitySetting();
        bool hasInput = false;

        if (lastInputWasGamepad)
        {
            Vector2 stick = ApplyDeadzone(lookInput, gamepadDeadzone);
            if (stick.sqrMagnitude > 0.0001f)
            {
                hasInput = true;
                float mult = Mathf.Lerp(gamepadMinSensitivityMultiplier, gamepadMaxSensitivityMultiplier, sensitivity);
                float speed = gamepadDegreesPerSecond * mult * Time.deltaTime;
                targetYaw += stick.x * speed;
                targetPitch -= stick.y * speed;
            }
        }
        else if (lookInput.sqrMagnitude > 0.0001f)
        {
            hasInput = true;
            // Curva esponenziale centrata su 0.5 per scalare in modo naturale la sensibilità del mouse
            float mult = Mathf.Pow(2f, (sensitivity - 0.5f) * 2f);
            float speed = degreesPerMouseUnit * mult;
            targetYaw += lookInput.x * speed;
            targetPitch -= lookInput.y * speed;
        }

        timeSinceLastInput = hasInput ? 0f : timeSinceLastInput + Time.deltaTime;
        targetYaw = Mathf.Clamp(targetYaw, -maxYaw, maxYaw);
        targetPitch = Mathf.Clamp(targetPitch, minPitch, maxPitch);
    }

    private void HandleAutoCenter()
    {
        if (!autoCenterLook || vehicleRigidbody == null || timeSinceLastInput < autoCenterDelay)
            return;

        if (vehicleRigidbody.linearVelocity.magnitude > minSpeedForAutoCenter)
        {
            float t = autoCenterSpeed * Time.deltaTime;
            targetYaw = Mathf.Lerp(targetYaw, 0f, t);
            targetPitch = Mathf.Lerp(targetPitch, 0f, t);
        }
    }

    /// <summary>
    /// Simula la fisica completa dell'abitacolo: sospensione elastica del collo, forze G,
    /// rollio in curva/drift, vibrazioni del fondo stradale e shake da collisione.
    /// </summary>
    private void SimulateCockpitPhysics()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f)
            return;

        // 1. Smorzamento della rotazione manuale della testa
        float smoothTime = lastInputWasGamepad ? gamepadSmoothTime : mouseSmoothTime;
        currentYaw = Mathf.SmoothDamp(currentYaw, targetYaw, ref yawVelocity, smoothTime);
        currentPitch = Mathf.SmoothDamp(currentPitch, targetPitch, ref pitchVelocity, smoothTime);

        // 2. Sospensione relativa all'abitacolo: calcola lo scarto angolare tra la scocca attuale
        // e una sua versione smorzata, limitandolo entro maxNeckLagDegrees per evitare clip con la carrozzeria
        Quaternion bodyTilt = carController != null ? carController.VisualTiltRotation : Quaternion.identity;
        laggedBodyRotation = Quaternion.Slerp(laggedBodyRotation, bodyTilt, neckRecoverySpeed * dt);

        Vector3 lagEuler = (Quaternion.Inverse(bodyTilt) * laggedBodyRotation).eulerAngles;
        float lagPitch = Mathf.Clamp(Mathf.DeltaAngle(0f, lagEuler.x) * neckSuspensionRatio, -maxNeckLagDegrees, maxNeckLagDegrees);
        float lagRoll = Mathf.Clamp(Mathf.DeltaAngle(0f, lagEuler.z) * neckSuspensionRatio, -maxNeckLagDegrees, maxNeckLagDegrees);
        Quaternion cockpitBaseRotation = bodyTilt * Quaternion.Euler(lagPitch, 0f, lagRoll);

        // 3. Calcolo delle forze G, rollio in curva e anticipo dello sguardo
        float targetDynRoll = 0f;
        float targetDynPitch = 0f;
        float targetDynYaw = 0f;
        Vector3 targetHeadShift = Vector3.zero;
        float bobY = 0f;

        if (vehicleRigidbody != null)
        {
            Vector3 worldVel = vehicleRigidbody.linearVelocity;
            Vector3 localVel = vehicleRigidbody.transform.InverseTransformDirection(worldVel);
            float speedRatio = Mathf.Clamp01(localVel.magnitude / Mathf.Max(maxReferenceSpeed, 0.01f));

            // Rileva decelerazioni brusche (urti) tramite il delta di velocità tra frame consecutivi
            float deltaSpeed = (worldVel - lastWorldVelocity).magnitude;
            if (deltaSpeed > autoImpactThreshold && lastWorldVelocity.sqrMagnitude > 0.2f)
            {
                float impactNorm = Mathf.Clamp01((deltaSpeed - autoImpactThreshold) / 18f);
                AddImpactShake(impactNorm * 0.85f);
            }
            lastWorldVelocity = worldVel;

            // Derivata della velocità locale per stimare le accelerazioni longitudinali e laterali
            Vector3 localAccel = (localVel - lastLocalVelocity) / dt;
            lastLocalVelocity = localVel;

            float steer = carController != null ? carController.EffectiveSteer : 0f;
            bool isDrifting = carController != null && carController.IsDrifting;

            float rollAmount = cornerRollDegrees + (isDrifting ? driftExtraRollDegrees : 0f);
            targetDynRoll = -steer * rollAmount * Mathf.Clamp01(speedRatio * 2.5f);
            targetDynYaw = steer * steerLookAheadYaw * Mathf.Clamp01(speedRatio * 2f);
            targetDynPitch = Mathf.Clamp(-localAccel.z * 0.025f, -1f, 1f) * accelPitchDegrees;

            targetHeadShift = new Vector3(
                Mathf.Clamp(-localAccel.x * 0.002f, -1f, 1f) * maxHeadGShift,
                0f,
                Mathf.Clamp(-localAccel.z * 0.002f, -1f, 1f) * maxHeadGShift
            );

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

        // 4. Genera micro-oscillazioni organiche tramite Perlin Noise 2D in caso di urto
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

        // 5. Composizione gerarchica finale: ancora la posizione al sedile inclinato e applica
        // in ordine: inclinazione abitacolo -> yaw collo -> rotazione base -> pitch/roll dinamici
        Vector3 anchoredSeatPos = bodyTilt * initialSeatLocalPosition;
        Vector3 localHeadDynamics = cockpitBaseRotation * (currentHeadOffset + new Vector3(0f, bobY, 0f) + shakePos);
        transform.localPosition = anchoredSeatPos + localHeadDynamics;

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

        float speedRatio = Mathf.Clamp01(vehicleRigidbody.linearVelocity.magnitude / Mathf.Max(maxReferenceSpeed, 0.01f));
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

    public void SetLookEnabled(bool isEnabled)
    {
        lookEnabled = isEnabled;
        if (isEnabled)
        {
            skipNextFrame = true;
            SyncVelocityCache();
        }

        Cursor.lockState = isEnabled ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !isEnabled;
    }

    private void SyncVelocityCache()
    {
        if (vehicleRigidbody == null)
            return;

        lastWorldVelocity = vehicleRigidbody.linearVelocity;
        lastLocalVelocity = vehicleRigidbody.transform.InverseTransformDirection(lastWorldVelocity);
    }

    /// <summary>
    /// Applica una deadzone radiale riscalando in modo continuo l'intervallo [deadzone, 1] su [0, 1].
    /// </summary>
    private static Vector2 ApplyDeadzone(Vector2 input, float deadzone)
    {
        float magnitude = input.magnitude;
        return magnitude <= deadzone
            ? Vector2.zero
            : (input / magnitude) * ((magnitude - deadzone) / (1f - deadzone));
    }

    private static float GetSensitivitySetting()
    {
        return SettingsManager.Instance != null
            ? Mathf.Clamp01(SettingsManager.Instance.Data.sensitivity)
            : 0.5f;
    }
}