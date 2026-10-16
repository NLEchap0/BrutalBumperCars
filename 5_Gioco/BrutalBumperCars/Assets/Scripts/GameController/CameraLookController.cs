
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
    private float sensitivityMultiplier = 1f;


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
            Debug.LogError(
                "CameraLookController: assegna l'azione Look nell'Inspector.",
                this
            );

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

        // Legge il movimento del mouse come Vector2.
        Vector2 mouseDelta =
            lookAction.action.ReadValue<Vector2>();

        float rotationSpeed = degreesPerMouseUnit * GetSensitivityMultiplier();

        // Rotazione orizzontale.
        yaw += mouseDelta.x * rotationSpeed;

        // Rotazione verticale.
        pitch -= mouseDelta.y * rotationSpeed;

        // Limita la rotazione verticale.
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        // Applica la rotazione alla telecamera.
        transform.localRotation =
            initialLocalRotation *
            Quaternion.Euler(pitch, yaw, 0f);
    }

    private float GetSensitivityMultiplier()
    {
        // Valore predefinito se il manager non è disponibile.
        float settingValue = 0.5f;

        if (SettingsManager.Instance != null)
        {
            settingValue = Mathf.Clamp01(
                SettingsManager.Instance.Data.sensitivity
            );
        }

        // Ricalcola il moltiplicatore solo quando cambia il valore.
        if (!Mathf.Approximately(
            settingValue, cachedSensitivity))
        {
            cachedSensitivity = settingValue;

            sensitivityMultiplier = Mathf.Pow(
                2f,
                (settingValue - 0.5f) * 2f
            );
        }

        return sensitivityMultiplier;
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
