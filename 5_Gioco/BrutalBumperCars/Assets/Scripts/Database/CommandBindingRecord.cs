using SQLite;

public class CommandBindingRecord
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public int SettingsId { get; set; }

    public string Action { get; set; }

    public string BindingId { get; set; }

    public string Path { get; set; }
}