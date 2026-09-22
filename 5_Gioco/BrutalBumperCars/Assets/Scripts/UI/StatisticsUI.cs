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

    [Header("Top 3 Auto")]
    [SerializeField] private TMP_Text top1CarNameText;
    [SerializeField] private TMP_Text top1DistanceText;
    [SerializeField] private TMP_Text top2CarNameText;
    [SerializeField] private TMP_Text top2DistanceText;
    [SerializeField] private TMP_Text top3CarNameText;
    [SerializeField] private TMP_Text top3DistanceText;

    private void Start()
    {
        StatisticsData statistics = StatisticsManager.Instance.Data;

        DisplayStatistics(statistics);
        DisplayTopCars(statistics);
    }

    private void DisplayStatistics(StatisticsData statistics)
    {
        victoriesText.text = FormatNumber(statistics.victories);
        defeatsText.text = FormatNumber(statistics.defeats);
        vsRatioText.text = statistics.VSRatio.ToString("F1");

        killsText.text = FormatNumber(statistics.kills);
        deathsText.text = FormatNumber(statistics.deaths);
        umRatioText.text = statistics.KDRatio.ToString("F1");

        damageDealtText.text = FormatNumber(statistics.damageDealt);
        damageTakenText.text = FormatNumber(statistics.damageTaken);
        damageDefendedText.text = FormatNumber(statistics.damageDefended);
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
            distanceText.text = FormatNumber(topCars[index].distance);
        } else { 
            carNameText.text = "-"; distanceText.text = "-"; 
        } 
    }

    private string FormatNumber(long value)
    {
        if (value < 1000)
            return value.ToString("0");

        if (value < 1000000)
            return (value / 1000.0).ToString("0.#") + "K";

        if (value < 1000000000)
            return (value / 1000000.0).ToString("0.#") + "M";

        return (value / 1000000000.0).ToString("0.#") + "B";
    }
}