using SQLite;

public class SettingsRecord
{
    [PrimaryKey]
    public int Id { get; set; }

    public bool Fullscreen { get; set; }
    public int Resolution { get; set; }

    public float Effect { get; set; }
    public float Master { get; set; }
    public float Music { get; set; }

    public float Sensitivity { get; set; }
}
