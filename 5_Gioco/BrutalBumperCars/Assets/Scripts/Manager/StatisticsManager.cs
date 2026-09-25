using UnityEngine;

public class StatisticsManager : MonoBehaviour
{
    public static StatisticsManager Instance { get; private set; }

    public StatisticsData Data { get; private set; }

    private JsonDataService dataService;

    private void Awake()
    {
        // Evita di avere più StatisticsManager
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // Mantiene il manager tra le scene
        DontDestroyOnLoad(gameObject);

        dataService = new JsonDataService();

        // Carica le statistiche all'avvio
        Data = dataService.LoadStatistics();
    }

    public void Save()
    {
        dataService.SaveStatistics(Data);
    }
}