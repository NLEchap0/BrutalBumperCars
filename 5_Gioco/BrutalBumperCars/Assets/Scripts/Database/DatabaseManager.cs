using System.IO;
using SQLite;
using UnityEngine;

public class DatabaseManager
{
    private readonly string databasePath;
    private readonly SQLiteConnection connection;

    public SQLiteConnection Connection => connection;

    public DatabaseManager()
    {
        databasePath = Path.Combine(
            Application.persistentDataPath,
            "brutal_bumper_cars.db"
        );

        connection = new SQLiteConnection(databasePath);

        InitializeDatabase();
    }

    private void InitializeDatabase()
    {
        connection.CreateTable<StatisticsRecord>();
        connection.CreateTable<CarDistanceRecord>();
        connection.CreateTable<SettingsRecord>();
    }
}