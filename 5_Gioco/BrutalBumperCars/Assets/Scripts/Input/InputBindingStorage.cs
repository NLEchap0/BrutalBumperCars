using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Persistenza degli override di binding (i tasti rimappati dall'utente).
/// Il file viene salvato in Application.persistentDataPath/input_bindings.json,
/// cosi' le modifiche sopravvivono alla chiusura del gioco.
/// </summary>
public static class InputBindingStorage
{
    private static string FilePath => Path.Combine(Application.persistentDataPath, "input_bindings.json");

    /// <summary>Scrive su disco tutti gli override dell'asset.</summary>
    public static void Save(InputActionAsset actions)
    {
        File.WriteAllText(FilePath, actions.SaveBindingOverridesAsJson());
    }

    /// <summary>Ricarica gli override salvati, se il file esiste.</summary>
    public static void Load(InputActionAsset actions)
    {
        if (File.Exists(FilePath))
            actions.LoadBindingOverridesFromJson(File.ReadAllText(FilePath));
    }
}
