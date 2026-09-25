using UnityEngine;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }
    
    public SettingsData Data { get; private set; }

    private JsonDataService dataService;

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

        dataService = new JsonDataService();

        Data = dataService.LoadSettings();
    }

    public void Save()
    {
        dataService.SaveSettings(Data);
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
}