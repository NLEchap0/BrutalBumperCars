using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SettingsUI : MonoBehaviour
{
    [Header("Graphics")]
    [SerializeField] private Toggle fullscreenToggle;
    [SerializeField] private Slider resolutionSlider;

    [Header("Audio")]
    [SerializeField] private Slider effectSlider;
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider musicSlider;

    [SerializeField] private TMP_Text effectValueText;
    [SerializeField] private TMP_Text masterValueText;
    [SerializeField] private TMP_Text musicValueText;

    [Header("Controls")]
    [SerializeField] private Slider sensitivitySlider;

    private void Start()
    {
        LoadSettingsIntoUI();
    }

    private void LoadSettingsIntoUI()
    {
        SettingsData data = SettingsManager.Instance.Data;

        // Graphics
        fullscreenToggle.SetIsOnWithoutNotify(data.fullscreen);
        resolutionSlider.SetValueWithoutNotify(data.resolution);

        // Audio
        effectSlider.SetValueWithoutNotify(data.effect);
        masterSlider.SetValueWithoutNotify(data.master);
        musicSlider.SetValueWithoutNotify(data.music);

        UpdateEffectText(data.effect);
        UpdateMasterText(data.master);
        UpdateMusicText(data.music);

        // Controls
        sensitivitySlider.SetValueWithoutNotify(data.sensitivity);
    }

    public void OnFullscreenChanged(bool value)
    {
        SettingsManager.Instance.OnFullScreenChange(value);
    }

    public void OnResolutionChanged(float value)
    {
        SettingsManager.Instance.OnResolutionChange(Mathf.RoundToInt(value));
    }

    public void OnEffectChanged(float value)
    {
        SettingsManager.Instance.OnEffectChange(value);
        UpdateEffectText(value);
    }

    public void OnMasterChanged(float value)
    {
        SettingsManager.Instance.OnMasterChange(value);
        UpdateMasterText(value);
    }

    public void OnMusicChanged(float value)
    {
        SettingsManager.Instance.OnMusicChange(value);
        UpdateMusicText(value);
    }

    public void OnSensitivityChanged(float value)
    {
        SettingsManager.Instance.OnSensitivityChange(value);
    }

    private void UpdateEffectText(float value)
    {
        effectValueText.text = Mathf.RoundToInt(value * 100f) + "%";
    }

    private void UpdateMasterText(float value)
    {
        masterValueText.text = Mathf.RoundToInt(value * 100f) + "%";
    }

    private void UpdateMusicText(float value)
    {
        musicValueText.text = Mathf.RoundToInt(value * 100f) + "%";
    }
}