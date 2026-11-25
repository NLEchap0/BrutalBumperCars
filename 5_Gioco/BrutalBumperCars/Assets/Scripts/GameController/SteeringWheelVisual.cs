using UnityEngine;

public class SteeringWheelVisual : MonoBehaviour
{
    [Header("Riferimenti")]
    [SerializeField] private BumperCarController carController;
    [SerializeField] private Transform steeringWheelMesh;

    [Header("Impostazioni Rotazione")]
    [Tooltip("Angolo massimo di rotazione del volante in gradi (es. 90 o 120).")]
    [SerializeField] private float maxTurnAngle = 110f;

    [Tooltip("Asse locale attorno a cui ruota il volante (solitamente Z o Y in base al modello 3D).")]
    [SerializeField] private Vector3 rotationAxis = Vector3.forward;

    [Tooltip("Inverti direzione se il volante gira al contrario.")]
    [SerializeField] private bool invertDirection = false;

    [Header("Fluidità (Smoothing)")]
    [Tooltip("Velocità con cui il volante raggiunge la rotazione desiderata. Valori bassi (8-12) = più morbido; valori alti (20+) = più reattivo.")]
    [SerializeField, Min(1f)] private float visualSmoothSpeed = 12f;

    [Tooltip("Usa EffectiveSteer (include l'inerzia in drift) invece di CurrentSteer.")]
    [SerializeField] private bool includeDriftInertia = false;

    private Quaternion initialLocalRotation;
    private float currentVisualAngle;

    private void Awake()
    {
        if (carController == null) carController = GetComponentInParent<BumperCarController>();
        if (steeringWheelMesh == null) steeringWheelMesh = transform;

        initialLocalRotation = steeringWheelMesh.localRotation;
    }

    private void OnEnable()
    {
        currentVisualAngle = 0f;
        if (steeringWheelMesh != null)
            steeringWheelMesh.localRotation = initialLocalRotation;
    }

    /// <summary>
    /// Aggiorna la rotazione visiva del volante con interpolazione esponenziale indipendente dal framerate.
    /// </summary>
    private void LateUpdate()
    {
        if (carController == null || steeringWheelMesh == null)
            return;

        float rawSteer = includeDriftInertia ? carController.EffectiveSteer : carController.CurrentSteer;
        float targetAngle = rawSteer * maxTurnAngle * (invertDirection ? -1f : 1f);

        float t = 1f - Mathf.Exp(-visualSmoothSpeed * Time.deltaTime);
        currentVisualAngle = Mathf.Lerp(currentVisualAngle, targetAngle, t);

        Vector3 axis = rotationAxis.sqrMagnitude > 0.0001f ? rotationAxis.normalized : Vector3.forward;
        steeringWheelMesh.localRotation = initialLocalRotation * Quaternion.AngleAxis(currentVisualAngle, axis);
    }
}