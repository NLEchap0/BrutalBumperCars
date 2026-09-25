using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Vista (solo grafica) di una singola riga della lista "Comandi".
/// Non conosce Input System: mostra i testi e mette a disposizione il
/// pulsante che il controller usera' per avviare il rebinding.
/// </summary>
public class ComandoRowUI : MonoBehaviour
{
    [SerializeField] private TMP_Text actionLabel;      // etichetta a sinistra (es. "Avanti")
    [SerializeField] private TMP_Text bindingLabel;     // testo dentro il pulsante (es. "W")
    [SerializeField] private Button bindingButton;       // pulsante di rimappatura

    /// <summary>Pulsante cliccabile per rimappare il tasto di questa riga.</summary>
    public Button BindingButton => bindingButton;

    /// <summary>Aggiorna i due testi della riga.</summary>
    /// <param name="action">Etichetta dell'azione (colonna sinistra).</param>
    /// <param name="binding">Tasto/binding da mostrare nel pulsante (colonna destra).</param>
    public void Show(string action, string binding)
    {
        actionLabel.text = action;
        bindingLabel.text = binding;
    }
}
