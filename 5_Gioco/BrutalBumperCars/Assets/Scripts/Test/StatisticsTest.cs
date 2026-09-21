using System.Collections.Generic;
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
            deaths = 18,
            damageDealt = 200,
            damageTaken = 100,
            damageDefended = 60,
            carDistances = new List<CarDistanceData> {
                new CarDistanceData { carName = "Thunder", distance = 152.4f },
                new CarDistanceData { carName = "Rocket", distance = 87.2f }, 
                new CarDistanceData { carName = "Buggy", distance = 203.8f }, 
                new CarDistanceData { carName = "Classic", distance = 45.6f },
                new CarDistanceData { carName = "Monster", distance = 124.7f } 
            }
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
        Debug.Log("Danni inflitti: " + statistics.damageDealt);
        Debug.Log("Danni subiti: " + statistics.damageTaken);
        Debug.Log("Danni difesi: " + statistics.damageDefended);

        List<CarDistanceData> topCars = statistics.GetTopCars(3); 
        for (int i = 0; i < topCars.Count; i++) { 
            Debug.Log((i + 1) + ". " + topCars[i].carName + " - " + topCars[i].distance + " m"); 
        }
    }
}