using System.IO;
using UnityEngine;

public class JsonDataService
{
    private readonly string filePath;

    public JsonDataService()
    {
        filePath = Path.Combine(
            Application.persistentDataPath,
            "statistics.json"
        );
    }

    public void Save(StatisticsData statistics)
    {
        string json = JsonUtility.ToJson(statistics, true);

        File.WriteAllText(filePath, json);
    }

    public StatisticsData Load()
    {
        if (!File.Exists(filePath))
        {
            return new StatisticsData();
        }

        string json = File.ReadAllText(filePath);

        return JsonUtility.FromJson<StatisticsData>(json);
    }
}