using UnityEngine;
using UnityEngine.InputSystem;

#region Legge l'Input System del Player e invia i valori a BumperCarController
[RequireComponent(typeof(BumperCarController))]
public class PlayerCarInput : MonoBehaviour
{
    #region Dichiarazioni Costanti, Parametri Inspector e Variabili

    #region Costanti Nomi Mappa e Azioni Input System
    private const string PlayerActionMapName = "Player";
    private const string MoveActionPath = "Player/Move";
    private const string DriftActionPath = "Player/Drift";
    #endregion

    #region Input Actions
    [Header("Input Actions")]
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference driftAction;
    #endregion

    #region Riferimenti Interni
    private BumperCarController carController;
    private InputAction resolvedMoveAction;
    private InputAction resolvedDriftAction;
    #endregion

    #endregion

    #region 1. Ciclo di Vita Unity (Awake, OnEnable, OnDisable, Update)

    private void Awake()
    {
        CacheCarControllerReference();
    }

    private void OnEnable()
    {
        EnablePlayerActionMap();
        ResolveInputActions();
        EnableResolvedInputActions();
    }

    private void OnDisable()
    {
        DisableResolvedInputActions();
    }

    private void Update()
    {
        Vector2 movementInputVector = ReadMovementInput();
        bool isDriftPressed = ReadDriftInput();
        ForwardInputsToCarController(movementInputVector, isDriftPressed);
    }

    #endregion

    #region 2. Inizializzazione e Gestione Input Actions

    private void CacheCarControllerReference()
    {
        carController = GetComponent<BumperCarController>();
    }

    private static void EnablePlayerActionMap()
    {
        InputSystem.actions?.FindActionMap(PlayerActionMapName)?.Enable();
    }

    private void ResolveInputActions()
    {
        resolvedMoveAction = ResolveSingleInputAction(moveAction, MoveActionPath);
        resolvedDriftAction = ResolveSingleInputAction(driftAction, DriftActionPath);
    }

    private static InputAction ResolveSingleInputAction(InputActionReference actionReference, string fallbackActionPath)
    {
        return actionReference != null
            ? actionReference.action
            : InputSystem.actions?.FindAction(fallbackActionPath);
    }

    private void EnableResolvedInputActions()
    {
        resolvedMoveAction?.Enable();
        resolvedDriftAction?.Enable();
    }

    private void DisableResolvedInputActions()
    {
        resolvedMoveAction?.Disable();
        resolvedDriftAction?.Disable();
    }

    #endregion

    #region 3. Lettura e Invio degli Input

    private Vector2 ReadMovementInput()
    {
        return resolvedMoveAction != null
            ? resolvedMoveAction.ReadValue<Vector2>()
            : Vector2.zero;
    }

    private bool ReadDriftInput()
    {
        return resolvedDriftAction != null && resolvedDriftAction.IsPressed();
    }

    private void ForwardInputsToCarController(Vector2 movementInputVector, bool isDriftPressed)
    {
        carController.SetInputs(movementInputVector.y, movementInputVector.x, isDriftPressed);
    }

    #endregion
}
#endregion