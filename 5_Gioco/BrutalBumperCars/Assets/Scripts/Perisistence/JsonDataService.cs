using System.IO;
using UnityEngine;

public class JsonDataService
{
    private readonly string filePath;

    public JsonDataService()
    {
        filePath = Application.persistentDataPath;
    }

    public string CombinePath(string name)
    {
        return Path.Combine(filePath, name);
    }

    public void SaveStatistics(StatisticsData statistics)
    {
        string json = JsonUtility.ToJson(statistics, true);

        File.WriteAllText(CombinePath("statistics.json"), json);
    }

    public StatisticsData LoadStatistics()
    {
        if (!File.Exists(CombinePath("statistics.json")))
        {
            return new StatisticsData();
        }

        string json = File.ReadAllText(CombinePath("statistics.json"));

        return JsonUtility.FromJson<StatisticsData>(json);
    }

    public void SaveSettings(SettingsData settings)
    {
        string json = JsonUtility.ToJson(settings, true);

        File.WriteAllText(CombinePath("settings.json"), json);
    }

    public SettingsData LoadSettings()
    {
        if (!File.Exists(CombinePath("settings.json")))
        {
            return new SettingsData();
        }

        string json = File.ReadAllText(CombinePath("settings.json"));

        return JsonUtility.FromJson<SettingsData>(json);
    }
}