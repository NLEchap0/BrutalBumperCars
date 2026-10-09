using System.Linq;

public class InputBindingRepository
{
    private readonly DatabaseManager database;

    public InputBindingRepository(DatabaseManager database)
    {
        this.database = database;
    }

    public void SaveBindings(InputBindingRecord bindings)
    {
        database.Connection.InsertOrReplace(bindings);
    }

    public InputBindingRecord LoadBindings()
    {
        return database.Connection
            .Table<InputBindingRecord>()
            .FirstOrDefault();
    }
}