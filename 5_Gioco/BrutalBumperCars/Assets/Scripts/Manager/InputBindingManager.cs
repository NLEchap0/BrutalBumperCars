
using UnityEngine;
using UnityEngine.InputSystem;

public class InputBindingManager : MonoBehaviour
{
    public static InputBindingManager Instance { get; private set; }

    [SerializeField]
    private InputActionAsset inputActions;

    private DatabaseManager database;
    private InputBindingRepository repository;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        DontDestroyOnLoad(gameObject);

        if (inputActions == null)
            inputActions = InputSystem.actions;

        if (inputActions == null)
        {
            Debug.LogError(
                "InputBindingManager: InputActionAsset non assegnato."
            );
            return;
        }

        database = DatabaseManager.Instance;
        repository = new InputBindingRepository(database);

        Load(inputActions);
    }

    public void Save(InputActionAsset actions)
    {
        if (actions == null)
        {
            Debug.LogWarning(
                "InputBindingManager: InputActionAsset mancante."
            );
            return;
        }

        InputBindingRecord record = new InputBindingRecord
        {
            Id = 1,
            OverridesJson = actions.SaveBindingOverridesAsJson()
        };

        repository.SaveBindings(record);
    }

    public void Load(InputActionAsset actions)
    {
        if (actions == null)
        {
            Debug.LogWarning(
                "InputBindingManager: InputActionAsset mancante."
            );
            return;
        }

        InputBindingRecord record = repository.LoadBindings();

        if (record != null &&
            !string.IsNullOrEmpty(record.OverridesJson))
        {
            actions.LoadBindingOverridesFromJson(
                record.OverridesJson
            );
        }
    }
}