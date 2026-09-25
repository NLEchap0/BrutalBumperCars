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

    public void SaveStatistics(StatisticsData statistics)
    {
        string json = JsonUtility.ToJson(statistics, true);

        File.WriteAllText(filePath, json);
    }

    public StatisticsData LoadStatistics()
    {
        if (!File.Exists(filePath))
        {
            return new StatisticsData();
        }

        string json = File.ReadAllText(filePath);

        return JsonUtility.FromJson<StatisticsData>(json);
    }

    public void SaveSettings(SettingsData settings)
    {
        string json = JsonUtility.ToJson(settings, true);

        File.WriteAllText(filePath, json);
    }

    public SettingsData LoadSettings()
    {
        if (!File.Exists(filePath))
        {
            return new SettingsData();
        }

        string json = File.ReadAllText(filePath);

        return JsonUtility.FromJson<SettingsData>(json);
    }
}