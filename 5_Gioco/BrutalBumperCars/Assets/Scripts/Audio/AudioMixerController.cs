
using UnityEngine;
using UnityEngine.Audio;

public class AudioMixerController : MonoBehaviour
{
    [SerializeField] private AudioMixer audioMixer;

    private const float MinDecibels = -80f;

    private void Start()
    {
        if (SettingsManager.Instance == null)
        {
            Debug.LogError("SettingsManager non trovato.");
            return;
        }

        var settings = SettingsManager.Instance.Data;

        if (settings == null)
        {
            Debug.LogError("Le impostazioni non sono state caricate.");
            return;
        }

        SetMasterVolume(settings.master);
        SetMusicVolume(settings.music);
        SetEffectsVolume(settings.effect);
    }

    public void SetMasterVolume(float value)
    {
        SetVolume("MasterVolume", value);
    }

    public void SetMusicVolume(float value)
    {
        SetVolume("MusicVolume", value);
    }

    public void SetEffectsVolume(float value)
    {
        SetVolume("EffectsVolume", value);
    }

    private void SetVolume(string parameterName, float value)
    {
        if (audioMixer == null)
        {
            Debug.LogError(
                "AudioMixerController: Audio Mixer non assegnato.",
                this
            );
            return;
        }

        value = Mathf.Clamp01(value);

        float decibels = value <= 0f
            ? MinDecibels
            : Mathf.Max(MinDecibels, 20f * Mathf.Log10(value));

        if (!audioMixer.SetFloat(parameterName, decibels))
        {
            Debug.LogError(
                $"Impossibile modificare il parametro '{parameterName}'. " +
                "Controlla il nome e lo stato dell'Audio Mixer.",
                this
            );
        }
    }
}
