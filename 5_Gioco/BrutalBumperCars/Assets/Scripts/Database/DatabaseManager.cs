using System;
using System.IO;
using SQLite;
using UnityEngine;

public class DatabaseManager
{
    private static DatabaseManager instance;

    public static DatabaseManager Instance
    {
        get
        {
            if(instance == null)
                instance = new DatabaseManager();

            return instance;
        }
    }

    private readonly SQLiteConnection connection;
    private bool closed;

    public SQLiteConnection Connection => connection;

    public DatabaseManager()
    {
        string databasePath = Path.Combine(
            Application.persistentDataPath,
            "brutal_bumper_cars.db"
        );

        connection = new SQLiteConnection(databasePath);

        InitializeDatabase();

        Application.quitting += Close;
    }

    private void InitializeDatabase()
    {
        connection.CreateTable<StatisticsRecord>();
        connection.CreateTable<CarDistanceRecord>();
        connection.CreateTable<SettingsRecord>();
        connection.CreateTable<InputBindingRecord>();
        connection.CreateTable<CarRecord>();
    }

    public void Close()
    {
        if(closed)
            return;

        connection.Close();
        closed = true;

        Application.quitting -= Close;

    }
}