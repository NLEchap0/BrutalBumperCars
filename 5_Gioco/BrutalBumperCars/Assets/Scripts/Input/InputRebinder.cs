using System;
using UnityEngine.InputSystem;

/// <summary>
/// Incapsula una singola operazione di rimappatura di un tasto.
/// Si occupa anche di disabilitare/riabilitare l'azione: l'Input System
/// non permette di rimappare un'azione mentre e' abilitata.
/// </summary>
public class InputRebinder
{
    private InputActionRebindingExtensions.RebindingOperation operation;

    /// <summary>True se una rimappatura e' in corso.</summary>
    public bool IsRebinding => operation != null;

    /// <summary>
    /// Avvia la rimappatura del binding indicato.
    /// Restituisce false se c'e' gia' un rebinding in corso o se l'indice non e' valido.
    /// <paramref name="onFinished"/> viene chiamato sia al successo sia all'annullamento (Esc).
    /// </summary>
    public bool Start(InputAction action, int bindingIndex, Action onFinished)
    {
        if (IsRebinding || action == null || bindingIndex < 0)
            return false;

        // L'azione va disabilitata durante il rebinding, altrimenti Unity lancia un errore.
        bool wasEnabled = action.enabled;
        if (wasEnabled)
            action.Disable();

        operation = action.PerformInteractiveRebinding(bindingIndex)
            .WithCancelingThrough("<Keyboard>/escape")   // Esc annulla
            .OnComplete(op => Finish(op, action, wasEnabled, onFinished))
            .OnCancel(op => Finish(op, action, wasEnabled, onFinished))
            .Start();

        return true;
    }

    /// <summary>Chiude l'operazione, ripristina lo stato e notifica il chiamante.</summary>
    private void Finish(InputActionRebindingExtensions.RebindingOperation op, InputAction action, bool wasEnabled, Action onFinished)
    {
        op.Dispose();
        operation = null;

        if (wasEnabled)
            action.Enable();

        onFinished?.Invoke();
    }
}
