
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Policies;

[RequireComponent(typeof(BehaviorParameters))]
public class BumperCarAgent : Agent
{
    // Chiamato quando inizia un nuovo episodio.
    public override void OnEpisodeBegin()
    {
        // Il riposizionamento dell'automobile
        // verrà implementato successivamente.
    }

    // Raccoglie le informazioni che il bot
    // utilizza per prendere le proprie decisioni.
    public override void CollectObservations(VectorSensor sensor)
    {
        // Le osservazioni verranno definite
        // dopo aver analizzato il controller e l'arena.
    }

    // Riceve le decisioni della rete neurale
    // e le traduce in comandi per l'automobile.
    public override void OnActionReceived(ActionBuffers actions)
    {
        // Il collegamento al sistema di movimento
        // verrà implementato successivamente.
    }
}
