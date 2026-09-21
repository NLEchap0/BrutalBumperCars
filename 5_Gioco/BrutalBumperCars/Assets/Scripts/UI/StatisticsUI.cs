using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class StatisticsUI : MonoBehaviour
{
    [Header("Statistiche")]
    [SerializeField] private TMP_Text victoriesText;
    [SerializeField] private TMP_Text defeatsText;
    [SerializeField] private TMP_Text vsRatioText;

    [SerializeField] private TMP_Text killsText;
    [SerializeField] private TMP_Text deathsText;
    [SerializeField] private TMP_Text umRatioText;

    [SerializeField] private TMP_Text damageDealtText;
    [SerializeField] private TMP_Text damageTakenText;
    [SerializeField] private TMP_Text damageDefendedText;

    [Header("Top 3 Auto")][SerializeField] private TMP_Text top1CarNameText;
    [SerializeField] private TMP_Text top1DistanceText;
    [SerializeField] private TMP_Text top2CarNameText;
    [SerializeField] private TMP_Text top2DistanceText;
    [SerializeField] private TMP_Text top3CarNameText;
    [SerializeField] private TMP_Text top3DistanceText;

    private JsonDataService dataService;

    private void Start()
    {
        dataService = new JsonDataService();

        LoadStatistics();
    }

    private void LoadStatistics()
    {
        StatisticsData statistics = dataService.Load();

        DisplayStatistics(statistics);
        DisplayTopCars(statistics);
    }

    private void DisplayStatistics(StatisticsData statistics)
    {
        victoriesText.text = statistics.victories.ToString();
        defeatsText.text = statistics.defeats.ToString();
        vsRatioText.text = statistics.VSRatio.ToString("F2");

        killsText.text = statistics.kills.ToString();
        deathsText.text = statistics.deaths.ToString();
        umRatioText.text = statistics.KDRatio.ToString("F2");

        damageDealtText.text = statistics.damageDealt.ToString("F1");
        damageTakenText.text = statistics.damageTaken.ToString("F1");
        damageDefendedText.text = statistics.damageDefended.ToString("F1");
    }

    private void DisplayTopCars(StatisticsData statistics)
    {
        List<CarDistanceData> topCars = statistics.GetTopCars(3); 
        DisplayCar(topCars, 0, top1CarNameText, top1DistanceText); 
        DisplayCar(topCars, 1, top2CarNameText, top2DistanceText); 
        DisplayCar(topCars, 2, top3CarNameText, top3DistanceText);
    }
    private void DisplayCar(List<CarDistanceData> topCars, int index, TMP_Text carNameText, TMP_Text distanceText) 
    { 
        if (index < topCars.Count) { 
            carNameText.text = topCars[index].carName; 
            distanceText.text = topCars[index].distance.ToString("F1") + " m"; 
        } else { 
            carNameText.text = "-"; distanceText.text = "-"; 
        } 
    }
}