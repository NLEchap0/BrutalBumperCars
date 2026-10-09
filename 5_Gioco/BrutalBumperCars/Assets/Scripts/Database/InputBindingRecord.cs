using SQLite;

public class InputBindingRecord
{
    [PrimaryKey]
    public int Id { get; set; }

    public string OverridesJson { get; set; }
}