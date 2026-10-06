using System.Collections.Generic;
using System.Linq;

public class SettingsRepository
{
    private readonly DatabaseManager database;

    public SettingsRepository(DatabaseManager database)
    {
        this.database = database;
    }

    public void SaveSettings(SettingsRecord record)
    {
        database.Connection.InsertOrReplace(record);
    }

    public SettingsRecord LoadSettings()
    {
        return database.Connection
            .Table<SettingsRecord>()
            .FirstOrDefault();
    }

}
