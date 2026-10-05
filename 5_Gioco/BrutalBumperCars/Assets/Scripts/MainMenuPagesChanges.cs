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

    [Header("Avvio Partita")]
    [SerializeField] private Animator cameraAnimator;

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
                if (cameraAnimator != null)
                {
                    cameraAnimator.Play("MainCameraGameSelect");
                }
                MenuNavigationManager.Instance.FaiUscireASinistra(pannelloMenu);
                break;

            case ActionType.ApriImpostazioni:
                Debug.Log($"Azione eseguita da {gameObject.name}: Apertura Impostazioni!");
                MenuNavigationManager.Instance.TransizioneAvanti(pannelloMenu, pannelloImpostazioni, bg);
                break;

            case ActionType.ImpostazioniToMain:
                Debug.Log($"Azione eseguita da {gameObject.name}: Apertura menu dalle impostazioni!");
                if (SettingsManager.Instance != null)
                {
                    SettingsManager.Instance.Save();
                }
                MenuNavigationManager.Instance.TransizioneIndietro(pannelloImpostazioni, pannelloMenu, bg);
                break;

            case ActionType.ImpostazioniToComandi:
                Debug.Log($"Azione eseguita da {gameObject.name}: Apertura comandi dalle impostazioni!");
                // Sposta le impostazioni e porta dentro i comandi, mantenendo il BG attivo
                MenuNavigationManager.Instance.TransizioneAvanti(pannelloImpostazioni, pannelloComandi);
                break;

            case ActionType.ComandiToImpostazioni:
                Debug.Log($"Azione eseguita da {gameObject.name}: Apertura impostazioni dai comandi!");
                MenuNavigationManager.Instance.TransizioneIndietro(pannelloComandi, pannelloImpostazioni);
                break;

            default:
                Debug.LogWarning("Nessuna azione associata a questo tipo.");
                break;
        }
    }
}