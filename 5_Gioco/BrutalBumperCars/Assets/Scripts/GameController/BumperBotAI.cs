using UnityEngine;

[RequireComponent(typeof(BumperCarController))]
public class BumperBotAI : MonoBehaviour
{
    [Header("Inseguimento & Bersagli")]
    [Tooltip("Bersaglio attuale (assegnato in automatico se nullo).")]
    [SerializeField] private Transform target;

    [Tooltip("Sensibilità di sterzo verso il bersaglio.")]
    [SerializeField, Min(0.1f)] private float steeringSensitivity = 1.6f;

    [Tooltip("Distanza minima sotto cui il bot derapa se deve curvare forte.")]
    [SerializeField] private float driftDistanceThreshold = 7f;

    [Tooltip("Intervallo in secondi per ricalcolare l'avversario più vicino.")]
    [SerializeField] private float retargetInterval = 2.5f;

    [Header("Rilevamento Ostacoli (Raycast)")]
    [SerializeField] private float sensorLength = 6f;
    [SerializeField] private float sideSensorAngle = 30f;
    [SerializeField] private LayerMask obstacleLayer = ~0;

    [Header("Anti-Stuck (Disincaglio)")]
    [SerializeField] private float stuckSpeedThreshold = 1.2f;
    [SerializeField] private float stuckTimeThreshold = 1.2f;
    [SerializeField] private float reverseDuration = 1.0f;

    private BumperCarController car;
    private Rigidbody body;

    private float stuckTimer;
    private float reverseTimer;
    private int reverseSteerDirection = 1;
    private float nextRetargetTime;

    private void Awake()
    {
        car = GetComponent<BumperCarController>();
        body = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        AcquireNearestTarget();
    }

    private void Update()
    {
        // 1. Manovra di retromarcia se incagliato contro una parete o un'altra auto
        if (reverseTimer > 0f)
        {
            reverseTimer -= Time.deltaTime;
            car.SetInputs(-1f, reverseSteerDirection, false);
            return;
        }

        // 2. Controllo periodico del bersaglio più vicino
        if (Time.time >= nextRetargetTime || target == null || !target.gameObject.activeInHierarchy)
        {
            AcquireNearestTarget();
            nextRetargetTime = Time.time + retargetInterval;
        }

        if (target == null)
        {
            car.SetInputs(0f, 0f, false);
            return;
        }

        CheckStuck();

        // 3. Calcolo angolo e distanza verso il bersaglio scelto
        Vector3 localTarget = transform.InverseTransformPoint(target.position);
        float targetAngle = Mathf.Atan2(localTarget.x, localTarget.z) * Mathf.Rad2Deg;
        float distance = localTarget.magnitude;

        float steer = Mathf.Clamp((targetAngle / 45f) * steeringSensitivity, -1f, 1f);

        // Se è quasi allineato spinge al 100%, altrimenti curva a velocità moderata
        float throttle = Mathf.Cos(targetAngle * Mathf.Deg2Rad) > 0.1f ? 1f : 0.45f;

        // Drift su curve strette a corta distanza
        bool drift = distance < driftDistanceThreshold && Mathf.Abs(targetAngle) > 40f;

        // 4. Correzione sterzo con i sensori di ostacolo
        steer += EvaluateObstacleAvoidance();
        steer = Mathf.Clamp(steer, -1f, 1f);

        car.SetInputs(throttle, steer, drift);
    }

    private void AcquireNearestTarget()
    {
        // Trova tutte le auto presenti nell'arena tramite il controller comune
        BumperCarController[] allCars = FindObjectsByType<BumperCarController>(FindObjectsSortMode.None);

        float closestDistanceSqr = float.MaxValue;
        Transform closest = null;
        Vector3 currentPos = transform.position;

        foreach (var otherCar in allCars)
        {
            if (otherCar == null || otherCar.gameObject == gameObject)
                continue;

            float distSqr = (otherCar.transform.position - currentPos).sqrMagnitude;
            if (distSqr < closestDistanceSqr)
            {
                closestDistanceSqr = distSqr;
                closest = otherCar.transform;
            }
        }

        target = closest;
    }

    private float EvaluateObstacleAvoidance()
    {
        float avoidMultiplier = 0f;
        Vector3 origin = transform.position + Vector3.up * 0.4f;

        // Raggio frontale
        if (Physics.Raycast(origin, transform.forward, out RaycastHit hitCenter, sensorLength, obstacleLayer))
        {
            if (hitCenter.transform != target)
                avoidMultiplier += hitCenter.normal.x > 0 ? 0.75f : -0.75f;
        }

        // Raggio angolato destro
        Vector3 rightDir = Quaternion.Euler(0, sideSensorAngle, 0) * transform.forward;
        if (Physics.Raycast(origin, rightDir, out RaycastHit hitRight, sensorLength * 0.75f, obstacleLayer))
        {
            if (hitRight.transform != target)
                avoidMultiplier -= 0.6f;
        }

        // Raggio angolato sinistro
        Vector3 leftDir = Quaternion.Euler(0, -sideSensorAngle, 0) * transform.forward;
        if (Physics.Raycast(origin, leftDir, out RaycastHit hitLeft, sensorLength * 0.75f, obstacleLayer))
        {
            if (hitLeft.transform != target)
                avoidMultiplier += 0.6f;
        }

        return avoidMultiplier;
    }

    private void CheckStuck()
    {
        if (body != null && body.linearVelocity.magnitude < stuckSpeedThreshold)
        {
            stuckTimer += Time.deltaTime;
            if (stuckTimer >= stuckTimeThreshold)
            {
                stuckTimer = 0f;
                reverseTimer = reverseDuration;
                reverseSteerDirection = Random.value > 0.5f ? 1 : -1;
            }
        }
        else
        {
            stuckTimer = 0f;
        }
    }

    public void SetTarget(Transform newTarget) => target = newTarget;
}