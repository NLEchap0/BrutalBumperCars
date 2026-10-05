using System.Collections.Generic;
using System.Linq;

public class StatisticsRepository
{
    private readonly DatabaseManager database;

    public StatisticsRepository(DatabaseManager database)
    {
        this.database = database;
    }

    public void SaveStatistics(StatisticsRecord statistics)
    {
        database.Connection.InsertOrReplace(statistics);
    }

    public StatisticsRecord LoadStatistics()
    {
        return database.Connection
            .Table<StatisticsRecord>()
            .FirstOrDefault();
    }

    public List<CarDistanceRecord> LoadCarDistances(int statisticsId)
    {
        return database.Connection
            .Table<CarDistanceRecord>()
            .Where(car => car.StatisticsId == statisticsId)
            .ToList();
    }

    public void SaveCarDistances(
        int statisticsId,
        List<CarDistanceRecord> carDistances)
    {
        database.Connection.Execute(
            "DELETE FROM CarDistanceRecord WHERE StatisticsId = ?",
            statisticsId
        );

        foreach (CarDistanceRecord carDistance in carDistances)
        {
            database.Connection.Insert(carDistance);
        }
    }
}