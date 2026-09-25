using System;
using UnityEngine.InputSystem;

/// <summary>
/// Utility di sola lettura sui binding di un'azione.
/// Serve a passare dal percorso salvato nel JSON ("&lt;Keyboard&gt;/w")
/// all'indice del binding e al testo leggibile da mostrare ("W").
/// </summary>
public static class InputBindingLookup
{
    /// <summary>
    /// Cerca nell'azione il binding con il percorso indicato.
    /// Restituisce l'indice, oppure -1 se non esiste.
    /// </summary>
    public static int Find(InputAction action, string path)
    {
        var bindings = action.bindings;
        for (int i = 0; i < bindings.Count; i++)
        {
            if (string.Equals(bindings[i].path, path, StringComparison.OrdinalIgnoreCase))
                return i;
        }

        return -1;
    }

    /// <summary>
    /// Restituisce il testo leggibile di un binding (es. "W", "Left Ctrl").
    /// Usa la formattazione di Unity, che tiene conto anche degli override di rebinding.
    /// </summary>
    public static string Display(InputAction action, int bindingIndex)
    {
        if (bindingIndex < 0)
            return "-";

        string label = action.GetBindingDisplayString(bindingIndex);
        return string.IsNullOrEmpty(label) ? "-" : label;
    }
}
