using SQLite;

public class CarDistanceRecord
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public int StatisticsId { get; set; }

    public string CarName { get; set; }

    public long Distance { get; set; }
}