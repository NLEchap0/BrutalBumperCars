using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Controller della schermata "Comandi".
/// Fa da ponte tra i dati (voci) / Input System e la grafica (<see cref="ComandiListView"/>):
/// risolve ogni voce sull'InputActionAsset, crea le righe e gestisce la rimappatura dei tasti.
/// </summary>
public class ComandiUI : MonoBehaviour
{
    [Header("Dati")]
    [SerializeField] private InputActionAsset inputActions;                 // asset .inputactions (il "JSON")
    [SerializeField] private string actionMapName = "Player";               // mappa da leggere
    [SerializeField] private List<ComandoVoce> voci = ComandoVoce.Predefinite();

    [Header("Riga")]
    [SerializeField] private ComandoRowUI rowTemplate;                      // riga di esempio da clonare

    private readonly InputRebinder rebinder = new InputRebinder();          // servizio di rimappatura
    private ComandiListView listView;                                       // vista (scroll + righe)

    private void Awake()
    {
        // Se la reference non e' assegnata in Inspector, uso le azioni "project-wide".
        if (inputActions == null)
            inputActions = InputSystem.actions;

        if (inputActions == null || rowTemplate == null)
        {
            Debug.LogWarning("ComandiUI: inputActions o rowTemplate mancanti.", this);
            return;
        }

        // Il pannello e' un Canvas annidato: senza GraphicRaycaster non riceve click/scroll.
        EnsureGraphicRaycaster();

        // Applica gli eventuali tasti rimappati in precedenza.
        InputBindingStorage.Load(inputActions);

        // Crea la parte grafica e poi una riga per ogni voce configurata.
        listView = new ComandiListView((RectTransform)transform, rowTemplate);
        listView.Build();

        foreach (ComandoVoce voce in voci)
            AddRow(voce);
    }

    /// <summary>Crea la riga grafica e collega il pulsante al rebinding.</summary>
    private void AddRow(ComandoVoce voce)
    {
        // Es. "Player/Move".
        InputAction action = inputActions.FindAction(actionMapName + "/" + voce.azione, false);
        if (action == null)
            return; // azione assente nel JSON: la salto

        // Indice del binding da mostrare/rimappare (es. il binding di "<Keyboard>/w").
        int bindingIndex = InputBindingLookup.Find(action, voce.percorso);

        ComandoRowUI row = listView.AddRow(voce.etichetta, InputBindingLookup.Display(action, bindingIndex));

        if (row.BindingButton != null)
            row.BindingButton.onClick.AddListener(() => StartRebinding(row, voce, action, bindingIndex));
    }

    /// <summary>Avvia la rimappatura del tasto associato alla riga.</summary>
    private void StartRebinding(ComandoRowUI row, ComandoVoce voce, InputAction action, int bindingIndex)
    {
        bool started = rebinder.Start(action, bindingIndex, () => RefreshRow(row, voce, action, bindingIndex));

        // Feedback visivo solo se il rebinding e' partito davvero.
        if (started)
            row.Show(voce.etichetta, "...");
    }

    /// <summary>Aggiorna la riga dopo il rebinding e salva il nuovo tasto.</summary>
    private void RefreshRow(ComandoRowUI row, ComandoVoce voce, InputAction action, int bindingIndex)
    {
        row.Show(voce.etichetta, InputBindingLookup.Display(action, bindingIndex));
        InputBindingStorage.Save(inputActions);
    }

    /// <summary>Aggiunge il GraphicRaycaster se il pannello non ce l'ha.</summary>
    private void EnsureGraphicRaycaster()
    {
        if (GetComponent<GraphicRaycaster>() == null)
            gameObject.AddComponent<GraphicRaycaster>();
    }
}
