using System.Collections.Generic;
using UnityEngine;

public class StatisticsManager : MonoBehaviour
{
    public static StatisticsManager Instance { get; private set; }

    public StatisticsData Data { get; private set; }

    private DatabaseManager database;
    private StatisticsRepository repository;

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

        database = new DatabaseManager();
        repository = new StatisticsRepository(database);

        LoadStatistics();
    }

    private void LoadStatistics()
    {
        StatisticsRecord record = repository.LoadStatistics();

        if (record == null)
        {
            Data = CreateDefaultStatistics();

            Save();
            return;
        }

        Data = ConvertToData(record);

        LoadCarDistances();
    }

    public void Save()
    {
        StatisticsRecord record = ConvertToRecord(Data);

        repository.SaveStatistics(record);

        SaveCarDistances();
    }

    private StatisticsData CreateDefaultStatistics()
    {
        return new StatisticsData
        {
            carDistances = new List<CarDistanceData>()
        };
    }

    private StatisticsRecord ConvertToRecord(
        StatisticsData data)
    {
        return new StatisticsRecord
        {
            Id = 1,

            Victories = data.victories,
            Defeats = data.defeats,

            Kills = data.kills,
            Deaths = data.deaths,

            DamageDealt = data.damageDealt,
            DamageTaken = data.damageTaken,
            DamageDefended = data.damageDefended
        };
    }

    private StatisticsData ConvertToData(
        StatisticsRecord record)
    {
        return new StatisticsData
        {
            victories = record.Victories,
            defeats = record.Defeats,

            kills = record.Kills,
            deaths = record.Deaths,

            damageDealt = record.DamageDealt,
            damageTaken = record.DamageTaken,
            damageDefended = record.DamageDefended,

            carDistances = new List<CarDistanceData>()
        };
    }

    private void LoadCarDistances()
    {
        List<CarDistanceRecord> records =
            repository.LoadCarDistances(1);

        foreach (CarDistanceRecord record in records)
        {
            Data.carDistances.Add(new CarDistanceData
            {
                carName = record.CarName,
                distance = record.Distance
            });
        }
    }

    private void SaveCarDistances()
    {
        List<CarDistanceRecord> records =
            new List<CarDistanceRecord>();

        foreach (CarDistanceData car in Data.carDistances)
        {
            records.Add(new CarDistanceRecord
            {
                StatisticsId = 1,
                CarName = car.carName,
                Distance = car.distance
            });
        }

        repository.SaveCarDistances(1, records);
    }
}