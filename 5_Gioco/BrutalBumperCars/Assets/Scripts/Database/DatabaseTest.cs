using System.IO;
using SQLite;
using UnityEngine;

public class TestRecord
{
    public int Id { get; set; }

    public string Name { get; set; }
}

public class DatabaseTest : MonoBehaviour
{
    private void Start()
    {
        string databasePath = Path.Combine(
            Application.persistentDataPath,
            "brutal_bumper_cars.db"
        );

        Debug.Log("Database path: " + databasePath);

        using (SQLiteConnection connection = new SQLiteConnection(databasePath))
        {
            Debug.Log("Connessione SQLite riuscita!");

            connection.Execute(@"
                CREATE TABLE IF NOT EXISTS Test (
                    Id INTEGER PRIMARY KEY,
                    Name TEXT
                );
            ");

            Debug.Log("Tabella Test creata!");

            connection.Execute(
                "INSERT INTO Test (Id, Name) VALUES (?, ?)",
                1,
                "Brutal Bumper Cars"
            );

            var results = connection.Query<TestRecord>(
            "SELECT * FROM Test"
            );

            foreach (TestRecord record in results)
            {
                Debug.Log(
                    "ID: " + record.Id +
                    " | Name: " + record.Name
                );
            }
        }
    }
}