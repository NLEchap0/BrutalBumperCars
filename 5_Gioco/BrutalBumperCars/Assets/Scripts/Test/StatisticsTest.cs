using UnityEngine;

public class StatisticsTest : MonoBehaviour
{
    private JsonDataService dataService;

    private void Start()
    {
        dataService = new JsonDataService();

        SaveTestData();
        LoadTestData();
    }

    private void SaveTestData()
    {
        StatisticsData statistics = new StatisticsData
        {
            victories = 15,
            defeats = 7,
            kills = 32,
            deaths = 18
        };

        dataService.Save(statistics);

        Debug.Log("Statistiche salvate.");
    }

    private void LoadTestData()
    {
        StatisticsData statistics = dataService.Load();

        Debug.Log("Vittorie: " + statistics.victories);
        Debug.Log("Sconfitte: " + statistics.defeats);
        Debug.Log("V/S: " + statistics.VSRatio);
        Debug.Log("Uccisioni: " + statistics.kills);
        Debug.Log("Morti: " + statistics.deaths);
        Debug.Log("U/M: " + statistics.KDRatio);
    }
}