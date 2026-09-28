using UnityEngine;
using UnityEngine.EventSystems;

public class TextClickHandler : MonoBehaviour, IPointerClickHandler
{
    public enum ActionType
    {
        AvviaPartita,
        ApriImpostazioni,
        ChiudiGioco,
        ImpostazioniToMain,
        ImpostazioniToComandi,
        ComandiToImpostazioni
    }

    [Header("Configurazione Oggetto")]
    [SerializeField] private ActionType azioneDaEseguire;

    [Header("Riferimenti UI")]
    [SerializeField] private GameObject bg;
    [SerializeField] private GameObject pannelloMenu;
    [SerializeField] private GameObject pannelloImpostazioni;
    [SerializeField] private GameObject pannelloComandi;

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

            case ActionType.ImpostazioniToMain:
                Debug.Log($"Azione eseguita da {gameObject.name}: Apertura menu dalle impostazioni!");
                pannelloImpostazioni.SetActive(false);
                bg.SetActive(false);
                pannelloMenu.SetActive(true);
                break;

            case ActionType.ImpostazioniToComandi:
                Debug.Log($"Azione eseguita da {gameObject.name}: Apertura comandi dalle impostazioni!");
                pannelloImpostazioni.SetActive(false);
                pannelloComandi.SetActive(true);
                break;

            case ActionType.ComandiToImpostazioni:
                Debug.Log($"Azione eseguita da {gameObject.name}: Apertura impostazioni dai comandi!");
                pannelloImpostazioni.SetActive(true);
                pannelloComandi.SetActive(false);
                break;

            default:
                Debug.LogWarning("Nessuna azione associata a questo tipo.");
                break;
        }
    }
}