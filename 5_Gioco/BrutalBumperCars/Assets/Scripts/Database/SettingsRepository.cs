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

    public List<CommandBindingRecord> LoadCommandBindings(int settingsId)
    {
        return database.Connection
            .Table<CommandBindingRecord>()
            .Where(binding => binding.SettingsId == settingsId)
            .ToList();
    }

    public void SaveCommandBindings(
        int settingsId,
        List<CommandBindingRecord> bindings)
    {
        database.Connection.Execute(
            "DELETE FROM CommandBindingRecord WHERE SettingsId = ?",
            settingsId
        );

        foreach (CommandBindingRecord binding in bindings)
        {
            database.Connection.Insert(binding);
        }
    }
}
