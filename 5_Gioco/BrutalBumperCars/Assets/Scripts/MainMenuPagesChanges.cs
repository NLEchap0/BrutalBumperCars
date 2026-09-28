using UnityEngine;
using UnityEngine.EventSystems;

public class TextClickHandler : MonoBehaviour, IPointerClickHandler
{
    public enum ActionType
    {
        AvviaPartita,
        ApriImpostazioni,
        ChiudiGioco
    }

    [Header("Configurazione Oggetto")]
    [SerializeField] private ActionType azioneDaEseguire;

    [Header("Riferimenti UI")]
    [SerializeField] private GameObject bg;
    [Header("Riferimenti UI")]
    [SerializeField] private GameObject pannelloMenu;
    [Header("Riferimenti UI")]
    [SerializeField] private GameObject pannelloImpostazioni;

    public void OnPointerClick(PointerEventData eventData)
    {
        GestisciAzione(azioneDaEseguire);
    }

    private void GestisciAzione(ActionType tipo)
    {
        switch (tipo)
        {
            case ActionType.ChiudiGioco:
                Debug.Log($"Azione eseguita da {gameObject.name}: Chiusura applicazione!");
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
                break;

            case ActionType.AvviaPartita:
                Debug.Log($"Azione eseguita da {gameObject.name}: Avvio Partita!");
                // Es: UnityEngine.SceneManagement.SceneManager.LoadScene("NomeScena");
                break;

            case ActionType.ApriImpostazioni:
                Debug.Log($"Azione eseguita da {gameObject.name}: Apertura Impostazioni!");
                pannelloImpostazioni.SetActive(true); 
                bg.SetActive(true);
                pannelloMenu.SetActive(false);
                break;

            default:
                Debug.LogWarning("Nessuna azione associata a questo tipo.");
                break;
        }
    }
}