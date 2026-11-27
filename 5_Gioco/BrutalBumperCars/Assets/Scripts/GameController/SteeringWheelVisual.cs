using UnityEngine;

#region Controllo visivo della rotazione del volante sincronizzato con lo sterzo del veicolo
public class SteeringWheelVisual : MonoBehaviour
{
    #region Dichiarazioni Costanti, Parametri Inspector e Variabili

    #region Costanti di Validazione
    private const float MinimumAxisSqrMagnitude = 0.0001f;
    #endregion

    #region Riferimenti
    [Header("Riferimenti")]
    [SerializeField] private BumperCarController carController;
    [SerializeField] private Transform steeringWheelMesh;
    #endregion

    #region Impostazioni Rotazione
    [Header("Impostazioni Rotazione")]
    [Tooltip("Angolo massimo di rotazione del volante in gradi (es. 90 o 120).")]
    [SerializeField] private float maxTurnAngle = 110f;

    [Tooltip("Asse locale attorno a cui ruota il volante (solitamente Z o Y in base al modello 3D).")]
    [SerializeField] private Vector3 rotationAxis = Vector3.forward;

    [Tooltip("Inverti direzione se il volante gira al contrario.")]
    [SerializeField] private bool invertDirection = false;
    #endregion

    #region Fluidità (Smoothing)
    [Header("Fluidità (Smoothing)")]
    [Tooltip("Velocità con cui il volante raggiunge la rotazione desiderata. Valori bassi (8-12) = più morbido; valori alti (20+) = più reattivo.")]
    [SerializeField, Min(1f)] private float visualSmoothSpeed = 12f;

    [Tooltip("Usa EffectiveSteer (include l'inerzia in drift) invece di CurrentSteer.")]
    [SerializeField] private bool includeDriftInertia = false;
    #endregion

    #region Stato Interno
    private Quaternion initialLocalRotation;
    private float currentVisualAngleDegrees;
    #endregion

    #endregion

    #region 1. Ciclo di Vita Unity (Awake, OnEnable, LateUpdate)

    private void Awake()
    {
        ResolveRequiredReferences();
        SaveInitialSteeringWheelRotation();
    }

    private void OnEnable()
    {
        ResetSteeringWheelVisualRotation();
    }

    #region Aggiorna la rotazione visiva del volante con interpolazione esponenziale indipendente dal framerate
    private void LateUpdate()
    {
        if (!HasValidReferences())
            return;

        float targetAngleDegrees = ComputeTargetSteeringWheelAngleDegrees();
        UpdateSmoothedVisualAngle(targetAngleDegrees, Time.deltaTime);
        ApplyRotationToSteeringWheelMesh();
    }
    #endregion

    #endregion

    #region 2. Inizializzazione e Ripristino Stato

    private void ResolveRequiredReferences()
    {
        if (carController == null)
            carController = GetComponentInParent<BumperCarController>();

        if (steeringWheelMesh == null)
            steeringWheelMesh = transform;
    }

    private void SaveInitialSteeringWheelRotation()
    {
        initialLocalRotation = steeringWheelMesh.localRotation;
    }

    private void ResetSteeringWheelVisualRotation()
    {
        currentVisualAngleDegrees = 0f;
        if (steeringWheelMesh != null)
            steeringWheelMesh.localRotation = initialLocalRotation;
    }

    private bool HasValidReferences() => carController != null && steeringWheelMesh != null;

    #endregion

    #region 3. Calcolo e Applicazione della Rotazione Visiva

    private float ComputeTargetSteeringWheelAngleDegrees()
    {
        float normalizedSteerInput = ReadNormalizedSteerFromController();
        float directionSign = invertDirection ? -1f : 1f;
        return normalizedSteerInput * maxTurnAngle * directionSign;
    }

    private float ReadNormalizedSteerFromController()
    {
        return includeDriftInertia
            ? carController.EffectiveSteer
            : carController.CurrentSteer;
    }

    private void UpdateSmoothedVisualAngle(float targetAngleDegrees, float deltaTime)
    {
        float interpolationFactor = ExpDecay(visualSmoothSpeed, deltaTime);
        currentVisualAngleDegrees = Mathf.Lerp(currentVisualAngleDegrees, targetAngleDegrees, interpolationFactor);
    }

    private void ApplyRotationToSteeringWheelMesh()
    {
        Vector3 normalizedRotationAxis = GetNormalizedRotationAxis();
        Quaternion steeringWheelOffsetRotation = Quaternion.AngleAxis(currentVisualAngleDegrees, normalizedRotationAxis);
        steeringWheelMesh.localRotation = initialLocalRotation * steeringWheelOffsetRotation;
    }

    private Vector3 GetNormalizedRotationAxis()
    {
        return rotationAxis.sqrMagnitude > MinimumAxisSqrMagnitude
            ? rotationAxis.normalized
            : Vector3.forward;
    }

    #endregion

    #region 4. Utility Matematiche

    #region Fattore di interpolazione esponenziale indipendente dal framerate: 1 - e^(-decayRate * deltaTime)
    private static float ExpDecay(float decayRate, float deltaTime) => 1f - Mathf.Exp(-decayRate * deltaTime);
    #endregion

    #endregion
}
#endregion