
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraLookController : MonoBehaviour
{
    [Header("Input")]
    [SerializeField]
    private InputActionReference lookAction;

    [Header("Rotazione")]
    [SerializeField, Min(0.01f)]
    private float degreesPerMouseUnit = 0.12f;

    // Velocità di rotazione con lo stick al massimo (gradi/s) alla sensibilità base (0.5).
    // Lo stick restituisce una posizione (0..1) e non un delta in pixel come il mouse,
    // quindi ha bisogno di una scala molto più alta per risultare reattivo.
    [SerializeField, Min(1f)]
    private float gamepadDegreesPerSecond = 200f;

    // Il controller parte già da una base alta: anche a sensibilità minima mantiene
    // una buona velocità di rotazione, indipendente dalla curva (più lenta) del mouse.
    [SerializeField, Min(0.01f)]
    private float gamepadMinSensitivityMultiplier = 0.8f;

    [SerializeField, Min(0.01f)]
    private float gamepadMaxSensitivityMultiplier = 1.8f;

    // Zona morta dello stick: evita che il piccolo drift dello stick faccia ruotare
    // la telecamera da sola (con la scala del controller sarebbe ben visibile).
    [SerializeField, Range(0f, 0.5f)]
    private float gamepadDeadzone = 0.15f;

    // Limiti della rotazione verticale.
    [SerializeField]
    private float minPitch = -60f;

    [SerializeField]
    private float maxPitch = 70f;

    private Quaternion initialLocalRotation;

    private float yaw;
    private float pitch;

    private bool lookEnabled;

    // Valori della sensibilità già calcolati.
    private float cachedSensitivity = -1f;
    private float mouseSensitivityMultiplier = 1f;

    private float cachedGamepadSensitivity = -1f;
    private float gamepadSensitivityMultiplier = 1f;


    private void Awake()
    {
        // Salva la rotazione iniziale della telecamera.
        initialLocalRotation = transform.localRotation;
    }


    private void OnEnable()
    {
        // Verifica che sia stata assegnata un'azione.
        if (lookAction == null || lookAction.action == null)
        {
            enabled = false;
            return;
        }

        // Attiva l'azione Look.
        lookAction.action.Enable();

        // Abilita la visuale e blocca il cursore.
        SetLookEnabled(true);
    }


    private void OnDisable()
    {
        // Disattiva l'azione gestita da questo script.
        if (lookAction != null && lookAction.action != null)
        {
            lookAction.action.Disable();
        }

        // Ripristina il cursore quando lo script viene disattivato.
        SetLookEnabled(false);
    }


    private void Update()
    {
        if (!lookEnabled)
            return;

        InputAction action = lookAction.action;
        Vector2 lookInput = action.ReadValue<Vector2>();

        // Lo stick del controller restituisce una posizione (0..1), il mouse un delta
        // in pixel: vanno convertiti con scale e curve diverse, altrimenti a parità di
        // sensibilità il controller risulta lentissimo e il mouse troppo veloce.
        if (IsGamepadInput(action))
        {
            Vector2 stick = ApplyDeadzone(lookInput, gamepadDeadzone);

            float rotationSpeed =
                gamepadDegreesPerSecond *
                GetGamepadSensitivityMultiplier() *
                Time.deltaTime;

            // Rotazione orizzontale.
            yaw += stick.x * rotationSpeed;

            // Rotazione verticale.
            pitch -= stick.y * rotationSpeed;
        }
        else
        {
            float rotationSpeed =
                degreesPerMouseUnit * GetMouseSensitivityMultiplier();

            // Rotazione orizzontale.
            yaw += lookInput.x * rotationSpeed;

            // Rotazione verticale.
            pitch -= lookInput.y * rotationSpeed;
        }

        // Limita la rotazione verticale.
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        // Applica la rotazione alla telecamera.
        transform.localRotation =
            initialLocalRotation *
            Quaternion.Euler(pitch, yaw, 0f);
    }


    // True se l'ultimo controllo che ha mosso la visuale è uno stick analogico.
    private static bool IsGamepadInput(InputAction action)
    {
        InputControl control = action.activeControl;

        if (control == null)
            return false;

        return control.device is Gamepad || control.device is Joystick;
    }


    // Zona morta radiale: il valore riparte da 0 in modo continuo dopo la soglia.
    private static Vector2 ApplyDeadzone(Vector2 input, float deadzone)
    {
        float magnitude = input.magnitude;

        if (magnitude <= deadzone)
            return Vector2.zero;

        return input / magnitude *
            ((magnitude - deadzone) / (1f - deadzone));
    }


    private float GetMouseSensitivityMultiplier()
    {
        float settingValue = GetSensitivitySetting();

        // Ricalcola il moltiplicatore solo quando cambia il valore.
        if (!Mathf.Approximately(
            settingValue, cachedSensitivity))
        {
            cachedSensitivity = settingValue;

            mouseSensitivityMultiplier = Mathf.Pow(
                2f,
                (settingValue - 0.5f) * 2f
            );
        }

        return mouseSensitivityMultiplier;
    }


    private float GetGamepadSensitivityMultiplier()
    {
        float settingValue = GetSensitivitySetting();

        // Il controller usa una curva lineare che parte già alta: a metà asticella
        // ruota a velocità piena, senza restare lento come con la curva del mouse.
        if (!Mathf.Approximately(
            settingValue, cachedGamepadSensitivity))
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
        // Valore predefinito se il manager non è disponibile.
        if (SettingsManager.Instance == null)
            return 0.5f;

        return Mathf.Clamp01(
            SettingsManager.Instance.Data.sensitivity
        );
    }


    public void SetLookEnabled(bool isEnabled)
    {
        lookEnabled = isEnabled;

        Cursor.lockState = isEnabled
            ? CursorLockMode.Locked
            : CursorLockMode.None;

        Cursor.visible = !isEnabled;
    }
}
