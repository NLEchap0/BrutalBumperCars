
using UnityEngine;
using Unity.MLAgents;

public class ArenaTrainingManager : MonoBehaviour
{
    [Header("Agenti presenti nell'arena")]
    [SerializeField] private BumperCarAgent[] agents;

    [Header("Giocatore (facoltativo)")]
    [SerializeField] private BumperCarController playerCar;

    [Header("Configurazione episodi")]
    [SerializeField, Min(1)]
    private int episodePhysicsSteps = 3000;

    [Header("Debug")]
    [SerializeField] private bool testResetsInEditor = false;
    [SerializeField] private bool logResets = true;

    private int currentStep;

    private Vector3 playerStartPosition;
    private Quaternion playerStartRotation;
    private bool initialized;

    private void LateUpdate()
    {
        if (initialized)
            return;

        // Acquisisce lo spawn del player dopo gli Start().
        // Il PlayerSpawnManager dovrebbe averlo già spostato.
        if (playerCar != null)
        {
            playerStartPosition = playerCar.transform.position;
            playerStartRotation = playerCar.transform.rotation;
        }

        initialized = true;
    }

    private void FixedUpdate()
    {
        if (!initialized || !ShouldRunEpisodes())
            return;

        currentStep++;

        if (currentStep < episodePhysicsSteps)
            return;

        currentStep = 0;
        ResetEpisode();
    }

    private bool ShouldRunEpisodes()
    {
        // Addestramento con Python collegato.
        if (Academy.Instance.IsCommunicatorOn)
            return true;

#if UNITY_EDITOR
        // Permette di verificare i reset senza Python.
        return testResetsInEditor;
#else
        return false;
#endif
    }

    private void ResetEpisode()
    {
        // Ripristina l'ottava auto, se presente.
        if (playerCar != null &&
            playerCar.gameObject.activeInHierarchy)
        {
            playerCar.ResetForEpisode(
                playerStartPosition,
                playerStartRotation
            );
        }

        int activeAgents = 0;

        // Interrompe gli episodi degli agenti attivi.
        foreach (BumperCarAgent agent in agents)
        {
            if (agent == null || !agent.isActiveAndEnabled)
                continue;

            agent.EpisodeInterrupted();
            activeAgents++;
        }

        if (logResets)
        {
            Debug.Log(
                $"Nuovo episodio. Agenti ripristinati: {activeAgents}"
            );
        }
    }
}
