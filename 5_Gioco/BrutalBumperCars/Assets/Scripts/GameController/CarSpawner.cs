using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gestisce lo spawn casuale di Player e Bot su 8 spawn point.
/// Rimuove automaticamente l'auto di test/originale lasciata nella scena all'avvio.
/// I bot nascono attivi di default (enabled = true).
/// </summary>
public class CarSpawner : MonoBehaviour
{
    [Header("Configurazione Prefab")]
    [Tooltip("Il prefab dell'auto (trascina l'asset dal Project o l'oggetto originale della scena).")]
    [SerializeField] private GameObject carPrefab;

    [Tooltip("Opzionale: se hai un'auto già posizionata nella scena da cancellare esplicitamente all'avvio.")]
    [SerializeField] private GameObject sceneCarToDestroy;

    [Header("Spawn Points")]
    [Tooltip("Gli 8 Transform posizionati nell'arena.")]
    [SerializeField] private Transform[] spawnPoints;

    [Header("Regole Spawn & Partita")]
    [Tooltip("Se true, istanzia automaticamente Player e Bot all'avvio della scena.")]
    [SerializeField] private bool autoSpawnOnStart = true;

    [Tooltip("Numero totale di bot da generare (7 se vuoi riempire gli 8 slot totali).")]
    [SerializeField, Range(0, 15)] private int botCount = 7;

    [Tooltip("Stato di attivazione dell'AI dei bot allo spawn (attivo di default).")]
    [SerializeField] private bool activateBotOnSpawn = true;

    private GameObject playerInstance;
    private readonly List<GameObject> botInstances = new List<GameObject>();

    public GameObject PlayerInstance => playerInstance;
    public IReadOnlyList<GameObject> BotInstances => botInstances;

    private void Start()
    {
        CleanUpSceneOriginalCar();

        if (autoSpawnOnStart)
        {
            SpawnMatch();
        }
    }

    /// <summary>
    /// Rileva ed elimina la macchina originale lasciata nella scena per evitare doppioni.
    /// </summary>
    private void CleanUpSceneOriginalCar()
    {
        if (sceneCarToDestroy != null)
        {
            Destroy(sceneCarToDestroy);
            return;
        }

        if (carPrefab != null && carPrefab.scene.IsValid() && carPrefab.scene.isLoaded)
        {
            GameObject sceneOriginal = carPrefab;
            SpawnMatchInternal();
            Destroy(sceneOriginal);
            return;
        }

        BumperCarController existingCar = FindFirstObjectByType<BumperCarController>();
        if (existingCar != null && existingCar.gameObject != playerInstance && !botInstances.Contains(existingCar.gameObject))
        {
            Destroy(existingCar.gameObject);
        }
    }

    /// <summary>
    /// Esegue lo spawn casuale: sceglie uno slot casuale per il Player e posiziona i bot negli altri.
    /// </summary>
    public void SpawnMatch()
    {
        if (playerInstance != null)
            return;

        SpawnMatchInternal();
    }

    private void SpawnMatchInternal()
    {
        if (carPrefab == null)
        {
            Debug.LogError("[CarSpawner] carPrefab non assegnato nell'Inspector!");
            return;
        }

        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError("[CarSpawner] Nessun punto di spawn assegnato!");
            return;
        }

        List<int> availableIndices = new List<int>();
        for (int i = 0; i < spawnPoints.Length; i++)
            availableIndices.Add(i);

        Shuffle(availableIndices);

        // Slot 0 assegnato al Player
        int playerSpawnIndex = availableIndices[0];
        Transform playerPoint = spawnPoints[playerSpawnIndex];
        playerInstance = Instantiate(carPrefab, playerPoint.position, playerPoint.rotation);
        playerInstance.name = "Player_BumperCar";
        ConfigureAsPlayer(playerInstance);

        // Gli altri slot assegnati ai Bot
        int botsToSpawn = Mathf.Min(botCount, availableIndices.Count - 1);
        for (int i = 1; i <= botsToSpawn; i++)
        {
            int botSpawnIndex = availableIndices[i];
            Transform botPoint = spawnPoints[botSpawnIndex];

            GameObject botCar = Instantiate(carPrefab, botPoint.position, botPoint.rotation);
            botCar.name = $"Bot_BumperCar_{i}";

            ConfigureAsBot(botCar);
            botInstances.Add(botCar);
        }
    }

    private void ConfigureAsPlayer(GameObject car)
    {
        // Il Player NON deve mai avere l'AI attiva
        BumperBotAI botAI = car.GetComponent<BumperBotAI>();
        if (botAI != null)
            botAI.enabled = false;

        PlayerCarInput playerInput = car.GetComponent<PlayerCarInput>();
        if (playerInput != null)
            playerInput.enabled = true;
    }

    private void ConfigureAsBot(GameObject car)
    {
        // Rimuove componenti specifici del Player
        PlayerCarInput playerInput = car.GetComponent<PlayerCarInput>();
        if (playerInput != null)
            Destroy(playerInput);

        CameraLookController[] lookControllers = car.GetComponentsInChildren<CameraLookController>(true);
        foreach (var look in lookControllers)
            Destroy(look);

        Camera[] cameras = car.GetComponentsInChildren<Camera>(true);
        foreach (var cam in cameras)
        {
            if (cam.gameObject != car)
                Destroy(cam.gameObject);
            else
                Destroy(cam);
        }

        AudioListener[] listeners = car.GetComponentsInChildren<AudioListener>(true);
        foreach (var listener in listeners)
            Destroy(listener);

        // Configura e attiva l'AI di default per i non-player
        BumperBotAI botAI = car.GetComponent<BumperBotAI>();
        if (botAI == null)
            botAI = car.AddComponent<BumperBotAI>();

        botAI.enabled = true; // Sempre attivo per i bot
    }

    public void EnableAllBots()
    {
        foreach (GameObject bot in botInstances)
        {
            if (bot == null) continue;

            BumperBotAI botAI = bot.GetComponent<BumperBotAI>();
            if (botAI != null)
                botAI.enabled = true;
        }
    }

    private static void Shuffle<T>(IList<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            T temp = list[i];
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }
}