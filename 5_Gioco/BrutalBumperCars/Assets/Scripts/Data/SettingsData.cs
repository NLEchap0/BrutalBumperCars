using System;
using UnityEngine;

[Serializable]
public class SettingsData
{
    //Graphics setting
    public bool fullscreen;
    public int resolution;

    //Audio setting
    public float effect;
    public float master;
    public float music;

    //Control setting
    public float sensitivity;
}
