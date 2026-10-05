using SQLite;

public class StatisticsRecord
{
    [PrimaryKey]
    public int Id { get; set; }

    public long Victories { get; set; }
    public long Defeats { get; set; }

    public long Kills { get; set; }
    public long Deaths { get; set; }

    public long DamageDealt { get; set; }
    public long DamageTaken { get; set; }
    public long DamageDefended { get; set; }
}