using System.Collections;
using UnityEngine;

public class MenuNavigationManager : MonoBehaviour
{
    public static MenuNavigationManager Instance;

    [Header("Impostazioni")]
    [SerializeField] private float durataAnimazione = 0.35f;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    /// <summary>
    /// Avanza: la pagina attiva scivola a destra (esce), la nuova entra da sinistra verso X=0.
    /// Entrambe si muovono dello stesso offset (distanza) bordo contro bordo.
    /// </summary>
    public void TransizioneAvanti(GameObject paginaDaChiudere, GameObject paginaDaAprire, GameObject elementoExtra = null)
    {
        float distanza = Screen.width;

        // 1. Pagina che entra: parte da -distanza e arriva a 0
        if (paginaDaAprire != null)
        {
            paginaDaAprire.SetActive(true);
            RectTransform rectApri = paginaDaAprire.GetComponent<RectTransform>();
            rectApri.anchoredPosition = new Vector2(-distanza, rectApri.anchoredPosition.y);
            StartCoroutine(Muovi(rectApri, new Vector2(0f, rectApri.anchoredPosition.y), durataAnimazione, false));
        }

        // 2. Pagina che esce: parte da dove si trova (0) e va a +distanza
        if (paginaDaChiudere != null)
        {
            RectTransform rectChiudi = paginaDaChiudere.GetComponent<RectTransform>();
            Vector2 target = new Vector2(rectChiudi.anchoredPosition.x + distanza, rectChiudi.anchoredPosition.y);
            StartCoroutine(Muovi(rectChiudi, target, durataAnimazione, true));
        }

        // 3. BG: entra assieme alla nuova pagina da -distanza a 0
        if (elementoExtra != null)
        {
            elementoExtra.SetActive(true);
            RectTransform rectExtra = elementoExtra.GetComponent<RectTransform>();
            rectExtra.anchoredPosition = new Vector2(-distanza, rectExtra.anchoredPosition.y);
            StartCoroutine(Muovi(rectExtra, new Vector2(0f, rectExtra.anchoredPosition.y), durataAnimazione, false));
        }
    }

    /// <summary>
    /// Torna indietro: la pagina attiva scivola a sinistra, la vecchia rientra da destra verso X=0.
    /// </summary>
    public void TransizioneIndietro(GameObject paginaDaChiudere, GameObject paginaDaAprire, GameObject elementoExtra = null)
    {
        float distanza = Screen.width;

        // 1. Pagina che esce: va a -distanza e poi si spegne
        if (paginaDaChiudere != null)
        {
            RectTransform rectChiudi = paginaDaChiudere.GetComponent<RectTransform>();
            Vector2 target = new Vector2(rectChiudi.anchoredPosition.x - distanza, rectChiudi.anchoredPosition.y);
            StartCoroutine(Muovi(rectChiudi, target, durataAnimazione, true));
        }

        // 2. BG: esce a sinistra assieme alla pagina che si chiude
        if (elementoExtra != null)
        {
            RectTransform rectExtra = elementoExtra.GetComponent<RectTransform>();
            Vector2 target = new Vector2(rectExtra.anchoredPosition.x - distanza, rectExtra.anchoredPosition.y);
            StartCoroutine(Muovi(rectExtra, target, durataAnimazione, true));
        }

        // 3. Pagina che rientra: parte da +distanza e torna al centro (0)
        if (paginaDaAprire != null)
        {
            paginaDaAprire.SetActive(true);
            RectTransform rectApri = paginaDaAprire.GetComponent<RectTransform>();
            rectApri.anchoredPosition = new Vector2(distanza, rectApri.anchoredPosition.y);
            StartCoroutine(Muovi(rectApri, new Vector2(0f, rectApri.anchoredPosition.y), durataAnimazione, false));
        }
    }

    /// <summary>
    /// Uscita del solo menu per avvio partita
    /// </summary>
    public void FaiUscireASinistra(GameObject pagina)
    {
        if (pagina != null)
        {
            RectTransform rect = pagina.GetComponent<RectTransform>();
            Vector2 target = new Vector2(rect.anchoredPosition.x - Screen.width, rect.anchoredPosition.y);
            StartCoroutine(Muovi(rect, target, durataAnimazione, true));
        }
    }

    private IEnumerator Muovi(RectTransform rect, Vector2 destinazione, float durata, bool disattivaAllaFine)
    {
        Vector2 start = rect.anchoredPosition;
        float t = 0f;

        while (t < durata)
        {
            t += Time.deltaTime;
            float smooth = Mathf.SmoothStep(0f, 1f, t / durata);
            rect.anchoredPosition = Vector2.Lerp(start, destinazione, smooth);
            yield return null;
        }

        rect.anchoredPosition = destinazione;

        if (disattivaAllaFine)
        {
            rect.gameObject.SetActive(false);
        }
    }
}