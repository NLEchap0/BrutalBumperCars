using UnityEngine;

/// <summary>
/// Controller AI di guida per i bot con tracciamento bersaglio, evitamento ostacoli con Raycast e gestione stallo/retromarcia.
/// </summary>
[RequireComponent(typeof(BumperCarController))]
public class BumperBotAI : MonoBehaviour
{
    [Header("Target & Inseguimento")]
    [Tooltip("Bersaglio corrente da inseguire / speronare.")]
    [SerializeField] private Transform target;

    [Tooltip("Sensibilità di sterzo verso il bersaglio.")]
    [SerializeField, Min(0.1f)] private float steeringSensitivity = 1.5f;

    [Tooltip("Distanza minima sotto cui il bot prova a derapare per curvare stretto.")]
    [SerializeField] private float driftDistanceThreshold = 8f;

    [Header("Rilevamento Ostacoli (Raycast)")]
    [SerializeField] private float sensorLength = 7f;
    [SerializeField] private float sideSensorAngle = 30f;
    [SerializeField] private LayerMask obstacleLayer = ~0;

    [Header("Rilevamento Blocco (Anti-Stuck)")]
    [SerializeField] private float stuckSpeedThreshold = 1f;
    [SerializeField] private float stuckTimeThreshold = 1.5f;
    [SerializeField] private float reverseDuration = 1.2f;

    private BumperCarController car;
    private Rigidbody body;

    private float stuckTimer;
    private float reverseTimer;
    private int reverseSteerDirection = 1;

    private void Awake()
    {
        car = GetComponent<BumperCarController>();
        body = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        // Se siamo in manovra di disincaglio
        if (reverseTimer > 0f)
        {
            reverseTimer -= Time.deltaTime;
            car.SetInputs(-1f, reverseSteerDirection, false);
            return;
        }

        // Se non c'è un target esplicito cerca l'auto del player
        if (target == null)
        {
            PlayerCarInput player = FindFirstObjectByType<PlayerCarInput>();
            if (player != null) target = player.transform;
            else
            {
                car.SetInputs(0f, 0f, false);
                return;
            }
        }

        CheckStuck();

        // 1. Calcolo direzione verso bersaglio
        Vector3 localTarget = transform.InverseTransformPoint(target.position);
        float targetAngle = Mathf.Atan2(localTarget.x, localTarget.z) * Mathf.Rad2Deg;
        float distance = localTarget.magnitude;

        float steer = Mathf.Clamp(targetAngle / 45f * steeringSensitivity, -1f, 1f);
        float throttle = Mathf.Cos(targetAngle * Mathf.Deg2Rad) > 0.05f ? 1f : 0.4f;
        bool drift = distance < driftDistanceThreshold && Mathf.Abs(targetAngle) > 35f;

        // 2. Modifica sterzo per evitamento ostacoli (3 sensori frontali)
        steer += EvaluateObstacleAvoidance();
        steer = Mathf.Clamp(steer, -1f, 1f);

        car.SetInputs(throttle, steer, drift);
    }

    private float EvaluateObstacleAvoidance()
    {
        float avoidMultiplier = 0f;
        Vector3 origin = transform.position + Vector3.up * 0.5f;

        // Sensore centrale
        if (Physics.Raycast(origin, transform.forward, out RaycastHit hitCenter, sensorLength, obstacleLayer))
        {
            if (hitCenter.transform != target)
                avoidMultiplier += hitCenter.normal.x > 0 ? 0.7f : -0.7f;
        }

        // Sensore destro
        Vector3 rightDir = Quaternion.Euler(0, sideSensorAngle, 0) * transform.forward;
        if (Physics.Raycast(origin, rightDir, out RaycastHit hitRight, sensorLength * 0.75f, obstacleLayer))
        {
            if (hitRight.transform != target)
                avoidMultiplier -= 0.6f;
        }

        // Sensore sinistro
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
