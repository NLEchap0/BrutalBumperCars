using System;
using System.Collections.Generic;

/// <summary>
/// Modello dati di una singola riga della schermata "Comandi".
/// NON contiene logica: e' solo il dato che viene serializzato nella scena
/// e che il controller (<see cref="ComandiUI"/>) usa per costruire la UI.
/// </summary>
[Serializable]
public class ComandoVoce
{
    /// <summary>Testo mostrato a sinistra nella riga (es. "Avanti").</summary>
    public string etichetta;

    /// <summary>Nome dell'azione dentro l'InputActionAsset (es. "Move", "Use", "Drift").</summary>
    public string azione;

    /// <summary>Percorso del controllo da mostrare/rimappare (es. "&lt;Keyboard&gt;/w").</summary>
    public string percorso;

    /// <summary>Costruttore vuoto richiesto dalla serializzazione di Unity.</summary>
    public ComandoVoce()
    {
    }

    /// <summary>Costruttore comodo usato per definire le voci predefinite.</summary>
    public ComandoVoce(string etichetta, string azione, string percorso)
    {
        this.etichetta = etichetta;
        this.azione = azione;
        this.percorso = percorso;
    }

    /// <summary>
    /// Voci di default usate quando la lista non e' configurata nella scena.
    /// Ogni voce associa un'etichetta italiana a un'azione del JSON e a un tasto.
    /// </summary>
    public static List<ComandoVoce> Predefinite()
    {
        return new List<ComandoVoce>
        {
            new ComandoVoce("Avanti", "Move", "<Keyboard>/w"),
            new ComandoVoce("Indietro", "Move", "<Keyboard>/s"),
            new ComandoVoce("Sinistra", "Move", "<Keyboard>/a"),
            new ComandoVoce("Destra", "Move", "<Keyboard>/d"),
            new ComandoVoce("Usa", "Use", "<Mouse>/leftButton"),
            new ComandoVoce("Turbo", "Sprint", "<Keyboard>/leftControl"),
            new ComandoVoce("Drift", "Drift", "<Keyboard>/leftShift")
        };
    }
}
