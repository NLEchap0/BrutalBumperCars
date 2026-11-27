
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Policies;

[RequireComponent(typeof(BumperCarController))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(BehaviorParameters))]
[RequireComponent(typeof(DecisionRequester))]
public class BumperCarAgent : Agent
{
    [Header("Normalizzazione osservazioni")]
    [SerializeField, Min(0.1f)]
    private float referenceSpeed = 50f;

    [SerializeField, Min(0.1f)]
    private float referenceAngularSpeed = 3f;

    private BumperCarController car;
    private Rigidbody body;

    private Vector3 initialPosition;
    private Quaternion initialRotation;

    private float lastThrottle;
    private float lastSteer;


    [Header("Ricompense - Movimento")]
    [SerializeField, Min(0.1f)]
    private float targetForwardSpeed = 15f;

    [SerializeField, Min(0f)]
    private float movementReward = 0.0015f;

    [SerializeField, Min(0f)]
    private float stationarySpeedThreshold = 1f;

    [SerializeField, Min(0f)]
    private float stationaryPenalty = 0.001f;

    [Header("Penalità - Collisioni")]
    [SerializeField, Min(0f)]
    private float obstacleCollisionPenalty = 0.15f;

    [SerializeField, Min(0f)]
    private float carCollisionPenalty = 0.05f;

    [Header("Stabilità della guida")]
    [SerializeField, Min(0f)]
    private float throttleChangePenalty = 0.0002f;

    [SerializeField, Min(0f)]
    private float directionSwitchPenalty = 0.0007f;

    [SerializeField, Range(0f, 1f)]
    private float directionSwitchThreshold = 0.25f;

    [Header("Controllo avanzamento")]
    [SerializeField, Min(0.1f)]
    private float progressCheckInterval = 1f;

    [SerializeField, Min(0f)]
    private float minimumProgressDistance = 0.6f;

    [SerializeField, Min(0f)]
    private float lowProgressPenalty = 0.01f;

    private Vector3 lastProgressPosition;
    private float progressTimer;

    [Header("Debug guida")]
    [SerializeField] private bool debugDriving = false;

    private float nextDebugTime;

    public override void Initialize()
    {
        car = GetComponent<BumperCarController>();
        body = GetComponent<Rigidbody>();

        // Memorizza la posizione iniziale del bot.
        initialPosition = transform.position;
        initialRotation = transform.rotation;

        // La durata degli episodi viene gestita
        // centralmente da ArenaTrainingManager.
        MaxStep = 0;
    }

    // Chiamato quando inizia un nuovo episodio.
    public override void OnEpisodeBegin()
    {
        lastThrottle = 0f;
        lastSteer = 0f;

        car.ResetForEpisode(initialPosition, initialRotation);

        // Ripristina il controllo dell'avanzamento.
        lastProgressPosition = transform.position;
        progressTimer = 0f;
    }

    // Raccoglie le informazioni che il bot
    // utilizza per prendere le proprie decisioni.
    public override void CollectObservations(VectorSensor sensor)
    {
        // Velocità rispetto alla direzione dell'automobile.
        Vector3 localVelocity =
            transform.InverseTransformDirection(body.linearVelocity);

        // Velocità angolare rispetto all'automobile.
        Vector3 localAngularVelocity =
            transform.InverseTransformDirection(body.angularVelocity);

        // 1. Velocità laterale.
        sensor.AddObservation(
            Mathf.Clamp(localVelocity.x / referenceSpeed, -1f, 1f));

        // 2. Velocità avanti/indietro.
        sensor.AddObservation(
            Mathf.Clamp(localVelocity.z / referenceSpeed, -1f, 1f));

        // 3. Velocità verticale.
        sensor.AddObservation(
            Mathf.Clamp(localVelocity.y / referenceSpeed, -1f, 1f));

        // 4. Velocità di rotazione orizzontale.
        sensor.AddObservation(
            Mathf.Clamp(localAngularVelocity.y / referenceAngularSpeed, -1f, 1f));

        // 5. Ultima accelerazione richiesta.
        sensor.AddObservation(lastThrottle);

        // 6. Ultima sterzata richiesta.
        sensor.AddObservation(lastSteer);
    }

    // Riceve le decisioni della rete neurale
    // e le traduce in comandi per l'automobile.
    public override void OnActionReceived(ActionBuffers actions)
    {
        // Legge le due azioni continue della rete neurale.
        float throttle = Mathf.Clamp(
            actions.ContinuousActions[0], -1f, 1f);

        float steer = Mathf.Clamp(
            actions.ContinuousActions[1], -1f, 1f);

        // Valuta i comandi prima di aggiornare
        // i valori della decisione precedente.
        EvaluateDrivingStability(throttle);

        // Valuta movimento e avanzamento.
        EvaluateMovementReward();
        EvaluateProgress();

        // Memorizza gli input per le osservazioni successive.
        lastThrottle = throttle;
        lastSteer = steer;

        // Applica i comandi al controller dell'automobile.
        // Il drift rimane disabilitato nel primo addestramento.
        car.SetInputs(throttle, steer, false);

        if (debugDriving && Time.time >= nextDebugTime)
        {
            nextDebugTime = Time.time + 0.25f;

            float forwardSpeed = Vector3.Dot(
                body.linearVelocity,
                transform.forward
            );

            Debug.Log(
                $"[{name}] Throttle: {throttle:F2} | " +
                $"Steer: {steer:F2} | " +
                $"Velocità: {forwardSpeed:F2} m/s"
            );
        }
    }

    private void EvaluateMovementReward()
    {
        // Calcola la velocità in avanti del veicolo.
        float forwardSpeed = Vector3.Dot(
            body.linearVelocity,
            transform.forward
        );

        // Normalizza la velocità tra 0 e 1.
        float speedRatio = Mathf.Clamp01(
            forwardSpeed / targetForwardSpeed
        );

        // Premia il movimento in avanti.
        AddReward(movementReward * speedRatio);

        // Penalizza il veicolo se rimane quasi fermo.
        if (body.linearVelocity.magnitude < stationarySpeedThreshold)
        {
            AddReward(-stationaryPenalty);
        }
    }

    private void EvaluateDrivingStability(float throttle)
    {
        // Quanto è cambiato il comando rispetto
        // alla decisione precedente?
        float change = Mathf.Abs(throttle - lastThrottle);

        // Piccola penalità proporzionale al cambiamento.
        AddReward(-throttleChangePenalty * change);

        // Penalizza un'inversione marcata avanti/retro.
        bool reversedDirection =
            Mathf.Abs(throttle) > directionSwitchThreshold &&
            Mathf.Abs(lastThrottle) > directionSwitchThreshold &&
            Mathf.Sign(throttle) != Mathf.Sign(lastThrottle);

        if (reversedDirection)
        {
            AddReward(-directionSwitchPenalty);
        }
    }

    private void EvaluateProgress()
    {
        progressTimer += Time.fixedDeltaTime;

        if (progressTimer < progressCheckInterval)
            return;

        // Calcola lo spostamento orizzontale
        // rispetto all'ultimo controllo.
        Vector3 displacement =
            transform.position - lastProgressPosition;

        displacement.y = 0f;

        float distance = displacement.magnitude;

        // Penalizza le situazioni in cui il bot
        // rimane praticamente nella stessa zona.
        if (distance < minimumProgressDistance)
        {
            AddReward(-lowProgressPenalty);
        }

        lastProgressPosition = transform.position;
        progressTimer = 0f;
    }

    private void OnCollisionEnter(Collision collision)
    {
        Collider other = collision.collider;

        // Collisione con pareti o ostacoli.
        if (other.CompareTag("Obstacle"))
        {
            AddReward(-obstacleCollisionPenalty);
        }
        // Collisione con altre auto.
        else if (other.CompareTag("Car"))
        {
            AddReward(-carCollisionPenalty);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Riconosce anche i sensori delle rampe.
        if (other.CompareTag("Obstacle"))
        {
            AddReward(-obstacleCollisionPenalty);
        }
    }


    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActions = actionsOut.ContinuousActions;

        // Valori temporanei per verificare il movimento.
        continuousActions[0] = 0.5f; // Accelerazione
        continuousActions[1] = 0f;   // Sterzata
    }

}
