using Newtonsoft.Json.Linq;
using UnityEngine;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }
    
    public SettingsData Data { get; private set; }

    private DatabaseManager database;
    private SettingsRepository repository;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // Mantiene il manager tra le scene
        DontDestroyOnLoad(gameObject);

        database = new DatabaseManager();
        repository = new SettingsRepository(database);

        LoadSettings();

        ApplySettings();
    }

    private void LoadSettings()
    {
        SettingsRecord record = repository.LoadSettings();

        if (record == null)
        {
            Data = CreateDefaultSettings();

            Save();
            return;
        }

        Data = ConvertToData(record);

        Debug.Log("caricato");
    }

    public void Save()
    {
        SettingsRecord record = ConverToRecord(Data);

        repository.SaveSettings(record);

        Debug.Log("salvato");
    }

    private SettingsData CreateDefaultSettings()
    {
        return new SettingsData
        {
            fullscreen = true,
            resolution = 2,

            effect = 100f,
            master = 100f,
            music = 100f,

            sensitivity = 0.5f,
        };
    }

    private SettingsRecord ConverToRecord (
        SettingsData data)
    {
        return new SettingsRecord
        {
            Id = 1,

            Fullscreen = data.fullscreen,
            Resolution = data.resolution,

            Effect = data.effect,
            Master = data.master,
            Music = data.music,

            Sensitivity = data.sensitivity
        };
    }

    private SettingsData ConvertToData(
        SettingsRecord record)
    {
        return new SettingsData
        {
            fullscreen = record.Fullscreen,
            resolution = record.Resolution,

            effect = record.Effect,
            master = record.Master,
            music = record.Music,

            sensitivity = record.Sensitivity
        };
    }

    private void ApplySettings()
    {
        Screen.fullScreen = Data.fullscreen;

        QualitySettings.SetQualityLevel(Data.resolution);

        // Ricordare implementazione per audio e sensibilità
    }

    public void OnFullScreenChange(bool value)
    {
        Data.fullscreen = value;
        Screen.fullScreen = value;

    }

    public void OnResolutionChange(int value)
    {
        Data.resolution = value;
        QualitySettings.SetQualityLevel(value);
    }

    public void OnMasterChange(float value)
    {
        Data.master = value;
        //inserire codice per modifica effettiva
    }

    public void OnMusicChange(float value)
    {
        Data.music = value;
        // Applicazione effettiva tramite AudioMixer
    }

    public void OnEffectChange(float value)
    {
        Data.effect = value;
        // Applicazione effettiva tramite AudioMixer
    }

    public void OnSensitivityChange(float value)
    {
        Data.sensitivity = value;
        // Applicazione effettiva (necessario avere la scena di gioco)
    }
}