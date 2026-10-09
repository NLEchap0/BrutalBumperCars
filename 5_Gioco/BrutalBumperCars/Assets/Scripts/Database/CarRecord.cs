
using SQLite;

public class CarRecord
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [NotNull, Unique]
    public string Name { get; set; }

    public float MinimumDamage { get; set; }

    public float MaxSpeed { get; set; }

    public int Health { get; set; }
}