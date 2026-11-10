using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Legge l'Input System del Player e invia i valori a BumperCarController.
/// </summary>
[RequireComponent(typeof(BumperCarController))]
public class PlayerCarInput : MonoBehaviour
{
    [Header("Input Actions")]
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference driftAction;

    private BumperCarController car;
    private InputAction move;
    private InputAction drift;

    private void Start()
    {
        // Trova la mappa Player e attivala, disattivando la mappa UI
        var playerMap = InputSystem.actions?.FindActionMap("Player");
        if (playerMap != null)
        {
            playerMap.Enable();
        }
        else
        {
            move?.Enable();
            drift?.Enable();
        }
    }

    private void Awake()
    {
        car = GetComponent<BumperCarController>();
    }

    private void OnEnable()
    {
        move = moveAction != null ? moveAction.action : InputSystem.actions?.FindAction("Player/Move");
        drift = driftAction != null ? driftAction.action : InputSystem.actions?.FindAction("Player/Drift");

        move?.Enable();
        drift?.Enable();
    }

    private void OnDisable()
    {
        move?.Disable();
        drift?.Disable();
    }

    private void Update()
    {
        Vector2 rawMove = move != null ? move.ReadValue<Vector2>() : Vector2.zero;
        bool isDrifting = drift != null && drift.IsPressed();

        car.SetInputs(rawMove.y, rawMove.x, isDrifting);
    }
}
